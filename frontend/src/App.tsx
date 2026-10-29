import { Container, Title } from "@mantine/core";
import ProductForm from "./products/ProductForm.tsx";
import ProductList from "./products/ProductList.tsx";

function App() {
  if (window.location.pathname === "/productos") {
    return (
      <Container component="main" size="lg" py="xl">
        <Title order={1} mb="lg">
          Productos
        </Title>
        <ProductList />
      </Container>
    );
  }

  return (
    <Container component="main" size="xs" py="xl">
      <Title order={1} mb="lg">
        Nuevo producto
      </Title>
      <ProductForm />
    </Container>
  );
}

export default App;
