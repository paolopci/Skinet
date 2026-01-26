import { Component, inject, viewChild } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { MatSelectionList } from '@angular/material/list';
import { MATERIAL_IMPORTS } from '../../../shared/material';

export interface FiltersDialogData {
  brands: string[];
  types: string[];
  selectedBrands: string[];
  selectedTypes: string[];
}

@Component({
  selector: 'app-filters-dialog',
  imports: [...MATERIAL_IMPORTS],
  standalone: true,
  templateUrl: './filters-dialog.component.html',
  styleUrl: './filters-dialog.component.scss',
})
export class FiltersDialogComponent {
  data = inject<FiltersDialogData>(MAT_DIALOG_DATA);
  private dialogRef = inject(MatDialogRef<FiltersDialogComponent>);
  private brandsList = viewChild.required<MatSelectionList>('brandsList');
  private typesList = viewChild.required<MatSelectionList>('typesList');

  applyFilters() {
    const selectedBrands = this.brandsList().selectedOptions.selected.map(
      (option) => option.value as string,
    );
    const selectedTypes = this.typesList().selectedOptions.selected.map(
      (option) => option.value as string,
    );

    this.dialogRef.close({ selectedBrands, selectedTypes });
  }
}
