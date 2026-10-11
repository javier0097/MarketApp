// Money travels as integer cents; these are the only conversions to and from it.

const numberFormat = new Intl.NumberFormat("es-BO", {
  minimumFractionDigits: 2,
  maximumFractionDigits: 2,
});

// Rounding absorbs the float error: 1.15 * 100 is 114.99999999999999, which rounds to 115
export function toCents(amount: number): number {
  return Math.round(amount * 100);
}

// The symbol is added by hand: Intl's currency style prints "Bs", "Bs." or "BOB" depending on the browser
export function formatMoney(cents: number): string {
  return `Bs ${numberFormat.format(cents / 100)}`;
}
