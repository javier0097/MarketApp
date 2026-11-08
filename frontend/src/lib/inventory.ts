export function unitCost(totalCost: number, quantity: number): number {
  return totalCost / quantity;
}

export function stockAfterInbound(stock: number, quantity: number): number {
  return stock + quantity;
}
