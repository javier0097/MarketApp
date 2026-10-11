import { Container, Title } from "@mantine/core";
import ProductForm from "./products/ProductForm.tsx";

function App() {
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
