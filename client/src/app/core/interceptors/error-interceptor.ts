import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';
import { SnackbarService } from '../services/snackbar.service';
import { extractValidationErrorMap } from '../../shared/utils/api-error';

export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const router = inject(Router);
  const snackbar = inject(SnackbarService);

  return next(req).pipe(
    catchError((err: HttpErrorResponse) => {
      const message = getErrorMessage(err);

      switch (err.status) {
        case 400:
          if (!extractValidationErrorMap(err.error)) {
            snackbar.showWarning(message ?? 'Richiesta non valida.');
          }
          break;
        case 401:
          snackbar.showWarning(message ?? 'Non autorizzato. Effettua il login.');
          if (!isAuthRoute(router.url)) {
            router.navigateByUrl(`/login?returnUrl=${encodeURIComponent(router.url)}`);
          }
          break;
        case 403:
          snackbar.showWarning(message ?? 'Accesso negato.');
          break;
        case 404:
          snackbar.showWarning(message ?? 'Risorsa non trovata.');
          router.navigateByUrl('/not-found');
          break;
        case 500:
          router.navigateByUrl('/server-error', { state: { error: mapServerError(err) } });
          break;
        case 503:
        case 505:
          router.navigateByUrl('/server-error', { state: { error: mapServerError(err) } });
          break;
        case 550:
          snackbar.showError(message ?? 'Errore imprevisto.');
          break;
        default:
          snackbar.showError('Si è verificato un errore.');
          break;
      }

      return throwError(() => err);
    }),
  );
};

const getErrorMessage = (error: HttpErrorResponse): string | null => {
  const payload = error.error;

  if (typeof payload === 'string' && payload.trim().length > 0) {
    return payload;
  }

  if (payload && typeof payload === 'object') {
    const title = (payload as { title?: unknown }).title;
    if (typeof title === 'string' && title.trim().length > 0) {
      return title;
    }
  }

  if (typeof error.message === 'string' && error.message.trim().length > 0) {
    return error.message;
  }

  return null;
};

const mapServerError = (error: HttpErrorResponse): Record<string, unknown> => {
  const payload = error.error;
  const mapped: Record<string, unknown> = {
    status: error.status,
    statusText: error.statusText,
    message: error.message,
  };

  if (typeof payload === 'string') {
    mapped['detail'] = payload;
    return mapped;
  }

  if (payload && typeof payload === 'object') {
    return { ...mapped, ...(payload as Record<string, unknown>) };
  }

  return mapped;
};

const isAuthRoute = (url: string): boolean => {
  const normalized = url.split('?')[0]?.toLowerCase() ?? '';
  return normalized === '/login' || normalized === '/register';
};
