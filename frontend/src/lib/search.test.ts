import { describe, expect, it } from "vitest";
import { normalizeForSearch } from "./search.ts";

describe("normalizeForSearch", () => {
  it("Converts uppercase letters to lowercase", () => {
    const result = normalizeForSearch("ARROZ");

    expect(result).toBe("arroz");
  });

  it("Converts a mixed case text to lowercase", () => {
    const result = normalizeForSearch("Leche Entera");

    expect(result).toBe("leche entera");
  });

  it.each([
    ["á", "a"],
    ["é", "e"],
    ["í", "i"],
    ["ó", "o"],
    ["ú", "u"],
  ])("Removes the accent from the lowercase vowel %s", (input, expected) => {
    const result = normalizeForSearch(input);

    expect(result).toBe(expected);
  });

  it.each([
    ["Á", "a"],
    ["É", "e"],
    ["Í", "i"],
    ["Ó", "o"],
    ["Ú", "u"],
  ])(
    "Removes the accent and lowercases the uppercase vowel %s",
    (input, expected) => {
      const result = normalizeForSearch(input);

      expect(result).toBe(expected);
    },
  );

  it("Removes several accents in the same text", () => {
    const result = normalizeForSearch("Café Molido Orgánico");

    expect(result).toBe("cafe molido organico");
  });

  it("Removes an accent written as a separate combining mark", () => {
    // U+0301 is the combining acute accent: "Cafe" + U+0301 is "Café" with the accent as a separate character.
    const combiningAcuteAccent = String.fromCharCode(0x0301);

    const result = normalizeForSearch("Cafe" + combiningAcuteAccent);

    expect(result).toBe("cafe");
  });

  it("Turns a lowercase ñ into n", () => {
    const result = normalizeForSearch("Piña");

    expect(result).toBe("pina");
  });

  it("Turns an uppercase Ñ into n", () => {
    const result = normalizeForSearch("ÑOQUIS");

    expect(result).toBe("noquis");
  });

  it("Removes the diaeresis from a lowercase ü", () => {
    const result = normalizeForSearch("Pingüino");

    expect(result).toBe("pinguino");
  });

  it("Removes the diaeresis and lowercases an uppercase Ü", () => {
    const result = normalizeForSearch("PINGÜINO");

    expect(result).toBe("pinguino");
  });

  it.each([
    ["à", "a"],
    ["â", "a"],
    ["ã", "a"],
    ["ö", "o"],
    ["ç", "c"],
    ["Ç", "c"],
  ])("Removes any other mark from the letter %s", (input, expected) => {
    const result = normalizeForSearch(input);

    expect(result).toBe(expected);
  });

  it("Removes marks of different kinds in the same text", () => {
    const result = normalizeForSearch("Crème Brûlée");

    expect(result).toBe("creme brulee");
  });

  it("Leaves a text that is already lowercase and without accents unchanged", () => {
    const result = normalizeForSearch("fideos tallarin");

    expect(result).toBe("fideos tallarin");
  });

  it("Keeps digits, spaces between words and punctuation", () => {
    const result = normalizeForSearch("Aceite 900 ml - Fino");

    expect(result).toBe("aceite 900 ml - fino");
  });

  it("Returns an empty string for an empty string", () => {
    const result = normalizeForSearch("");

    expect(result).toBe("");
  });
});
