using DanaCotech.Api.Security;
using DanaCotech.Api.Services;
using DanaCotech.Application.Platform;
using DanaCotech.Application.Common;
using Microsoft.AspNetCore.Mvc;

namespace DanaCotech.Api.Controllers;

[ApiController]
[Route("api/platform")]
public sealed class PlatformController(IPlatformService platformService, SessionTokenService tokenService, IConfiguration configuration, MepsGatewayClient meps) : ControllerBase
{
    [HttpGet("catalog")]
    public async Task<IActionResult> Catalog([FromQuery] string lang = "en", CancellationToken cancellationToken = default) =>
        Ok(new { Success = true, Data = await platformService.GetCatalogAsync(lang, cancellationToken) });

    [HttpGet("journey")]
    public async Task<IActionResult> Journey([FromQuery] string lang = "en", CancellationToken cancellationToken = default) =>
        await WithUser(userId => platformService.GetJourneyAsync(userId, lang, cancellationToken));

    [HttpGet("admin/courses/{productSlug}/students")]
    public async Task<IActionResult> CourseStudents(string productSlug, CancellationToken cancellationToken)
    {
        if (!tokenService.TryValidate(Request.Headers.Authorization, out _, out var role) || !string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase))
            return StatusCode(StatusCodes.Status403Forbidden, new { Success = false, Message = "Admin access is required" });
        var response = await platformService.GetCourseStudentsAsync(productSlug, cancellationToken);
        return response.Success ? Ok(response) : NotFound(response);
    }

    [HttpGet("admin/notifications")]
    public async Task<IActionResult> AdminNotifications(
        [FromQuery] string? channel,
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken cancellationToken = default)
    {
        if (!IsAdmin()) return AdminAccessDenied();
        return Ok(await platformService.GetNotificationsAsync(channel, status, page, pageSize, cancellationToken));
    }

    [HttpPost("admin/notifications/{notificationId:long}/resend")]
    public async Task<IActionResult> ResendNotification(long notificationId, CancellationToken cancellationToken)
    {
        if (!IsAdmin()) return AdminAccessDenied();
        if (!configuration.GetValue<bool>("Email:Enabled"))
            return BadRequest(ServiceResponse<long>.Fail("Email delivery is disabled. Enable Email:Enabled before resending."));
        var response = await platformService.ResendNotificationAsync(notificationId, cancellationToken);
        return response.Success ? Ok(response) : BadRequest(response);
    }

    [HttpPost("admin/notifications/test-email")]
    public async Task<IActionResult> SendTestEmail(TestEmailRequest request, CancellationToken cancellationToken)
    {
        if (!tokenService.TryValidate(Request.Headers.Authorization, out var adminUserId, out var role) || !string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase))
            return AdminAccessDenied();
        if (!configuration.GetValue<bool>("Email:Enabled"))
            return BadRequest(ServiceResponse<string>.Fail("Email delivery is disabled. Enable Email:Enabled before sending a test email."));
        var response = await platformService.QueueTestEmailAsync(adminUserId, request.Email, request.Language, cancellationToken);
        return response.Success ? Ok(response) : BadRequest(response);
    }

    [HttpPost("checkout")]
    public async Task<IActionResult> Checkout(CheckoutRequest request, CancellationToken cancellationToken) =>
        await WithUser(userId => platformService.EnrollAsync(userId, request.ProductSlug, request.Language, cancellationToken));

    [HttpPost("guest-coaching-checkout")]
    public async Task<IActionResult> GuestCoachingCheckout(GuestCoachingCheckoutRequest request, CancellationToken cancellationToken)
    {
        var response = await platformService.GuestCoachingCheckoutAsync(request.Name, request.Email, request.SessionId, request.Language, cancellationToken);
        return response.Success ? Ok(response) : BadRequest(response);
    }

    [HttpPost("lessons/{lessonId}/complete")]
    public async Task<IActionResult> CompleteLesson(string lessonId, LanguageRequest request, CancellationToken cancellationToken) =>
        await WithUser(userId => platformService.CompleteLessonAsync(userId, lessonId, request.Language, cancellationToken));

    [HttpPost("assessments/{key}")]
    public async Task<IActionResult> SaveAssessment(string key, AssessmentRequest request, CancellationToken cancellationToken) =>
        await WithUser(userId => platformService.SaveAssessmentAsync(userId, key, request.Answers, request.Language, cancellationToken));

    [HttpPost("bookings")]
    public async Task<IActionResult> Book(BookingRequest request, CancellationToken cancellationToken) =>
        await WithUser(userId => platformService.BookSessionAsync(userId, request.SessionType, request.StartsAtUtc, request.Language, cancellationToken));

    [HttpGet("sessions")]
    public async Task<IActionResult> Sessions(CancellationToken cancellationToken) => Ok(await platformService.GetSessionsAsync(true, cancellationToken));

    [HttpPost("sessions/{sessionId:guid}/book")]
    public async Task<IActionResult> BookSession(Guid sessionId, LanguageRequest request, CancellationToken cancellationToken) =>
        await WithUser(userId => platformService.BookSessionAsync(userId, sessionId, request.Language, cancellationToken));

    [HttpGet("admin/sessions")]
    public async Task<IActionResult> AdminSessions(CancellationToken cancellationToken)
    {
        if (!IsAdmin()) return AdminAccessDenied();
        return Ok(await platformService.GetSessionsAsync(false, cancellationToken));
    }

    [HttpPost("admin/sessions")]
    public async Task<IActionResult> CreateSession(CreateSessionRequest request, CancellationToken cancellationToken)
    {
        if (!IsAdmin()) return AdminAccessDenied();
        var response = await platformService.CreateSessionAsync(request.SessionType, request.TitleEn, request.TitleAr, request.StartsAtUtc, request.EndsAtUtc, request.Capacity, request.MeetingUrl, cancellationToken);
        return response.Success ? Ok(response) : BadRequest(response);
    }

    [HttpGet("admin/payments")]
    public async Task<IActionResult> AdminPayments(CancellationToken cancellationToken)
    {
        if (!IsAdmin()) return AdminAccessDenied();
        return Ok(await platformService.GetPaymentsAsync(cancellationToken));
    }

    [HttpPost("admin/payments/{paymentId:guid}/refund")]
    public async Task<IActionResult> RefundPayment(Guid paymentId, RefundPaymentRequest request, CancellationToken cancellationToken)
    {
        if (!IsAdmin()) return AdminAccessDenied();
        var existing=(await platformService.GetPaymentsAsync(cancellationToken)).Data?.FirstOrDefault(item=>item.Id==paymentId);
        if(existing?.Provider.Equals("stripe",StringComparison.OrdinalIgnoreCase)==true)
        {
            var secret=configuration["Payments:StripeSecretKey"]; if(string.IsNullOrWhiteSpace(secret)) return BadRequest(new { Success=false,Message="Stripe secret key is not configured" });
            using var client=new HttpClient(); client.DefaultRequestHeaders.Authorization=new("Bearer",secret); using var stripeResponse=await client.PostAsync("https://api.stripe.com/v1/refunds",new FormUrlEncodedContent(new Dictionary<string,string>{{"payment_intent",existing.ProviderReference},{"metadata[internal_payment_id]",paymentId.ToString()}}),cancellationToken);
            if(!stripeResponse.IsSuccessStatusCode) return StatusCode(502,new { Success=false,Message="Stripe rejected the refund",Detail=await stripeResponse.Content.ReadAsStringAsync(cancellationToken) });
        }
        if(existing?.Provider.Equals("meps",StringComparison.OrdinalIgnoreCase)==true)
        {
            try
            {
                var orderId=existing.ProviderReference.Split('|')[0]; var order=await meps.RetrieveOrderAsync(orderId,cancellationToken);
                if(string.IsNullOrWhiteSpace(order.TransactionId)) return BadRequest(new { Success=false,Message="No successful MEPS transaction was found for this order" });
                await meps.RefundAsync(orderId,order.TransactionId,existing.Amount,existing.Currency,cancellationToken);
            }
            catch(MepsGatewayException exception) { return StatusCode(502,new { Success=false,Message=exception.Message,Detail=exception.GatewayResponse }); }
        }
        var response = await platformService.RefundPaymentAsync(paymentId, request.Reason, cancellationToken);
        return response.Success ? Ok(response) : BadRequest(response);
    }

    [HttpGet("admin/program-clients")]
    public async Task<IActionResult> ProgramClients(CancellationToken cancellationToken)
    {
        if (!IsAdmin()) return AdminAccessDenied();
        return Ok(await platformService.GetProgramClientsAsync(cancellationToken));
    }

    [HttpPost("admin/program-clients/{userId:guid}/profile")]
    public async Task<IActionResult> ReviewProfile(Guid userId, ReviewProfileRequest request, CancellationToken cancellationToken)
    {
        if (!IsAdmin()) return AdminAccessDenied();
        var response = await platformService.ReviewCareerProfileAsync(userId, request.Notes, request.Release, cancellationToken);
        return response.Success ? Ok(response) : BadRequest(response);
    }

    [HttpGet("career-profile.pdf")]
    public async Task<IActionResult> CareerProfilePdf([FromQuery] string lang = "en", CancellationToken cancellationToken = default)
    {
        if (!tokenService.TryValidate(Request.Headers.Authorization, out var userId, out _)) return Unauthorized();
        var response = await platformService.GetJourneyAsync(userId, lang, cancellationToken);
        if (!response.Success || response.Data?.State.Profile.Status != "ready" || response.Data.CareerIdentity is null) return NotFound(new { Success = false, Message = "Career profile is not ready" });
        return File(SimplePdf.Create(response.Data.ClientName, response.Data.CareerIdentity, response.Data.State.Profile.AdminNotes), "application/pdf", "career-profile.pdf");
    }

    private async Task<IActionResult> WithUser<T>(Func<Guid, Task<ServiceResponse<T>>> action)
    {
        if (!tokenService.TryValidate(Request.Headers.Authorization, out var userId, out _)) return Unauthorized(new { Success = false, Message = "A valid session is required" });
        var response = await action(userId);
        return response.Success ? Ok(response) : BadRequest(response);
    }

    private bool IsAdmin() => tokenService.TryValidate(Request.Headers.Authorization, out _, out var role) && string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase);
    private IActionResult AdminAccessDenied() => StatusCode(StatusCodes.Status403Forbidden, new { Success = false, Message = "Admin access is required" });
}

