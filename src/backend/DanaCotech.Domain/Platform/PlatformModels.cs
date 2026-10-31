namespace DanaCotech.Domain.Platform;

public static class UserRoles
{
    public const string Admin = "Admin";
    public const string Client = "Client";
}

public sealed record ClientAccount(Guid Id, string Name, string Email, string PasswordHash, string PasswordSalt, string Role, DateTime CreatedAtUtc, bool IsEmailVerified = true, bool IsDeleted = false, DateTime? DeletedAtUtc = null);
public sealed record PasswordResetToken(Guid Id, Guid UserId, string TokenHash, DateTime ExpiresAtUtc, DateTime CreatedAtUtc, DateTime? UsedAtUtc = null);
public sealed record EmailVerificationToken(Guid Id, Guid UserId, string TokenHash, DateTime ExpiresAtUtc, DateTime CreatedAtUtc, DateTime? UsedAtUtc = null);

public sealed class ClientJourneyState
{
    public Guid UserId { get; set; }
    public List<ClientEnrollment> Enrollments { get; set; } = [];
    public List<ClientBooking> Bookings { get; set; } = [];
    public Dictionary<string, AssessmentResult> Assessments { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public CareerProfileState Profile { get; set; } = new();
}

public sealed class CareerProfileState
{
    public string Status { get; set; } = "not-started";
    public string AdminNotes { get; set; } = string.Empty;
    public DateTime? SubmittedAtUtc { get; set; }
    public DateTime? ReviewedAtUtc { get; set; }
}

public sealed class ClientEnrollment
{
    public string ProductSlug { get; set; } = string.Empty;
    public DateTime PurchasedAtUtc { get; set; }
    public HashSet<string> CompletedLessonIds { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed record ClientBooking(Guid Id, string SessionType, DateTime StartsAtUtc, string Status, string MeetingUrl);
public sealed record AssessmentResult(string Key, IReadOnlyDictionary<string, string> Answers, string Summary, DateTime CompletedAtUtc);

public sealed record JourneyProduct(string Slug, string Type, string Title, string Summary, decimal Price, string Currency, string Duration, IReadOnlyCollection<JourneyModule> Modules, string CoverUrl = "");
public sealed record JourneyModule(string Key, string Title, string Description, IReadOnlyCollection<JourneyLesson> Lessons);
public sealed record JourneyLesson(string Id, string Title, string Type, int DurationMinutes, string ContentUrl);

public sealed record JourneySnapshot(
    Guid UserId,
    string ClientName,
    IReadOnlyCollection<JourneyProduct> Catalog,
    ClientJourneyState State,
    CareerIdentity? CareerIdentity);

public sealed record CareerIdentity(string Headline, string Strengths, string Interests, string Values, string Purpose, IReadOnlyCollection<string> SuggestedPaths);
public sealed record AuthResult(Guid UserId, string Name, string Email, string Role);
public sealed record PasswordResetDispatch(string? ResetUrl, DateTime? ExpiresAtUtc);
public sealed record EmailVerificationDispatch(string? VerificationUrl, DateTime? ExpiresAtUtc);
public sealed record AdminUserSummary(Guid UserId, string Name, string Email, string Role, DateTime CreatedAtUtc);
public sealed record AdminUserDeletionLog(Guid ActorUserId, string ActorName, string ActorEmail, Guid TargetUserId, string TargetName, string TargetEmail, string TargetRole, DateTime DeletedAtUtc);
public sealed record UserDeletionOutcome(string? Error, AdminUserDeletionLog? AuditLog);
public sealed record AdminNotificationRecord(long Id, Guid UserId, string RecipientName, string RecipientEmail, string Channel, string TemplateKey, string Status, string? ProviderReference, int Attempts, DateTime CreatedAtUtc, DateTime? ClaimedAtUtc, DateTime? SentAtUtc, string? LastError, string PayloadJson);
public sealed record AdminNotificationPage(IReadOnlyCollection<AdminNotificationRecord> Items, long TotalCount, int Page, int PageSize);
public sealed record CourseStudentSummary(Guid UserId, string Name, string Email, DateTime JoinedAtUtc, int CompletedLessons, int TotalLessons, int ProgressPercent);
public sealed record SessionAttendee(Guid UserId, string Name, string Email, DateTime BookedAtUtc);
public sealed record PlatformSession(Guid Id, string SessionType, string TitleEn, string TitleAr, DateTime StartsAtUtc, DateTime EndsAtUtc, int? Capacity, string MeetingUrl, bool IsBookable, IReadOnlyCollection<SessionAttendee> Attendees);
public sealed record PaymentTransaction(Guid Id, Guid? UserId, string CustomerName, string CustomerEmail, string ServiceType, string ProductSlug, string ProductTitle, decimal Amount, string Currency, string Status, string Provider, string ProviderReference, DateTime CreatedAtUtc, DateTime? PaidAtUtc, DateTime? RefundedAtUtc, string RefundReason);
public sealed record CheckoutLaunch(string Mode, string? RedirectUrl, Guid? PaymentId, string? SessionId = null, string? CheckoutScriptUrl = null);
public sealed record AdminJourneySummary(Guid UserId, string Name, string Email, IReadOnlyCollection<ClientEnrollment> Enrollments, IReadOnlyDictionary<string, AssessmentResult> Assessments, CareerProfileState Profile, IReadOnlyCollection<ClientBooking> Bookings);
