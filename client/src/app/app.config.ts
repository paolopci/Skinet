import { ApplicationConfig, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideAnimations } from '@angular/platform-browser/animations';

import { routes } from './app.routes';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { errorInterceptor } from './core/interceptors/error-interceptor';
import { MAT_SNACK_BAR_DEFAULT_OPTIONS } from '@angular/material/snack-bar';
import { importProvidersFrom } from '@angular/core';
import { MatSnackBarModule } from '@angular/material/snack-bar';

export const appConfig: ApplicationConfig = {
  // prettier-ignore
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(routes),
    provideHttpClient(withInterceptors([errorInterceptor])),
    provideAnimations(),
    importProvidersFrom(MatSnackBarModule),
    {
      provide: MAT_SNACK_BAR_DEFAULT_OPTIONS,
      useValue: {
        duration: 4000,
        horizontalPosition: 'end',
        verticalPosition: 'bottom',
      },
    },
  ],
};
