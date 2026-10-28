import { useEffect, useState } from 'react';
import { getLandingContent } from './landingApi';

export function useLandingContent(language) {
  const [state, setState] = useState({
    content: null,
    isLoading: true,
    error: null,
  });

  useEffect(() => {
    let isMounted = true;

    setState((current) => ({ ...current, isLoading: true, error: null }));

    getLandingContent(language)
      .then((content) => {
        if (isMounted) {
          setState({ content, isLoading: false, error: null });
        }
      })
      .catch((error) => {
        if (isMounted) {
          setState({
            content: null,
            isLoading: false,
            error: error instanceof Error ? error.message : 'Unexpected error',
          });
        }
      });

    return () => {
      isMounted = false;
    };
  }, [language]);

  return state;
}
