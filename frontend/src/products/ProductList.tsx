import { useEffect, useState } from "react";
import { Alert, Button, Loader, Stack, Table, Text } from "@mantine/core";
import { getErrorMessage } from "../api/errors.ts";
import { getProducts, type ProductResponse } from "../api/products.ts";
import { formatDateTime } from "../lib/dates.ts";
import { formatMoney } from "../lib/money.ts";

function ProductList() {
  const [products, setProducts] = useState<ProductResponse[] | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const controller = new AbortController();

    async function loadProducts() {
      try {
        setProducts(await getProducts(controller.signal));
      } catch (error) {
        if (!controller.signal.aborted) {
          setError(getErrorMessage(error));
        }
      }
    }

    loadProducts();
    return () => controller.abort();
  }, []);

  if (error) {
    return <Alert color="red">{error}</Alert>;
  }

  if (products === null) {
    return <Loader />;
  }

  if (products.length === 0) {
    return (
      <Stack gap="md" align="flex-start">
        <Text>
          Todavía no hay productos registrados. Los productos que cree
          aparecerán aquí, empezando por el más reciente.
        </Text>
        <Button component="a" href="/">
          Crear producto
        </Button>
      </Stack>
    );
  }

  return (
    <Table striped>
      <Table.Thead>
        <Table.Tr>
          <Table.Th>Código</Table.Th>
          <Table.Th>Nombre</Table.Th>
          <Table.Th ta="right" style={{ whiteSpace: "nowrap" }}>
            Precio de venta
          </Table.Th>
          <Table.Th pl="xl">Creado</Table.Th>
        </Table.Tr>
      </Table.Thead>
      <Table.Tbody>
        {products.map((product) => (
          <Table.Tr key={product.id}>
            <Table.Td>{product.code}</Table.Td>
            <Table.Td style={{ overflowWrap: "anywhere" }}>
              {product.name}
            </Table.Td>
            <Table.Td ta="right" style={{ whiteSpace: "nowrap" }}>
              {formatMoney(product.salePrice)}
            </Table.Td>
            <Table.Td pl="xl" style={{ whiteSpace: "nowrap" }}>
              {formatDateTime(product.createdAt)}
            </Table.Td>
          </Table.Tr>
        ))}
      </Table.Tbody>
    </Table>
  );
}

export default ProductList;
