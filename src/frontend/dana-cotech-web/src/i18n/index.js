import i18n from 'i18next';
import { initReactI18next } from 'react-i18next';

i18n.use(initReactI18next).init({
  lng: localStorage.getItem('language') ?? 'en',
  fallbackLng: 'en',
  interpolation: {
    escapeValue: false,
  },
  resources: {
    en: {
      translation: {
        actions: {
          login: 'Log in',
          switchLanguage: 'Switch language',
        },
        nav: {
          programs: 'Programs',
          experience: 'The experience',
          about: 'About Dana',
        },
        errors: {
          empty: 'No content is available right now.',
        },
        landing: {
          programsEyebrow: 'Dynamic offers',
          programsTitle: 'Choose between three career experiences',
          whyEyebrow: 'Why Dana',
          dashboardEyebrow: 'Platform dashboard',
          dashboardTitle: 'Everything the client tracks in one place',
          testimonialsEyebrow: 'Client stories',
          testimonialsTitle: 'What clients say after the journey',
          personal: 'Personal guidance',
          growth: 'Whole-person growth',
          paths: 'Career paths',
          nextChapter: 'Your next chapter',
          startsHere: 'Starts right here',
          stayConnected: 'Stay connected',
          rights: 'All rights reserved.',
        },
      },
    },
    ar: {
      translation: {
        actions: {
          login: 'تسجيل الدخول',
          switchLanguage: 'تغيير اللغة',
        },
        nav: {
          programs: 'البرامج',
          experience: 'التجربة',
          about: 'عن دانا',
        },
        errors: {
          empty: 'لا يوجد محتوى متاح حاليًا.',
        },
        landing: {
          programsEyebrow: 'العروض والبرامج',
          programsTitle: 'اختر بين ثلاث تجارب مهنية',
          whyEyebrow: 'لماذا دانا',
          dashboardEyebrow: 'لوحة المنصة',
          dashboardTitle: 'كل ما يتابعه العميل في مكان واحد',
          testimonialsEyebrow: 'قصص العملاء',
          testimonialsTitle: 'ماذا يقول العملاء بعد التجربة',
          personal: 'توجيه شخصي',
          growth: 'تطور متكامل',
          paths: 'مسارات مهنية',
          nextChapter: 'فصلك القادم',
          startsHere: 'يبدأ من هنا',
          stayConnected: 'ابقَ على تواصل',
          rights: 'جميع الحقوق محفوظة.',
        },
      },
    },
  },
});

i18n.on('languageChanged', (language) => {
  localStorage.setItem('language', language);
  document.documentElement.lang = language;
  document.documentElement.dir = language === 'ar' ? 'rtl' : 'ltr';
});

document.documentElement.lang = i18n.language;
document.documentElement.dir = i18n.language === 'ar' ? 'rtl' : 'ltr';

export default i18n;
