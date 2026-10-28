import { afterEach, describe, expect, it, vi } from "vitest";
import { ApiError, ValidationError } from "./errors.ts";
import { request } from "./request.ts";

const UNEXPECTED_ERROR = "Ocurrió un error inesperado.";
const CONNECTION_ERROR =
  "No se pudo conectar con el sistema. Verifique que el servidor esté iniciado.";

interface Product {
  id: number;
  name: string;
  salePrice: number;
}

function stubFetch(response: Response) {
  const fetchMock = vi.fn<typeof fetch>().mockResolvedValue(response);
  vi.stubGlobal("fetch", fetchMock);
  return fetchMock;
}

function stubFetchFailure(error: unknown) {
  vi.stubGlobal("fetch", vi.fn<typeof fetch>().mockRejectedValue(error));
}

function jsonResponse(status: number, body: unknown): Response {
  return new Response(JSON.stringify(body), {
    status,
    headers: { "Content-Type": "application/json" },
  });
}

function htmlResponse(status: number): Response {
  return new Response("<html><body>Bad Gateway</body></html>", {
    status,
    headers: { "Content-Type": "text/html" },
  });
}

async function catchError(promise: Promise<unknown>): Promise<unknown> {
  try {
    await promise;
  } catch (error) {
    return error;
  }
  throw new Error("Expected the request to fail, but it succeeded.");
}

afterEach(() => {
  vi.unstubAllGlobals();
});

