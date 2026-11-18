import { useEffect, useState } from "react";
import { Alert, Loader } from "@mantine/core";
import { getErrorMessage } from "../api/errors.ts";
import {
  getInventoryMovement,
  type InventoryMovementResponse,
} from "../api/inventoryMovements.ts";
import InboundForm from "./InboundForm.tsx";

function InboundEdit({ movementId }: { movementId: number }) {
  const [movement, setMovement] = useState<InventoryMovementResponse | null>(
    null,
  );
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const controller = new AbortController();

    async function loadMovement() {
      try {
        setMovement(await getInventoryMovement(movementId, controller.signal));
      } catch (error) {
        if (!controller.signal.aborted) {
          setError(getErrorMessage(error));
        }
      }
    }

    loadMovement();
    return () => controller.abort();
  }, [movementId]);

  if (error) {
    return <Alert color="red">{error}</Alert>;
  }

  if (movement === null) {
    return <Loader />;
  }

  return <InboundForm movement={movement} />;
}

export default InboundEdit;
