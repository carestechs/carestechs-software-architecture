import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./features/catalog/catalog-list.component').then((m) => m.CatalogListComponent),
  },
];
