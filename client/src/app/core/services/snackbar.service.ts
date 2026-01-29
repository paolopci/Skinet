import { Injectable } from '@angular/core';
import { MatSnackBar, MatSnackBarConfig } from '@angular/material/snack-bar';
import { SNACKBAR_PANEL_CLASSES } from '../../shared/material';

type SnackbarLevel = 'info' | 'warning' | 'error';

@Injectable({ providedIn: 'root' })
export class SnackbarService {
    constructor(private readonly snackBar: MatSnackBar) {}

    showError(message: string, action?: string, config?: MatSnackBarConfig): void {
        this.open(message, action, 'error', config);
    }

    showWarning(message: string, action?: string, config?: MatSnackBarConfig): void {
        this.open(message, action, 'warning', config);
    }

    showInfo(message: string, action?: string, config?: MatSnackBarConfig): void {
        this.open(message, action, 'info', config);
    }

    private open(
        message: string,
        action: string | undefined,
        level: SnackbarLevel,
        config?: MatSnackBarConfig,
    ): void {
        this.snackBar.open(message, action, {
            ...config,
            panelClass: this.mergePanelClass(config?.panelClass, SNACKBAR_PANEL_CLASSES[level]),
        });
    }

    private mergePanelClass(
        existing: MatSnackBarConfig['panelClass'],
        addClass: string,
    ): MatSnackBarConfig['panelClass'] {
        if (!existing) {
            return addClass;
        }
        return Array.isArray(existing) ? [...existing, addClass] : [existing, addClass];
    }
}
