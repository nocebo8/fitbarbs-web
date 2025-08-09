/** @type {import('tailwindcss').Config} */
module.exports = {
  content: [
    './Views/**/*.cshtml',
    './wwwroot/js/**/*.js',
  ],
  theme: {
    container: { center: true, screens: { xl: '1280px' } },
    extend: {
      colors: {
        brand: {
          DEFAULT: '#EC4899',
          soft: '#FCE7F3',
        },
        mint: '#2EC4B6',
        surface: '#FAFAFA',
        bg: '#FFFFFF',
        fb: {
          text: '#0A0A0A',
          muted: '#52525B',
          border: '#E5E7EB',
          warning: '#F59E0B',
          error: '#EF4444',
          success: '#10B981',
          info: '#3B82F6',
        },
      },
      borderRadius: {
        xl2: '1rem',
      },
      boxShadow: {
        soft: '0 10px 30px rgba(0,0,0,.08)'
      },
    },
  },
  plugins: [],
};


