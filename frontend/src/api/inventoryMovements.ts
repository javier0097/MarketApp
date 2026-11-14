import { request } from "./request.ts";

export type CreateInboundMovementRequest = {
  productId: number | null;
  quantity: number | null;
  totalCost: number | null;
  movementDate: string | null;
  note: string;
};

export type InboundMovementResponse = {
  id: number;
  productId: number;
  quantity: number;
  totalCost: number;
  movementDate: string;
  note: string | null;
  createdAt: string;
};

export function createInboundMovement(
  movement: CreateInboundMovementRequest,
): Promise<InboundMovementResponse> {
  return request("/api/inventory-movements/inbound", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(movement),
  });
}

export type StockSummaryResponse = {
  stock: number;
  lastInboundQuantity: number | null;
  lastInboundTotalCost: number | null;
};

export function getStockSummary(
  productId: number,
  signal?: AbortSignal,
): Promise<StockSummaryResponse> {
  return request(
    `/api/inventory-movements/stock-summary?productId=${productId}`,
    { signal },
  );
}

export type MovementType = "Inbound" | "Outbound";

export type InventoryMovementResponse = {
  id: number;
  productId: number;
  productCode: string;
  productName: string;
  type: MovementType;
  quantity: number;
  amount: number;
  movementDate: string;
  note: string | null;
  createdAt: string;
};

export function getInventoryMovements(
  signal?: AbortSignal,
): Promise<InventoryMovementResponse[]> {
  return request("/api/inventory-movements", { signal });
}
