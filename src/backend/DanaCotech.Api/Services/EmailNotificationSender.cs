using System.Globalization;
using System.Net;
using System.Net.Mail;
using System.Text;

namespace DanaCotech.Api.Services;

public sealed class EmailNotificationSender(IConfiguration configuration)
{
    public async Task SendAsync(string email, string name, string template, string payload, CancellationToken cancellationToken)
    {
        var host = Required("Email:SmtpHost");
        var fromAddress = Required("Email:FromAddress");
        var username = configuration["Email:Username"];
        var password = configuration["Email:Password"];
        if (string.IsNullOrWhiteSpace(username) != string.IsNullOrWhiteSpace(password))
            throw new InvalidOperationException("Email:Username and Email:Password must both be configured, or both be empty.");

        using var data = new OutboxPayload(payload);
        var content = Compose(name, template, data);
        var fromName = configuration["Email:FromName"] ?? "Bawsala";
        using var message = new MailMessage
        {
            From = new MailAddress(fromAddress, fromName, Encoding.UTF8),
            Subject = content.Subject,
            SubjectEncoding = Encoding.UTF8,
            BodyEncoding = Encoding.UTF8,
            IsBodyHtml = true,
            Body = content.Html
        };
        message.To.Add(new MailAddress(email));
        message.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(content.Text, Encoding.UTF8, "text/plain"));

        using var client = new SmtpClient(host, configuration.GetValue("Email:SmtpPort", 587))
        {
            EnableSsl = configuration.GetValue("Email:EnableSsl", true),
            Timeout = configuration.GetValue("Email:TimeoutMilliseconds", 30000)
        };
        if (!string.IsNullOrWhiteSpace(username))
        {
            client.UseDefaultCredentials = false;
            client.Credentials = new NetworkCredential(username, password);
        }

