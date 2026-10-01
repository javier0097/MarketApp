import { useEffect, useState } from "react";
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
    <main>
      <h1>MarketApp</h1>
      <p role="status">{messages[status]}</p>
    </main>
  );
}

export default App;
