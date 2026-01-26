import { MatBadge } from '@angular/material/badge';
import { MatButton } from '@angular/material/button';
import { MatIcon } from '@angular/material/icon';
import { MatCard } from '@angular/material/card';
import { MatCardActions, MatCardContent } from '@angular/material/card';
import { MatDivider } from '@angular/material/divider';
import { MatSelectionList, MatListOption } from '@angular/material/list';
import { MatDialogModule } from '@angular/material/dialog';
import { MatMenuModule } from '@angular/material/menu';
import { MatPaginator } from '@angular/material/paginator';

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
    MatListOption,
    MatMenuModule,
    MatPaginator
];
export { MATERIAL_IMPORTS };
