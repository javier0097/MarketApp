import { Container, Title } from "@mantine/core";
import InboundForm from "./inventory/InboundForm.tsx";
import MovementList from "./inventory/MovementList.tsx";
import ProductEdit from "./products/ProductEdit.tsx";
import ProductForm from "./products/ProductForm.tsx";
import ProductList from "./products/ProductList.tsx";

function App() {
  const editPath = window.location.pathname.match(
    /^\/productos\/(\d+)\/editar$/,
  );
  if (editPath) {
    return (
      <Container component="main" size="xs" py="xl">
        <title>Editar producto - MarketApp</title>
        <Title order={1} mb="lg">
          Editar producto
        </Title>
        <ProductEdit productId={Number(editPath[1])} />
      </Container>
    );
  }

  if (window.location.pathname === "/stock/ingresar") {
    return (
      <Container component="main" size="xs" py="xl">
        <title>Ingresar stock - MarketApp</title>
        <Title order={1} mb="lg">
          Ingresar stock
        </Title>
        <InboundForm />
      </Container>
    );
  }

  if (window.location.pathname === "/inventario/movimientos") {
    return (
      <Container component="main" size="xl" py="xl">
        <title>Movimientos de inventario - MarketApp</title>
        <Title order={1} mb="lg">
          Movimientos de inventario
        </Title>
        <MovementList />
      </Container>
    );
  }

  if (window.location.pathname === "/productos") {
    return (
      <Container component="main" size="lg" py="xl">
        <title>Productos - MarketApp</title>
        <Title order={1} mb="lg">
          Productos
        </Title>
        <ProductList />
      </Container>
    );
  }

  return (
    <Container component="main" size="xs" py="xl">
      <title>Nuevo producto - MarketApp</title>
      <Title order={1} mb="lg">
        Nuevo producto
      </Title>
      <ProductForm />
    </Container>
  );
}

export default App;
