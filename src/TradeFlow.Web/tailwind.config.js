/** @type {import('tailwindcss').Config} */
module.exports = {
  prefix: 'tw-',
  corePlugins: {
    preflight: false, // PRESERVE BOOTSTRAP & TF-* STYLES: Do not reset HTML base elements
  },
  content: [
    './Components/**/*.{razor,html}',
    './Pages/**/*.{razor,html}',
    './Shared/**/*.{razor,html}',
    './wwwroot/**/*.js'
  ],
  theme: {
    extend: {
      colors: {
        'tf-primary': '#4338ca',
        'tf-primary-hover': '#3730a3',
        'tf-primary-light': '#e0e7ff',
        'tf-primary-dark': '#312e81',
      }
    },
  },
  plugins: [],
}
