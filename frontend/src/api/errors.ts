// A 400 from the API: Spanish messages grouped by field, ready to show next to each input
export class ValidationError extends Error {
  readonly errors: Record<string, string[]>;

  constructor(errors: Record<string, string[]>) {
    super("Validation failed");
    this.errors = errors;
  }
}
