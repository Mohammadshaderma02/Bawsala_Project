using DanaCotech.Application.Common;
using DanaCotech.Domain.Content;

namespace DanaCotech.Application.Content;

public sealed class LandingContentService(ILandingContentRepository repository) : ILandingContentService
{
    private static readonly HashSet<string> SupportedLanguages = new(StringComparer.OrdinalIgnoreCase) { "en", "ar" };

    public async Task<ServiceResponse<LandingContent>> GetLandingContentAsync(string language, CancellationToken cancellationToken)
    {
        var normalizedLanguage = string.IsNullOrWhiteSpace(language) ? "en" : language.Trim().ToLowerInvariant();

        if (!SupportedLanguages.Contains(normalizedLanguage))
        {
            return ServiceResponse<LandingContent>.Fail(
                "Unsupported language",
                new[] { "Supported languages are 'en' and 'ar'." });
        }

        var content = await repository.GetLandingContentAsync(normalizedLanguage, cancellationToken);

        if (content is not null && content.UiText is null)
        {
            content = content with { UiText = CreateDefaultUiText(normalizedLanguage) };
        }

        if (content is not null && content.Appearance is null)
        {
            content = content with { Appearance = CreateDefaultAppearance() };
        }

        if (content is not null && content.CustomHtmlSections is null)
        {
            content = content with { CustomHtmlSections = Array.Empty<CustomHtmlSection>() };
        }

        if (content is not null && content.LoginPage is null)
        {
            content = content with { LoginPage = CreateDefaultLoginPage(normalizedLanguage) };
        }

        if (content is not null && content.CourseOffers is null)
        {
            content = content with { CourseOffers = CreateDefaultCourseOffers(normalizedLanguage) };
        }

        if (content is not null && content.Stories is null)
        {
            content = content with { Stories = CreateDefaultStories(normalizedLanguage) };
        }

        if (content is not null && content.PaymentTerms is null)
        {
            content = content with { PaymentTerms = CreateDefaultPaymentTerms(normalizedLanguage) };
        }

        return content is null
            ? ServiceResponse<LandingContent>.Fail("Landing content was not found")
            : ServiceResponse<LandingContent>.Ok(content);
    }

    public async Task<ServiceResponse<LandingContent>> SaveLandingContentAsync(
        string language,
        LandingContent content,
        CancellationToken cancellationToken)
    {
        var normalizedLanguage = string.IsNullOrWhiteSpace(language) ? "en" : language.Trim().ToLowerInvariant();

        if (!SupportedLanguages.Contains(normalizedLanguage))
        {
            return ServiceResponse<LandingContent>.Fail(
                "Unsupported language",
                new[] { "Supported languages are 'en' and 'ar'." });
        }

        if (string.IsNullOrWhiteSpace(content.Hero.Title))
        {
            return ServiceResponse<LandingContent>.Fail(
                "Landing content is invalid",
                new[] { "The hero title is required." });
        }

        content = content with
        {
            UiText = content.UiText ?? CreateDefaultUiText(normalizedLanguage),
            Appearance = content.Appearance ?? CreateDefaultAppearance(),
            CustomHtmlSections = content.CustomHtmlSections ?? Array.Empty<CustomHtmlSection>(),
            LoginPage = content.LoginPage ?? CreateDefaultLoginPage(normalizedLanguage),
            CourseOffers = content.CourseOffers ?? CreateDefaultCourseOffers(normalizedLanguage),
            Stories = content.Stories ?? CreateDefaultStories(normalizedLanguage),
            PaymentTerms = content.PaymentTerms ?? CreateDefaultPaymentTerms(normalizedLanguage)
        };

        await repository.SaveLandingContentAsync(normalizedLanguage, content, cancellationToken);
        return ServiceResponse<LandingContent>.Ok(content, "Landing page published successfully");
    }

    private static LandingUiText CreateDefaultUiText(string language) => language == "ar"
        ? new LandingUiText(
            "البرامج", "التجربة", "عن دانا", "العروض والبرامج", "اختر بين ثلاث تجارب مهنية",
            "لماذا دانا", "لوحة المنصة", "كل ما يتابعه العميل في مكان واحد", "قصص العملاء",
            "ماذا يقول العملاء بعد التجربة", "عن دانا", "توجيه شخصي", "تطور متكامل", "مسارات مهنية",
            "فصلك القادم", "يبدأ من هنا", "ابقَ على تواصل", "جميع الحقوق محفوظة.")
        : new LandingUiText(
            "Programs", "The experience", "About Dana", "Dynamic offers", "Choose between three career experiences",
            "Why Dana", "Platform dashboard", "Everything the client tracks in one place", "Client stories",
            "What clients say after the journey", "About Dana", "Personal guidance", "Whole-person growth", "Career paths",
            "Your next chapter", "Starts right here", "Stay connected", "All rights reserved.");

    private static LandingAppearance CreateDefaultAppearance() => new(
        new LandingTheme(
            "#2f4f23", "#b8cf7c", "#fffdf6", "#fffdf2", "#152013", "#4d5947",
            "#20295b", "#2f4f23", "#f4f1d9", "#d8bd74", 16),
        new LandingSectionVisibility(true, true, true, true, true, true, true, true, true, true));