        await client.SendMailAsync(message, cancellationToken);
    }

    private EmailContent Compose(string name, string template, OutboxPayload data)
    {
        var arabic = string.Equals(data.GetString("language"), "ar", StringComparison.OrdinalIgnoreCase);
        var recipient = string.IsNullOrWhiteSpace(name) ? (arabic ? "مرحباً" : "Hello") : name.Trim();
        var dashboardUrl = DashboardUrl();
        var paragraphs = new List<string>();
        var subject = "Bawsala update";
        var heading = arabic ? "تحديث من بوصلة" : "An update from Bawsala";
        string? actionLabel = null;
        string? actionUrl = null;

        switch (template)
        {
            case "email-verification":
                subject = arabic ? "تحقّق من بريدك الإلكتروني | بوصلة" : "Verify your email | Bawsala";
                heading = arabic ? "أهلاً بك في بوصلة" : "Welcome to Bawsala";
                paragraphs.Add(arabic ? "يرجى تأكيد بريدك الإلكتروني لإكمال إنشاء حسابك." : "Confirm your email address to finish setting up your account.");
                paragraphs.Add(arabic ? "تنتهي صلاحية رابط التأكيد خلال 24 ساعة." : "This verification link expires in 24 hours.");
                actionLabel = arabic ? "تأكيد البريد الإلكتروني" : "Verify email";
                actionUrl = data.GetString("verificationUrl");
                break;
            case "password-reset":
                subject = arabic ? "إعادة تعيين كلمة المرور | بوصلة" : "Reset your password | Bawsala";
                heading = arabic ? "إعادة تعيين كلمة المرور" : "Reset your password";
                paragraphs.Add(arabic ? "استخدم الرابط أدناه لاختيار كلمة مرور جديدة. تنتهي صلاحيته خلال ساعة." : "Use the secure link below to choose a new password. It expires in one hour.");
                actionLabel = arabic ? "اختيار كلمة مرور جديدة" : "Choose a new password";
                actionUrl = data.GetString("resetUrl");
                break;
            case "welcome-enrollment":
                subject = arabic ? "تم تأكيد تسجيلك | بوصلة" : "Your enrollment is confirmed | Bawsala";
                heading = arabic ? "مرحباً بك في رحلتك" : "Your journey starts here";
                paragraphs.Add(arabic ? "تم تأكيد تسجيلك. سجّل الدخول لمتابعة برنامجك والمواد المتاحة لك." : "Your enrollment is confirmed. Sign in to open your program and available learning materials.");
                actionLabel = arabic ? "فتح لوحة التحكم" : "Open your dashboard";
                actionUrl = dashboardUrl;
                break;
            case "career-profile-submitted":
                subject = arabic ? "تم استلام ملفك المهني | بوصلة" : "Your career profile was submitted | Bawsala";
                heading = arabic ? "تم استلام تقييماتك" : "We received your assessments";
                paragraphs.Add(arabic ? "اكتملت تقييماتك الأربعة. ستراجع دانا ملفك المهني، وسنرسل إليك رسالة عند جهوزيته." : "Your four assessments are complete. Dana will review your career profile, and we’ll email you when it is ready.");
                break;
            case "career-profile-ready":
                subject = arabic ? "ملفك المهني جاهز | بوصلة" : "Your career profile is ready | Bawsala";
                heading = arabic ? "ملفك المهني جاهز" : "Your career profile is ready";
                paragraphs.Add(arabic ? "راجعت دانا ملفك. سجّل الدخول للاطلاع على النتائج وملاحظات المراجعة." : "Dana has reviewed your profile. Sign in to see your results and review notes.");
                actionLabel = arabic ? "عرض الملف المهني" : "View your career profile";
                actionUrl = dashboardUrl;
                break;
            case "booking-confirmation":
                subject = arabic ? "تم تأكيد جلستك | بوصلة" : "Your session is confirmed | Bawsala";
                heading = arabic ? "تم تأكيد جلستك" : "Your session is confirmed";
                var title = SessionTitle(data, arabic);
                paragraphs.Add(arabic ? $"نوع الجلسة: {title}" : $"Session: {title}");
                if (data.GetDateTimeOffset("startsAtUtc") is { } startsAt)
                    paragraphs.Add(arabic ? $"الموعد: {FormatJordanTime(startsAt, true)}" : $"When: {FormatJordanTime(startsAt, false)}");
                paragraphs.Add(arabic ? "أُضيفت الجلسة إلى طلبك. ستصلك دعوة التقويم عبر البريد إذا كان الربط مفعّلاً." : "Your session is booked. A calendar invitation will arrive by email when Calendar is connected.");
                actionLabel = arabic ? "الانضمام إلى الجلسة" : "Join the session";
                actionUrl = data.GetString("meetingUrl");
                break;
            case "payment-refunded":
                subject = arabic ? "تم تأكيد استرداد المبلغ | بوصلة" : "Your refund is confirmed | Bawsala";
                heading = arabic ? "تم تأكيد استرداد المبلغ" : "Your refund is confirmed";
                var amount = data.GetString("amount");
                var currency = data.GetString("currency");
                paragraphs.Add(arabic ? "تمت معالجة استرداد المبلغ للدفعة المرتبطة بحسابك." : "The payment associated with your account has been refunded.");
                if (!string.IsNullOrWhiteSpace(amount) || !string.IsNullOrWhiteSpace(currency))
                    paragraphs.Add(arabic ? $"المبلغ: {amount} {currency}" : $"Amount: {amount} {currency}");
                if (!string.IsNullOrWhiteSpace(data.GetString("reason")))
                    paragraphs.Add(arabic ? $"السبب: {data.GetString("reason")}" : $"Reason: {data.GetString("reason")}");
                break;
            case "paid-session-unavailable":
                subject = arabic ? "نحتاج إلى إعادة جدولة جلستك | بوصلة" : "Let’s reschedule your session | Bawsala";
                heading = arabic ? "تم استلام دفعتك" : "Your payment was received";
                paragraphs.Add(arabic ? "لم يعد الموعد المختار متاحاً. سيتواصل فريق بوصلة معك لترتيب موعد بديل أو استرداد المبلغ." : "The selected time is no longer available. The Bawsala team will contact you to arrange another time or a refund.");
                break;
            default:
                paragraphs.Add(arabic ? "لديك تحديث جديد من بوصلة. سجّل الدخول لمتابعة رحلتك." : "You have a new update from Bawsala. Sign in to continue your journey.");
                actionLabel = arabic ? "فتح لوحة التحكم" : "Open your dashboard";
                actionUrl = dashboardUrl;
                break;
        }

        if (!IsSafeLink(actionUrl))
        {
            actionLabel = null;
            actionUrl = null;
        }

        var direction = arabic ? "rtl" : "ltr";
        var supportEmail = configuration["Email:SupportEmail"];
        var supportHtml = string.IsNullOrWhiteSpace(supportEmail)
            ? ""
            : $"<p style=\"margin:8px 0 0;color:#858999;font-size:12px\">{(arabic ? "للمساعدة:" : "Need help?")} <a href=\"mailto:{WebUtility.HtmlEncode(supportEmail)}\" style=\"color:#20295b\">{WebUtility.HtmlEncode(supportEmail)}</a></p>";
        var encodedParagraphs = string.Join("", paragraphs.Select(paragraph => $"<p style=\"margin:0 0 16px;line-height:1.7;color:#4d5368\">{WebUtility.HtmlEncode(paragraph)}</p>"));
        var actionHtml = actionUrl is null ? "" : $"<p style=\"margin:28px 0\"><a href=\"{WebUtility.HtmlEncode(actionUrl)}\" style=\"display:inline-block;background:#20295b;color:#fff;padding:13px 20px;border-radius:8px;text-decoration:none;font-weight:700\">{WebUtility.HtmlEncode(actionLabel)}</a></p>";
        var html = $"<!doctype html><html lang=\"{(arabic ? "ar" : "en")}\"><body style=\"margin:0;background:#f5f6f3;font-family:Arial,sans-serif\"><div dir=\"{direction}\" style=\"max-width:620px;margin:24px auto;padding:32px;background:#fff;border-radius:14px\"><div style=\"color:#2f4f23;font-size:13px;font-weight:700;letter-spacing:.08em\">BAWSALA</div><h1 style=\"margin:16px 0;color:#20295b;font-size:26px\">{WebUtility.HtmlEncode(heading)}</h1><p style=\"margin:0 0 18px;line-height:1.7;color:#4d5368\">{(arabic ? "مرحباً" : "Hello")} {WebUtility.HtmlEncode(recipient)},</p>{encodedParagraphs}{actionHtml}<p style=\"border-top:1px solid #e8e9ed;padding-top:18px;margin-top:30px;color:#858999;font-size:12px;line-height:1.6\">{(arabic ? "هذه رسالة آلية من منصة بوصلة." : "This is an automated message from the Bawsala platform.")}</p>{supportHtml}</div></body></html>";
        var textLines = new List<string> { heading, $"{(arabic ? "مرحباً" : "Hello")} {recipient}," };
        textLines.AddRange(paragraphs);
        if (actionUrl is not null) textLines.Add($"{actionLabel}: {actionUrl}");
        if (!string.IsNullOrWhiteSpace(supportEmail)) textLines.Add($"{(arabic ? "للمساعدة" : "Need help?")}: {supportEmail}");
        var text = string.Join(Environment.NewLine + Environment.NewLine, textLines);
        return new EmailContent(subject, html, text);
    }

    private string DashboardUrl() => $"{(configuration["Frontend:BaseUrl"] ?? "https://www.bawsala1.com").TrimEnd('/')}/dashboard";

    private string Required(string key) =>
        string.IsNullOrWhiteSpace(configuration[key])
            ? throw new InvalidOperationException($"{key} is missing.")
            : configuration[key]!;

    private static bool IsSafeLink(string? value) =>
        Uri.TryCreate(value, UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp);

    private static string SessionTitle(OutboxPayload data, bool arabic)
    {
        var explicitTitle = arabic
            ? data.GetString("titleAr", "sessionTitleAr")
            : data.GetString("titleEn", "sessionTitleEn");
        if (!string.IsNullOrWhiteSpace(explicitTitle)) return explicitTitle;
        var sessionType = data.GetString("sessionType", "focus") ?? "coaching";
        var displayName = string.Join(" ", sessionType.Replace('-', ' ').Replace('_', ' ').Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(part => char.ToUpperInvariant(part[0]) + part[1..]));
        return arabic ? $"جلسة {displayName}" : $"{displayName} with Dana";
    }

    private static string FormatJordanTime(DateTimeOffset value, bool arabic)
    {
        TimeZoneInfo zone;
        try { zone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Amman"); }
        catch (TimeZoneNotFoundException) { zone = TimeZoneInfo.FindSystemTimeZoneById("Jordan Standard Time"); }
        var local = TimeZoneInfo.ConvertTime(value, zone);
        var culture = CultureInfo.GetCultureInfo(arabic ? "ar-JO" : "en-GB");
        return local.ToString(arabic ? "dddd، d MMMM yyyy h:mm tt" : "dddd, d MMMM yyyy h:mm tt", culture) + (arabic ? " (عمّان)" : " (Amman)");
    }

    private sealed record EmailContent(string Subject, string Html, string Text);
}
