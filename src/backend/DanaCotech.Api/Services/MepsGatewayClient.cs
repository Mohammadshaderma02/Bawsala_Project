using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace DanaCotech.Api.Services;

public sealed class MepsGatewayClient(IConfiguration configuration, IHttpClientFactory httpClientFactory)
{
    private string MerchantId => configuration["Payments:Meps:MerchantId"] ?? string.Empty;
    private string ApiPassword => configuration["Payments:Meps:ApiPassword"] ?? string.Empty;
    private string GatewayUrl => (configuration["Payments:Meps:GatewayUrl"] ?? "https://mepspay.gateway.mastercard.com").TrimEnd('/');
    private string ApiVersion => configuration["Payments:Meps:ApiVersion"] ?? "100";
    public string CheckoutScriptUrl => $"{GatewayUrl}/static/checkout/checkout.min.js";
    public bool IsConfigured => !string.IsNullOrWhiteSpace(MerchantId) && !string.IsNullOrWhiteSpace(ApiPassword);

    public async Task<MepsCheckoutSession> InitiateCheckoutAsync(string orderId, decimal amount, string currency, string description, string customerEmail, string customerName, string returnUrl, string cancelUrl, CancellationToken cancellationToken)
    {
        EnsureConfigured();
        var payload = new
        {
            apiOperation = "INITIATE_CHECKOUT",
            checkoutMode = "WEBSITE",
            interaction = new
            {
                operation = "PURCHASE",
                returnUrl,
                cancelUrl,
                merchant = new { name = configuration["Payments:Meps:MerchantName"] ?? "Bawsala Consultancy" },
                displayControl = new { billingAddress = "HIDE", customerEmail = "READ_ONLY" }
            },
            order = new { id = orderId, amount = amount.ToString("0.00", CultureInfo.InvariantCulture), currency = currency.ToUpperInvariant(), description },
            customer = new { email = customerEmail, firstName = customerName }
        };
        using var response = await SendAsync(HttpMethod.Post, "session", payload, cancellationToken);
        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode) throw new MepsGatewayException("MEPS could not create the payment session", json);
        using var document = JsonDocument.Parse(json); var root = document.RootElement;
        if (!string.Equals(root.GetProperty("result").GetString(), "SUCCESS", StringComparison.OrdinalIgnoreCase)) throw new MepsGatewayException("MEPS rejected the payment session", json);
        return new(root.GetProperty("session").GetProperty("id").GetString()!, root.GetProperty("successIndicator").GetString()!, CheckoutScriptUrl);
    }

    public async Task<MepsOrderResult> RetrieveOrderAsync(string orderId, CancellationToken cancellationToken)
    {
        EnsureConfigured();
        using var response = await SendAsync(HttpMethod.Get, $"order/{Uri.EscapeDataString(orderId)}", null, cancellationToken);
        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode) throw new MepsGatewayException("MEPS could not verify the payment", json);
        using var document = JsonDocument.Parse(json); var root = document.RootElement;
        var transactions = root.TryGetProperty("transaction", out var list) && list.ValueKind == JsonValueKind.Array ? list.EnumerateArray().ToArray() : [];
        var successful = transactions.LastOrDefault(item => string.Equals(item.TryGetProperty("result", out var result) ? result.GetString() : null, "SUCCESS", StringComparison.OrdinalIgnoreCase));
        var transactionId = successful.ValueKind == JsonValueKind.Object && successful.TryGetProperty("id", out var id) ? id.GetString() ?? string.Empty : string.Empty;
        var paidAmount = root.TryGetProperty("amount", out var amount) ? amount.GetDecimal() : root.TryGetProperty("totalCapturedAmount", out var captured) ? captured.GetDecimal() : root.TryGetProperty("totalAuthorizedAmount", out var authorized) ? authorized.GetDecimal() : 0;
        return new(root.TryGetProperty("result", out var resultValue) ? resultValue.GetString() ?? "" : "", root.TryGetProperty("status", out var status) ? status.GetString() ?? "" : "", paidAmount, root.TryGetProperty("currency", out var currency) ? currency.GetString() ?? "" : "", transactionId, json);
    }

    public async Task RefundAsync(string orderId, string transactionId, decimal amount, string currency, CancellationToken cancellationToken)
    {
        EnsureConfigured();
        var refundId = $"REFUND-{Guid.NewGuid():N}"[..39];
        var payload = new { apiOperation = "REFUND", transaction = new { amount = amount.ToString("0.00", CultureInfo.InvariantCulture), currency = currency.ToUpperInvariant(), targetTransactionId = transactionId } };
        using var response = await SendAsync(HttpMethod.Put, $"order/{Uri.EscapeDataString(orderId)}/transaction/{refundId}", payload, cancellationToken);
        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode) throw new MepsGatewayException("MEPS rejected the refund", json);
        using var document = JsonDocument.Parse(json);
        if (!string.Equals(document.RootElement.GetProperty("result").GetString(), "SUCCESS", StringComparison.OrdinalIgnoreCase)) throw new MepsGatewayException("MEPS rejected the refund", json);
    }

    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string relativePath, object? payload, CancellationToken cancellationToken)
    {
        var client = httpClientFactory.CreateClient(nameof(MepsGatewayClient));
        var token = Convert.ToBase64String(Encoding.UTF8.GetBytes($"merchant.{MerchantId}:{ApiPassword}"));
        using var request = new HttpRequestMessage(method, $"{GatewayUrl}/api/rest/version/{ApiVersion}/merchant/{Uri.EscapeDataString(MerchantId)}/{relativePath}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", token);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        if (payload is not null) request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        return await client.SendAsync(request, cancellationToken);
    }

    private void EnsureConfigured() { if (!IsConfigured) throw new InvalidOperationException("MEPS Merchant ID and API Password must be configured on the backend"); }
}

public sealed record MepsCheckoutSession(string SessionId, string SuccessIndicator, string CheckoutScriptUrl);
public sealed record MepsOrderResult(string Result, string Status, decimal Amount, string Currency, string TransactionId, string RawResponse);
public sealed class MepsGatewayException(string message, string gatewayResponse) : Exception(message) { public string GatewayResponse { get; } = gatewayResponse; }
