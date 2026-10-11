import { useState, type SubmitEvent } from "react";
import { Button, Checkbox, NumberInput, Stack, TextInput } from "@mantine/core";
import { createProduct, getNextInternalCode } from "../api/products.ts";
import { toCents } from "../lib/money.ts";

function ProductForm() {
  const [code, setCode] = useState("");
  const [withoutBarcode, setWithoutBarcode] = useState(false);
  const [name, setName] = useState("");
  const [price, setPrice] = useState<number | string>("");
  const [saving, setSaving] = useState(false);

  async function loadNextInternalCode() {
    const next = await getNextInternalCode();
    setCode(next.code);
  }

  async function handleWithoutBarcodeChange(checked: boolean) {
    setWithoutBarcode(checked);
    if (checked) {
      await loadNextInternalCode();
    } else {
      setCode("");
    }
  }

  async function handleSubmit(event: SubmitEvent<HTMLFormElement>) {
    event.preventDefault();

    // NumberInput gives a string while the number is unfinished ("12.50", "12.") and "" when empty;
    // null lets the API answer with its own message
    const salePrice = price === "" ? null : toCents(Number(price));

    setSaving(true);
    try {
      await createProduct({ code, name, salePrice });
      setName("");
      setPrice("");
      if (withoutBarcode) {
        await loadNextInternalCode();
      } else {
        setCode("");
      }
    } finally {
      setSaving(false);
    }
  }

  return (
    <form onSubmit={handleSubmit}>
      <Stack gap="md">
        <Stack gap="xs">
          <TextInput
            label="Código"
            value={code}
            onChange={(event) => setCode(event.currentTarget.value)}
            readOnly={withoutBarcode}
            autoFocus
          />
          <Checkbox
            label="Sin código de barras"
            checked={withoutBarcode}
            onChange={(event) =>
              handleWithoutBarcodeChange(event.currentTarget.checked)
            }
          />
        </Stack>
        <TextInput
          label="Nombre"
          value={name}
          onChange={(event) => setName(event.currentTarget.value)}
        />
        <NumberInput
          label="Precio de venta"
          leftSection="Bs"
          decimalSeparator=","
          decimalScale={2}
          allowNegative={false}
          hideControls
          value={price}
          onChange={setPrice}
        />
        <Button type="submit" loading={saving}>
          Guardar producto
        </Button>
      </Stack>
    </form>
  );
}

export default ProductForm;
