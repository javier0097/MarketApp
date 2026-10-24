import { describe, expect, it } from "vitest";
import { formatMoney, toCents } from "./money.ts";

// Only non-negative amounts with at most two decimals reach this function.
describe("toCents", () => {
  it("converts an amount with two decimals to cents", () => {
    const amount = 12.34;

    const cents = toCents(amount);

    expect(cents).toBe(1234);
  });

  it("converts an amount with one decimal to cents", () => {
    const amount = 12.5;

    const cents = toCents(amount);

    expect(cents).toBe(1250);
  });

  it("converts a whole amount to cents", () => {
    const amount = 12;

    const cents = toCents(amount);

    expect(cents).toBe(1200);
  });

  it("converts zero to zero cents", () => {
    const amount = 0;

    const cents = toCents(amount);

    expect(cents).toBe(0);
  });

  it.each([
    { amount: 0.01, expected: 1 },
    { amount: 0.1, expected: 10 },
    { amount: 0.99, expected: 99 },
  ])(
    "converts an amount below one boliviano: $amount -> $expected",
    ({ amount, expected }) => {
      const cents = toCents(amount);

      expect(cents).toBe(expected);
    },
  );

  // In binary floating point, amount * 100 lands just below the right value
  // (19.99 * 100 === 1998.9999999999998), so cutting off the decimals loses a cent.
  it.each([
    { amount: 19.99, expected: 1999 },
    { amount: 0.29, expected: 29 },
    { amount: 0.57, expected: 57 },
    { amount: 1.15, expected: 115 },
    { amount: 4.35, expected: 435 },
  ])(
    "returns the exact cents when multiplying by 100 falls just below them: $amount -> $expected",
    ({ amount, expected }) => {
      const cents = toCents(amount);

      expect(cents).toBe(expected);
    },
  );

  // Here amount * 100 lands just above the right value
  // (0.07 * 100 === 7.000000000000001), so rounding up adds a cent.
  it.each([
    { amount: 0.07, expected: 7 },
    { amount: 1.1, expected: 110 },
  ])(
    "returns the exact cents when multiplying by 100 falls just above them: $amount -> $expected",
    ({ amount, expected }) => {
      const cents = toCents(amount);

      expect(cents).toBe(expected);
    },
  );

  it.each([
    { amount: 99999.99, expected: 9999999 },
    { amount: 1234567.89, expected: 123456789 },
  ])(
    "converts a large amount to cents: $amount -> $expected",
    ({ amount, expected }) => {
      const cents = toCents(amount);

      expect(cents).toBe(expected);
    },
  );
});

describe("formatMoney", () => {
  it("formats cents as bolivianos with two decimals", () => {
    const cents = 1234;

    const text = formatMoney(cents);

    expect(text).toBe("Bs 12,34");
  });

  it("keeps the trailing zero of the cents", () => {
    const cents = 1250;

    const text = formatMoney(cents);

    expect(text).toBe("Bs 12,50");
  });

  it("shows two zero decimals for a whole amount", () => {
    const cents = 1200;

    const text = formatMoney(cents);

    expect(text).toBe("Bs 12,00");
  });

  it("formats zero cents", () => {
    const cents = 0;

    const text = formatMoney(cents);

    expect(text).toBe("Bs 0,00");
  });

  it.each([
    { cents: 1, expected: "Bs 0,01" },
    { cents: 10, expected: "Bs 0,10" },
    { cents: 99, expected: "Bs 0,99" },
  ])(
    "shows a leading zero for amounts below one boliviano: $cents -> $expected",
    ({ cents, expected }) => {
      const text = formatMoney(cents);

      expect(text).toBe(expected);
    },
  );

  it("formats exactly one boliviano", () => {
    const cents = 100;

    const text = formatMoney(cents);

    expect(text).toBe("Bs 1,00");
  });

  it("does not add a thousands separator below one thousand", () => {
    const cents = 99999;

    const text = formatMoney(cents);

    expect(text).toBe("Bs 999,99");
  });

  it.each([
    { cents: 100000, expected: "Bs 1.000,00" },
    { cents: 123456, expected: "Bs 1.234,56" },
    { cents: 999999, expected: "Bs 9.999,99" },
  ])(
    "adds a dot as thousands separator from one thousand: $cents -> $expected",
    ({ cents, expected }) => {
      const text = formatMoney(cents);

      expect(text).toBe(expected);
    },
  );

  it.each([
    { cents: 10000000, expected: "Bs 100.000,00" },
    { cents: 99999999, expected: "Bs 999.999,99" },
    { cents: 100000000, expected: "Bs 1.000.000,00" },
    { cents: 123456789, expected: "Bs 1.234.567,89" },
  ])(
    "separates every group of three digits in large amounts: $cents -> $expected",
    ({ cents, expected }) => {
      const text = formatMoney(cents);

      expect(text).toBe(expected);
    },
  );

  it("separates the currency from the amount with a regular space", () => {
    const cents = 1250;

    const text = formatMoney(cents);

    expect(text.slice(0, 3)).toBe("Bs ");
  });

  it("formats back an amount converted with toCents", () => {
    const cents = toCents(19.99);

    const text = formatMoney(cents);

    expect(text).toBe("Bs 19,99");
  });
});
