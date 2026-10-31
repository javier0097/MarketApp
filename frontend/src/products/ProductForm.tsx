import { useRef, useState, type SubmitEvent } from "react";
import {
  Alert,
  Button,
  Checkbox,
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
  createProduct,
  getNextInternalCode,
  updateProduct,
  type ProductResponse,
} from "../api/products.ts";
import { formatMoney, fromCents, toCents } from "../lib/money.ts";

type FieldErrors = Record<string, string[]>;

const nameMaxLength = 200;
const emptyValue = "(vacío)";

function getChanges(
  product: ProductResponse,
  code: string,
  name: string,
  salePrice: number | null,
) {
  const fields = [
    { label: "Nombre", before: product.name, after: name.trim() || emptyValue },
    {
      label: "Precio de venta",
      before: formatMoney(product.salePrice),
      after: salePrice === null ? emptyValue : formatMoney(salePrice),
    },
    { label: "Código", before: product.code, after: code.trim() || emptyValue },
  ];
  return fields.filter((field) => field.before !== field.after);
}

function ProductForm({ product }: { product?: ProductResponse }) {
  const ownInternalCode = product?.code.startsWith("TMV") ? product.code : null;
  const [name, setName] = useState(product?.name ?? "");
  const [price, setPrice] = useState<number | string>(
    product ? fromCents(product.salePrice).toFixed(2) : "",
  );
  const [code, setCode] = useState(product?.code ?? "");
  const [withoutBarcode, setWithoutBarcode] = useState(
    ownInternalCode !== null,
  );
  const [saving, setSaving] = useState(false);
  const [errors, setErrors] = useState<FieldErrors>({});
  const [generalError, setGeneralError] = useState<string | null>(null);
  const [lastSaved, setLastSaved] = useState<ProductResponse | null>(null);
  const nameInputRef = useRef<HTMLInputElement>(null);
  const priceInputRef = useRef<HTMLInputElement>(null);
  const codeInputRef = useRef<HTMLInputElement>(null);
  const typedCodeRef = useRef("");

  // NumberInput gives a string while the number is unfinished ("12.50", "12.") and "" when empty;
  // null lets the API answer with its own message
  const salePrice = price === "" ? null : toCents(Number(price));
  const changes = product ? getChanges(product, code, name, salePrice) : [];

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
    if (ownInternalCode) {
      setCode(ownInternalCode);
      return;
    }

    try {
      const next = await getNextInternalCode();
      setCode(next.code);
    } catch (error) {
      setWithoutBarcode(false);
      setGeneralError(getErrorMessage(error));
    }
  }

  async function handleSubmit(event: SubmitEvent<HTMLFormElement>) {
    event.preventDefault();

    // Saving would change UpdatedAt and move the product to the top of the list
    if (product && changes.length === 0) {
      window.location.assign("/productos");
      return;
    }

    setErrors({});
    setGeneralError(null);
    setLastSaved(null);
    setSaving(true);
    try {
      if (product) {
        await updateProduct(product.id, { code, name, salePrice });
        window.location.assign("/productos");
        return;
      }

      setLastSaved(await createProduct({ code, name, salePrice }));
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
          <Alert
            color="green"
            title="Producto guardado"
            style={{ overflowWrap: "anywhere" }}
          >
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
          description={`${name.length}/${nameMaxLength}`}
          inputWrapperOrder={["label", "input", "description", "error"]}
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
          // Saving moves the focus away before the cleared price is rendered,
          // so this has to read the latest value, not the one from this render
          onBlur={() =>
            setPrice((current) =>
              current === "" ? current : Number(current).toFixed(2),
            )
          }
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
            error={
              withoutBarcode &&
              errors.code?.[0] &&
              "Desmárquela y vuelva a marcarla para obtener un código nuevo."
            }
          />
        </Stack>
        {product && (
          <Stack gap="xs" style={{ overflowWrap: "anywhere" }}>
            <Text fw={500}>Cambios</Text>
            {changes.length === 0 ? (
              <Text size="sm">No ha modificado ningún dato.</Text>
            ) : (
              changes.map((change) => (
                <Text size="sm" key={change.label}>
                  {change.label}: {change.before} → {change.after}
                </Text>
              ))
            )}
          </Stack>
        )}
        <Button type="submit" loading={saving}>
          {product ? "Guardar cambios" : "Guardar producto"}
        </Button>
        {product && (
          <Button component="a" href="/productos" variant="default">
            Cancelar
          </Button>
        )}
      </Stack>
    </form>
  );
}

export default ProductForm;
