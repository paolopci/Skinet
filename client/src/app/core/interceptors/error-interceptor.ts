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
          break;
        case 403:
          snackbar.showWarning(message ?? 'Accesso negato.');
          break;
        case 404:
          snackbar.showWarning(message ?? 'Risorsa non trovata.');
          router.navigateByUrl('/not-found');
          break;
        case 500:
          router.navigateByUrl('/server');
          break;
        case 503:
        case 505:
          router.navigateByUrl('/server');
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