public sealed record CheckoutRequest(string ProductSlug, string Language = "en");
public sealed record LanguageRequest(string Language = "en");
public sealed record AssessmentRequest(IReadOnlyDictionary<string, string> Answers, string Language = "en");
public sealed record BookingRequest(string SessionType, DateTime StartsAtUtc, string Language = "en");
public sealed record CreateSessionRequest(string SessionType, string TitleEn, string TitleAr, DateTime StartsAtUtc, DateTime EndsAtUtc, int? Capacity, string MeetingUrl = "");
public sealed record RefundPaymentRequest(string Reason);
public sealed record ReviewProfileRequest(string Notes, bool Release);
public sealed record GuestCoachingCheckoutRequest(string Name, string Email, Guid SessionId, string Language = "en");
public sealed record TestEmailRequest(string Email, string Language = "en");

internal static class SimplePdf
{
    public static byte[] Create(string name, DanaCotech.Domain.Platform.CareerIdentity identity, string notes)
    {
        static string Clean(string value) => value.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)").Replace("\r", " ").Replace("\n", " ");
        var lines = new[] { "CAREER CLARITY PROFILE", name, $"Strengths: {identity.Strengths}", $"Interests: {identity.Interests}", $"Values: {identity.Values}", $"Purpose: {identity.Purpose}", $"Coach notes: {notes}" };
        var stream = "BT /F1 18 Tf 56 770 Td " + string.Join(" Tj 0 -42 Td ", lines.Select(line => $"({Clean(line)})")) + " Tj ET";
        var objects = new[] { "<< /Type /Catalog /Pages 2 0 R >>", "<< /Type /Pages /Kids [3 0 R] /Count 1 >>", "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Resources << /Font << /F1 5 0 R >> >> /Contents 4 0 R >>", $"<< /Length {System.Text.Encoding.ASCII.GetByteCount(stream)} >>\nstream\n{stream}\nendstream", "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>" };
        var sb = new System.Text.StringBuilder("%PDF-1.4\n"); var offsets = new List<int> { 0 };
        for (var i = 0; i < objects.Length; i++) { offsets.Add(System.Text.Encoding.ASCII.GetByteCount(sb.ToString())); sb.Append($"{i + 1} 0 obj\n{objects[i]}\nendobj\n"); }
        var xref = System.Text.Encoding.ASCII.GetByteCount(sb.ToString()); sb.Append($"xref\n0 {objects.Length + 1}\n0000000000 65535 f \n"); foreach (var offset in offsets.Skip(1)) sb.Append($"{offset:0000000000} 00000 n \n"); sb.Append($"trailer << /Size {objects.Length + 1} /Root 1 0 R >>\nstartxref\n{xref}\n%%EOF");
        return System.Text.Encoding.ASCII.GetBytes(sb.ToString());
    }
}
