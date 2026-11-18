import { formatMoney } from "./money.ts";

export function unitCost(totalCost: number, quantity: number): number {
  return totalCost / quantity;
}

export function stockAfterInbound(stock: number, quantity: number): number {
  return stock + quantity;
}

export function stockAfterInboundEdit(
  stock: number,
  oldQuantity: number,
  newQuantity: number,
): number {
  return stock - oldQuantity + newQuantity;
}

export function formatLastUnitCost(summary: {
  lastInboundQuantity: number | null;
  lastInboundTotalCost: number | null;
}): string {
  return summary.lastInboundQuantity === null ||
    summary.lastInboundTotalCost === null
    ? "Sin ingresos anteriores"
    : formatMoney(
        unitCost(summary.lastInboundTotalCost, summary.lastInboundQuantity),
        3,
      );
}

const quantityFormat = new Intl.NumberFormat("es-BO");

export function formatQuantity(quantity: number): string {
  return quantityFormat.format(quantity);
}
