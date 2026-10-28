using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using DanaCotech.Api.Security;
using DanaCotech.Api.Services;
using DanaCotech.Application.Platform;
using DanaCotech.Domain.Platform;
using Microsoft.AspNetCore.Mvc;

namespace DanaCotech.Api.Controllers;

[ApiController]
[Route("api/payments")]
public sealed class PaymentsController(IPlatformService platformService, IPlatformRepository repository, SessionTokenService tokenService, IConfiguration configuration, MepsGatewayClient meps) : ControllerBase
{
    [HttpPost("checkout-session")]
    public async Task<IActionResult> CreateCheckoutSession(StripeCheckoutRequest request, CancellationToken cancellationToken)
    {
        if (!tokenService.TryValidate(Request.Headers.Authorization, out var userId, out _)) return Unauthorized(new { Success=false, Message="A valid session is required" });
        var provider = configuration["Payments:Provider"] ?? "Test";
        var account = await repository.GetUserByIdAsync(userId, cancellationToken); var product=(await platformService.GetCatalogAsync(request.Language,cancellationToken)).FirstOrDefault(item=>item.Slug==request.ProductSlug);
        if(account is null || product is null) return BadRequest(new { Success=false, Message="Account or product was not found" });
        if (provider.Equals("MEPS", StringComparison.OrdinalIgnoreCase))
        {
            if (!meps.IsConfigured) return BadRequest(new { Success=false, Message="MEPS Merchant ID or API Password is not configured on the backend" });
            var mepsNow=DateTime.UtcNow; var mepsPayment=new PaymentTransaction(Guid.NewGuid(),userId,account.Name,account.Email,product.Type,product.Slug,product.Title,product.Price,product.Currency,"pending","meps","",mepsNow,null,null,""); await repository.CreatePaymentAsync(mepsPayment,cancellationToken);
            var mepsFrontend=(configuration["Frontend:BaseUrl"] ?? "http://localhost:5174").TrimEnd('/');
            var orderId=mepsPayment.Id.ToString("N"); var returnUrl=$"{mepsFrontend}/checkout/{Uri.EscapeDataString(product.Slug)}?payment=meps-return&paymentId={mepsPayment.Id}"; var cancelUrl=$"{mepsFrontend}/checkout/{Uri.EscapeDataString(product.Slug)}?payment=cancelled&paymentId={mepsPayment.Id}";
            try
            {
                var session=await meps.InitiateCheckoutAsync(orderId,product.Price,product.Currency,product.Title,account.Email,account.Name,returnUrl,cancelUrl,cancellationToken);
                await repository.SetPaymentProviderReferenceAsync(mepsPayment.Id,$"{orderId}|{session.SessionId}|{session.SuccessIndicator}",cancellationToken);
                return Ok(new { Success=true, Data=new CheckoutLaunch("meps",null,mepsPayment.Id,session.SessionId,session.CheckoutScriptUrl) });
            }
            catch (MepsGatewayException exception) { return StatusCode(502,new { Success=false,Message=exception.Message,Detail=exception.GatewayResponse }); }
        }
        var secret = configuration["Payments:StripeSecretKey"];
        if (!provider.Equals("Stripe", StringComparison.OrdinalIgnoreCase) || string.IsNullOrWhiteSpace(secret))
            return Ok(new { Success=true, Data=new CheckoutLaunch("test", null, null) });
        var now=DateTime.UtcNow; var payment=new PaymentTransaction(Guid.NewGuid(),userId,account.Name,account.Email,product.Type,product.Slug,product.Title,product.Price,product.Currency,"pending","stripe","",now,null,null,""); await repository.CreatePaymentAsync(payment,cancellationToken);
        var frontend=(configuration["Frontend:BaseUrl"] ?? "http://localhost:5174").TrimEnd('/');
        var form=new Dictionary<string,string> { ["mode"]="payment",["success_url"]=$"{frontend}/checkout/{Uri.EscapeDataString(product.Slug)}?payment=success&session_id={{CHECKOUT_SESSION_ID}}",["cancel_url"]=$"{frontend}/checkout/{Uri.EscapeDataString(product.Slug)}?payment=cancelled",["customer_email"]=account.Email,["client_reference_id"]=payment.Id.ToString(),["line_items[0][quantity]"]="1",["line_items[0][price_data][currency]"]=product.Currency.ToLowerInvariant(),["line_items[0][price_data][unit_amount]"]=decimal.Round(product.Price*100,0).ToString(CultureInfo.InvariantCulture),["line_items[0][price_data][product_data][name]"]=product.Title,["metadata[payment_id]"]=payment.Id.ToString(),["metadata[user_id]"]=userId.ToString(),["metadata[product_slug]"]=product.Slug,["metadata[language]"]=request.Language };
        using var client=new HttpClient(); client.DefaultRequestHeaders.Authorization=new("Bearer",secret); using var response=await client.PostAsync("https://api.stripe.com/v1/checkout/sessions",new FormUrlEncodedContent(form),cancellationToken); var json=await response.Content.ReadAsStringAsync(cancellationToken);
        if(!response.IsSuccessStatusCode) return StatusCode(502,new { Success=false, Message="Stripe could not create the checkout session", Detail=json });
        using var document=JsonDocument.Parse(json); var id=document.RootElement.GetProperty("id").GetString()!; var url=document.RootElement.GetProperty("url").GetString(); await repository.SetPaymentProviderReferenceAsync(payment.Id,id,cancellationToken);
        return Ok(new { Success=true, Data=new CheckoutLaunch("stripe",url,payment.Id) });
    }

