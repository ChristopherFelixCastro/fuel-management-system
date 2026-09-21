/** @type {import('tailwindcss').Config} */
export default {
  content: [
    "./index.html",
    "./src/**/*.{js,ts,jsx,tsx}",
  ],
  theme: {
    extend: {
      colors: {
        brand: {
          dark: '#0f172a',
          primary: '#087e8b',
          accent: '#f03030',
          light: '#f8fafc',
          surface: '#f1f5f9',
        }
      }
    },
  },
  plugins: [],
}
