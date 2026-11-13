import { describe, expect, it } from "vitest";
import { formatQuantity } from "./inventory.ts";

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
