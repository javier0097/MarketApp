// Dates travel as UTC ISO strings; this is the only conversion to local time.

const dateTimeFormat = new Intl.DateTimeFormat("es-BO", {
  day: "2-digit",
  month: "2-digit",
  year: "numeric",
  hour: "2-digit",
  minute: "2-digit",
  hourCycle: "h23",
});

export function formatDateTime(isoDate: string): string {
  return dateTimeFormat.format(new Date(isoDate));
}
