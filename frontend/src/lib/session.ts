export const savedProductKey = "savedProduct";
export const savedInboundKey = "savedInbound";

export function rememberInSession<T>(key: string, value: T) {
  sessionStorage.setItem(key, JSON.stringify(value));
}

export function readFromSession<T>(key: string): T | null {
  const saved = sessionStorage.getItem(key);
  return saved === null ? null : JSON.parse(saved);
}

export function clearFromSession(key: string) {
  sessionStorage.removeItem(key);
}
