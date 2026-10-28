import { createTheme } from '@mui/material/styles';

export function createAppTheme(direction, cmsTheme = {}) {
  return createTheme({
    direction,
    palette: {
      mode: 'light',
      primary: {
        main: cmsTheme.primary ?? '#2f4f23',
        contrastText: '#fffdf2',
      },
      secondary: {
        main: cmsTheme.secondary ?? '#b8cf7c',
        contrastText: '#182114',
      },
      background: {
        default: cmsTheme.background ?? '#fffdf6',
        paper: cmsTheme.paper ?? '#fffdf2',
      },
      text: {
        primary: cmsTheme.textPrimary ?? '#152013',
        secondary: cmsTheme.textSecondary ?? '#4d5947',
      },
      brand: {
        navy: cmsTheme.navy ?? '#20295b',
        forest: cmsTheme.forest ?? '#2f4f23',
        sage: cmsTheme.secondary ?? '#b8cf7c',
        cream: cmsTheme.cream ?? '#f4f1d9',
        gold: cmsTheme.gold ?? '#d8bd74',
      },
    },
    shape: {
      borderRadius: cmsTheme.borderRadius ?? 16,
    },
    typography: {
      fontFamily:
        direction === 'rtl'
          ? '"Segoe UI", Tahoma, Arial, sans-serif'
          : 'Inter, "Segoe UI", Arial, sans-serif',
      h1: {
        fontSize: 'clamp(3rem, 6.2vw, 6.5rem)',
        lineHeight: 0.98,
        fontWeight: 900,
        letterSpacing: '-0.045em',
      },
      h2: {
        fontSize: 'clamp(2rem, 4vw, 3.6rem)',
        lineHeight: 1.15,
        fontWeight: 850,
        letterSpacing: '-0.035em',
      },
      h5: {
        fontWeight: 500,
        letterSpacing: 0,
      },
      button: {
        fontWeight: 800,
        textTransform: 'none',
      },
    },
    components: {
      MuiButton: {
        styleOverrides: {
          root: {
            borderRadius: 12,
            minHeight: 48,
            paddingInline: 22,
            transition: 'transform .25s ease, box-shadow .25s ease, background-color .25s ease',
            '&:hover': {
              transform: 'translateY(-2px)',
            },
          },
        },
      },
      MuiCard: {
        styleOverrides: {
          root: {
            borderRadius: 20,
            border: '1px solid rgba(47, 79, 35, .16)',
            boxShadow: '0 16px 50px rgba(30, 45, 26, .08)',
          },
        },
      },
      MuiContainer: {
        defaultProps: {
          maxWidth: 'lg',
        },
      },
    },
  });
}
