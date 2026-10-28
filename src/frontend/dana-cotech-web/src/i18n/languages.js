export const languages = ['en', 'ar'];

export function isSupportedLanguage(language) {
  return languages.includes(language);
}

export function getDirection(language) {
  return language === 'ar' ? 'rtl' : 'ltr';
}

export function oppositeLanguage(language) {
  return language === 'ar' ? 'en' : 'ar';
}
