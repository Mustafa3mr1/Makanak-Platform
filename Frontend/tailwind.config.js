/** @type {import('tailwindcss').Config} */
export default {
  content: ['./index.html', './src/**/*.{js,ts,jsx,tsx}'],
  theme: {
    extend: {
      colors: {
        primary: {
          DEFAULT: '#3B82F6',
          dark: '#2563EB',
          light: '#EFF6FF',
          soft: '#DBEAFE',
        },
        secondary: {
          DEFAULT: '#64748B',
          light: '#F8FAFC',
        },
        accent: {
          DEFAULT: '#06B6D4',
          dark: '#0891B2',
          light: '#ECFEFF',
        },
        success: {
          DEFAULT: '#22C55E',
          soft: '#DCFCE7',
          text: '#15803D',
        },
        warning: {
          DEFAULT: '#F59E0B',
          soft: '#FEF3C7',
          text: '#B45309',
        },
        error: {
          DEFAULT: '#EF4444',
          soft: '#FFE4E6',
          text: '#BE123C',
        },
        background: '#F1F5FB',
        surface: '#FFFFFF',
        text: {
          primary: '#0F172A',
          secondary: '#64748B',
          muted: '#94A3B8',
        },
        line: '#E2E8F0',
      },
      fontFamily: {
        sans: ['Plus Jakarta Sans', 'Cairo', 'system-ui', 'sans-serif'],
        cairo: ['Cairo', 'Plus Jakarta Sans', 'system-ui', 'sans-serif'],
      },
      borderRadius: {
        card: '14px',
      },
      boxShadow: {
        soft: '0 4px 18px rgba(15, 23, 42, 0.06)',
        hover: '0 10px 25px rgba(15, 23, 42, 0.09)',
      },
    },
  },
  plugins: [],
};
