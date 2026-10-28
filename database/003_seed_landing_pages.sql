INSERT INTO dbo.LandingPages (LanguageCode, ContentJson, IsPublished)
VALUES
(
    N'en',
    N'{
      "hero": {
        "eyebrow": "Dana Gharaibeh Career Platform",
        "title": "Your successful career starts when your story, strengths, and next step finally make sense.",
        "subtitle": "A practical coaching and learning platform for career clarity, CV building, LinkedIn, interviews, and workplace survival.",
        "imageUrl": "/hero-reference.png",
        "videoUrl": "",
        "primaryCta": { "label": "Start your journey", "url": "#programs" },
        "secondaryCta": { "label": "Book a session", "url": "#contact" }
      },
      "featuredOffer": {
        "eyebrow": "Featured cohort",
        "title": "Career Clarity Program",
        "summary": "A guided five-week cohort that turns assessments, reflection, and live coaching into a downloadable career identity.",
        "imageUrl": "/hero-reference.png",
        "cta": { "label": "Reserve your seat", "url": "#programs" }
      },
      "masterClass": {
        "title": "What is the Career Clarity masterclass?",
        "body": "This experience is built for people who feel stuck between options. Dana helps you connect character strengths, career interests, values, and life purpose into a practical direction you can act on.",
        "closingLine": "Begin with clarity. Leave with a plan.",
        "imageUrl": "/hero-reference.png",
        "cta": { "label": "Explore the program", "url": "#programs" }
      },
      "programs": [
        { "slug": "career-clarity-program", "title": "Career Clarity Program", "summary": "Assessments, live group sessions, personal reflection, and a career identity download.", "priceLabel": "Cohort pricing", "duration": "5 weeks", "imageUrl": "/hero-reference.png", "cta": { "label": "Join the cohort", "url": "#contact" } },
        { "slug": "one-to-one-coaching", "title": "1:1 Coaching Session", "summary": "A focused coaching session for career path, CV, LinkedIn, or interview preparation.", "priceLabel": "Per session", "duration": "30-60 min", "imageUrl": "/assets/logo-dana-gharaibeh.svg", "cta": { "label": "Reserve time", "url": "#contact" } },
        { "slug": "career-survival-library", "title": "Career Survival Library", "summary": "Recorded lessons for salary negotiation, difficult managers, job search, and daily workplace decisions.", "priceLabel": "Bundle or single course", "duration": "Self-paced", "imageUrl": "/hero-reference.png", "cta": { "label": "View library", "url": "#contact" } }
      ],
      "whyDana": {
        "title": "Why Dana Gharaibeh is the right guide for career clarity?",
        "reasons": [
          { "order": 1, "title": "Assessment-based guidance", "body": "The work is built around strengths, interests, values, and purpose rather than generic advice.", "imageUrl": "/hero-reference.png" },
          { "order": 2, "title": "Practical career outputs", "body": "Clients leave with concrete next steps, CV direction, LinkedIn structure, and interview preparation.", "imageUrl": "/assets/logo-dana-gharaibeh.svg" },
          { "order": 3, "title": "A platform journey", "body": "The experience combines video, live sessions, calendar booking, progress tracking, and downloadable results.", "imageUrl": "/hero-reference.png" }
        ]
      },
      "dashboardModules": [
        { "key": "character-strengths", "title": "Character Strengths", "description": "Assessment upload, recorded explanations, and week-one discussion." },
        { "key": "career-interests", "title": "Career Interests", "description": "In-platform forms that help narrow suitable career paths." },
        { "key": "values", "title": "Values", "description": "Clarify what should guide your next career decision." },
        { "key": "life-purpose", "title": "Life Purpose", "description": "Connect strengths, interests, and values into one practical direction." },
        { "key": "cv-building", "title": "CV Building", "description": "Downloadable templates and guided improvements." },
        { "key": "linkedin", "title": "LinkedIn", "description": "Profile structure and job search session." },
        { "key": "interview-skills", "title": "Interview Skills", "description": "Live practice session and preparation framework." }
      ],
      "about": {
        "title": "Meet Dana",
        "body": "Dana helps professionals translate their experience, strengths, and ambitions into a clear career story and an actionable next step.",
        "highlights": ["Career clarity coaching", "Assessment-based guidance", "Practical CV, LinkedIn, and interview support"]
      },
      "testimonials": [
        { "name": "Client Name", "role": "Career Clarity Participant", "handle": "@client", "quote": "The program helped me understand my options and turn confusion into a clear plan.", "imageUrl": "/assets/logo-dana-gharaibeh.svg" },
        { "name": "Client Name", "role": "1:1 Coaching Client", "handle": "@client", "quote": "Dana guidance made my CV and interview story sharper and more confident.", "imageUrl": "/hero-reference.png" }
      ],
      "leadCapture": {
        "title": "Stay updated on Dana latest programs",
        "subtitle": "Subscribe and we will send you the newest cohorts, sessions, and career resources.",
        "emailPlaceholder": "Email address",
        "submitLabel": "Subscribe",
        "contactTitle": "Let us connect",
        "contactSubtitle": "Reach out any time to ask about cohorts, coaching, or the career survival library."
      }
    }',
    1
),
(
    N'ar',
    N'{
      "hero": {
        "eyebrow": "منصة دانا غرايبة المهنية",
        "title": "مسارك المهني يبدأ عندما تفهم قصتك ونقاط قوتك وخطوتك القادمة بوضوح.",
        "subtitle": "منصة تدريب واستشارات عملية لوضوح المسار، بناء السيرة الذاتية، لينكدإن، المقابلات، ومهارات التعامل مع تحديات العمل.",
        "imageUrl": "/hero-reference.png",
        "videoUrl": "",
        "primaryCta": { "label": "ابدأ رحلتك", "url": "#programs" },
        "secondaryCta": { "label": "احجز جلسة", "url": "#contact" }
      },
      "featuredOffer": {
        "eyebrow": "البرنامج المميز",
        "title": "برنامج وضوح المسار المهني",
        "summary": "رحلة جماعية من خمسة أسابيع تحول التقييمات والتأمل والجلسات الحية إلى هوية مهنية قابلة للتحميل.",
        "imageUrl": "/hero-reference.png",
        "cta": { "label": "احجز مقعدك", "url": "#programs" }
      },
      "masterClass": {
        "title": "ما هو ماستر كلاس وضوح المسار المهني؟",
        "body": "هذه التجربة مصممة لكل شخص يشعر أنه عالق بين خيارات كثيرة. تساعدك دانا على ربط نقاط القوة والاهتمامات المهنية والقيم والغاية باتجاه عملي واضح.",
        "closingLine": "ابدأ بوضوح. واخرج بخطة.",
        "imageUrl": "/hero-reference.png",
        "cta": { "label": "استكشف البرنامج", "url": "#programs" }
      },
      "programs": [
        { "slug": "career-clarity-program", "title": "برنامج وضوح المسار المهني", "summary": "تقييمات وجلسات جماعية مباشرة وتأمل شخصي وملف هوية مهنية قابل للتحميل.", "priceLabel": "تسعير البرنامج", "duration": "5 أسابيع", "imageUrl": "/hero-reference.png", "cta": { "label": "انضم للبرنامج", "url": "#contact" } },
        { "slug": "one-to-one-coaching", "title": "جلسة استشارية فردية", "summary": "جلسة مركزة للمسار المهني أو السيرة الذاتية أو لينكدإن أو التحضير للمقابلات.", "priceLabel": "حسب الجلسة", "duration": "30-60 دقيقة", "imageUrl": "/assets/logo-dana-gharaibeh.svg", "cta": { "label": "احجز موعد", "url": "#contact" } },
        { "slug": "career-survival-library", "title": "مكتبة النجاة المهنية", "summary": "دروس مسجلة عن التفاوض على الراتب والتعامل مع المدراء الصعبين والبحث عن وظيفة.", "priceLabel": "باقة أو كورس منفرد", "duration": "تعلم ذاتي", "imageUrl": "/hero-reference.png", "cta": { "label": "شاهد المكتبة", "url": "#contact" } }
      ],
      "whyDana": {
        "title": "لماذا دانا غرايبة هي الدليل المناسب لوضوحك المهني؟",
        "reasons": [
          { "order": 1, "title": "توجيه مبني على التقييمات", "body": "الرحلة تعتمد على نقاط القوة والاهتمامات والقيم والغاية، وليس على نصائح عامة.", "imageUrl": "/hero-reference.png" },
          { "order": 2, "title": "مخرجات عملية للمسار", "body": "يخرج العميل بخطوات واضحة واتجاه للسيرة الذاتية وهيكلة لينكدإن وتحضير للمقابلات.", "imageUrl": "/assets/logo-dana-gharaibeh.svg" },
          { "order": 3, "title": "رحلة منصة متكاملة", "body": "التجربة تجمع الفيديو والجلسات الحية والحجز والتقدم والنتائج القابلة للتحميل.", "imageUrl": "/hero-reference.png" }
        ]
      },
      "dashboardModules": [
        { "key": "character-strengths", "title": "نقاط القوة الشخصية", "description": "رفع نتائج التقييم وفيديوهات شرح ونقاش نهاية الأسبوع الأول." },
        { "key": "career-interests", "title": "الاهتمامات المهنية", "description": "نماذج داخل المنصة تساعد على تضييق خيارات المسار." },
        { "key": "values", "title": "القيم", "description": "توضيح ما يجب أن يقود قرارك المهني القادم." },
        { "key": "life-purpose", "title": "الغاية", "description": "ربط نقاط القوة والاهتمامات والقيم باتجاه عملي واضح." },
        { "key": "cv-building", "title": "بناء السيرة الذاتية", "description": "قوالب قابلة للتحميل وتحسينات موجهة." },
        { "key": "linkedin", "title": "لينكدإن", "description": "هيكلة الملف الشخصي وجلسة البحث عن وظيفة." },
        { "key": "interview-skills", "title": "مهارات المقابلات", "description": "جلسة تدريب مباشرة وإطار للتحضير." }
      ],
      "about": {
        "title": "عن دانا",
        "body": "تساعد دانا المهنيين على تحويل خبراتهم ونقاط قوتهم وطموحاتهم إلى قصة مهنية واضحة وخطوة عملية تالية.",
        "highlights": ["استشارات وضوح المسار", "توجيه مبني على التقييمات", "دعم عملي للسيرة الذاتية ولينكدإن والمقابلات"]
      },
      "testimonials": [
        { "name": "اسم العميل", "role": "مشارك في برنامج وضوح المسار", "handle": "@client", "quote": "ساعدني البرنامج على فهم خياراتي وتحويل التشتت إلى خطة واضحة.", "imageUrl": "/assets/logo-dana-gharaibeh.svg" },
        { "name": "اسم العميل", "role": "عميل جلسة فردية", "handle": "@client", "quote": "توجيه دانا جعل سيرتي وقصتي في المقابلات أوضح وأكثر ثقة.", "imageUrl": "/hero-reference.png" }
      ],
      "leadCapture": {
        "title": "ابق على اطلاع على آخر برامج دانا",
        "subtitle": "اشترك ليصلك جديد البرامج والجلسات والموارد المهنية.",
        "emailPlaceholder": "البريد الإلكتروني",
        "submitLabel": "اشترك",
        "contactTitle": "لنتواصل",
        "contactSubtitle": "تواصل في أي وقت للسؤال عن البرامج أو الجلسات أو مكتبة النجاة المهنية."
      }
    }',
    1
);
