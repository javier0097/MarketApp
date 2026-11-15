import { useEffect, useRef, useState } from "react";
import {
  Alert,
  Button,
  CloseButton,
  Group,
  Loader,
  Stack,
  Table,
  Text,
  TextInput,
  Tooltip,
} from "@mantine/core";
import { getErrorMessage } from "../api/errors.ts";
import {
  getInventoryMovements,
  type InventoryMovementResponse,
  type MovementType,
} from "../api/inventoryMovements.ts";
import {
  formatDateTime,
  formatShortDate,
  localDate,
  today,
} from "../lib/dates.ts";
import { formatQuantity, unitCost } from "../lib/inventory.ts";
import { formatMoney } from "../lib/money.ts";
import { matchesSearchWords, toSearchWords } from "../lib/search.ts";

const typeLabels: Record<MovementType, string> = {
  Inbound: "Ingreso",
  Outbound: "Salida",
};

function MovementList() {
  const [movements, setMovements] = useState<
    InventoryMovementResponse[] | null
  >(null);
  const [error, setError] = useState<string | null>(null);
  const [dateText, setDateText] = useState(today);
  const [selectedDate, setSelectedDate] = useState(today);
  const [search, setSearch] = useState("");
  const searchInputRef = useRef<HTMLInputElement>(null);

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

  const dateError =
    dateText === ""
      ? "Complete la fecha para ver sus movimientos."
      : dateText > today()
        ? "La fecha no puede ser posterior a hoy."
        : undefined;
  const words = toSearchWords(search);
  const searching = words.length > 0;
  const listedMovements = searching
    ? movements.filter((movement) =>
        matchesSearchWords(
          { name: movement.productName, code: movement.productCode },
          words,
        ),
      )
    : movements.filter(
        (movement) => localDate(new Date(movement.createdAt)) === selectedDate,
      );
  const single = listedMovements.length === 1;
  const count = `${listedMovements.length} ${single ? "movimiento" : "movimientos"}`;

  return (
    <Stack gap="lg">
      <Group align="flex-start">
        <TextInput
          ref={searchInputRef}
          label="Buscar"
          description="Por nombre o código del producto"
          inputWrapperOrder={["label", "input", "description"]}
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
          w={400}
          autoFocus
        />
        {!searching && (
          <TextInput
            type="date"
            label="Fecha de registro"
            max={today()}
            value={dateText}
            onChange={(event) => {
              const value = event.currentTarget.value;
              setDateText(value);
              if (value !== "" && value <= today()) {
                setSelectedDate(value);
              }
            }}
            error={dateError}
            w={220}
          />
        )}
      </Group>
      {listedMovements.length === 0 ? (
        searching ? (
          <Text style={{ overflowWrap: "anywhere" }}>
            No se encontraron movimientos de «{search.trim()}». Puede que el
            producto todavía no tenga movimientos, o que lo escrito no coincida
            con ningún producto registrado.
          </Text>
        ) : selectedDate === today() ? (
          <Stack gap="md" align="flex-start">
            <Text>
              Todavía no se registraron movimientos hoy. Aquí aparecen los
              ingresos y salidas de stock del día.
            </Text>
            <Button component="a" href="/inventario/ingresar">
              Ingresar stock
            </Button>
          </Stack>
        ) : (
          <Text>No se registraron movimientos en la fecha elegida.</Text>
        )
      ) : (
        <>
          <Text size="sm" style={{ overflowWrap: "anywhere" }}>
            {searching
              ? `${count} de «${search.trim()}» en todas las fechas`
              : `${count} ${single ? "registrado" : "registrados"} el ${formatShortDate(selectedDate)}`}
          </Text>
          <MovementTable movements={listedMovements} />
        </>
      )}
    </Stack>
  );
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
                formatMoney(unitCost(movement.amount, movement.quantity), 3)}
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
