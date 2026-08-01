import { useQuery } from "@tanstack/react-query";
import { useState } from "react";

import { Button } from "../../components/ui/button";
import { Input } from "../../components/ui/input";
import { fetchProducts } from "./api";

export function CatalogList() {
  const [filter, setFilter] = useState("");
  const { data, isPending, isError, error } = useQuery({
    queryKey: ["products"],
    queryFn: fetchProducts,
  });

  if (isPending) {
    return <p className="text-muted-foreground">Loading products…</p>;
  }
  if (isError) {
    return <p className="text-red-600">{error.message}</p>;
  }

  const visible = data.data.filter(
    (product) =>
      product.name.toLowerCase().includes(filter.toLowerCase()) ||
      product.sku.toLowerCase().includes(filter.toLowerCase()),
  );

  return (
    <div>
      <div className="mb-4 flex gap-2">
        <Input
          placeholder="Filter by name or SKU…"
          value={filter}
          onChange={(event) => setFilter(event.target.value)}
        />
        <Button variant="outline" disabled={!filter} onClick={() => setFilter("")}>
          Clear
        </Button>
      </div>
      <ul className="divide-y rounded-lg border border-border">
        {visible.map((product) => (
          <li key={product.id} className="flex items-center justify-between p-4">
            <span>{product.name}</span>
            <span className="font-mono text-sm text-muted-foreground">{product.sku}</span>
          </li>
        ))}
        {visible.length === 0 && (
          <li className="p-4 text-muted-foreground">No products match.</li>
        )}
      </ul>
      <p className="mt-2 text-sm text-muted-foreground">
        {visible.length} of {data.meta.totalCount} product(s)
      </p>
    </div>
  );
}
