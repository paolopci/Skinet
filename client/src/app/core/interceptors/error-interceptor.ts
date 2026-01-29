import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';

export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const router = inject(Router);

  return next(req).pipe(
    catchError((err: HttpErrorResponse) => {
      if (err.status === 400) {
        alert(err.error.title || err.error);
      }
      if (err.status === 401) {
        alert(err.error.title || err.error);
      }
      if (err.status === 404) {
        router.navigateByUrl('/not-found');
      }
      if (err.status === 403) {
        alert(err.error.title || err.error);
      }
      if (err.status === 503) {
        router.navigateByUrl('/server');
      }
      if (err.status === 550) {
        alert(err.error.title || err.error);
      }
      if (err.status === 505) {
        router.navigateByUrl('/server');
      }
      if (err.status === 302) {
        alert(err.error.title || err.error);
      }
      if (err.status === 500) {
        router.navigateByUrl('/server');
      }

      return throwError(() => err);  
    }),
  );
};
