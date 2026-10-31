using DanaCotech.Application.Common;
using DanaCotech.Domain.Platform;

namespace DanaCotech.Application.Platform;

public interface IPlatformService
{
    Task<ServiceResponse<AuthResult>> CreateUserAsync(string name, string email, string password, string role, CancellationToken cancellationToken);
    Task<ServiceResponse<IReadOnlyCollection<AdminUserSummary>>> GetUsersAsync(CancellationToken cancellationToken);
    Task<ServiceResponse<IReadOnlyCollection<AdminUserDeletionLog>>> GetUserDeletionLogsAsync(CancellationToken cancellationToken);
    Task<ServiceResponse<AdminUserDeletionLog>> DeleteUserAsync(Guid actorUserId, Guid userId, CancellationToken cancellationToken);
    Task<ServiceResponse<IReadOnlyCollection<CourseStudentSummary>>> GetCourseStudentsAsync(string productSlug, CancellationToken cancellationToken);
    Task<ServiceResponse<AdminUserSummary>> UpdateUserRoleAsync(Guid actorUserId, Guid userId, string role, CancellationToken cancellationToken);
    Task<ServiceResponse<AuthResult>> LoginAsync(string email, string password, CancellationToken cancellationToken);
    Task<ServiceResponse<PasswordResetDispatch>> RequestPasswordResetAsync(string email, string resetPageUrl, CancellationToken cancellationToken);
    Task<ServiceResponse<bool>> ResetPasswordAsync(string token, string newPassword, CancellationToken cancellationToken);
    Task<ServiceResponse<EmailVerificationDispatch>> RegisterAsync(string name, string email, string password, string verificationPageUrl, CancellationToken cancellationToken);
    Task<ServiceResponse<bool>> VerifyEmailAsync(string token, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<JourneyProduct>> GetCatalogAsync(string language, CancellationToken cancellationToken);
    Task<ServiceResponse<JourneySnapshot>> GetJourneyAsync(Guid userId, string language, CancellationToken cancellationToken);
    Task<ServiceResponse<JourneySnapshot>> EnrollAsync(Guid userId, string productSlug, string language, CancellationToken cancellationToken);
    Task<ServiceResponse<JourneySnapshot>> CompleteLessonAsync(Guid userId, string lessonId, string language, CancellationToken cancellationToken);
    Task<ServiceResponse<JourneySnapshot>> SaveAssessmentAsync(Guid userId, string key, IReadOnlyDictionary<string, string> answers, string language, CancellationToken cancellationToken);
    Task<ServiceResponse<JourneySnapshot>> BookSessionAsync(Guid userId, string sessionType, DateTime startsAtUtc, string language, CancellationToken cancellationToken);
    Task<ServiceResponse<IReadOnlyCollection<PlatformSession>>> GetSessionsAsync(bool futureOnly, CancellationToken cancellationToken);
    Task<ServiceResponse<PlatformSession>> CreateSessionAsync(string sessionType, string titleEn, string titleAr, DateTime startsAtUtc, DateTime endsAtUtc, int? capacity, string meetingUrl, CancellationToken cancellationToken);
    Task<ServiceResponse<JourneySnapshot>> BookSessionAsync(Guid userId, Guid sessionId, string language, CancellationToken cancellationToken);
    Task<ServiceResponse<IReadOnlyCollection<PaymentTransaction>>> GetPaymentsAsync(CancellationToken cancellationToken);
    Task<ServiceResponse<PaymentTransaction>> RefundPaymentAsync(Guid paymentId, string reason, CancellationToken cancellationToken);
    Task<ServiceResponse<PaymentTransaction>> GuestCoachingCheckoutAsync(string name, string email, Guid sessionId, string language, CancellationToken cancellationToken);
    Task<ServiceResponse<IReadOnlyCollection<AdminJourneySummary>>> GetProgramClientsAsync(CancellationToken cancellationToken);
    Task<ServiceResponse<AdminJourneySummary>> ReviewCareerProfileAsync(Guid userId, string notes, bool release, CancellationToken cancellationToken);
}
