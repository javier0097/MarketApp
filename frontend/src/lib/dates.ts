// Instants travel as UTC ISO strings; formatDateTime is the only conversion to local time.
// Calendar dates travel as "yyyy-MM-dd" and are never converted.

const dateTimeFormat = new Intl.DateTimeFormat("es-BO", {
  day: "2-digit",
  month: "2-digit",
  year: "numeric",
  hour: "2-digit",
  minute: "2-digit",
  hourCycle: "h23",
});

const longDateFormat = new Intl.DateTimeFormat("es-BO", {
  weekday: "long",
  day: "numeric",
  month: "long",
  year: "numeric",
});

export function formatDateTime(isoDate: string): string {
  return dateTimeFormat.format(new Date(isoDate));
}

export function today(): string {
  const now = new Date();
  const month = String(now.getMonth() + 1).padStart(2, "0");
  const day = String(now.getDate()).padStart(2, "0");
  return `${now.getFullYear()}-${month}-${day}`;
}

// Built from its parts as a local date: new Date("2026-11-03") would read it as UTC midnight, the day before in Bolivia
export function formatDate(date: string): string {
  const [year, month, day] = date.split("-").map(Number);
  return longDateFormat.format(new Date(year, month - 1, day));
}
