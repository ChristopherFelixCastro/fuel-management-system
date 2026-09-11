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
          dark: '#062d4f',
          primary: '#087e8b',
          accent: '#0f9aa8',
          light: '#e8f4f5',
          surface: '#eef4f7',
        }
      }
    },
  },
  plugins: [],
}
