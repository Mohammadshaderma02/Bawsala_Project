using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using DanaCotech.Application.Common;
using DanaCotech.Application.Content;
using DanaCotech.Domain.Platform;

namespace DanaCotech.Application.Platform;

public sealed class PlatformService(IPlatformRepository repository, ILandingContentRepository contentRepository) : IPlatformService
{
    public async Task<ServiceResponse<IReadOnlyCollection<AdminUserSummary>>> GetUsersAsync(CancellationToken cancellationToken)
    {
        var users = await repository.GetUsersAsync(cancellationToken);
        return ServiceResponse<IReadOnlyCollection<AdminUserSummary>>.Ok(
            users.Select(ToAdminSummary).ToArray());
    }

    public async Task<ServiceResponse<IReadOnlyCollection<AdminUserDeletionLog>>> GetUserDeletionLogsAsync(CancellationToken cancellationToken) =>
        ServiceResponse<IReadOnlyCollection<AdminUserDeletionLog>>.Ok(await repository.GetUserDeletionLogsAsync(cancellationToken));

    public async Task<ServiceResponse<AdminUserDeletionLog>> DeleteUserAsync(Guid actorUserId, Guid userId, CancellationToken cancellationToken)
    {
        var outcome = await repository.DeleteUserAsync(actorUserId, userId, cancellationToken);
        if (outcome.AuditLog is not null)
            return ServiceResponse<AdminUserDeletionLog>.Ok(outcome.AuditLog, "User deleted and deletion logged");

        var message = outcome.Error switch
        {
            "actor-unavailable" => "An active administrator account is required",
            "self-delete" => "You cannot delete your own account",
            "last-admin" => "The last active administrator cannot be deleted",
            _ => "User not found"
        };
        return ServiceResponse<AdminUserDeletionLog>.Fail(message);
    }

    public async Task<ServiceResponse<IReadOnlyCollection<CourseStudentSummary>>> GetCourseStudentsAsync(string productSlug, CancellationToken cancellationToken)
    {
        var product = (await GetCatalogAsync("en", cancellationToken)).FirstOrDefault(item => item.Slug == productSlug);
        if (product is null) return ServiceResponse<IReadOnlyCollection<CourseStudentSummary>>.Fail("Course not found");
        var totalLessons = product.Modules.Sum(module => module.Lessons.Count);
        var users = await repository.GetUsersWithJourneysAsync(cancellationToken);
        var students = users.Select(entry => new { entry.Account, Enrollment = entry.Journey.Enrollments.FirstOrDefault(item => item.ProductSlug == productSlug) })
            .Where(entry => entry.Enrollment is not null)
            .Select(entry => new CourseStudentSummary(entry.Account.Id, entry.Account.Name, entry.Account.Email, entry.Enrollment!.PurchasedAtUtc,
                entry.Enrollment.CompletedLessonIds.Count, totalLessons,
                totalLessons == 0 ? 0 : Math.Min(100, (int)Math.Round(entry.Enrollment.CompletedLessonIds.Count * 100d / totalLessons))))
            .OrderByDescending(student => student.JoinedAtUtc).ToArray();
        return ServiceResponse<IReadOnlyCollection<CourseStudentSummary>>.Ok(students);
    }

    public async Task<ServiceResponse<AdminUserSummary>> UpdateUserRoleAsync(Guid actorUserId, Guid userId, string role, CancellationToken cancellationToken)
    {
        if (role is not UserRoles.Admin and not UserRoles.Client)
            return ServiceResponse<AdminUserSummary>.Fail("Role must be Admin or Client");
        if (actorUserId == userId)
            return ServiceResponse<AdminUserSummary>.Fail("You cannot change your own administrator role");

        var account = await repository.GetUserByIdAsync(userId, cancellationToken);
        if (account is null || account.IsDeleted) return ServiceResponse<AdminUserSummary>.Fail("User not found");
        await repository.UpdateUserRoleAsync(userId, role, cancellationToken);
        return ServiceResponse<AdminUserSummary>.Ok(ToAdminSummary(account with { Role = role }), "User role updated");
    }

