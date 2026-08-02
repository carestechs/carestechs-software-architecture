import { HttpClient } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';

import { environment } from '../../../environments/environment';

export type Product = {
  id: string;
  sku: string;
  name: string;
  createdAt: string; // ISO 8601
};

@Component({
  selector: 'app-catalog-list',
  templateUrl: './catalog-list.component.html',
  styles: [],
})
export class CatalogListComponent {
  private readonly http = inject(HttpClient);

  // Signals own component state; RxJS only for the HTTP call
  // (adrs/angular/signals-state.md). Bare DTO array — this profile does not
  // use the rest-envelope (Optional tier, not adopted).
  readonly products = signal<Product[]>([]);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);

  constructor() {
    this.http.get<Product[]>(`${environment.apiBaseUrl}/v1/products`).subscribe({
      next: (products) => {
        this.products.set(products);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Failed to load products.');
        this.loading.set(false);
      },
    });
  }
}
