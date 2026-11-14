import { useEffect, useState } from "react";
import { Alert, Loader, Table, Text } from "@mantine/core";
import { getErrorMessage } from "../api/errors.ts";
import {
  getInventoryMovements,
  type InventoryMovementResponse,
  type MovementType,
} from "../api/inventoryMovements.ts";
import { formatDateTime, formatShortDate } from "../lib/dates.ts";
import { formatQuantity, unitCost } from "../lib/inventory.ts";
import { formatMoney } from "../lib/money.ts";

const typeLabels: Record<MovementType, string> = {
  Inbound: "Ingreso",
  Outbound: "Salida",
};

function MovementList() {
  const [movements, setMovements] = useState<
    InventoryMovementResponse[] | null
  >(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const controller = new AbortController();

    async function loadMovements() {
      try {
        setMovements(await getInventoryMovements(controller.signal));
      } catch (error) {
        if (!controller.signal.aborted) {
          setError(getErrorMessage(error));
        }
      }
    }

    loadMovements();
    return () => controller.abort();
  }, []);

  if (error) {
    return <Alert color="red">{error}</Alert>;
  }

  if (movements === null) {
    return <Loader />;
  }

  return <MovementTable movements={movements} />;
}

function MovementTable({
  movements,
}: {
  movements: InventoryMovementResponse[];
}) {
  return (
    <Table striped>
      <Table.Thead>
        <Table.Tr>
          <Table.Th>Registrado</Table.Th>
          <Table.Th>Fecha del movimiento</Table.Th>
          <Table.Th>Producto</Table.Th>
          <Table.Th>Tipo</Table.Th>
          <Table.Th ta="right">Cantidad</Table.Th>
          <Table.Th ta="right">Monto total</Table.Th>
          <Table.Th ta="right">Costo unitario</Table.Th>
          <Table.Th>Nota</Table.Th>
        </Table.Tr>
      </Table.Thead>
      <Table.Tbody>
        {movements.map((movement) => (
          <Table.Tr key={movement.id}>
            <Table.Td style={{ whiteSpace: "nowrap" }}>
              {formatDateTime(movement.createdAt)}
            </Table.Td>
            <Table.Td>{formatShortDate(movement.movementDate)}</Table.Td>
            <Table.Td miw={200} style={{ overflowWrap: "anywhere" }}>
              <Text size="sm">{movement.productName}</Text>
              <Text size="xs" c="dimmed">
                {movement.productCode}
              </Text>
            </Table.Td>
            <Table.Td>{typeLabels[movement.type]}</Table.Td>
            <Table.Td ta="right">{formatQuantity(movement.quantity)}</Table.Td>
            <Table.Td ta="right" style={{ whiteSpace: "nowrap" }}>
              {formatMoney(movement.amount)}
            </Table.Td>
            <Table.Td ta="right" style={{ whiteSpace: "nowrap" }}>
              {movement.type === "Inbound" &&
                formatMoney(unitCost(movement.amount, movement.quantity))}
            </Table.Td>
            <Table.Td style={{ overflowWrap: "anywhere" }}>
              {movement.note}
            </Table.Td>
          </Table.Tr>
        ))}
      </Table.Tbody>
    </Table>
  );
}

export default MovementList;
