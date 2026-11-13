export function unitCost(totalCost: number, quantity: number): number {
  return totalCost / quantity;
}

export function stockAfterInbound(stock: number, quantity: number): number {
  return stock + quantity;
}

const quantityFormat = new Intl.NumberFormat("es-BO");

export function formatQuantity(quantity: number): string {
  return quantityFormat.format(quantity);
}
