import { useEffect, useState } from "react";
import { Container, Text, Title } from "@mantine/core";
import { getHealth } from "./api/health.ts";

type HealthStatus = "checking" | "ok" | "error";

const messages: Record<HealthStatus, string> = {
  checking: "Verificando la conexión con el sistema…",
  ok: "El sistema está funcionando.",
  error:
    "No se pudo conectar con el sistema. Verifique que el servidor esté iniciado.",
};

function App() {
  const [status, setStatus] = useState<HealthStatus>("checking");

  useEffect(() => {
    const controller = new AbortController();

    async function checkHealth() {
      try {
        const health = await getHealth(controller.signal);
        setStatus(health.status === "ok" ? "ok" : "error");
      } catch {
        if (!controller.signal.aborted) setStatus("error");
      }
    }

    checkHealth();

    return () => {
      controller.abort();
    };
  }, []);

  return (
    <Container component="main" py="xl">
      <Title order={1}>MarketApp</Title>
      <Text role="status" mt="md">
        {messages[status]}
      </Text>
    </Container>
  );
}

export default App;
