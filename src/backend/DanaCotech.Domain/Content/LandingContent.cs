namespace DanaCotech.Domain.Content;

public sealed record LandingContent(
    HeroSection Hero,
    FeaturedOffer FeaturedOffer,
    MasterClassSection MasterClass,
    IReadOnlyCollection<ProgramCard> Programs,
    WhyDanaSection WhyDana,
    IReadOnlyCollection<DashboardModule> DashboardModules,
    AboutSection About,
    IReadOnlyCollection<Testimonial> Testimonials,
    LeadCaptureSection LeadCapture,
    LandingUiText? UiText = null,
    LandingAppearance? Appearance = null,
    IReadOnlyCollection<CustomHtmlSection>? CustomHtmlSections = null,
    LoginPageContent? LoginPage = null,
    IReadOnlyCollection<CheckoutOfferContent>? CourseOffers = null,
    IReadOnlyCollection<StoryVideo>? Stories = null,
    PaymentTermsContent? PaymentTerms = null);

public sealed record PaymentTermsContent(string Title, string Body, string AcceptLabel, string OpenLabel, string RequiredMessage);

public sealed record StoryVideo(
    string Key,
    string Title,
    string Caption,
    string VideoUrl,
    string? PosterUrl,
    string Audience,
    int Order,
    bool IsVisible = true);

public sealed record CheckoutOfferContent(
    string Slug,
    string Eyebrow,
    string Title,
    string Description,
    string ImageUrl,
    IReadOnlyCollection<string> Benefits,
    string PaymentTitle,
    string PaymentSubtitle,
    string FullNameLabel,
    string EmailLabel,
    string CardLabel,
    string PayButtonLabel,
    string SecurePaymentText,
    IReadOnlyCollection<OfferVideoLesson> Lessons,
    string Type = "course",
    decimal Price = 0,
    string Currency = "USD",
    string Duration = "Self-paced",
    bool IsPublished = true,
    int SortOrder = 0,
    IReadOnlyCollection<OfferCourseSection>? Sections = null);

public sealed record OfferVideoLesson(string Key, string Title, string VideoUrl, int DurationMinutes);
public sealed record OfferCourseSection(string Key, string Title, string Description, int SortOrder, IReadOnlyCollection<OfferVideoLesson> Lessons);

public sealed record LoginPageContent(
    string Eyebrow,
    string Title,
    string Subtitle,
    string FormTitle,
    string FormSubtitle,
    string EmailLabel,
    string PasswordLabel,
    string RememberLabel,
    string ForgotLabel,
    string LoginLabel,
    string BackLabel,
    string FeatureOne,
    string FeatureTwo,
    string FeatureThree);

public sealed record CustomHtmlSection(
    string Key,
    string Title,
    string Html,
    int Order,
    bool IsVisible,
    string Type = "html",
    string? MediaUrl = null,
    string? AltText = null,
    string? Eyebrow = null,
    string? Body = null,
    string Layout = "media-right",
    string Theme = "light",
    string? PosterUrl = null,
    bool Autoplay = false,
    bool Muted = true,
    bool Loop = false,
    string? CtaLabel = null,
    string? CtaUrl = null);

public sealed record LandingAppearance(LandingTheme Theme, LandingSectionVisibility Sections);

public sealed record LandingTheme(
    string Primary,
    string Secondary,
    string Background,
    string Paper,
    string TextPrimary,
    string TextSecondary,
    string Navy,
    string Forest,
    string Cream,
    string Gold,
    int BorderRadius);

public sealed record LandingSectionVisibility(
    bool Header,
    bool Hero,
    bool FeaturedOffer,
    bool Programs,
    bool WhyDana,
    bool Dashboard,
    bool About,
    bool Testimonials,
    bool LeadCapture,
    bool Footer);

public sealed record LandingUiText(
    string NavPrograms,
    string NavExperience,
    string NavAbout,
    string ProgramsEyebrow,
    string ProgramsTitle,
    string WhyEyebrow,
    string DashboardEyebrow,
    string DashboardTitle,
    string TestimonialsEyebrow,
    string TestimonialsTitle,
    string AboutEyebrow,
    string PersonalLabel,
    string GrowthLabel,
    string PathsLabel,
    string NextChapterLabel,
    string StartsHereLabel,
    string StayConnectedLabel,
    string RightsText);

public sealed record HeroSection(string Eyebrow, string Title, string Subtitle, string ImageUrl, string VideoUrl, Cta PrimaryCta, Cta SecondaryCta);

public sealed record FeaturedOffer(string Eyebrow, string Title, string Summary, string ImageUrl, Cta Cta);

public sealed record MasterClassSection(string Title, string Body, string ClosingLine, string ImageUrl, Cta Cta);

public sealed record ProgramCard(string Slug, string Title, string Summary, string PriceLabel, string Duration, string ImageUrl, Cta Cta);

public sealed record WhyDanaSection(string Title, IReadOnlyCollection<WhyDanaReason> Reasons);

public sealed record WhyDanaReason(int Order, string Title, string Body, string ImageUrl);

public sealed record DashboardModule(string Key, string Title, string Description);

public sealed record AboutSection(string Title, string Body, IReadOnlyCollection<string> Highlights);

public sealed record Testimonial(string Name, string Role, string Handle, string Quote, string ImageUrl);

public sealed record LeadCaptureSection(string Title, string Subtitle, string EmailPlaceholder, string SubmitLabel, string ContactTitle, string ContactSubtitle);
