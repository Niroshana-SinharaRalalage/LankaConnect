import type { Config } from 'tailwindcss'

const config: Config = {
  darkMode: 'class',
  content: [
    './src/pages/**/*.{js,ts,jsx,tsx,mdx}',
    './src/components/**/*.{js,ts,jsx,tsx,mdx}',
    './src/app/**/*.{js,ts,jsx,tsx,mdx}',
    './src/presentation/**/*.{js,ts,jsx,tsx,mdx}',
  ],
  theme: {
    extend: {
      colors: {
        // Sri Lankan Flag Colors
        saffron: {
          DEFAULT: '#FF7900',
          50: '#FFE8D6',
          100: '#FFDDC2',
          200: '#FFC799',
          300: '#FFB170',
          400: '#FF9B47',
          500: '#FF7900',
          600: '#CC6100',
          700: '#994900',
          800: '#663000',
          900: '#331800',
        },
        maroon: {
          DEFAULT: '#8B1538',
          50: '#F5D9E1',
          100: '#EFC3D1',
          200: '#E39CB1',
          300: '#D77591',
          400: '#CB4E71',
          500: '#8B1538',
          600: '#70112D',
          700: '#550D22',
          800: '#3A0917',
          900: '#1F050C',
        },
        lankaGreen: {
          DEFAULT: '#006400',
          50: '#C9F0C9',
          100: '#B3EBB3',
          200: '#87E187',
          300: '#5CD75C',
          400: '#30CD30',
          500: '#006400',
          600: '#005000',
          700: '#003C00',
          800: '#002800',
          900: '#001400',
        },
        gold: {
          DEFAULT: '#FFD700',
          50: '#FFFAEB',
          100: '#FFF5D6',
          200: '#FFEBAD',
          300: '#FFE185',
          400: '#FFD75C',
          500: '#FFD700',
          600: '#CCAC00',
          700: '#998100',
          800: '#665600',
          900: '#332B00',
        },
        border: 'hsl(var(--border))',
        input: 'hsl(var(--input))',
        ring: 'hsl(var(--ring))',
        background: 'hsl(var(--background))',
        foreground: 'hsl(var(--foreground))',
        primary: {
          DEFAULT: 'hsl(var(--primary))',
          foreground: 'hsl(var(--primary-foreground))',
        },
        secondary: {
          DEFAULT: 'hsl(var(--secondary))',
          foreground: 'hsl(var(--secondary-foreground))',
        },
        destructive: {
          DEFAULT: 'hsl(var(--destructive))',
          foreground: 'hsl(var(--destructive-foreground))',
        },
        muted: {
          DEFAULT: 'hsl(var(--muted))',
          foreground: 'hsl(var(--muted-foreground))',
        },
        accent: {
          DEFAULT: 'hsl(var(--accent))',
          foreground: 'hsl(var(--accent-foreground))',
        },
        popover: {
          DEFAULT: 'hsl(var(--popover))',
          foreground: 'hsl(var(--popover-foreground))',
        },
        card: {
          DEFAULT: 'hsl(var(--card))',
          foreground: 'hsl(var(--card-foreground))',
        },
      },
      borderRadius: {
        lg: 'var(--radius)',
        md: 'calc(var(--radius) - 2px)',
        sm: 'calc(var(--radius) - 4px)',
      },
      fontFamily: {
        sans: ['var(--font-sans)', 'system-ui', 'sans-serif'],
      },
      backgroundImage: {
        'gradient-sri-lanka': 'linear-gradient(135deg, #FF7900 0%, #8B1538 50%, #006400 100%)',
      },
    },
  },
  plugins: [require('tailwindcss-animate')],
}

export default config
