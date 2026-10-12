import { ApiError, ValidationError } from "./errors.ts";

type ProblemDetails = {
  title?: string;
  detail?: string;
  errors?: Record<string, string[]>;
};

export async function request<T>(url: string, init?: RequestInit): Promise<T> {
  let response: Response;
  try {
    response = await fetch(url, init);
  } catch {
    throw new ApiError(
      "No se pudo conectar con el sistema. Verifique que el servidor esté iniciado.",
    );
  }

  if (response.ok) {
    return response.json();
  }

  const problem = await readProblem(response);
  if (response.status === 400 && problem?.errors) {
    throw new ValidationError(problem.errors);
  }

  if (response.status === 500 && problem?.title && problem.detail) {
    throw new ApiError(`${problem.title} ${problem.detail}`);
  }
  throw new ApiError("Ocurrió un error inesperado.");
}

async function readProblem(response: Response): Promise<ProblemDetails | null> {
  try {
    return await response.json();
  } catch {
    return null;
  }
}
