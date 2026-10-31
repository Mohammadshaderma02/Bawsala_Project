using DanaCotech.Domain.Platform;

namespace DanaCotech.Application.Platform;

public interface IPlatformRepository
{
    Task<ClientAccount?> GetUserByEmailAsync(string email, CancellationToken cancellationToken);
    Task<ClientAccount?> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken);
    Task AddUserAsync(ClientAccount account, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<ClientAccount>> GetUsersAsync(CancellationToken cancellationToken);
    Task<IReadOnlyCollection<AdminUserDeletionLog>> GetUserDeletionLogsAsync(CancellationToken cancellationToken);
    Task<UserDeletionOutcome> DeleteUserAsync(Guid actorUserId, Guid userId, CancellationToken cancellationToken);
    Task<AdminNotificationPage> GetNotificationsAsync(string? channel, string? status, int page, int pageSize, CancellationToken cancellationToken);
    Task<long?> ResendNotificationAsync(long notificationId, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<(ClientAccount Account, ClientJourneyState Journey)>> GetUsersWithJourneysAsync(CancellationToken cancellationToken);
    Task UpdateUserRoleAsync(Guid userId, string role, CancellationToken cancellationToken);
    Task CreatePasswordResetAsync(PasswordResetToken token, CancellationToken cancellationToken);
    Task<bool> ResetPasswordAsync(string tokenHash, string passwordHash, string passwordSalt, DateTime usedAtUtc, CancellationToken cancellationToken);
    Task CreateEmailVerificationAsync(EmailVerificationToken token, CancellationToken cancellationToken);
    Task<bool> VerifyEmailAsync(string tokenHash, DateTime usedAtUtc, CancellationToken cancellationToken);
    Task<ClientJourneyState> GetJourneyAsync(Guid userId, CancellationToken cancellationToken);
    Task SaveJourneyAsync(ClientJourneyState state, CancellationToken cancellationToken);
    Task QueueNotificationAsync(Guid userId, string channel, string templateKey, string payloadJson, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<PlatformSession>> GetSessionsAsync(bool futureOnly, CancellationToken cancellationToken);
    Task<PlatformSession> CreateSessionAsync(PlatformSession session, CancellationToken cancellationToken);
    Task<PlatformSession?> BookSessionAsync(Guid userId, Guid sessionId, CancellationToken cancellationToken);
    Task<PaymentTransaction> CreatePaymentAsync(PaymentTransaction payment, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<PaymentTransaction>> GetPaymentsAsync(CancellationToken cancellationToken);
    Task<PaymentTransaction?> RefundPaymentAsync(Guid paymentId, string reason, DateTime refundedAtUtc, CancellationToken cancellationToken);
    Task<PaymentTransaction?> SetPaymentProviderReferenceAsync(Guid paymentId, string providerReference, CancellationToken cancellationToken);
    Task<PaymentTransaction?> CompletePaymentAsync(Guid paymentId, string providerReference, DateTime paidAtUtc, CancellationToken cancellationToken);
}
