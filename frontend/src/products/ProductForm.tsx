import { useRef, useState, type SubmitEvent } from "react";
import {
  Alert,
  Button,
  Checkbox,
  NumberInput,
  Stack,
  TextInput,
} from "@mantine/core";
import { ApiError, ValidationError } from "../api/errors.ts";
import {
  createProduct,
  getNextInternalCode,
  type ProductResponse,
} from "../api/products.ts";
import { formatMoney, toCents } from "../lib/money.ts";

type FieldErrors = Record<string, string[]>;

const unexpectedErrorMessage = "Ocurrió un error inesperado.";

function getErrorMessage(error: unknown): string {
  if (error instanceof ApiError) {
    return error.message;
  }
  console.error(error);
  return unexpectedErrorMessage;
}

function ProductForm() {
  const [name, setName] = useState("");
  const [price, setPrice] = useState<number | string>("");
  const [code, setCode] = useState("");
  const [withoutBarcode, setWithoutBarcode] = useState(false);
  const [saving, setSaving] = useState(false);
  const [errors, setErrors] = useState<FieldErrors>({});
  const [generalError, setGeneralError] = useState<string | null>(null);
  const [lastSaved, setLastSaved] = useState<ProductResponse | null>(null);
  const nameInputRef = useRef<HTMLInputElement>(null);
  const priceInputRef = useRef<HTMLInputElement>(null);
  const codeInputRef = useRef<HTMLInputElement>(null);
  const typedCodeRef = useRef("");

  function clearFieldError(field: string) {
    setErrors((current) => ({ ...current, [field]: [] }));
  }

  function showFieldErrors(fieldErrors: FieldErrors) {
    setErrors(fieldErrors);

    // In screen order, so focus goes to the first field with an error
    const fieldInputs = [
      ["name", nameInputRef],
      ["salePrice", priceInputRef],
      ["code", codeInputRef],
    ] as const;
    const firstWithError = fieldInputs.find(([field]) => fieldErrors[field]);
    if (firstWithError) {
      firstWithError[1].current?.focus();
    } else {
      setGeneralError(unexpectedErrorMessage);
    }
  }

  async function handleWithoutBarcodeChange(checked: boolean) {
    setWithoutBarcode(checked);
    clearFieldError("code");
    if (!checked) {
      setCode(typedCodeRef.current);
      return;
    }

    typedCodeRef.current = code;
    try {
      const next = await getNextInternalCode();
      setCode(next.code);
    } catch (error) {
      // Unchecked so the code field is editable again
      setWithoutBarcode(false);
      setGeneralError(getErrorMessage(error));
    }
  }

  async function handleSubmit(event: SubmitEvent<HTMLFormElement>) {
    event.preventDefault();

    // NumberInput gives a string while the number is unfinished ("12.50", "12.") and "" when empty;
    // null lets the API answer with its own message
    const salePrice = price === "" ? null : toCents(Number(price));

    setErrors({});
    setGeneralError(null);
    setLastSaved(null);
    setSaving(true);
    try {
      const product = await createProduct({ code, name, salePrice });
      setLastSaved(product);
      setName("");
      setPrice("");
      setCode("");
      setWithoutBarcode(false);
      nameInputRef.current?.focus();
    } catch (error) {
      if (error instanceof ValidationError) {
        showFieldErrors(error.errors);
      } else {
        setGeneralError(getErrorMessage(error));
      }
    } finally {
      setSaving(false);
    }
  }

  return (
    <form onSubmit={handleSubmit}>
      <Stack gap="md">
        {generalError && <Alert color="red">{generalError}</Alert>}
        {lastSaved && (
          <Alert color="green" title="Producto guardado">
            {lastSaved.name} · {lastSaved.code} ·{" "}
            {formatMoney(lastSaved.salePrice)}
          </Alert>
        )}
        <TextInput
          ref={nameInputRef}
          label="Nombre"
          value={name}
          onChange={(event) => {
            setName(event.currentTarget.value);
            clearFieldError("name");
          }}
          error={errors.name?.[0]}
          autoFocus
        />
        <NumberInput
          ref={priceInputRef}
          label="Precio de venta"
          leftSection="Bs"
          decimalSeparator=","
          decimalScale={2}
          allowNegative={false}
          isAllowed={({ value }) => value.split(".")[0].length <= 7}
          hideControls
          value={price}
          onChange={(value) => {
            setPrice(value);
            clearFieldError("salePrice");
          }}
          error={errors.salePrice?.[0]}
        />
        <Stack gap="xs">
          <TextInput
            ref={codeInputRef}
            label="Código"
            value={code}
            onChange={(event) => {
              setCode(event.currentTarget.value);
              clearFieldError("code");
            }}
            readOnly={withoutBarcode}
            error={errors.code?.[0]}
          />
          <Checkbox
            label="Sin código de barras"
            checked={withoutBarcode}
            onChange={(event) =>
              handleWithoutBarcodeChange(event.currentTarget.checked)
            }
          />
        </Stack>
        <Button type="submit" loading={saving}>
          Guardar producto
        </Button>
      </Stack>
    </form>
  );
}

export default ProductForm;
