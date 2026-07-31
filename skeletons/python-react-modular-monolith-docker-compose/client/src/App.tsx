import { CatalogList } from "./features/catalog/CatalogList";

export function App() {
  return (
    <main className="mx-auto max-w-2xl p-8">
      <h1 className="mb-6 text-2xl font-semibold">Catalog</h1>
      <CatalogList />
    </main>
  );
}
