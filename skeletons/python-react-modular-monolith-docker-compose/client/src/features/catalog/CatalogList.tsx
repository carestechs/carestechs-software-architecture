import { useQuery } from "@tanstack/react-query";

import { fetchProducts } from "./api";

export function CatalogList() {
  const { data, isPending, isError, error } = useQuery({
    queryKey: ["products"],
    queryFn: fetchProducts,
  });

  if (isPending) {
    return <p className="text-gray-500">Loading products…</p>;
  }
  if (isError) {
    return <p className="text-red-600">{error.message}</p>;
  }
  return (
    <div>
      <ul className="divide-y rounded-lg border">
        {data.data.map((product) => (
          <li key={product.id} className="flex items-center justify-between p-4">
            <span>{product.name}</span>
            <span className="font-mono text-sm text-gray-500">{product.sku}</span>
          </li>
        ))}
      </ul>
      <p className="mt-2 text-sm text-gray-500">{data.meta.totalCount} product(s)</p>
    </div>
  );
}