    [HttpGet("meps/complete")]
    public async Task<IActionResult> CompleteMepsPayment([FromQuery] Guid paymentId, [FromQuery] string resultIndicator, [FromQuery] string language = "en", CancellationToken cancellationToken = default)
    {
        if (!tokenService.TryValidate(Request.Headers.Authorization, out var userId, out _)) return Unauthorized(new { Success=false,Message="A valid session is required" });
        var payment=(await repository.GetPaymentsAsync(cancellationToken)).FirstOrDefault(item=>item.Id==paymentId && item.UserId==userId);
        if(payment is null) return NotFound(new { Success=false,Message="Payment was not found" });
        if(payment.Status=="paid") return Ok(new { Success=true,Data=(await platformService.GetJourneyAsync(userId,language,cancellationToken)).Data });
        if(!payment.Provider.Equals("meps",StringComparison.OrdinalIgnoreCase) || payment.Status!="pending") return BadRequest(new { Success=false,Message="Payment cannot be completed" });
        var reference=payment.ProviderReference.Split('|'); if(reference.Length<3 || !SecureEquals(reference[2],resultIndicator)) return BadRequest(new { Success=false,Message="MEPS did not confirm this payment" });
        try
        {
            var order=await meps.RetrieveOrderAsync(reference[0],cancellationToken);
            var paidStatus=order.Status is "CAPTURED" or "AUTHORIZED" or "PARTIALLY_CAPTURED";
            if(!order.Result.Equals("SUCCESS",StringComparison.OrdinalIgnoreCase) || !paidStatus || order.Amount!=payment.Amount || !order.Currency.Equals(payment.Currency,StringComparison.OrdinalIgnoreCase))
                return BadRequest(new { Success=false,Message="The MEPS order is not a successful payment",Status=order.Status });
            var completed=await repository.CompletePaymentAsync(payment.Id,reference[0],DateTime.UtcNow,cancellationToken); if(completed is null) return BadRequest(new { Success=false,Message="Payment could not be completed" });
            var state=await repository.GetJourneyAsync(userId,cancellationToken); if(state.Enrollments.All(item=>item.ProductSlug!=payment.ProductSlug)) state.Enrollments.Add(new ClientEnrollment { ProductSlug=payment.ProductSlug,PurchasedAtUtc=DateTime.UtcNow }); await repository.SaveJourneyAsync(state,cancellationToken);
            await repository.QueueNotificationAsync(userId,"email","welcome-enrollment",JsonSerializer.Serialize(new { productSlug=payment.ProductSlug,language,paymentId }),cancellationToken);
            return Ok(new { Success=true,Data=(await platformService.GetJourneyAsync(userId,language,cancellationToken)).Data });
        }
        catch(MepsGatewayException exception) { return StatusCode(502,new { Success=false,Message=exception.Message,Detail=exception.GatewayResponse }); }
    }

