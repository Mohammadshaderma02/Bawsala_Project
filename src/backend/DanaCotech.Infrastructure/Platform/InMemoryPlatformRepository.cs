using System.Collections.Concurrent;
using DanaCotech.Application.Platform;
using DanaCotech.Domain.Platform;

namespace DanaCotech.Infrastructure.Platform;

public sealed class InMemoryPlatformRepository : IPlatformRepository
{
    private readonly ConcurrentDictionary<Guid, ClientAccount> users = new();
    private readonly ConcurrentDictionary<Guid, ClientJourneyState> journeys = new();
    private readonly ConcurrentDictionary<Guid, PasswordResetToken> passwordResets = new();
    private readonly ConcurrentDictionary<Guid, EmailVerificationToken> emailVerifications = new();
    private readonly ConcurrentDictionary<Guid, PlatformSession> sessions = new();
    private readonly ConcurrentDictionary<Guid, PaymentTransaction> payments = new();
    private readonly ConcurrentQueue<AdminUserDeletionLog> deletionLogs = new();
    private readonly object deletionLock = new();

    public Task<ClientAccount?> GetUserByEmailAsync(string email, CancellationToken cancellationToken) =>
        Task.FromResult(users.Values.FirstOrDefault(user => user.Email.Equals(email, StringComparison.OrdinalIgnoreCase)));
    public Task<ClientAccount?> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken) => Task.FromResult(users.GetValueOrDefault(userId));
    public Task AddUserAsync(ClientAccount account, CancellationToken cancellationToken) { users[account.Id] = account; return Task.CompletedTask; }
    public Task<IReadOnlyCollection<ClientAccount>> GetUsersAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyCollection<ClientAccount>>(users.Values.Where(user => !user.IsDeleted).OrderByDescending(user => user.CreatedAtUtc).ToArray());
    public Task<IReadOnlyCollection<AdminUserDeletionLog>> GetUserDeletionLogsAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyCollection<AdminUserDeletionLog>>(deletionLogs.OrderByDescending(entry => entry.DeletedAtUtc).ToArray());
    public Task<AdminNotificationPage> GetNotificationsAsync(string? channel, string? status, int page, int pageSize, CancellationToken cancellationToken) =>
        Task.FromResult(new AdminNotificationPage(Array.Empty<AdminNotificationRecord>(), 0, page, pageSize));
    public Task<UserDeletionOutcome> DeleteUserAsync(Guid actorUserId, Guid userId, CancellationToken cancellationToken)
    {
        lock (deletionLock)
        {
            if (!users.TryGetValue(actorUserId, out var actor) || actor.IsDeleted || actor.Role != UserRoles.Admin)
                return Task.FromResult(new UserDeletionOutcome("actor-unavailable", null));
            if (actorUserId == userId)
                return Task.FromResult(new UserDeletionOutcome("self-delete", null));
            if (!users.TryGetValue(userId, out var target) || target.IsDeleted)
                return Task.FromResult(new UserDeletionOutcome("not-found", null));
            if (target.Role == UserRoles.Admin && users.Values.Count(user => !user.IsDeleted && user.Role == UserRoles.Admin) <= 1)
                return Task.FromResult(new UserDeletionOutcome("last-admin", null));

            var deletedAtUtc = DateTime.UtcNow;
            var auditLog = new AdminUserDeletionLog(actor.Id, actor.Name, actor.Email, target.Id, target.Name, target.Email, target.Role, deletedAtUtc);
            users[target.Id] = target with
            {
                Name = "Deleted user",
                Email = $"deleted+{target.Id:N}@deleted.invalid",
                PasswordHash = string.Empty,
                PasswordSalt = string.Empty,
                IsEmailVerified = false,
                IsDeleted = true,
                DeletedAtUtc = deletedAtUtc
            };
            deletionLogs.Enqueue(auditLog);
            return Task.FromResult(new UserDeletionOutcome(null, auditLog));
        }
    }
    public Task<IReadOnlyCollection<(ClientAccount Account, ClientJourneyState Journey)>> GetUsersWithJourneysAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyCollection<(ClientAccount, ClientJourneyState)>>(users.Values.Where(user => !user.IsDeleted).Select(user => (user, journeys.GetOrAdd(user.Id, id => new ClientJourneyState { UserId = id }))).ToArray());
    public Task UpdateUserRoleAsync(Guid userId, string role, CancellationToken cancellationToken)
    {
        if (users.TryGetValue(userId, out var user)) users[userId] = user with { Role = role };
        return Task.CompletedTask;
    }
    public Task CreatePasswordResetAsync(PasswordResetToken token, CancellationToken cancellationToken)
    {
        foreach (var current in passwordResets.Values.Where(item => item.UserId == token.UserId && item.UsedAtUtc is null))
            passwordResets[current.Id] = current with { UsedAtUtc = token.CreatedAtUtc };
        passwordResets[token.Id] = token;
        return Task.CompletedTask;
    }
    public Task<bool> ResetPasswordAsync(string tokenHash, string passwordHash, string passwordSalt, DateTime usedAtUtc, CancellationToken cancellationToken)
    {
        var reset = passwordResets.Values.FirstOrDefault(item => item.TokenHash == tokenHash && item.UsedAtUtc is null && item.ExpiresAtUtc > usedAtUtc);
        if (reset is null || !users.TryGetValue(reset.UserId, out var user) || user.IsDeleted) return Task.FromResult(false);
        users[user.Id] = user with { PasswordHash = passwordHash, PasswordSalt = passwordSalt };
        passwordResets[reset.Id] = reset with { UsedAtUtc = usedAtUtc };
        return Task.FromResult(true);
    }
    public Task CreateEmailVerificationAsync(EmailVerificationToken token, CancellationToken cancellationToken) { emailVerifications[token.Id] = token; return Task.CompletedTask; }
    public Task<bool> VerifyEmailAsync(string tokenHash, DateTime usedAtUtc, CancellationToken cancellationToken)
    {
        var token = emailVerifications.Values.FirstOrDefault(item => item.TokenHash == tokenHash && item.UsedAtUtc is null && item.ExpiresAtUtc > usedAtUtc);
        if (token is null || !users.TryGetValue(token.UserId, out var user) || user.IsDeleted) return Task.FromResult(false);
        users[user.Id] = user with { IsEmailVerified = true }; emailVerifications[token.Id] = token with { UsedAtUtc = usedAtUtc }; return Task.FromResult(true);
    }
    public Task<ClientJourneyState> GetJourneyAsync(Guid userId, CancellationToken cancellationToken) => Task.FromResult(journeys.GetOrAdd(userId, id => new ClientJourneyState { UserId = id }));
    public Task SaveJourneyAsync(ClientJourneyState state, CancellationToken cancellationToken) { journeys[state.UserId] = state; return Task.CompletedTask; }
    public Task QueueNotificationAsync(Guid userId, string channel, string templateKey, string payloadJson, CancellationToken cancellationToken) => Task.CompletedTask;
    public Task<IReadOnlyCollection<PlatformSession>> GetSessionsAsync(bool futureOnly, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyCollection<PlatformSession>>(sessions.Values.Where(item => !futureOnly || item.StartsAtUtc > DateTime.UtcNow).OrderBy(item => item.StartsAtUtc).ToArray());
    public Task<PlatformSession> CreateSessionAsync(PlatformSession session, CancellationToken cancellationToken) { sessions[session.Id] = session; return Task.FromResult(session); }
    public Task<PlatformSession?> BookSessionAsync(Guid userId, Guid sessionId, CancellationToken cancellationToken)
    {
        if (!sessions.TryGetValue(sessionId, out var session) || !session.IsBookable || session.StartsAtUtc <= DateTime.UtcNow || session.Attendees.Any(item => item.UserId == userId) || session.Capacity is int capacity && session.Attendees.Count >= capacity) return Task.FromResult<PlatformSession?>(null);
        var user = users.GetValueOrDefault(userId); if (user is null) return Task.FromResult<PlatformSession?>(null);
        var updated = session with { Attendees = [.. session.Attendees, new SessionAttendee(userId, user.Name, user.Email, DateTime.UtcNow)] }; sessions[sessionId] = updated; return Task.FromResult<PlatformSession?>(updated);
    }
    public Task<PaymentTransaction> CreatePaymentAsync(PaymentTransaction payment, CancellationToken cancellationToken) { payments[payment.Id] = payment; return Task.FromResult(payment); }
    public Task<IReadOnlyCollection<PaymentTransaction>> GetPaymentsAsync(CancellationToken cancellationToken) => Task.FromResult<IReadOnlyCollection<PaymentTransaction>>(payments.Values.OrderByDescending(item => item.CreatedAtUtc).ToArray());
    public Task<PaymentTransaction?> RefundPaymentAsync(Guid paymentId, string reason, DateTime refundedAtUtc, CancellationToken cancellationToken)
    {
        if (!payments.TryGetValue(paymentId, out var payment) || payment.Status != "paid") return Task.FromResult<PaymentTransaction?>(null);
        var updated = payment with { Status = "refunded", RefundedAtUtc = refundedAtUtc, RefundReason = reason }; payments[paymentId] = updated; return Task.FromResult<PaymentTransaction?>(updated);
    }
    public Task<PaymentTransaction?> SetPaymentProviderReferenceAsync(Guid paymentId, string providerReference, CancellationToken cancellationToken) { if (!payments.TryGetValue(paymentId, out var payment)) return Task.FromResult<PaymentTransaction?>(null); var updated=payment with { ProviderReference=providerReference }; payments[paymentId]=updated; return Task.FromResult<PaymentTransaction?>(updated); }
    public Task<PaymentTransaction?> CompletePaymentAsync(Guid paymentId, string providerReference, DateTime paidAtUtc, CancellationToken cancellationToken) { if (!payments.TryGetValue(paymentId, out var payment)) return Task.FromResult<PaymentTransaction?>(null); var updated=payment with { Status="paid", ProviderReference=providerReference, PaidAtUtc=paidAtUtc }; payments[paymentId]=updated; return Task.FromResult<PaymentTransaction?>(updated); }
}