    public async Task<ServiceResponse<AuthResult>> CreateUserAsync(string name, string email, string password, string role, CancellationToken cancellationToken)
    {
        email = email.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(name) || !email.Contains('@') || password.Length < 8)
            return ServiceResponse<AuthResult>.Fail("Invalid user details", ["Name, a valid email, and a password of at least 8 characters are required."]);
        if (role is not UserRoles.Admin and not UserRoles.Client)
            return ServiceResponse<AuthResult>.Fail("Invalid user role", ["Role must be Admin or Client."]);
        if (await repository.GetUserByEmailAsync(email, cancellationToken) is not null)
            return ServiceResponse<AuthResult>.Fail("An account with this email already exists");

        var salt = RandomNumberGenerator.GetBytes(16);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, 120_000, HashAlgorithmName.SHA256, 32);
        var account = new ClientAccount(Guid.NewGuid(), name.Trim(), email, Convert.ToBase64String(hash), Convert.ToBase64String(salt), role, DateTime.UtcNow);
        await repository.AddUserAsync(account, cancellationToken);
        await repository.SaveJourneyAsync(new ClientJourneyState { UserId = account.Id }, cancellationToken);
        return ServiceResponse<AuthResult>.Ok(new AuthResult(account.Id, account.Name, account.Email, account.Role), "User created successfully");
    }

    public async Task<ServiceResponse<AuthResult>> LoginAsync(string email, string password, CancellationToken cancellationToken)
    {
        var account = await repository.GetUserByEmailAsync(email.Trim().ToLowerInvariant(), cancellationToken);
        if (account is null || account.IsDeleted) return ServiceResponse<AuthResult>.Fail("Invalid email or password");
        var actual = Rfc2898DeriveBytes.Pbkdf2(password, Convert.FromBase64String(account.PasswordSalt), 120_000, HashAlgorithmName.SHA256, 32);
        if (!CryptographicOperations.FixedTimeEquals(actual, Convert.FromBase64String(account.PasswordHash)))
            return ServiceResponse<AuthResult>.Fail("Invalid email or password");
        if (!account.IsEmailVerified) return ServiceResponse<AuthResult>.Fail("Verify your email address before signing in");
        return ServiceResponse<AuthResult>.Ok(new AuthResult(account.Id, account.Name, account.Email, account.Role));
    }

    public async Task<ServiceResponse<EmailVerificationDispatch>> RegisterAsync(string name, string email, string password, string verificationPageUrl, CancellationToken cancellationToken)
    {
        email = email.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(name) || !email.Contains('@') || password.Length < 8) return ServiceResponse<EmailVerificationDispatch>.Fail("Name, a valid email, and an 8-character password are required");
        if (await repository.GetUserByEmailAsync(email, cancellationToken) is not null) return ServiceResponse<EmailVerificationDispatch>.Fail("An account with this email already exists");
        var salt = RandomNumberGenerator.GetBytes(16); var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, 120_000, HashAlgorithmName.SHA256, 32);
        var account = new ClientAccount(Guid.NewGuid(), name.Trim(), email, Convert.ToBase64String(hash), Convert.ToBase64String(salt), UserRoles.Client, DateTime.UtcNow, false);
        await repository.AddUserAsync(account, cancellationToken); await repository.SaveJourneyAsync(new ClientJourneyState { UserId = account.Id }, cancellationToken);
        var rawToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        var tokenHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken))).ToLowerInvariant(); var expires = DateTime.UtcNow.AddHours(24);
        await repository.CreateEmailVerificationAsync(new EmailVerificationToken(Guid.NewGuid(), account.Id, tokenHash, expires, DateTime.UtcNow), cancellationToken);
        var url = $"{verificationPageUrl}?token={Uri.EscapeDataString(rawToken)}";
        await repository.QueueNotificationAsync(account.Id, "email", "email-verification", JsonSerializer.Serialize(new { account.Name, account.Email, verificationUrl = url, expiresAtUtc = expires }), cancellationToken);
        return ServiceResponse<EmailVerificationDispatch>.Ok(new EmailVerificationDispatch(url, expires), "Account created. Check your email to verify it.");
    }

    public async Task<ServiceResponse<bool>> VerifyEmailAsync(string token, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(token)) return ServiceResponse<bool>.Fail("Invalid verification link");
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token.Trim()))).ToLowerInvariant();
        return await repository.VerifyEmailAsync(hash, DateTime.UtcNow, cancellationToken) ? ServiceResponse<bool>.Ok(true, "Email verified successfully") : ServiceResponse<bool>.Fail("This verification link is invalid or expired");
    }

    public async Task<ServiceResponse<PasswordResetDispatch>> RequestPasswordResetAsync(string email, string resetPageUrl, CancellationToken cancellationToken)
    {
        const string genericMessage = "If an account exists for this email, a password reset link has been sent.";
        email = email.Trim().ToLowerInvariant();
        var account = email.Contains('@') ? await repository.GetUserByEmailAsync(email, cancellationToken) : null;
        if (account is null || account.IsDeleted)
            return ServiceResponse<PasswordResetDispatch>.Ok(new PasswordResetDispatch(null, null), genericMessage);

        var rawToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');
        var tokenHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken))).ToLowerInvariant();
        var expiresAtUtc = DateTime.UtcNow.AddHours(1);
        await repository.CreatePasswordResetAsync(
            new PasswordResetToken(Guid.NewGuid(), account.Id, tokenHash, expiresAtUtc, DateTime.UtcNow), cancellationToken);

        var separator = resetPageUrl.Contains('?') ? '&' : '?';
        var resetUrl = $"{resetPageUrl}{separator}token={Uri.EscapeDataString(rawToken)}";
        await repository.QueueNotificationAsync(account.Id, "email", "password-reset",
            JsonSerializer.Serialize(new { account.Name, account.Email, resetUrl, expiresAtUtc }), cancellationToken);
        return ServiceResponse<PasswordResetDispatch>.Ok(new PasswordResetDispatch(resetUrl, expiresAtUtc), genericMessage);
    }

    public async Task<ServiceResponse<bool>> ResetPasswordAsync(string token, string newPassword, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(token))
            return ServiceResponse<bool>.Fail("This password reset link is invalid or has expired.");
        if (newPassword.Length < 8)
            return ServiceResponse<bool>.Fail("Password must contain at least 8 characters.");

        var tokenHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token.Trim()))).ToLowerInvariant();
        var salt = RandomNumberGenerator.GetBytes(16);
        var hash = Rfc2898DeriveBytes.Pbkdf2(newPassword, salt, 120_000, HashAlgorithmName.SHA256, 32);
        var reset = await repository.ResetPasswordAsync(tokenHash, Convert.ToBase64String(hash), Convert.ToBase64String(salt), DateTime.UtcNow, cancellationToken);
        return reset
            ? ServiceResponse<bool>.Ok(true, "Your password has been changed successfully.")
            : ServiceResponse<bool>.Fail("This password reset link is invalid or has expired.");
    }

    public async Task<IReadOnlyCollection<JourneyProduct>> GetCatalogAsync(string language, CancellationToken cancellationToken)
    {
        var normalizedLanguage = language == "ar" ? "ar" : "en";
        var content = await contentRepository.GetLandingContentAsync(normalizedLanguage, cancellationToken);
        var offers = content?.CourseOffers?.Where(offer => offer.IsPublished).OrderBy(offer => offer.SortOrder).ToArray();
        if (offers is null || offers.Length == 0) return CreateCatalog(normalizedLanguage == "ar");
        return offers.Select(offer => new JourneyProduct(
            offer.Slug,
            string.IsNullOrWhiteSpace(offer.Type) ? "course" : offer.Type,
            offer.Title,
            offer.Description,
            offer.Price,
            string.IsNullOrWhiteSpace(offer.Currency) ? "JOD" : offer.Currency,
            string.IsNullOrWhiteSpace(offer.Duration) ? "Self-paced" : offer.Duration,
            CreateModules(offer),
            offer.ImageUrl)).ToArray();
    }

    public async Task<ServiceResponse<JourneySnapshot>> GetJourneyAsync(Guid userId, string language, CancellationToken cancellationToken)
    {
        var account = await repository.GetUserByIdAsync(userId, cancellationToken);
        if (account is null) return ServiceResponse<JourneySnapshot>.Fail("Account not found");
        var state = await repository.GetJourneyAsync(userId, cancellationToken);
        return ServiceResponse<JourneySnapshot>.Ok(await CreateSnapshotAsync(account, state, language, cancellationToken));
    }

    public async Task<ServiceResponse<JourneySnapshot>> EnrollAsync(Guid userId, string productSlug, string language, CancellationToken cancellationToken)
    {
        var catalog = await GetCatalogAsync(language, cancellationToken);
        var product = catalog.FirstOrDefault(item => item.Slug == productSlug);
        if (product is null) return ServiceResponse<JourneySnapshot>.Fail("Product not found");
        var account = await repository.GetUserByIdAsync(userId, cancellationToken);
        if (account is null) return ServiceResponse<JourneySnapshot>.Fail("Account not found");
        var state = await repository.GetJourneyAsync(userId, cancellationToken);
        if (state.Enrollments.All(enrollment => enrollment.ProductSlug != productSlug))
        {
            state.Enrollments.Add(new ClientEnrollment { ProductSlug = productSlug, PurchasedAtUtc = DateTime.UtcNow });
            var now = DateTime.UtcNow;
            await repository.CreatePaymentAsync(new PaymentTransaction(Guid.NewGuid(), userId, account.Name, account.Email, product.Type, product.Slug, product.Title, product.Price, product.Currency, "paid", "test", $"TEST-{now:yyyyMMddHHmmss}-{Guid.NewGuid():N}"[..31], now, now, null, ""), cancellationToken);
        }
        await repository.SaveJourneyAsync(state, cancellationToken);
        await repository.QueueNotificationAsync(userId, "email", "welcome-enrollment", JsonSerializer.Serialize(new { productSlug, language }), cancellationToken);
        return await GetJourneyAsync(userId, language, cancellationToken);
    }

    public async Task<ServiceResponse<IReadOnlyCollection<PaymentTransaction>>> GetPaymentsAsync(CancellationToken cancellationToken) =>
        ServiceResponse<IReadOnlyCollection<PaymentTransaction>>.Ok(await repository.GetPaymentsAsync(cancellationToken));

    public async Task<ServiceResponse<PaymentTransaction>> RefundPaymentAsync(Guid paymentId, string reason, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(reason)) return ServiceResponse<PaymentTransaction>.Fail("A refund reason is required");
        var payment = await repository.RefundPaymentAsync(paymentId, reason.Trim(), DateTime.UtcNow, cancellationToken);
        if (payment is null) return ServiceResponse<PaymentTransaction>.Fail("Payment was not found or has already been refunded");
        if (payment.UserId is Guid userId)
        {
            var state = await repository.GetJourneyAsync(userId, cancellationToken);
            state.Enrollments.RemoveAll(item => item.ProductSlug == payment.ProductSlug);
            await repository.SaveJourneyAsync(state, cancellationToken);
            await repository.QueueNotificationAsync(userId, "email", "payment-refunded", JsonSerializer.Serialize(new { payment.Id, payment.ProductTitle, payment.Amount, payment.Currency, reason }), cancellationToken);
        }
        return ServiceResponse<PaymentTransaction>.Ok(payment, "Payment refunded and client access revoked");
    }

    public async Task<ServiceResponse<PaymentTransaction>> GuestCoachingCheckoutAsync(string name, string email, Guid sessionId, string language, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(name) || !email.Contains('@')) return ServiceResponse<PaymentTransaction>.Fail("Name and a valid email are required");
        var session = (await repository.GetSessionsAsync(true, cancellationToken)).FirstOrDefault(item => item.Id == sessionId && item.SessionType.Equals("coaching", StringComparison.OrdinalIgnoreCase));
        if (session is null) return ServiceResponse<PaymentTransaction>.Fail("Choose an available coaching session");
        var product = (await GetCatalogAsync(language, cancellationToken)).FirstOrDefault(item => item.Slug == "one-to-one-coaching");
        if (product is null) return ServiceResponse<PaymentTransaction>.Fail("Coaching service is unavailable");
        var now = DateTime.UtcNow; var payment = new PaymentTransaction(Guid.NewGuid(), null, name.Trim(), email.Trim().ToLowerInvariant(), "coaching", product.Slug, product.Title, product.Price, product.Currency, "paid", "test", $"GUEST-{now:yyyyMMddHHmmss}-{Guid.NewGuid():N}"[..32], now, now, null, "");
        await repository.CreatePaymentAsync(payment, cancellationToken);
        return ServiceResponse<PaymentTransaction>.Ok(payment, $"Payment confirmed. Your meeting link is {session.MeetingUrl}");
    }

    public async Task<ServiceResponse<JourneySnapshot>> CompleteLessonAsync(Guid userId, string lessonId, string language, CancellationToken cancellationToken)
    {
        var state = await repository.GetJourneyAsync(userId, cancellationToken);
        var product = (await GetCatalogAsync(language, cancellationToken)).FirstOrDefault(item => item.Modules.SelectMany(module => module.Lessons).Any(lesson => lesson.Id == lessonId));
        var enrollment = state.Enrollments.FirstOrDefault(item => item.ProductSlug == product?.Slug);
        if (enrollment is null) return ServiceResponse<JourneySnapshot>.Fail("You are not enrolled in this product");
        enrollment.CompletedLessonIds.Add(lessonId);
        await repository.SaveJourneyAsync(state, cancellationToken);
        return await GetJourneyAsync(userId, language, cancellationToken);
    }

    public async Task<ServiceResponse<JourneySnapshot>> SaveAssessmentAsync(Guid userId, string key, IReadOnlyDictionary<string, string> answers, string language, CancellationToken cancellationToken)
    {
        var state = await repository.GetJourneyAsync(userId, cancellationToken);
        if (state.Enrollments.All(item => item.ProductSlug != "career-clarity-program")) return ServiceResponse<JourneySnapshot>.Fail("Career Clarity Program enrollment is required");
        var sequence = new[] { "strengths", "interests", "values", "purpose" };
        var position = Array.IndexOf(sequence, key.ToLowerInvariant());
        if (position < 0) return ServiceResponse<JourneySnapshot>.Fail("Unknown assessment");
        if (position > 0 && !state.Assessments.ContainsKey(sequence[position - 1])) return ServiceResponse<JourneySnapshot>.Fail("Complete the previous assessment first");
        var nonEmpty = answers.Where(item => !string.IsNullOrWhiteSpace(item.Value)).ToArray();
        var normalizedKey = key.ToLowerInvariant();
        if (normalizedKey == "strengths" && (CountPrefix(answers, "strength") != 5 || CountValue(answers, "watched", "true") != 5))
            return ServiceResponse<JourneySnapshot>.Fail("Select your top five strengths and watch all five matching videos");
        if (normalizedKey == "interests" && (CountPrefix(answers, "rating-") != 72 || CountPrefix(answers, "interest") != 3))
            return ServiceResponse<JourneySnapshot>.Fail("Rate all 72 RIASEC activities before completing this assessment");
        if (normalizedKey == "values" && (CountPrefix(answers, "rating-") != 54 || CountPrefix(answers, "value") != 5))
            return ServiceResponse<JourneySnapshot>.Fail("Rate all 54 career values and select your top five");
        if (normalizedKey == "purpose" && (CountPurposeReflections(answers) != 11 || !answers.TryGetValue("statement", out var statement) || string.IsNullOrWhiteSpace(statement)))
            return ServiceResponse<JourneySnapshot>.Fail("Complete every Life Purpose reflection and write your purpose statement");
        var resultPrefix = normalizedKey switch { "strengths" => "strength", "interests" => "interest", "values" => "value", _ => "statement" };
        var summary = string.Join(" · ", answers.Where(item => item.Key.StartsWith(resultPrefix, StringComparison.OrdinalIgnoreCase)).Select(item => item.Value).Where(value => !string.IsNullOrWhiteSpace(value)).Take(normalizedKey == "interests" ? 3 : 5));
        state.Assessments[key] = new AssessmentResult(key, answers, summary, DateTime.UtcNow);
        var assessmentLessonId = $"{key}-assessment";
        var assessmentProduct = (await GetCatalogAsync(language, cancellationToken)).FirstOrDefault(product => product.Modules.SelectMany(module => module.Lessons).Any(lesson => lesson.Id == assessmentLessonId));
        state.Enrollments.FirstOrDefault(enrollment => enrollment.ProductSlug == assessmentProduct?.Slug)?.CompletedLessonIds.Add(assessmentLessonId);
        if (sequence.All(item => state.Assessments.ContainsKey(item)))
        {
            state.Profile.Status = "in-review";
            state.Profile.SubmittedAtUtc ??= DateTime.UtcNow;
            await repository.QueueNotificationAsync(userId, "email", "career-profile-submitted", JsonSerializer.Serialize(new { language }), cancellationToken);
        }
        await repository.SaveJourneyAsync(state, cancellationToken);
        return await GetJourneyAsync(userId, language, cancellationToken);
    }

    public async Task<ServiceResponse<IReadOnlyCollection<AdminJourneySummary>>> GetProgramClientsAsync(CancellationToken cancellationToken)
    {
        var users = await repository.GetUsersWithJourneysAsync(cancellationToken);
        return ServiceResponse<IReadOnlyCollection<AdminJourneySummary>>.Ok(users.Select(item => new AdminJourneySummary(item.Account.Id, item.Account.Name, item.Account.Email, item.Journey.Enrollments, item.Journey.Assessments, item.Journey.Profile, item.Journey.Bookings)).ToArray());
    }

    public async Task<ServiceResponse<AdminJourneySummary>> ReviewCareerProfileAsync(Guid userId, string notes, bool release, CancellationToken cancellationToken)
    {
        var account = await repository.GetUserByIdAsync(userId, cancellationToken);
        if (account is null) return ServiceResponse<AdminJourneySummary>.Fail("Client not found");
        var state = await repository.GetJourneyAsync(userId, cancellationToken);
        if (state.Assessments.Count < 4) return ServiceResponse<AdminJourneySummary>.Fail("The client has not completed all assessments");
        state.Profile.AdminNotes = notes.Trim(); state.Profile.Status = release ? "ready" : "in-review"; state.Profile.ReviewedAtUtc = DateTime.UtcNow;
        await repository.SaveJourneyAsync(state, cancellationToken);
        if (release) await repository.QueueNotificationAsync(userId, "email", "career-profile-ready", JsonSerializer.Serialize(new { userId }), cancellationToken);
        return ServiceResponse<AdminJourneySummary>.Ok(new AdminJourneySummary(account.Id, account.Name, account.Email, state.Enrollments, state.Assessments, state.Profile, state.Bookings), release ? "Career profile released" : "Review notes saved");
    }

    public async Task<ServiceResponse<JourneySnapshot>> BookSessionAsync(Guid userId, string sessionType, DateTime startsAtUtc, string language, CancellationToken cancellationToken)
    {
        if (startsAtUtc <= DateTime.UtcNow) return ServiceResponse<JourneySnapshot>.Fail("Choose a future appointment");
        var state = await repository.GetJourneyAsync(userId, cancellationToken);
        var booking = new ClientBooking(Guid.NewGuid(), sessionType, startsAtUtc, "confirmed", string.Empty);
        state.Bookings.Add(booking);
        await repository.SaveJourneyAsync(state, cancellationToken);
        var endsAtUtc = startsAtUtc.AddHours(1);
        var notificationPayload = JsonSerializer.Serialize(new { booking.Id, sessionType, startsAtUtc, endsAtUtc, booking.MeetingUrl, language });
        await repository.QueueNotificationAsync(userId, "email", "booking-confirmation", notificationPayload, cancellationToken);
        await repository.QueueNotificationAsync(userId, "calendar", "meeting-invitation", notificationPayload, cancellationToken);
        return await GetJourneyAsync(userId, language, cancellationToken);
    }

    public async Task<ServiceResponse<IReadOnlyCollection<PlatformSession>>> GetSessionsAsync(bool futureOnly, CancellationToken cancellationToken) =>
        ServiceResponse<IReadOnlyCollection<PlatformSession>>.Ok(await repository.GetSessionsAsync(futureOnly, cancellationToken));

    public async Task<ServiceResponse<PlatformSession>> CreateSessionAsync(string sessionType, string titleEn, string titleAr, DateTime startsAtUtc, DateTime endsAtUtc, int? capacity, string meetingUrl, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(titleEn) || string.IsNullOrWhiteSpace(titleAr)) return ServiceResponse<PlatformSession>.Fail("English and Arabic titles are required");
        if (startsAtUtc <= DateTime.UtcNow || endsAtUtc <= startsAtUtc) return ServiceResponse<PlatformSession>.Fail("Choose a valid future start and end time");
        if (capacity is <= 0) return ServiceResponse<PlatformSession>.Fail("Capacity must be greater than zero");
        var session = new PlatformSession(Guid.NewGuid(), string.IsNullOrWhiteSpace(sessionType) ? "coaching" : sessionType.Trim(), titleEn.Trim(), titleAr.Trim(), startsAtUtc, endsAtUtc, capacity, meetingUrl.Trim(), true, []);
        return ServiceResponse<PlatformSession>.Ok(await repository.CreateSessionAsync(session, cancellationToken), "Session created successfully");
    }

    public async Task<ServiceResponse<JourneySnapshot>> BookSessionAsync(Guid userId, Guid sessionId, string language, CancellationToken cancellationToken)
    {
        var session = await repository.BookSessionAsync(userId, sessionId, cancellationToken);
        if (session is null) return ServiceResponse<JourneySnapshot>.Fail("This session is unavailable, full, or already booked");
        var state = await repository.GetJourneyAsync(userId, cancellationToken);
        if (state.Bookings.All(item => item.Id != session.Id)) state.Bookings.Add(new ClientBooking(session.Id, session.SessionType, session.StartsAtUtc, "confirmed", session.MeetingUrl));
        await repository.SaveJourneyAsync(state, cancellationToken);
        var payload = JsonSerializer.Serialize(new { session.Id, session.SessionType, session.TitleEn, session.TitleAr, session.StartsAtUtc, session.EndsAtUtc, session.MeetingUrl, language });
        await repository.QueueNotificationAsync(userId, "email", "booking-confirmation", payload, cancellationToken);
        await repository.QueueNotificationAsync(userId, "calendar", "meeting-invitation", payload, cancellationToken);
        return await GetJourneyAsync(userId, language, cancellationToken);
    }

    private async Task<JourneySnapshot> CreateSnapshotAsync(ClientAccount account, ClientJourneyState state, string language, CancellationToken cancellationToken) =>
        new(account.Id, account.Name, await GetCatalogAsync(language, cancellationToken), state, CreateIdentity(state, language == "ar"));

    private static IReadOnlyCollection<JourneyModule> CreateModules(DanaCotech.Domain.Content.CheckoutOfferContent offer)
    {
        if (offer.Sections is { Count: > 0 })
            return offer.Sections.OrderBy(section => section.SortOrder).Select(section => new JourneyModule(
                section.Key, section.Title, section.Description,
                section.Lessons.Select(lesson => new JourneyLesson(lesson.Key, lesson.Title, "video", lesson.DurationMinutes, lesson.VideoUrl)).ToArray())).ToArray();
        if (offer.Lessons.Count > 0)
            return [new JourneyModule("course-content", "Course content", "Video lessons", offer.Lessons.Select(lesson => new JourneyLesson(lesson.Key, lesson.Title, "video", lesson.DurationMinutes, lesson.VideoUrl)).ToArray())];
        return [];
    }

    private static AdminUserSummary ToAdminSummary(ClientAccount account) =>
        new(account.Id, account.Name, account.Email, account.Role, account.CreatedAtUtc);

    private static CareerIdentity? CreateIdentity(ClientJourneyState state, bool ar)
    {
        string Value(string key) => state.Assessments.TryGetValue(key, out var result) ? result.Summary : string.Empty;
        if (!state.Assessments.ContainsKey("strengths") || !state.Assessments.ContainsKey("interests") || !state.Assessments.ContainsKey("values") || !state.Assessments.ContainsKey("purpose")) return null;
        return new CareerIdentity(
            ar ? "هويتك المهنية" : "Your career identity", Value("strengths"), Value("interests"), Value("values"), Value("purpose"),
            ar ? ["تطوير الأفراد", "إدارة المشاريع", "التعلّم والتطوير"] : ["People development", "Project management", "Learning and development"]);
    }

    private static int CountPrefix(IReadOnlyDictionary<string, string> answers, string prefix) =>
        answers.Count(item => item.Key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(item.Value));

    private static int CountValue(IReadOnlyDictionary<string, string> answers, string prefix, string expected) =>
        answers.Count(item => item.Key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) && string.Equals(item.Value, expected, StringComparison.OrdinalIgnoreCase));

    private static int CountPurposeReflections(IReadOnlyDictionary<string, string> answers) =>
        answers.Count(item => !string.Equals(item.Key, "statement", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(item.Value));

    private static IReadOnlyCollection<JourneyProduct> CreateCatalog(bool ar)
    {
        JourneyLesson L(string id, string en, string arTitle, string type = "video", int minutes = 15) => new(id, ar ? arTitle : en, type, minutes, "");
        JourneyModule M(string key, string en, string arTitle, params JourneyLesson[] lessons) => new(key, ar ? arTitle : en, ar ? "محتوى وتطبيقات عملية" : "Guided content and practical activities", lessons);
        return [
            new JourneyProduct("career-clarity-program", "cohort", ar ? "برنامج وضوح المسار المهني" : "Career Clarity Program", ar ? "رحلة متكاملة لمدة خمسة أسابيع." : "A complete five-week guided journey.", 249, "JOD", ar ? "5 أسابيع" : "5 weeks", [
                M("week-1", "Stage 1 · Character strengths", "المرحلة 1 · نقاط القوة", L("strengths-intro", "Dana's introduction to character strengths", "مقدمة دانا لنقاط القوة"), L("strengths-assessment", "VIA strengths assessment and five result videos", "اختبار VIA وفيديوهات النتائج الخمس", "assessment", 20)),
                M("week-2", "Stage 2 · Career interests", "المرحلة 2 · الميول المهنية", L("interests-intro", "Dana's introduction to RIASEC", "مقدمة دانا للميول المهنية"), L("interests-assessment", "72-activity RIASEC assessment", "تقييم RIASEC المكوّن من 72 نشاطاً", "assessment")),
                M("week-3", "Stage 3 · Career values", "المرحلة 3 · القيم المهنية", L("values-intro", "Dana's introduction to career values", "مقدمة دانا للقيم المهنية"), L("values-assessment", "54-value career assessment", "تقييم القيم المهنية الـ54", "assessment")),
                M("week-4", "Stage 4 · Life purpose", "المرحلة 4 · غاية الحياة", L("purpose-intro", "Dana's introduction to life purpose", "مقدمة دانا لغاية الحياة"), L("purpose-assessment", "Life Purpose guided exercise", "تمرين غاية الحياة الموجّه", "assessment")),
                M("week-5", "Stage 5 · Career Profile and 1:1", "المرحلة 5 · الملف المهني والجلسة", L("career-identity", "Dana reviews your Career Profile", "مراجعة دانا لملفك المهني"), L("one-to-one", "Book your included 1:1 session", "احجز جلستك الفردية المشمولة", "booking", 30))
            ]),
            new JourneyProduct("one-to-one-coaching", "coaching", ar ? "جلسة استشارية فردية" : "1:1 Coaching Session", ar ? "جلسة مركزة حسب احتياجك." : "A focused session built around your goal.", 75, "JOD", "60 min", []),
            new JourneyProduct("career-survival-library", "library", ar ? "مكتبة النجاة المهنية" : "Career Survival Library", ar ? "دروس عملية مسجلة لمواقف العمل." : "Practical recorded lessons for real workplace challenges.", 99, "JOD", ar ? "تعلم ذاتي" : "Self-paced", [
                M("library", "Career Survival Library", "مكتبة النجاة المهنية", L("bad-bosses", "Dealing with difficult managers", "التعامل مع المدراء الصعبين"), L("salary", "Salary negotiation", "التفاوض على الراتب"), L("job-search", "A smarter job search", "بحث أذكى عن وظيفة"), L("workplace", "Workplace decisions", "قرارات بيئة العمل"))
            ])
        ];
    }
}
