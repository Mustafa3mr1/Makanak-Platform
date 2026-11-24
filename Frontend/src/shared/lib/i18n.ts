import i18n from 'i18next';
import { initReactI18next } from 'react-i18next';
import LanguageDetector from 'i18next-browser-languagedetector';

import enCommon from '../../../public/locales/en/common.json';
import arCommon from '../../../public/locales/ar/common.json';

i18n
  .use(LanguageDetector)
  .use(initReactI18next)
  .init({
    fallbackLng: 'en',
    supportedLngs: ['en', 'ar'],
    debug: false,
    interpolation: {
      escapeValue: false,
    },
    resources: {
      en: {
        common: enCommon,
      },
      ar: {
        common: arCommon,
      },
    },
    defaultNS: 'common',
    detection: {
      order: ['localStorage', 'navigator'],
      caches: ['localStorage'],
    },
  });

// Synchronize document dir and lang attributes on change
const applyDocumentDirection = (lng: string) => {
  const dir = lng === 'ar' ? 'rtl' : 'ltr';
  document.documentElement.dir = dir;
  document.documentElement.lang = lng;
};

applyDocumentDirection(i18n.language || 'en');

i18n.on('languageChanged', (lng) => {
  applyDocumentDirection(lng);
});

export default i18n;