describe("request", () => {
  describe("on success", () => {
    it("returns the JSON body of a 200 response", async () => {
      const product: Product = { id: 7, name: "Arroz 1 kg", salePrice: 1250 };
      stubFetch(jsonResponse(200, product));

      const result = await request<Product>("/api/products/7");

      expect(result).toEqual(product);
    });

    it("returns the JSON body of a 201 response", async () => {
      const product: Product = { id: 8, name: "Azúcar 1 kg", salePrice: 980 };
      stubFetch(jsonResponse(201, product));

      const result = await request<Product>("/api/products", {
        method: "POST",
      });

      expect(result).toEqual(product);
    });

    it("returns a JSON array body", async () => {
      const products: Product[] = [
        { id: 1, name: "Arroz 1 kg", salePrice: 1250 },
        { id: 2, name: "Leche 1 L", salePrice: 800 },
      ];
      stubFetch(jsonResponse(200, products));

      const result = await request<Product[]>("/api/products");

      expect(result).toEqual(products);
    });
  });

  describe("call to fetch", () => {
    it("passes the url to fetch unchanged", async () => {
      const fetchMock = stubFetch(jsonResponse(200, {}));

      await request("/api/products?search=arroz");

      expect(fetchMock.mock.calls[0]?.[0]).toBe("/api/products?search=arroz");
    });

    it("passes the init options to fetch unchanged", async () => {
      const fetchMock = stubFetch(jsonResponse(201, {}));
      const init: RequestInit = {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ name: "Arroz 1 kg", salePrice: 1250 }),
      };

      await request("/api/products", init);

      expect(fetchMock.mock.calls[0]?.[1]).toBe(init);
    });

    it("calls fetch without init options when none are given", async () => {
      const fetchMock = stubFetch(jsonResponse(200, {}));

      await request("/api/products");

      expect(fetchMock.mock.calls[0]?.[1]).toBeUndefined();
    });

    it("calls fetch exactly once", async () => {
      const fetchMock = stubFetch(jsonResponse(200, {}));

      await request("/api/products");

      expect(fetchMock).toHaveBeenCalledTimes(1);
    });
  });

  describe("on a 400 response", () => {
    it("throws a ValidationError", async () => {
      stubFetch(
        jsonResponse(400, {
          errors: { salePrice: ["El precio de venta es obligatorio."] },
        }),
      );

      const error = await catchError(
        request("/api/products", { method: "POST" }),
      );

      expect(error).toBeInstanceOf(ValidationError);
    });

    it("puts the errors object from the body on the ValidationError", async () => {
      const errors = { salePrice: ["El precio de venta es obligatorio."] };
      stubFetch(
        jsonResponse(400, {
          title: "One or more validation errors occurred.",
          errors,
        }),
      );

      const error = await catchError(
        request("/api/products", { method: "POST" }),
      );

      expect((error as ValidationError).errors).toEqual(errors);
    });

    it("keeps every field and every message of the errors object", async () => {
      const errors = {
        name: ["El nombre es obligatorio."],
        barcode: [
          "El código de barras no es válido.",
          "El código de barras ya existe.",
        ],
      };
      stubFetch(jsonResponse(400, { errors }));

      const error = await catchError(
        request("/api/products", { method: "POST" }),
      );

      expect((error as ValidationError).errors).toEqual(errors);
    });

    it("throws an ApiError with the unexpected error message when the body is not JSON", async () => {
      stubFetch(htmlResponse(400));

      const error = await catchError(
        request("/api/products", { method: "POST" }),
      );

      expect(error).toBeInstanceOf(ApiError);
      expect((error as Error).message).toBe(UNEXPECTED_ERROR);
    });
  });

  describe("on a 500 response", () => {
    it("throws an ApiError", async () => {
      stubFetch(
        jsonResponse(500, {
          title: "Ocurrió un error en el servidor.",
          detail: "Referencia: 3F2A9C",
        }),
      );

      const error = await catchError(request("/api/products"));

      expect(error).toBeInstanceOf(ApiError);
    });

    it("uses the title and the detail separated by a space as the message", async () => {
      stubFetch(
        jsonResponse(500, {
          title: "Ocurrió un error en el servidor.",
          detail: "Referencia: 3F2A9C",
        }),
      );

      const error = await catchError(request("/api/products"));

      expect((error as Error).message).toBe(
        "Ocurrió un error en el servidor. Referencia: 3F2A9C",
      );
    });

    // A field made only of spaces is not empty: it is used as it comes, without trimming.
    it.each([
      {
        case: "the title is only spaces",
        body: { title: "  ", detail: "Referencia: 3F2A9C" },
        expected: "   Referencia: 3F2A9C",
      },
      {
        case: "the detail is only spaces",
        body: { title: "Ocurrió un error en el servidor.", detail: "  " },
        expected: "Ocurrió un error en el servidor.   ",
      },
    ])(
      "uses a field made only of spaces as it comes when $case",
      async ({ body, expected }) => {
        stubFetch(jsonResponse(500, body));

        const error = await catchError(request("/api/products"));

        expect(error).toBeInstanceOf(ApiError);
        expect((error as Error).message).toBe(expected);
      },
    );

    it.each([
      {
        case: "there is no detail",
        body: { title: "Ocurrió un error en el servidor." },
      },
      { case: "there is no title", body: { detail: "Referencia: 3F2A9C" } },
      { case: "there is neither title nor detail", body: {} },
      {
        case: "the detail is empty",
        body: { title: "Ocurrió un error en el servidor.", detail: "" },
      },
      {
        case: "the title is empty",
        body: { title: "", detail: "Referencia: 3F2A9C" },
      },
      {
        case: "both title and detail are empty",
        body: { title: "", detail: "" },
      },
    ])(
      "throws an ApiError with the unexpected error message when $case",
      async ({ body }) => {
        stubFetch(jsonResponse(500, body));

        const error = await catchError(request("/api/products"));

        expect(error).toBeInstanceOf(ApiError);
        expect((error as Error).message).toBe(UNEXPECTED_ERROR);
      },
    );

    it("throws an ApiError with the unexpected error message when the body is not JSON", async () => {
      stubFetch(htmlResponse(500));

      const error = await catchError(request("/api/products"));

      expect(error).toBeInstanceOf(ApiError);
      expect((error as Error).message).toBe(UNEXPECTED_ERROR);
    });

    it("throws an ApiError with the unexpected error message when the body is empty", async () => {
      stubFetch(new Response(null, { status: 500 }));

      const error = await catchError(request("/api/products"));

      expect(error).toBeInstanceOf(ApiError);
      expect((error as Error).message).toBe(UNEXPECTED_ERROR);
    });
  });

  describe("on any other error status", () => {
    // The body carries title, detail and errors, so a status handled like 400 or 500 would show them.
    it.each([401, 403, 404, 409, 422, 501, 502, 503])(
      "throws an ApiError with the unexpected error message for status %i",
      async (status) => {
        stubFetch(
          jsonResponse(status, {
            title: "Título del servidor",
            detail: "Detalle del servidor",
            errors: { name: ["El nombre es obligatorio."] },
          }),
        );

        const error = await catchError(request("/api/products"));

        expect(error).toBeInstanceOf(ApiError);
        expect((error as Error).message).toBe(UNEXPECTED_ERROR);
      },
    );
  });

  describe("on a successful response without a JSON body", () => {
    it("throws an ApiError with the unexpected error message for a 204 without body", async () => {
      stubFetch(new Response(null, { status: 204 }));

      const error = await catchError(
        request("/api/products/7", { method: "DELETE" }),
      );

      expect(error).toBeInstanceOf(ApiError);
      expect((error as Error).message).toBe(UNEXPECTED_ERROR);
    });

    it("throws an ApiError with the unexpected error message for a 200 whose body is not JSON", async () => {
      stubFetch(htmlResponse(200));

      const error = await catchError(request("/api/products"));

      expect(error).toBeInstanceOf(ApiError);
      expect((error as Error).message).toBe(UNEXPECTED_ERROR);
    });

    it("throws an ApiError with the unexpected error message for a 200 with an empty body", async () => {
      stubFetch(
        new Response("", {
          status: 200,
          headers: { "Content-Type": "application/json" },
        }),
      );

      const error = await catchError(request("/api/products"));

      expect(error).toBeInstanceOf(ApiError);
      expect((error as Error).message).toBe(UNEXPECTED_ERROR);
    });
  });

  describe("when fetch fails", () => {
    it("throws an ApiError with the connection message when the server cannot be reached", async () => {
      stubFetchFailure(new TypeError("Failed to fetch"));

      const error = await catchError(request("/api/products"));

      expect(error).toBeInstanceOf(ApiError);
      expect((error as Error).message).toBe(CONNECTION_ERROR);
    });

    it("throws an ApiError with the connection message when the request is aborted", async () => {
      stubFetchFailure(
        new DOMException("The operation was aborted.", "AbortError"),
      );

      const error = await catchError(request("/api/products"));

      expect(error).toBeInstanceOf(ApiError);
      expect((error as Error).message).toBe(CONNECTION_ERROR);
    });

    it("throws an ApiError with the connection message for any other error thrown by fetch", async () => {
      stubFetchFailure(new Error("Something else went wrong"));

      const error = await catchError(request("/api/products"));

      expect(error).toBeInstanceOf(ApiError);
      expect((error as Error).message).toBe(CONNECTION_ERROR);
    });
  });
});
