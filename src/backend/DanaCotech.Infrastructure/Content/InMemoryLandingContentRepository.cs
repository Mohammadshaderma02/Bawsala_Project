using DanaCotech.Application.Content;
using DanaCotech.Domain.Content;

namespace DanaCotech.Infrastructure.Content;

public sealed class InMemoryLandingContentRepository : ILandingContentRepository
{
    private readonly Dictionary<string, LandingContent> contentByLanguage = new(StringComparer.OrdinalIgnoreCase)
    {
        ["en"] = EnglishContent(),
        ["ar"] = ArabicContent()
    };

    public Task<LandingContent?> GetLandingContentAsync(string language, CancellationToken cancellationToken)
    {
        contentByLanguage.TryGetValue(language, out var content);
        return Task.FromResult(content);
    }

    public Task SaveLandingContentAsync(string language, LandingContent content, CancellationToken cancellationToken)
    {
        contentByLanguage[language] = content;
        return Task.CompletedTask;
    }

    private static LandingContent EnglishContent() => new(
        Hero: new HeroSection(
            "Dana Gharaibeh Career Platform",
            "Your successful career starts when your story, strengths, and next step finally make sense.",
            "A practical coaching and learning platform for career clarity, CV building, LinkedIn, interviews, and workplace survival.",
            "/hero-reference.png",
            "",
            new Cta("Start your journey", "#programs"),
            new Cta("Book a session", "#contact")),
        FeaturedOffer: new FeaturedOffer(
            "Featured cohort",
            "Career Clarity Program",
            "A guided five-week cohort that turns assessments, reflection, and live coaching into a downloadable career identity.",
            "/hero-reference.png",
            new Cta("Reserve your seat", "#programs")),
        MasterClass: new MasterClassSection(
            "What is the Career Clarity masterclass?",
            "This experience is built for people who feel stuck between options. Dana helps you connect character strengths, career interests, values, and life purpose into a practical direction you can act on.",
            "Begin with clarity. Leave with a plan.",
            "/hero-reference.png",
            new Cta("Explore the program", "#programs")),
        Programs: new[]
        {
            new ProgramCard("career-clarity-program", "Career Clarity Program", "Assessments, live group sessions, personal reflection, and a career identity download.", "Cohort pricing", "5 weeks", "/hero-reference.png", new Cta("Join the cohort", "#contact")),
            new ProgramCard("one-to-one-coaching", "1:1 Coaching Session", "A focused coaching session for career path, CV, LinkedIn, or interview preparation.", "Per session", "30-60 min", "/assets/logo-dana-gharaibeh.svg", new Cta("Reserve time", "#contact")),
            new ProgramCard("career-survival-library", "Career Survival Library", "Recorded lessons for salary negotiation, difficult managers, job search, and daily workplace decisions.", "Bundle or single course", "Self-paced", "/hero-reference.png", new Cta("View library", "#contact"))
        },
        WhyDana: new WhyDanaSection(
            "Why Dana Gharaibeh is the right guide for career clarity?",
            new[]
            {
                new WhyDanaReason(1, "Assessment-based guidance", "The work is built around strengths, interests, values, and purpose rather than generic advice.", "/hero-reference.png"),
                new WhyDanaReason(2, "Practical career outputs", "Clients leave with concrete next steps, CV direction, LinkedIn structure, and interview preparation.", "/assets/logo-dana-gharaibeh.svg"),
                new WhyDanaReason(3, "A platform journey", "The experience combines video, live sessions, calendar booking, progress tracking, and downloadable results.", "/hero-reference.png")
            }),
        DashboardModules: new[]
        {
            new DashboardModule("character-strengths", "Character Strengths", "Assessment upload, recorded explanations, and week-one discussion."),
            new DashboardModule("career-interests", "Career Interests", "In-platform forms that help narrow suitable career paths."),
            new DashboardModule("values", "Values", "Clarify what should guide your next career decision."),
            new DashboardModule("life-purpose", "Life Purpose", "Connect strengths, interests, and values into one practical direction."),
            new DashboardModule("cv-building", "CV Building", "Downloadable templates and guided improvements."),
            new DashboardModule("linkedin", "LinkedIn", "Profile structure and job search session."),
            new DashboardModule("interview-skills", "Interview Skills", "Live practice session and preparation framework.")
        },
        About: new AboutSection(
            "Meet Dana",
            "Dana helps professionals translate their experience, strengths, and ambitions into a clear career story and an actionable next step.",
            new[] { "Career clarity coaching", "Assessment-based guidance", "Practical CV, LinkedIn, and interview support" }),
        Testimonials: new[]
        {
            new Testimonial("Client Name", "Career Clarity Participant", "@client", "The program helped me understand my options and turn confusion into a clear plan.", "/assets/logo-dana-gharaibeh.svg"),
            new Testimonial("Client Name", "1:1 Coaching Client", "@client", "Dana's guidance made my CV and interview story sharper and more confident.", "/hero-reference.png")
        },
        LeadCapture: new LeadCaptureSection(
            "Stay updated on Dana's latest programs",
            "Subscribe and we will send you the newest cohorts, sessions, and career resources.",
            "Email address",
            "Subscribe",
            "Let's connect",
            "Reach out any time to ask about cohorts, coaching, or the career survival library."));

