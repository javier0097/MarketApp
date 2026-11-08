import { useEffect, useRef, useState, type SubmitEvent } from "react";
import {
  Alert,
  Button,
  NumberInput,
  Stack,
  Text,
  TextInput,
} from "@mantine/core";
import {
  getErrorMessage,
  unexpectedErrorMessage,
  ValidationError,
} from "../api/errors.ts";
import {
  createInboundMovement,
  getStockSummary,
  type InboundMovementResponse,
  type StockSummaryResponse,
} from "../api/inventoryMovements.ts";
import { getProducts, type ProductResponse } from "../api/products.ts";
import { formatDate, today } from "../lib/dates.ts";
import { stockAfterInbound, unitCost } from "../lib/inventory.ts";
import { formatMoney, toCents } from "../lib/money.ts";
import { findProductByCode } from "../lib/search.ts";
import ProductPicker from "./ProductPicker.tsx";

type FieldErrors = Record<string, string[]>;

const noteMaxLength = 500;
function InboundForm() {
  const [products, setProducts] = useState<ProductResponse[] | null>(null);
  const [productText, setProductText] = useState("");
  const [pickedProduct, setPickedProduct] = useState<ProductResponse | null>(
    null,
  );
  const [quantity, setQuantity] = useState<number | string>("");
  const [cost, setCost] = useState<number | string>("");
  const [movementDate, setMovementDate] = useState(today);
  const [note, setNote] = useState("");
  const [saving, setSaving] = useState(false);
  const [errors, setErrors] = useState<FieldErrors>({});
  const [generalError, setGeneralError] = useState<string | null>(null);
  const [loadedSummary, setLoadedSummary] = useState<{
    productId: number;
    summary: StockSummaryResponse;
  } | null>(null);
  const [lastSaved, setLastSaved] = useState<{
    productName: string;
    movement: InboundMovementResponse;
  } | null>(null);

  const productInputRef = useRef<HTMLInputElement>(null);
  const quantityInputRef = useRef<HTMLInputElement>(null);
  const costInputRef = useRef<HTMLInputElement>(null);
  const dateInputRef = useRef<HTMLInputElement>(null);
  const noteInputRef = useRef<HTMLInputElement>(null);

  // Derived on every render, so a code scanned before the products finished loading is recognized when they arrive
  const product = pickedProduct ?? findProductByCode(products, productText);
  const productId = product?.id;
  // A summary that arrives for another product is never shown
  const summary =
    loadedSummary !== null && loadedSummary.productId === productId
      ? loadedSummary.summary
      : null;

  const validQuantity = Number(quantity) >= 1 ? Number(quantity) : null;
  const costCents = cost === "" ? null : toCents(Number(cost));

  useEffect(() => {
    const controller = new AbortController();

    async function loadProducts() {
      try {
        setProducts(await getProducts(controller.signal));
      } catch (error) {
        if (!controller.signal.aborted) {
          setGeneralError(getErrorMessage(error));
        }
      }
    }

    loadProducts();
    return () => controller.abort();
  }, []);

  useEffect(() => {
    if (productId === undefined) {
      return;
    }

    const controller = new AbortController();

    async function loadSummary(id: number) {
      try {
        const summary = await getStockSummary(id, controller.signal);
        setLoadedSummary({ productId: id, summary });
      } catch (error) {
        if (!controller.signal.aborted) {
          setGeneralError(getErrorMessage(error));
        }
      }
    }

    loadSummary(productId);
    return () => controller.abort();
  }, [productId]);

  function clearFieldError(field: string) {
    setErrors((current) => ({ ...current, [field]: [] }));
  }

  function showFieldErrors(fieldErrors: FieldErrors) {
    setErrors(fieldErrors);

    // In screen order, so focus goes to the first field with an error
    const fieldInputs = [
      ["productId", productInputRef],
      ["quantity", quantityInputRef],
      ["totalCost", costInputRef],
      ["movementDate", dateInputRef],
      ["note", noteInputRef],
    ] as const;
    const firstWithError = fieldInputs.find(([field]) => fieldErrors[field]);
    if (firstWithError) {
      firstWithError[1].current?.focus();
    } else {
      setGeneralError(unexpectedErrorMessage);
    }
  }

  async function handleSubmit(event: SubmitEvent<HTMLFormElement>) {
    event.preventDefault();

    if (saving) {
      return;
    }

    setGeneralError(null);
    setLastSaved(null);
    setSaving(true);
    try {
      const movement = await createInboundMovement({
        productId: product?.id ?? null,
        quantity: quantity === "" ? null : Number(quantity),
        totalCost: costCents,
        movementDate: movementDate === "" ? null : movementDate,
        note,
      });
      setLastSaved({ productName: product!.name, movement });
      setProductText("");
      setPickedProduct(null);
      setQuantity("");
      setCost("");
      setMovementDate(today());
      setNote("");
      productInputRef.current?.focus();
    } catch (error) {
      if (error instanceof ValidationError) {
        const fieldErrors = { ...error.errors };
        const typed = productText.trim();
        if (fieldErrors.productId && product === null && typed !== "") {
          fieldErrors.productId = [
            `«${typed}» no es el nombre ni el código de ningún producto. Elija uno en la lista que aparece al escribir o escanee el código de barras del producto.`,
          ];
        }
        showFieldErrors(fieldErrors);
      } else {
        setGeneralError(getErrorMessage(error));
      }
    } finally {
      setSaving(false);
    }
  }

  return (
    // noValidate: the date's max would otherwise block saving with the browser's own fading tooltip,
    // instead of the API's message next to the field
    <form onSubmit={handleSubmit} noValidate>
      <Stack gap="md">
        {generalError && <Alert color="red">{generalError}</Alert>}
        {lastSaved && (
          <Alert
            color="green"
            title="Ingreso guardado"
            style={{ overflowWrap: "anywhere" }}
          >
            {lastSaved.productName} · {lastSaved.movement.quantity} u. ·{" "}
            {formatMoney(lastSaved.movement.totalCost)} ·{" "}
            {formatDate(lastSaved.movement.movementDate)}
          </Alert>
        )}
        <ProductPicker
          products={products}
          text={productText}
          product={product}
          onChange={(text, picked) => {
            setProductText(text);
            setPickedProduct(picked);
            clearFieldError("productId");
          }}
          onPick={() => quantityInputRef.current?.focus()}
          error={errors.productId?.[0]}
          inputRef={productInputRef}
        />
        <NumberInput
          ref={quantityInputRef}
          label="Cantidad"
          allowDecimal={false}
          allowNegative={false}
          // Beyond 9 digits the value no longer fits the API's integer
          isAllowed={({ value }) => value.length <= 9}
          hideControls
          value={quantity}
          onChange={(value) => {
            setQuantity(value);
            clearFieldError("quantity");
          }}
          error={errors.quantity?.[0]}
        />
        <NumberInput
          ref={costInputRef}
          label="Costo total"
          leftSection="Bs"
          decimalSeparator=","
          decimalScale={2}
          allowNegative={false}
          // More digits than the API accepts: a barcode scanned here must exceed the limit, not be cut down to a valid cost
          isAllowed={({ value }) => value.split(".")[0].length <= 7}
          hideControls
          value={cost}
          onChange={(value) => {
            setCost(value);
            clearFieldError("totalCost");
          }}
          onBlur={() =>
            setCost((current) =>
              current === "" ? current : Number(current).toFixed(2),
            )
          }
          error={errors.totalCost?.[0]}
        />
        <TextInput
          ref={dateInputRef}
          type="date"
          label="Fecha"
          max={today()}
          value={movementDate}
          onChange={(event) => {
            setMovementDate(event.currentTarget.value);
            clearFieldError("movementDate");
          }}
          error={errors.movementDate?.[0]}
        />
        <TextInput
          ref={noteInputRef}
          label="Nota"
          value={note}
          onChange={(event) => {
            setNote(event.currentTarget.value);
            clearFieldError("note");
          }}
          description={`${note.length}/${noteMaxLength}`}
          inputWrapperOrder={["label", "input", "description", "error"]}
          error={errors.note?.[0]}
        />
        {product && (
          <Stack gap="xs">
            <Text fw={500}>Resumen</Text>
            {summary === null ? (
              <Text size="sm">Cargando el stock del producto…</Text>
            ) : (
              <>
                <Text size="sm">
                  {validQuantity === null
                    ? `Stock actual: ${summary.stock}`
                    : `Stock: ${summary.stock} → ${stockAfterInbound(summary.stock, validQuantity)}`}
                </Text>
                {validQuantity !== null && costCents !== null && (
                  <Text size="sm">
                    Costo unitario:{" "}
                    {formatMoney(unitCost(costCents, validQuantity))}
                  </Text>
                )}
                <Text size="sm">
                  Último costo unitario:{" "}
                  {summary.lastInboundQuantity === null ||
                  summary.lastInboundTotalCost === null
                    ? "Sin ingresos anteriores"
                    : formatMoney(
                        unitCost(
                          summary.lastInboundTotalCost,
                          summary.lastInboundQuantity,
                        ),
                      )}
                </Text>
              </>
            )}
            {movementDate !== "" && movementDate !== today() && (
              <Text size="sm" fw={700}>
                Fecha del ingreso: {formatDate(movementDate)} (no es hoy)
              </Text>
            )}
          </Stack>
        )}
        <Button type="submit" loading={saving}>
          Guardar ingreso
        </Button>
      </Stack>
    </form>
  );
}

export default InboundForm;
