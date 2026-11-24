import { Alert } from "@mantine/core";
import type { InboundMovementResponse } from "../api/inventoryMovements.ts";
import { formatDate } from "../lib/dates.ts";
import { formatQuantity } from "../lib/inventory.ts";
import { formatMoney } from "../lib/money.ts";

export type SavedInbound = {
  productName: string;
  movement: InboundMovementResponse;
};

function InboundSavedAlert({ saved }: { saved: SavedInbound }) {
  return (
    <Alert
      color="green"
      title="Ingreso guardado"
      style={{ overflowWrap: "anywhere" }}
    >
      {saved.productName} · {formatQuantity(saved.movement.quantity)} u. ·{" "}
      {formatMoney(saved.movement.totalCost)} ·{" "}
      {formatDate(saved.movement.movementDate)}
    </Alert>
  );
}

export default InboundSavedAlert;
