import { HttpInterceptorFn, HttpRequest } from '@angular/common/http';
import { inject } from '@angular/core';
import { delay, finalize } from 'rxjs';
import { LoadingService } from '../services/loading.service';

const LOADING_SKIP_HEADER = 'X-Skip-Loading';

export const loadingInterceptor: HttpInterceptorFn = (req, next) => {
  const loading = inject(LoadingService);

  if (shouldSkip(req)) {
    const cleaned = req.clone({
      headers: req.headers.delete(LOADING_SKIP_HEADER),
    });
    return next(cleaned);
  }

  loading.show();

  return next(req).pipe(
    // delay(500),
    finalize(() => {
      loading.hide();
    }),
  );
};

const shouldSkip = (req: HttpRequest<unknown>): boolean => {
  return req.headers.has(LOADING_SKIP_HEADER);
};
