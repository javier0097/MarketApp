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
