import { Stack, Text } from "@mantine/core";
import type {
  InventoryMovementResponse,
  StockSummaryResponse,
} from "../api/inventoryMovements.ts";
import { formatDate } from "../lib/dates.ts";
import {
  formatQuantity,
  stockAfterInboundEdit,
  unitCost,
} from "../lib/inventory.ts";
import { formatMoney } from "../lib/money.ts";

type InboundEditSummaryProps = {
  movement: InventoryMovementResponse;
  summary: StockSummaryResponse | null;
  quantity: number | null;
  costCents: number | null;
  movementDate: string;
};

function InboundEditSummary({
  movement,
  summary,
  quantity,
  costCents,
  movementDate,
}: InboundEditSummaryProps) {
  const quantityChanged = quantity !== null && quantity !== movement.quantity;
  const unitCostChanged =
    quantity !== null &&
    costCents !== null &&
    (quantity !== movement.quantity || costCents !== movement.amount);
  const dateChanged =
    movementDate !== "" && movementDate !== movement.movementDate;

  if (!quantityChanged && !unitCostChanged && !dateChanged) {
    return null;
  }

  return (
    <Stack gap="xs">
      <Text fw={500}>Resumen</Text>
      {quantityChanged && summary === null && (
        <Text size="sm">Cargando el stock del producto…</Text>
      )}
      {quantityChanged && summary !== null && (
        <Text size="sm">
          Stock: {formatQuantity(summary.stock)} →{" "}
          {formatQuantity(
            stockAfterInboundEdit(summary.stock, movement.quantity, quantity),
          )}
        </Text>
      )}
      {unitCostChanged && (
        <Text size="sm">
          Costo unitario:{" "}
          {formatMoney(unitCost(movement.amount, movement.quantity), 3)} →{" "}
          {formatMoney(unitCost(costCents, quantity), 3)}
        </Text>
      )}
      {dateChanged && (
        <Text size="sm">
          Fecha del movimiento: {formatDate(movement.movementDate)} →{" "}
          {formatDate(movementDate)}
        </Text>
      )}
    </Stack>
  );
}

export default InboundEditSummary;