    [HttpPost("meps/guest-checkout-session")]
    public async Task<IActionResult> CreateGuestMepsCheckout(GuestMepsCheckoutRequest request, CancellationToken cancellationToken)
    {
        if (!meps.IsConfigured) return BadRequest(new { Success=false,Message="MEPS Merchant ID or API Password is not configured on the backend" });
        if(string.IsNullOrWhiteSpace(request.Name) || !request.Email.Contains('@')) return BadRequest(new { Success=false,Message="Name and a valid email are required" });
        var session=(await repository.GetSessionsAsync(true,cancellationToken)).FirstOrDefault(item=>item.Id==request.SessionId && item.SessionType.Equals("coaching",StringComparison.OrdinalIgnoreCase));
        var product=(await platformService.GetCatalogAsync(request.Language,cancellationToken)).FirstOrDefault(item=>item.Slug=="one-to-one-coaching");
        if(session is null || product is null) return BadRequest(new { Success=false,Message="The selected coaching session is not available" });
        var payment=new PaymentTransaction(Guid.NewGuid(),null,request.Name.Trim(),request.Email.Trim().ToLowerInvariant(),"coaching",product.Slug,product.Title,product.Price,product.Currency,"pending","meps","",DateTime.UtcNow,null,null,""); await repository.CreatePaymentAsync(payment,cancellationToken);
        var frontend=(configuration["Frontend:BaseUrl"] ?? "http://localhost:5174").TrimEnd('/'); var orderId=payment.Id.ToString("N");
        try
        {
            var gatewaySession=await meps.InitiateCheckoutAsync(orderId,product.Price,product.Currency,product.Title,payment.CustomerEmail,payment.CustomerName,$"{frontend}/checkout/one-to-one-coaching?payment=meps-guest-return&paymentId={payment.Id}",$"{frontend}/checkout/one-to-one-coaching?payment=cancelled&paymentId={payment.Id}",cancellationToken);
            await repository.SetPaymentProviderReferenceAsync(payment.Id,$"{orderId}|{gatewaySession.SessionId}|{gatewaySession.SuccessIndicator}|{request.SessionId}",cancellationToken);
            return Ok(new { Success=true,Data=new CheckoutLaunch("meps",null,payment.Id,gatewaySession.SessionId,gatewaySession.CheckoutScriptUrl) });
        }
        catch(MepsGatewayException exception) { return StatusCode(502,new { Success=false,Message=exception.Message,Detail=exception.GatewayResponse }); }
    }

    [HttpGet("meps/guest-complete")]
    public async Task<IActionResult> CompleteGuestMepsPayment([FromQuery] Guid paymentId,[FromQuery] string resultIndicator,[FromQuery] string language="en",CancellationToken cancellationToken=default)
    {
        var payment=(await repository.GetPaymentsAsync(cancellationToken)).FirstOrDefault(item=>item.Id==paymentId && item.UserId is null);
        if(payment is null) return NotFound(new { Success=false,Message="Payment was not found" });
        if(payment.Status=="paid") return Ok(new { Success=true,Data=payment });
        var reference=payment.ProviderReference.Split('|'); if(reference.Length<4 || !SecureEquals(reference[2],resultIndicator) || !Guid.TryParse(reference[3],out var sessionId)) return BadRequest(new { Success=false,Message="MEPS did not confirm this payment" });
        try
        {
            var order=await meps.RetrieveOrderAsync(reference[0],cancellationToken); var paidStatus=order.Status is "CAPTURED" or "AUTHORIZED" or "PARTIALLY_CAPTURED";
            if(!order.Result.Equals("SUCCESS",StringComparison.OrdinalIgnoreCase) || !paidStatus || order.Amount!=payment.Amount || !order.Currency.Equals(payment.Currency,StringComparison.OrdinalIgnoreCase)) return BadRequest(new { Success=false,Message="The MEPS order is not a successful payment",Status=order.Status });
            var account=await repository.GetUserByEmailAsync(payment.CustomerEmail,cancellationToken);
            if(account is null) { var created=await platformService.CreateUserAsync(payment.CustomerName,payment.CustomerEmail,$"Guest-{Guid.NewGuid():N}",UserRoles.Client,cancellationToken); if(!created.Success || created.Data is null) return BadRequest(new { Success=false,Message="Guest booking account could not be prepared" }); account=await repository.GetUserByIdAsync(created.Data.UserId,cancellationToken); }
            if(account is null) return BadRequest(new { Success=false,Message="Guest booking account could not be prepared" });
            var completed=await repository.CompletePaymentAsync(payment.Id,reference[0],DateTime.UtcNow,cancellationToken); if(completed is null) return BadRequest(new { Success=false,Message="Payment could not be completed" });
            var booked=await repository.BookSessionAsync(account.Id,sessionId,cancellationToken);
            if(booked is null)
            {
                await repository.QueueNotificationAsync(account.Id,"email","paid-session-unavailable",JsonSerializer.Serialize(new { paymentId=payment.Id,sessionId,language }),cancellationToken);
                return Conflict(new { Success=false,PaymentReceived=true,PaymentId=payment.Id,Message="Payment was received, but the selected session is no longer available. Support has been notified to reschedule or refund it." });
            }
            var state=await repository.GetJourneyAsync(account.Id,cancellationToken); if(state.Bookings.All(item=>item.Id!=booked.Id)) state.Bookings.Add(new ClientBooking(booked.Id,booked.SessionType,booked.StartsAtUtc,"confirmed",booked.MeetingUrl)); await repository.SaveJourneyAsync(state,cancellationToken);
            var payload=JsonSerializer.Serialize(new { booked.Id,booked.SessionType,booked.StartsAtUtc,booked.EndsAtUtc,booked.MeetingUrl,language }); await repository.QueueNotificationAsync(account.Id,"email","booking-confirmation",payload,cancellationToken); await repository.QueueNotificationAsync(account.Id,"calendar","meeting-invitation",payload,cancellationToken);
            return Ok(new { Success=true,Data=completed });
        }
        catch(MepsGatewayException exception) { return StatusCode(502,new { Success=false,Message=exception.Message,Detail=exception.GatewayResponse }); }
    }

