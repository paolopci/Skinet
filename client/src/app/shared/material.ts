import { MatBadge } from '@angular/material/badge';
import { MatButton } from '@angular/material/button';
import { MatIcon } from '@angular/material/icon';
import { MatCard } from '@angular/material/card';
import { MatCardActions, MatCardContent } from '@angular/material/card';
import { MatDivider } from '@angular/material/divider';
import { MatSelectionList, MatListOption } from '@angular/material/list';
import { MatDialogModule } from '@angular/material/dialog';

// prettier-ignore
const MATERIAL_IMPORTS = [
    MatBadge, 
    MatButton, 
    MatIcon, 
    MatCard, 
    MatCardContent, 
    MatCardActions, 
    MatDialogModule,
    MatDivider,
    MatSelectionList,
    MatListOption
];
export { MATERIAL_IMPORTS };