    private static LoginPageContent CreateDefaultLoginPage(string language) => language == "ar"
        ? new("مرحباً بعودتك", "أكمل رحلتك المهنية.", "ادخل إلى برامجك وجلساتك وتقدمك ومواردك المهنية في مساحة واحدة واضحة.", "تسجيل الدخول إلى حسابك", "أدخل البيانات المرتبطة بحسابك.", "البريد الإلكتروني", "كلمة المرور", "تذكرني", "نسيت كلمة المرور؟", "تسجيل الدخول", "العودة للرئيسية", "تقدمك التعليمي", "جلساتك القادمة", "مواردك المهنية الشخصية")
        : new("Welcome back", "Continue your career journey.", "Access your programs, coaching sessions, progress, and career resources in one focused space.", "Log in to your account", "Enter the details associated with your account.", "Email address", "Password", "Remember me", "Forgot password?", "Log in", "Back to home", "Your learning progress", "Upcoming sessions", "Personal career resources");

    private static PaymentTermsContent CreateDefaultPaymentTerms(string language) => language == "ar"
        ? new("الشروط والأحكام", "بإتمام عملية الدفع، أنت توافق على شراء البرنامج المحدد والالتزام بسياسة الحضور والاستخدام. الوصول إلى المحتوى مخصص لصاحب الحساب فقط ولا يجوز مشاركة المواد أو تسجيلات الجلسات. تخضع طلبات الإلغاء والاسترداد للسياسة المعلنة من المنصة قبل الشراء.", "أوافق على الشروط والأحكام وسياسة الدفع", "قراءة الشروط والأحكام", "يجب الموافقة على الشروط والأحكام لإتمام التسجيل.")
        : new("Terms & Conditions", "By completing payment, you agree to purchase the selected program and follow its attendance and usage policy. Course access is personal to the account holder and materials or session recordings may not be shared. Cancellation and refund requests are governed by the platform policy presented before purchase.", "I agree to the Terms & Conditions and payment policy", "Read the Terms & Conditions", "You must accept the Terms & Conditions to complete enrollment.");

    private static IReadOnlyCollection<CheckoutOfferContent> CreateDefaultCourseOffers(string language)
    {
        CheckoutOfferContent Offer(string slug, string type, decimal price, string enDuration, string arDuration, int sortOrder, string enTitle, string arTitle, string enDescription, string arDescription) => new(
            slug, language == "ar" ? "عرض البرنامج" : "Program offer", language == "ar" ? arTitle : enTitle,
            language == "ar" ? arDescription : enDescription, "/hero-reference.png",
            language == "ar" ? ["وصول كامل إلى محتوى البرنامج", "مواد وتمارين عملية", "متابعة التقدم من لوحة التحكم"] : ["Full access to the program content", "Practical resources and activities", "Track your progress from your dashboard"],
            language == "ar" ? "أكمل عملية الدفع" : "Complete your payment",
            language == "ar" ? "أدخل بياناتك للبدء" : "Enter your details to get started",
            language == "ar" ? "الاسم الكامل" : "Full name", language == "ar" ? "البريد الإلكتروني" : "Email address",
            language == "ar" ? "بيانات البطاقة" : "Card details", language == "ar" ? "ادفع والتحق الآن" : "Pay and enroll now",
            language == "ar" ? "دفع آمن ومشفّر" : "Secure encrypted payment", [], type, price, "USD",
            language == "ar" ? arDuration : enDuration, true, sortOrder, []);
        return [
            Offer("career-clarity-program", "cohort", 249, "5 weeks", "5 أسابيع", 1, "Career Clarity Program", "برنامج وضوح المسار المهني", "A guided five-week journey to build your career identity and next steps.", "رحلة موجهة من خمسة أسابيع لبناء هويتك المهنية وخطواتك القادمة."),
            Offer("one-to-one-coaching", "coaching", 75, "60 min", "60 دقيقة", 2, "1:1 Coaching Session", "جلسة استشارية فردية", "A focused coaching session built around your career goal.", "جلسة مركزة مبنية حول هدفك المهني."),
            Offer("career-survival-library", "library", 99, "Self-paced", "تعلم ذاتي", 3, "Career Survival Library", "مكتبة النجاة المهنية", "Recorded practical courses for real workplace challenges.", "دورات عملية مسجلة لتحديات العمل الحقيقية.")
        ];
    }

    private static IReadOnlyCollection<StoryVideo> CreateDefaultStories(string language)
    {
        var arabic = language == "ar";
        return Enumerable.Range(1, 8)
            .Select(index => new StoryVideo(
                $"dana-story-{index:00}",
                arabic ? $"لحظة مهنية {index:00}" : $"Career moment {index:00}",
                arabic ? "نصيحة قصيرة من دانا تساعدك في رحلتك المهنية." : "A short perspective from Dana for your career journey.",
                $"/media/stories/dana-story-{index:00}.mp4",
                null,
                "both",
                index,
                true))
            .ToArray();
    }
}
