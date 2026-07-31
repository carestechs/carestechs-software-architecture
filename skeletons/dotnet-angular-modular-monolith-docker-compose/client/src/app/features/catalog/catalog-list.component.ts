import { HttpClient } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';

export type Product = {
  id: string;
  sku: string;
  name: string;
  createdAt: string; // ISO 8601
};

export type ListEnvelope<T> = {
  data: T;
  meta: { totalCount: number };
};

@Component({
  selector: 'app-catalog-list',
  templateUrl: './catalog-list.component.html',
  styles: [],
})
export class CatalogListComponent {
  private readonly http = inject(HttpClient);

  // Signals own component state; RxJS is used only for the HTTP call
  // (adrs/angular/signals-state.md)
  readonly products = signal<Product[]>([]);
  readonly totalCount = signal(0);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);

  constructor() {
    this.http.get<ListEnvelope<Product[]>>('/api/products').subscribe({
      next: (envelope) => {
        this.products.set(envelope.data);
        this.totalCount.set(envelope.meta.totalCount);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Failed to load products.');
        this.loading.set(false);
      },
    });
  }
}
