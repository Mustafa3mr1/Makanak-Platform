import { useTranslation } from 'react-i18next';

export const useRtl = () => {
  const { i18n } = useTranslation();
  const isRtl = i18n.language === 'ar';

  const toggleLanguage = () => {
    const nextLang = isRtl ? 'en' : 'ar';
    i18n.changeLanguage(nextLang);
  };

  const setLanguage = (lang: 'en' | 'ar') => {
    i18n.changeLanguage(lang);
  };

  return {
    isRtl,
    currentLanguage: i18n.language,
    toggleLanguage,
    setLanguage,
  };
};
