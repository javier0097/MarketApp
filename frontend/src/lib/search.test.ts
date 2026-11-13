import { describe, expect, it } from "vitest";
import {
  findProductByCode,
  matchesSearchWords,
  normalizeForSearch,
  toSearchWords,
} from "./search.ts";

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

function sorted(words: string[]): string[] {
  return [...words].sort();
}

describe("toSearchWords", () => {
  it("Returns a single word as a list of one", () => {
    const result = toSearchWords("arroz");

    expect(result).toEqual(["arroz"]);
  });

  it("Splits a text into its words", () => {
    const result = toSearchWords("aceite de oliva extra");

    expect(sorted(result)).toEqual(sorted(["aceite", "de", "oliva", "extra"]));
  });

  it("Normalizes each word", () => {
    const result = toSearchWords("Café ORGÁNICO");

    expect(sorted(result)).toEqual(sorted(["cafe", "organico"]));
  });

  it("Keeps numbers and product codes as words", () => {
    const result = toSearchWords("aceite 900 7751234567890");

    expect(sorted(result)).toEqual(sorted(["aceite", "900", "7751234567890"]));
  });

  it("Ignores extra spaces at the start, at the end and between words", () => {
    const result = toSearchWords("  leche   entera  ");

    expect(sorted(result)).toEqual(sorted(["leche", "entera"]));
  });

  it.each([
    ["a tab", "\t"],
    ["a line break", "\n"],
    ["a Windows line break", "\r\n"],
    ["a non-breaking space", " "],
  ])("Treats %s as a separator between words", (_name, separator) => {
    const result = toSearchWords("leche" + separator + "entera");

    expect(sorted(result)).toEqual(sorted(["leche", "entera"]));
  });

  it("Ignores tabs, line breaks and non-breaking spaces at the start and at the end", () => {
    const result = toSearchWords("\t\n arroz \r\n\t");

    expect(result).toEqual(["arroz"]);
  });

  it("Returns an empty list for a text with only tabs, line breaks and non-breaking spaces", () => {
    const result = toSearchWords("\t\r\n  \n");

    expect(result).toEqual([]);
  });

  it("Returns an empty list for an empty string", () => {
    const result = toSearchWords("");

    expect(result).toEqual([]);
  });

  it("Returns an empty list for a text with only spaces", () => {
    const result = toSearchWords("     ");

    expect(result).toEqual([]);
  });
});

describe("findProductByCode", () => {
  const rice = { code: "ARZ-001", name: "Arroz Grano de Oro 1 kg" };
  const oil = { code: "7751234567890", name: "Aceite Fino 900 ml" };
  const sugar = { code: "AZU-002", name: "Azúcar Guabirá 1 kg" };
  const products = [rice, oil, sugar];

  it("Returns the product whose code matches the text", () => {
    const result = findProductByCode(products, "7751234567890");

    expect(result).toBe(oil);
  });

  it.each([
    ["lowercase", "arz-001"],
    ["mixed case", "aRz-001"],
  ])("Ignores the case of a text written in %s", (_name, text) => {
    const result = findProductByCode(products, text);

    expect(result).toBe(rice);
  });

  it("Ignores spaces at the start and at the end of the text", () => {
    const result = findProductByCode(products, "   7751234567890   ");

    expect(result).toBe(oil);
  });

  it("Returns null when no product has the code", () => {
    const result = findProductByCode(products, "7750000000000");

    expect(result).toBeNull();
  });

  it.each([
    ["only the start of a code", "7751"],
    ["a code with extra characters", "77512345678901"],
  ])("Returns null for a text that is %s", (_name, text) => {
    const result = findProductByCode(products, text);

    expect(result).toBeNull();
  });

  it("Returns null for an empty text", () => {
    const result = findProductByCode(products, "");

    expect(result).toBeNull();
  });

  it("Returns null for a text with only spaces", () => {
    const result = findProductByCode(products, "   ");

    expect(result).toBeNull();
  });

  it("Returns null when there is no list", () => {
    const result = findProductByCode(null, "7751234567890");

    expect(result).toBeNull();
  });
});

describe("matchesSearchWords", () => {
  const rice = { code: "ARZ-001", name: "Arroz Grano de Oro 1 kg" };

  it("Matches a word found in the name", () => {
    const result = matchesSearchWords(rice, ["arroz"]);

    expect(result).toBe(true);
  });

  it("Matches a word found in the code", () => {
    const result = matchesSearchWords(rice, ["arz-001"]);

    expect(result).toBe(true);
  });

  it("Matches the middle of a word in the name", () => {
    const result = matchesSearchWords(rice, ["ran"]);

    expect(result).toBe(true);
  });

  it("Matches part of the code", () => {
    const result = matchesSearchWords(rice, ["001"]);

    expect(result).toBe(true);
  });

  it("Matches when every word is in the name", () => {
    const result = matchesSearchWords(rice, ["arroz", "oro"]);

    expect(result).toBe(true);
  });

  it("Matches when some words are in the code and the others in the name", () => {
    const result = matchesSearchWords(rice, ["arz", "oro"]);

    expect(result).toBe(true);
  });

  it("Matches the words in any order", () => {
    const result = matchesSearchWords(rice, ["oro", "arroz"]);

    expect(result).toBe(true);
  });

  it("Ignores the accents and the case of the name", () => {
    const sugar = { code: "AZU-002", name: "Azúcar Guabirá 1 kg" };

    const result = matchesSearchWords(sugar, ["azucar", "guabira"]);

    expect(result).toBe(true);
  });

  it("Does not match when one of the words is missing", () => {
    const result = matchesSearchWords(rice, ["arroz", "integral"]);

    expect(result).toBe(false);
  });

  it("Does not match when no word is found", () => {
    const result = matchesSearchWords(rice, ["leche"]);

    expect(result).toBe(false);
  });

  it("Does not match a word made of the end of the code and the start of the name", () => {
    const result = matchesSearchWords(rice, ["001arroz"]);

    expect(result).toBe(false);
  });

  it("Matches any product when there are no words", () => {
    const result = matchesSearchWords(rice, []);

    expect(result).toBe(true);
  });
});
