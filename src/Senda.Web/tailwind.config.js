/** @type {import('tailwindcss').Config} */
export default {
  content: [
    "./index.html",
    "./src/**/*.{vue,js,ts,jsx,tsx}",
  ],
  theme: {
    extend: {
      colors: {
        prime: 'var(--p-primary-color)',
        'prime-contrast': 'var(--p-primary-contrast-color)',
      }
    },
  },
  plugins: [require('tailwindcss-primeui')],
}
