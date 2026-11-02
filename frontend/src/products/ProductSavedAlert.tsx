import { Alert } from "@mantine/core";
import type { ProductResponse } from "../api/products.ts";
import { formatMoney } from "../lib/money.ts";

function ProductSavedAlert({ product }: { product: ProductResponse }) {
  return (
    <Alert
      color="green"
      title="Producto guardado"
      style={{ overflowWrap: "anywhere" }}
    >
      {product.name} · {product.code} · {formatMoney(product.salePrice)}
    </Alert>
  );
}

export default ProductSavedAlert;