    private static LandingContent ArabicContent() => new(
        Hero: new HeroSection(
            "منصة دانا غرايبة المهنية",
            "مسارك المهني يبدأ عندما تفهم قصتك ونقاط قوتك وخطوتك القادمة بوضوح.",
            "منصة تدريب واستشارات عملية لوضوح المسار، بناء السيرة الذاتية، لينكدإن، المقابلات، ومهارات التعامل مع تحديات العمل.",
            "/hero-reference.png",
            "",
            new Cta("ابدأ رحلتك", "#programs"),
            new Cta("احجز جلسة", "#contact")),
        FeaturedOffer: new FeaturedOffer(
            "البرنامج المميز",
            "برنامج وضوح المسار المهني",
            "رحلة جماعية من خمسة أسابيع تحول التقييمات والتأمل والجلسات الحية إلى هوية مهنية قابلة للتحميل.",
            "/hero-reference.png",
            new Cta("احجز مقعدك", "#programs")),
        MasterClass: new MasterClassSection(
            "ما هو ماستر كلاس وضوح المسار المهني؟",
            "هذه التجربة مصممة لكل شخص يشعر أنه عالق بين خيارات كثيرة. تساعدك دانا على ربط نقاط القوة والاهتمامات المهنية والقيم والغاية باتجاه عملي واضح.",
            "ابدأ بوضوح. واخرج بخطة.",
            "/hero-reference.png",
            new Cta("استكشف البرنامج", "#programs")),
        Programs: new[]
        {
            new ProgramCard("career-clarity-program", "برنامج وضوح المسار المهني", "تقييمات وجلسات جماعية مباشرة وتأمل شخصي وملف هوية مهنية قابل للتحميل.", "تسعير البرنامج", "5 أسابيع", "/hero-reference.png", new Cta("انضم للبرنامج", "#contact")),
            new ProgramCard("one-to-one-coaching", "جلسة استشارية فردية", "جلسة مركزة للمسار المهني أو السيرة الذاتية أو لينكدإن أو التحضير للمقابلات.", "حسب الجلسة", "30-60 دقيقة", "/assets/logo-dana-gharaibeh.svg", new Cta("احجز موعد", "#contact")),
            new ProgramCard("career-survival-library", "مكتبة النجاة المهنية", "دروس مسجلة عن التفاوض على الراتب والتعامل مع المدراء الصعبين والبحث عن وظيفة.", "باقة أو كورس منفرد", "تعلم ذاتي", "/hero-reference.png", new Cta("شاهد المكتبة", "#contact"))
        },
        WhyDana: new WhyDanaSection(
            "لماذا دانا غرايبة هي الدليل المناسب لوضوحك المهني؟",
            new[]
            {
                new WhyDanaReason(1, "توجيه مبني على التقييمات", "الرحلة تعتمد على نقاط القوة والاهتمامات والقيم والغاية، وليس على نصائح عامة.", "/hero-reference.png"),
                new WhyDanaReason(2, "مخرجات عملية للمسار", "يخرج العميل بخطوات واضحة واتجاه للسيرة الذاتية وهيكلة لينكدإن وتحضير للمقابلات.", "/assets/logo-dana-gharaibeh.svg"),
                new WhyDanaReason(3, "رحلة منصة متكاملة", "التجربة تجمع الفيديو والجلسات الحية والحجز والتقدم والنتائج القابلة للتحميل.", "/hero-reference.png")
            }),
        DashboardModules: new[]
        {
            new DashboardModule("character-strengths", "نقاط القوة الشخصية", "رفع نتائج التقييم وفيديوهات شرح ونقاش نهاية الأسبوع الأول."),
            new DashboardModule("career-interests", "الاهتمامات المهنية", "نماذج داخل المنصة تساعد على تضييق خيارات المسار."),
            new DashboardModule("values", "القيم", "توضيح ما يجب أن يقود قرارك المهني القادم."),
            new DashboardModule("life-purpose", "الغاية", "ربط نقاط القوة والاهتمامات والقيم باتجاه عملي واضح."),
            new DashboardModule("cv-building", "بناء السيرة الذاتية", "قوالب قابلة للتحميل وتحسينات موجهة."),
            new DashboardModule("linkedin", "لينكدإن", "هيكلة الملف الشخصي وجلسة البحث عن وظيفة."),
            new DashboardModule("interview-skills", "مهارات المقابلات", "جلسة تدريب مباشرة وإطار للتحضير.")
        },
        About: new AboutSection(
            "عن دانا",
            "تساعد دانا المهنيين على تحويل خبراتهم ونقاط قوتهم وطموحاتهم إلى قصة مهنية واضحة وخطوة عملية تالية.",
            new[] { "استشارات وضوح المسار", "توجيه مبني على التقييمات", "دعم عملي للسيرة الذاتية ولينكدإن والمقابلات" }),
        Testimonials: new[]
        {
            new Testimonial("اسم العميل", "مشارك في برنامج وضوح المسار", "@client", "ساعدني البرنامج على فهم خياراتي وتحويل التشتت إلى خطة واضحة.", "/assets/logo-dana-gharaibeh.svg"),
            new Testimonial("اسم العميل", "عميل جلسة فردية", "@client", "توجيه دانا جعل سيرتي وقصتي في المقابلات أوضح وأكثر ثقة.", "/hero-reference.png")
        },
        LeadCapture: new LeadCaptureSection(
            "ابق على اطلاع على آخر برامج دانا",
            "اشترك ليصلك جديد البرامج والجلسات والموارد المهنية.",
            "البريد الإلكتروني",
            "اشترك",
            "لنتواصل",
            "تواصل في أي وقت للسؤال عن البرامج أو الجلسات أو مكتبة النجاة المهنية."));
}
