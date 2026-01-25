import { Component, inject, signal } from '@angular/core';
import { ShopService } from '../../core/services/shop.service';
import { Product } from '../../shared/models/product';
import { ProductItemComponent } from './product-item/product-item.component';
import { MatDialog } from '@angular/material/dialog';
import { MATERIAL_IMPORTS } from '../../shared/material';
import { MatSelectionListChange } from '@angular/material/list';
import { ShopParams } from '../../shared/models/shop-params';
import {
  FiltersDialogComponent,
  FiltersDialogData,
} from './filters-dialog/filters-dialog.component';

@Component({
  selector: 'app-shop',
  imports: [ProductItemComponent, ...MATERIAL_IMPORTS],
  standalone: true,
  templateUrl: './shop.component.html',
  styleUrl: './shop.component.scss',
})
export class ShopComponent {
  private shopService = inject(ShopService);
  private dialogService = inject(MatDialog);
  products = signal<Product[]>([]);
  shopParams = new ShopParams();

  selectedBrands: string[] = [];
  selectedTypes: string[] = [];
  selectedSort = this.shopParams.sort;
  sortOptions = [
    { name: 'Nome A-Z', value: 'name' },
    { name: 'Prezzo \u2191', value: 'priceAsc' },
    { name: 'Prezzo \u2193', value: 'priceDesc' },
  ];

  get selectedSortLabel() {
    return this.sortOptions.find((option) => option.value === this.selectedSort)?.name ?? 'Sort';
  }

  get selectedSortIcon() {
    if (this.selectedSort === 'name') return 'sort_by_alpha';
    if (this.selectedSort === 'priceAsc') return 'arrow_upward';
    return 'arrow_downward';
  }

  ngOnInit(): void {
    this.initializeShop();
  }

  initializeShop() {
    this.shopService.getBrands();
    this.shopService.getTypes();

    this.loadProducts();
  }

  openFiltersDialog() {
    this.dialogService
      .open<
        FiltersDialogComponent,
        FiltersDialogData,
        {
          selectedBrands: string[];
          selectedTypes: string[];
        }
      >(FiltersDialogComponent, {
        minWidth: '500px',
        data: {
          brands: this.shopService.brands,
          types: this.shopService.types,
          selectedBrands: this.selectedBrands,
          selectedTypes: this.selectedTypes,
        },
      })
      .afterClosed()
      .subscribe((result) => {
        if (!result) return;
        this.selectedBrands = result.selectedBrands;
        this.selectedTypes = result.selectedTypes;
        this.shopParams.brands = [...this.selectedBrands];
        this.shopParams.types = [...this.selectedTypes];
        this.shopParams.pageIndex = 1;
        this.loadProducts();
      });
  }

  onSortChange(event: MatSelectionListChange) {
    const selectedOption = event.options[0];
    if (!selectedOption) return;
    this.selectedSort = selectedOption.value as string;
    this.shopParams.sort = this.selectedSort;
    this.shopParams.pageIndex = 1;
    this.loadProducts();
  }

  private loadProducts() {
    this.shopService.getProducts(this.shopParams).subscribe({
      next: (response) => {
        this.products.set(response);
      },
      error: (error) => {
        console.log(error);
      },
      complete: () => {
        console.log('Request has completed');
      },
    });
  }
}
