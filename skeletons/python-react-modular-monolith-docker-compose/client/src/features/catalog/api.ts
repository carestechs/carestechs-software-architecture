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

export async function fetchProducts(): Promise<ListEnvelope<Product[]>> {
  const response = await fetch("/api/products");
  if (!response.ok) {
    throw new Error(`Failed to load products (HTTP ${response.status})`);
  }
  return response.json();
}