    [HttpPost("stripe-webhook")]
    public async Task<IActionResult> StripeWebhook(CancellationToken cancellationToken)
    {
        using var reader=new StreamReader(Request.Body); var body=await reader.ReadToEndAsync(cancellationToken); var webhookSecret=configuration["Payments:StripeWebhookSecret"];
        if(string.IsNullOrWhiteSpace(webhookSecret) || !ValidSignature(body,Request.Headers["Stripe-Signature"].ToString(),webhookSecret)) return BadRequest();
        using var document=JsonDocument.Parse(body); var root=document.RootElement; if(root.GetProperty("type").GetString()!="checkout.session.completed") return Ok();
        var session=root.GetProperty("data").GetProperty("object"); if(session.GetProperty("payment_status").GetString()!="paid") return Ok();
        var metadata=session.GetProperty("metadata"); if(!Guid.TryParse(metadata.GetProperty("payment_id").GetString(),out var paymentId) || !Guid.TryParse(metadata.GetProperty("user_id").GetString(),out var userId)) return BadRequest();
        var slug=metadata.GetProperty("product_slug").GetString()!; var language=metadata.TryGetProperty("language",out var lang)?lang.GetString() ?? "en":"en"; var reference=session.TryGetProperty("payment_intent",out var intent)?intent.GetString() ?? session.GetProperty("id").GetString()!:session.GetProperty("id").GetString()!;
        var completed=await repository.CompletePaymentAsync(paymentId,reference,DateTime.UtcNow,cancellationToken); if(completed is null) return NotFound();
        var state=await repository.GetJourneyAsync(userId,cancellationToken); if(state.Enrollments.All(item=>item.ProductSlug!=slug)) state.Enrollments.Add(new ClientEnrollment { ProductSlug=slug,PurchasedAtUtc=DateTime.UtcNow }); await repository.SaveJourneyAsync(state,cancellationToken); await repository.QueueNotificationAsync(userId,"email","welcome-enrollment",JsonSerializer.Serialize(new { productSlug=slug,language,paymentId }),cancellationToken);
        return Ok();
    }

    private static bool ValidSignature(string payload,string header,string secret)
    {
        var parts=header.Split(',').Select(item=>item.Split('=',2)).Where(item=>item.Length==2).ToArray(); var timestamp=parts.FirstOrDefault(item=>item[0]=="t")?[1]; var signatures=parts.Where(item=>item[0]=="v1").Select(item=>item[1]); if(!long.TryParse(timestamp,out var seconds) || Math.Abs(DateTimeOffset.UtcNow.ToUnixTimeSeconds()-seconds)>300) return false;
        var expected=Convert.ToHexString(HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret),Encoding.UTF8.GetBytes($"{timestamp}.{payload}"))).ToLowerInvariant(); return signatures.Any(signature=>CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(expected),Encoding.ASCII.GetBytes(signature)));
    }

    private static bool SecureEquals(string expected,string actual)
    {
        var left=Encoding.UTF8.GetBytes(expected); var right=Encoding.UTF8.GetBytes(actual ?? string.Empty);
        return left.Length==right.Length && CryptographicOperations.FixedTimeEquals(left,right);
    }
}

public sealed record StripeCheckoutRequest(string ProductSlug,string Language="en");
public sealed record GuestMepsCheckoutRequest(string Name,string Email,Guid SessionId,string Language="en");
