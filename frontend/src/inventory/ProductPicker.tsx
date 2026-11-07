import { useEffect, type Ref } from "react";
import {
  Combobox,
  ScrollArea,
  Text,
  TextInput,
  useCombobox,
} from "@mantine/core";
import type { ProductResponse } from "../api/products.ts";
import {
  findProductByCode,
  matchesSearchWords,
  toSearchWords,
} from "../lib/search.ts";

type ProductPickerProps = {
  products: ProductResponse[] | null;
  text: string;
  product: ProductResponse | null;
  onChange: (text: string, picked: ProductResponse | null) => void;
  onPick: () => void;
  error?: string;
  inputRef: Ref<HTMLInputElement>;
};

function ProductPicker({
  products,
  text,
  product,
  onChange,
  onPick,
  error,
  inputRef,
}: ProductPickerProps) {
  const combobox = useCombobox();
  const { selectFirstOption } = combobox;
  const search = text.trim();
  const words = toSearchWords(text);
  const matches =
    products?.filter((candidate) => matchesSearchWords(candidate, words)) ?? [];

  useEffect(() => {
    selectFirstOption();
  }, [selectFirstOption, text, products]);

  function pick(productId: string) {
    const picked = products?.find(
      (candidate) => String(candidate.id) === productId,
    );
    if (picked) {
      onChange(picked.name, picked);
      combobox.closeDropdown();
      onPick();
    }
  }

  return (
    <Combobox store={combobox} onOptionSubmit={pick}>
      <Combobox.Target>
        <TextInput
          ref={inputRef}
          label="Producto"
          value={text}
          onChange={(event) => {
            const value = event.currentTarget.value;
            onChange(value, null);
            if (findProductByCode(products, value)) {
              combobox.closeDropdown();
            } else {
              combobox.openDropdown();
            }
          }}
          onBlur={() => combobox.closeDropdown()}
          description={product && `Elegido: ${product.name} · ${product.code}`}
          inputWrapperOrder={["label", "input", "description", "error"]}
          error={error}
          autoFocus
        />
      </Combobox.Target>
      <Combobox.Dropdown hidden={search === ""}>
        <Combobox.Options>
          <ScrollArea.Autosize mah={280} type="scroll">
            {products === null ? (
              <Combobox.Empty>Cargando productos…</Combobox.Empty>
            ) : matches.length === 0 ? (
              <Combobox.Empty style={{ overflowWrap: "anywhere" }}>
                Ningún producto coincide con «{search}».
              </Combobox.Empty>
            ) : (
              matches.map((candidate) => (
                <Combobox.Option
                  value={String(candidate.id)}
                  key={candidate.id}
                >
                  <Text size="sm" style={{ overflowWrap: "anywhere" }}>
                    {candidate.name}
                  </Text>
                  <Text size="xs" c="dimmed">
                    {candidate.code}
                  </Text>
                </Combobox.Option>
              ))
            )}
          </ScrollArea.Autosize>
        </Combobox.Options>
      </Combobox.Dropdown>
    </Combobox>
  );
}

export default ProductPicker;
