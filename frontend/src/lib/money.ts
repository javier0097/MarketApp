// Money travels as integer cents; these are the only conversions to and from it.

// Rounding absorbs the float error: 1.15 * 100 is 114.99999999999999, which rounds to 115
export function toCents(amount: number): number {
  return Math.round(amount * 100);
}

export function fromCents(cents: number): number {
  return cents / 100;
}

// The symbol is added by hand: Intl's currency style prints "Bs", "Bs." or "BOB" depending on the browser
export function formatMoney(cents: number, fractionDigits = 2): string {
  const numberFormat = new Intl.NumberFormat("es-BO", {
    minimumFractionDigits: fractionDigits,
    maximumFractionDigits: fractionDigits,
  });
  return `Bs ${numberFormat.format(fromCents(cents))}`;
}
