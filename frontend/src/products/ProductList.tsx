import { useEffect, useRef, useState } from "react";
import {
  Alert,
  Button,
  CloseButton,
  Loader,
  Stack,
  Table,
  Text,
  TextInput,
  Tooltip,
} from "@mantine/core";
import { getErrorMessage } from "../api/errors.ts";
import { getProducts, type ProductResponse } from "../api/products.ts";
import { formatDateTime } from "../lib/dates.ts";
import { formatMoney } from "../lib/money.ts";
import { normalizeForSearch } from "../lib/search.ts";

function ProductList() {
  const [products, setProducts] = useState<ProductResponse[] | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [search, setSearch] = useState("");
  const searchInputRef = useRef<HTMLInputElement>(null);

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

  const searchTerm = search.trim();
  const words = normalizeForSearch(search).split(/\s+/).filter(Boolean);
  const filteredProducts = products.filter((product) => {
    const name = normalizeForSearch(product.name);
    const code = normalizeForSearch(product.code);
    return words.every((word) => name.includes(word) || code.includes(word));
  });

  return (
    <Stack gap="lg">
      <TextInput
        ref={searchInputRef}
        label="Buscar"
        description="Por nombre o código"
        value={search}
        onChange={(event) => setSearch(event.currentTarget.value)}
        rightSectionPointerEvents="all"
        rightSection={
          search && (
            <Tooltip label="Limpiar búsqueda">
              <CloseButton
                aria-label="Limpiar búsqueda"
                onClick={() => {
                  setSearch("");
                  searchInputRef.current?.focus();
                }}
              />
            </Tooltip>
          )
        }
        maw={400}
        autoFocus
      />
      {filteredProducts.length === 0 ? (
        <Text style={{ overflowWrap: "anywhere" }}>
          Ningún producto coincide con «{searchTerm}». Revise lo escrito o
          busque por otra parte del nombre o del código.
        </Text>
      ) : (
        <>
          <Text size="sm">
            {words.length > 0 && `${filteredProducts.length} de `}
            {products.length} {products.length === 1 ? "producto" : "productos"}
          </Text>
          <ProductTable products={filteredProducts} />
        </>
      )}
    </Stack>
  );
}

function ProductTable({ products }: { products: ProductResponse[] }) {
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
