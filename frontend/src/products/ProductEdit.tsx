import { useEffect, useState } from "react";
import { Alert, Loader } from "@mantine/core";
import { getErrorMessage } from "../api/errors.ts";
import { getProduct, type ProductResponse } from "../api/products.ts";
import ProductForm from "./ProductForm.tsx";

function ProductEdit({ productId }: { productId: number }) {
  const [product, setProduct] = useState<ProductResponse | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const controller = new AbortController();

    async function loadProduct() {
      try {
        setProduct(await getProduct(productId, controller.signal));
      } catch (error) {
        if (!controller.signal.aborted) {
          setError(getErrorMessage(error));
        }
      }
    }

    loadProduct();
    return () => controller.abort();
  }, [productId]);

  if (error) {
    return <Alert color="red">{error}</Alert>;
  }

  if (product === null) {
    return <Loader />;
  }

  return <ProductForm product={product} />;
}

export default ProductEdit;
