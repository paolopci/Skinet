import { MatBadge } from '@angular/material/badge';
import { MatButton } from '@angular/material/button';
import { MatIcon } from '@angular/material/icon';
import { MatCard, MatCardTitle } from '@angular/material/card';
import { MatCardActions, MatCardContent } from '@angular/material/card';
import { MatDivider } from '@angular/material/divider';
import { MatSelectionList, MatListOption } from '@angular/material/list';
import { MatDialogModule } from '@angular/material/dialog';
import { MatMenuModule } from '@angular/material/menu';
import { MatPaginator } from '@angular/material/paginator';
import { MatFormField, MatLabel, MatError, MatSuffix } from '@angular/material/form-field';
import { MatInput } from '@angular/material/input';

// prettier-ignore
const MATERIAL_IMPORTS = [
    MatBadge, 
    MatButton, 
    MatIcon, 
    MatInput,
    MatCard, 
    MatCardContent, 
    MatCardActions, 
    MatDialogModule,
    MatDivider,
    MatSelectionList,
    MatListOption,
    MatMenuModule,
    MatPaginator,
    MatFormField,
    MatLabel,
    MatError,
    MatSuffix,
    MatCardTitle
];
const SNACKBAR_PANEL_CLASSES = {
    error: 'snackbar-error',
    warning: 'snackbar-warning',
    info: 'snackbar-info',
} as const;

export { MATERIAL_IMPORTS, SNACKBAR_PANEL_CLASSES };
