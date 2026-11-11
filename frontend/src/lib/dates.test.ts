import {
  afterAll,
  afterEach,
  beforeEach,
  describe,
  expect,
  it,
  vi,
} from "vitest";
import { formatDate, formatDateTime, today } from "./dates.ts";

// "Local time" is the time zone of the device running the app. The Vitest
// config fixes it to America/La_Paz, so the results do not depend on the
// machine running them.
describe("formatDateTime in America/La_Paz", () => {
  it("Formats_a_utc_date_as_local_day_month_year_hours_and_minutes", () => {
    const result = formatDateTime("2026-11-08T22:05:30.1234567Z");

    expect(result).toBe("08/11/2026, 18:05");
  });

  it("Formats_a_date_without_fractional_seconds", () => {
    const result = formatDateTime("2026-11-08T22:05:30Z");

    expect(result).toBe("08/11/2026, 18:05");
  });

  it("Pads_the_day_and_the_month_with_a_leading_zero", () => {
    const result = formatDateTime("2026-03-05T15:00:00Z");

    expect(result).toBe("05/03/2026, 11:00");
  });

  it("Pads_the_hour_and_the_minute_with_a_leading_zero", () => {
    const result = formatDateTime("2026-11-08T13:07:00Z");

    expect(result).toBe("08/11/2026, 09:07");
  });

  it("Shows_afternoon_hours_on_a_24_hour_clock", () => {
    const result = formatDateTime("2026-11-08T17:30:00Z");

    expect(result).toBe("08/11/2026, 13:30");
  });

  it("Shows_midnight_as_00_on_the_new_day", () => {
    const result = formatDateTime("2026-11-09T04:00:00Z");

    expect(result).toBe("09/11/2026, 00:00");
  });

  it("Moves_to_the_previous_day_when_the_local_time_is_still_the_day_before", () => {
    const result = formatDateTime("2026-11-09T02:30:00Z");

    expect(result).toBe("08/11/2026, 22:30");
  });

  it("Drops_seven_fractional_digits_instead_of_rounding_them_to_the_next_minute", () => {
    const result = formatDateTime("2026-11-08T22:05:59.9999999Z");

    expect(result).toBe("08/11/2026, 18:05");
  });
});

describe("formatDateTime in another time zone", () => {
  afterAll(() => {
    vi.unstubAllEnvs();
  });

  // Asia/Tokyo is UTC+9 all year: 22:05 UTC on 08/11 is 07:05 on 09/11.
  it("Uses_the_time_zone_of_the_device_running_it", async () => {
    // Imported again after the change, in case the module builds its formatter on load
    vi.resetModules();
    vi.stubEnv("TZ", "Asia/Tokyo");
    const { formatDateTime } = await import("./dates.ts");

    const result = formatDateTime("2026-11-08T22:05:30.1234567Z");

    expect(result).toBe("09/11/2026, 07:05");
  });
});

// The clock is fixed on dates far from the day the tests run, so a function that
// ignored it could not pass by coincidence. America/La_Paz is UTC-4 all year:
// a local day runs from 04:00 UTC to 03:59:59.999 UTC of the next day.
describe("today in America/La_Paz", () => {
  beforeEach(() => {
    vi.useFakeTimers({ toFake: ["Date"] });
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  it("Returns_the_local_date_as_year_month_day", () => {
    vi.setSystemTime(new Date("2025-07-14T15:00:00Z"));

    const result = today();

    expect(result).toBe("2025-07-14");
  });

  it("Pads_the_month_and_the_day_with_a_leading_zero", () => {
    vi.setSystemTime(new Date("2025-03-05T15:00:00Z"));

    const result = today();

    expect(result).toBe("2025-03-05");
  });

  it("Keeps_the_same_date_until_the_last_millisecond_of_the_local_day", () => {
    vi.setSystemTime(new Date("2025-07-15T03:59:59.999Z"));

    const result = today();

    expect(result).toBe("2025-07-14");
  });

  it("Changes_the_date_at_local_midnight", () => {
    vi.setSystemTime(new Date("2025-07-15T04:00:00Z"));

    const result = today();

    expect(result).toBe("2025-07-15");
  });

  it("Returns_the_last_day_of_the_year_when_it_is_already_new_year_in_utc", () => {
    vi.setSystemTime(new Date("2026-01-01T03:59:00Z"));

    const result = today();

    expect(result).toBe("2025-12-31");
  });
});

describe("today in another time zone", () => {
  beforeEach(() => {
    vi.useFakeTimers({ toFake: ["Date"] });
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  afterAll(() => {
    vi.unstubAllEnvs();
  });

  // Asia/Tokyo is UTC+9 all year: 15:00 UTC on 2025-07-14 is 00:00 on 2025-07-15.
  it("Uses_the_time_zone_of_the_device_running_it", async () => {
    vi.resetModules();
    vi.stubEnv("TZ", "Asia/Tokyo");
    const { today } = await import("./dates.ts");
    vi.setSystemTime(new Date("2025-07-14T15:00:00Z"));

    const result = today();

    expect(result).toBe("2025-07-15");
  });
});

describe("formatDate", () => {
  it("Formats_a_date_as_weekday_day_month_and_year_in_spanish", () => {
    const result = formatDate("2026-11-08");

    expect(result).toBe("domingo, 8 de noviembre de 2026");
  });
});
