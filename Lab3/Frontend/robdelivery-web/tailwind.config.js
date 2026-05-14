export default {
  content: [
    "./index.html",
    "./src/**/*.{js,ts,jsx,tsx}",
  ],
  darkMode: "class",
  theme: {
    extend: {
      colors: {
        "primary-container": "#2f3132",
        "tertiary-fixed-dim": "#ffb783",
        "tertiary-container": "#502500",
        "surface-tint": "#5d5f60",
        "on-surface-variant": "#444749",
        "primary-fixed": "#e2e2e3",
        "on-error-container": "#93000a",
        "inverse-surface": "#31312c",
        "inverse-on-surface": "#f3f0e9",
        "on-secondary": "#ffffff",
        "outline-variant": "#c4c7c8",
        "on-primary-fixed": "#1a1c1d",
        "on-surface": "#1c1c18",
        "error": "#ba1a1a",
        "on-error": "#ffffff",
        "on-primary-fixed-variant": "#454748",
        "on-primary-container": "#98999a",
        "on-background": "#1c1c18",
        "primary-fixed-dim": "#c6c6c7",
        "secondary-fixed": "#d4e3ff",
        "tertiary": "#301400",
        "secondary-fixed-dim": "#a4c9ff",
        "surface-dim": "#dcdad3",
        "surface-container": "#f1eee7",
        "on-secondary-container": "#003e73",
        "secondary": "#0060ac",
        "surface-variant": "#e5e2db",
        "surface-container-high": "#ebe8e1",
        "on-tertiary": "#ffffff",
        "surface-container-lowest": "#ffffff",
        "primary": "#1a1c1d",
        "secondary-container": "#68abff",
        "surface-container-highest": "#e5e2db",
        "on-secondary-fixed": "#001c39",
        "surface-container-low": "#f6f3ec",
        "on-primary": "#ffffff",
        "outline": "#747779",
        "background": "#fcf9f2",
        "error-container": "#ffdad6",
        "surface": "#fcf9f2",
        "on-secondary-fixed-variant": "#004883",
        "inverse-primary": "#c6c6c7",
        "on-tertiary-container": "#e57d21",
        "on-tertiary-fixed": "#301400",
        "surface-bright": "#fcf9f2",
        "on-tertiary-fixed-variant": "#713700",
        "tertiary-fixed": "#ffdcc5"
      },
      borderRadius: {
        "DEFAULT": "0.125rem",
        "lg": "0.25rem",
        "xl": "0.5rem",
        "full": "0.75rem"
      },
      spacing: {
        "margin": "32px",
        "container-max": "1200px",
        "unit": "8px",
        "gutter": "24px"
      },
      fontFamily: {
        "headline-md": ["Be Vietnam Pro", "sans-serif"],
        "body-md": ["Be Vietnam Pro", "sans-serif"],
        "body-lg": ["Be Vietnam Pro", "sans-serif"],
        "headline-lg": ["Be Vietnam Pro", "sans-serif"],
        "label-md": ["Lexend", "sans-serif"]
      },
      fontSize: {
        "headline-md": ["28px", { "lineHeight": "1.3", "fontWeight": "600" }],
        "body-md": ["16px", { "lineHeight": "1.5", "fontWeight": "400" }],
        "body-lg": ["18px", { "lineHeight": "1.6", "fontWeight": "400" }],
        "headline-lg": ["40px", { "lineHeight": "1.2", "letterSpacing": "-0.02em", "fontWeight": "700" }],
        "label-md": ["14px", { "lineHeight": "1.2", "letterSpacing": "0.05em", "fontWeight": "500" }]
      }
    }
  },
  plugins: [
    require('@tailwindcss/forms'),
    require('@tailwindcss/container-queries'),
  ],
}
