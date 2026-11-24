export const formatEgp = (amount?: number | null, locale = 'en'): string => {
  if (amount == null || isNaN(amount)) return '0 EGP';
  const formatted = new Intl.NumberFormat(locale === 'ar' ? 'ar-EG' : 'en-EG', {
    style: 'decimal',
    minimumFractionDigits: 0,
    maximumFractionDigits: 2,
  }).format(amount);

  return locale === 'ar' ? `${formatted} ج.م` : `${formatted} EGP`;
};

export const formatArea = (areaSqm?: number | null, unit = 'm²'): string => {
  if (areaSqm == null || isNaN(areaSqm)) return `0 ${unit}`;
  return `${areaSqm.toLocaleString()} ${unit}`;
};

export const formatDate = (dateString?: string | null, locale = 'en'): string => {
  if (!dateString) return '';
  const date = new Date(dateString);
  if (isNaN(date.getTime())) return '';
  return new Intl.DateTimeFormat(locale === 'ar' ? 'ar-EG' : 'en-GB', {
    day: 'numeric',
    month: 'short',
    year: 'numeric',
  }).format(date);
};

export const formatDateTime = (dateString?: string | null, locale = 'en'): string => {
  if (!dateString) return '';
  const date = new Date(dateString);
  if (isNaN(date.getTime())) return '';
  return new Intl.DateTimeFormat(locale === 'ar' ? 'ar-EG' : 'en-GB', {
    day: 'numeric',
    month: 'short',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  }).format(date);
};
