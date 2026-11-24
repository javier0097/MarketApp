import { describe, expect, it } from "vitest";
import {
  formatLastUnitCost,
  formatQuantity,
  stockAfterInboundEdit,
} from "./inventory.ts";

describe("formatQuantity", () => {
  it("formats a quantity below ten without separator", () => {
    const quantity = 5;

    const text = formatQuantity(quantity);

    expect(text).toBe("5");
  });

  it("does not add a thousands separator below one thousand", () => {
    const quantity = 999;

    const text = formatQuantity(quantity);

    expect(text).toBe("999");
  });

  it("adds a dot as thousands separator to a four-digit quantity", () => {
    const quantity = 1000;

    const text = formatQuantity(quantity);

    expect(text).toBe("1.000");
  });

  it("separates every group of three digits in large quantities", () => {
    const quantity = 1234567;

    const text = formatQuantity(quantity);

    expect(text).toBe("1.234.567");
  });
});

describe("stockAfterInboundEdit", () => {
  it("adds only the difference when the inbound quantity is raised", () => {
    const stock = 20;
    const oldQuantity = 10;
    const newQuantity = 15;

    const result = stockAfterInboundEdit(stock, oldQuantity, newQuantity);

    expect(result).toBe(25);
  });

  it("returns a negative stock when more was sold than the lowered quantity allows", () => {
    const stock = 2;
    const oldQuantity = 10;
    const newQuantity = 5;

    const result = stockAfterInboundEdit(stock, oldQuantity, newQuantity);

    expect(result).toBe(-3);
  });

  it("starts from a stock that is already negative", () => {
    const stock = -3;
    const oldQuantity = 5;
    const newQuantity = 12;

    const result = stockAfterInboundEdit(stock, oldQuantity, newQuantity);

    expect(result).toBe(4);
  });
});

describe("formatLastUnitCost", () => {
  it("shows that there are no previous inbounds when the quantity is null", () => {
    const summary = { lastInboundQuantity: null, lastInboundTotalCost: 1250 };

    const text = formatLastUnitCost(summary);

    expect(text).toBe("Sin ingresos anteriores");
  });

  it("shows that there are no previous inbounds when the total cost is null", () => {
    const summary = { lastInboundQuantity: 10, lastInboundTotalCost: null };

    const text = formatLastUnitCost(summary);

    expect(text).toBe("Sin ingresos anteriores");
  });

  it("divides the total cost by the quantity", () => {
    const summary = { lastInboundQuantity: 10, lastInboundTotalCost: 1250 };

    const text = formatLastUnitCost(summary);

    expect(text).toBe("Bs 1,250");
  });

  it("rounds down when the fourth decimal is below five", () => {
    const summary = { lastInboundQuantity: 3, lastInboundTotalCost: 1000 };

    const text = formatLastUnitCost(summary);

    expect(text).toBe("Bs 3,333");
  });

  it("rounds up when the fourth decimal is above five", () => {
    const summary = { lastInboundQuantity: 3, lastInboundTotalCost: 2000 };

    const text = formatLastUnitCost(summary);

    expect(text).toBe("Bs 6,667");
  });

  it("rounds an exact half up", () => {
    const summary = { lastInboundQuantity: 20, lastInboundTotalCost: 25 };

    const text = formatLastUnitCost(summary);

    expect(text).toBe("Bs 0,013");
  });

  it("shows a zero unit cost when the total cost is zero", () => {
    const summary = { lastInboundQuantity: 5, lastInboundTotalCost: 0 };

    const text = formatLastUnitCost(summary);

    expect(text).toBe("Bs 0,000");
  });

  it("adds a dot as thousands separator to a unit cost of a thousand bolivianos or more", () => {
    const summary = { lastInboundQuantity: 10, lastInboundTotalCost: 12345600 };

    const text = formatLastUnitCost(summary);

    expect(text).toBe("Bs 12.345,600");
  });
});
