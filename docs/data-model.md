# Data model

Two tables: `Products` and `InventoryMovements`. The entities live in
`backend/MarketApp.Api/Entities/`, the enum in `Enums/` and the mapping in
`Data/MarketAppDbContext.cs`.

## Product

| Field | Type | Notes |
| --- | --- | --- |
| `Id` | `int` | Auto-increment |
| `Name` | `string` | Required |
| `Code` | `string` | Required, unique. A barcode, or an internal code when the product has none |
| `SalePrice` | `int` | Cents |
| `MinStock` | `int` | Units. Below this, the product needs restocking |
| `IsActive` | `bool` | `true` by default. Queries that only want active products filter it explicitly |
| `CreatedAt` / `UpdatedAt` | `DateTime` | UTC |
| `CreatedById` / `UpdatedById` | `int?` | Null until authentication exists |

## InventoryMovement

| Field | Type | Notes |
| --- | --- | --- |
| `Id` | `int` | Auto-increment |
| `ProductId` | `int` | Foreign key to `Product`, indexed |
| `Type` | `MovementType` | `Inbound = 1`, `Outbound = 2`, stored as an integer |
| `Quantity` | `int` | Units, always greater than 0 |
| `Amount` | `int` | Cents, never negative. Cost on inbound, income on outbound, `0` when no money was involved |
| `MovementDate` | `DateTime` | UTC. When it happened; the user can edit it |
| `RecordedAt` | `DateTime` | UTC. When it was entered |
| `RecordedById` | `int?` | Null until authentication exists |
| `Note` | `string?` | |
| `IsVoided` | `bool` | `true` when the movement was cancelled |
| `VoidReason` | `string?` | Why it was voided |

## Decisions

**Stock and cost are not stored.** They are calculated by adding up the
movements that are not voided. There is a single source of truth, and any
figure can be traced back to the movements behind it.

**Money is stored as integer cents.** SQLite has no decimal type, and
floating point rounding errors in a finance app are silent and add up over
time. Amounts are converted to currency only when shown.

**Quantity is always positive.** Whether it adds or subtracts stock is given
by `Type`, so a sign can never be wrong.

**Two dates per movement.** `MovementDate` is when it happened and can be
edited, so movements written on paper while the app was unavailable can be
loaded later. `RecordedAt` is when it was entered.

**Nothing is deleted.** Products are deactivated with `IsActive`, and
movements are voided with a reason. The database backs this up: deleting a
product that has movements is rejected (`ON DELETE RESTRICT`).

**The database rejects invalid values.** Check constraints enforce
`Quantity > 0` and `Amount >= 0`. The API validates first; the constraints
catch whatever slips past it.

**Dates are UTC.** The services set them with `DateTime.UtcNow`. SQLite does
not store time zones, so `UtcDateTimeConverter` marks every date read from
the database as UTC; without it, the browser would show the time shifted.

**User columns are nullable** until authentication exists.

**`MovementType` values are explicit** (`1`, `2`) because they are stored as
numbers: reordering the enum must not change what is already saved.

## Database file and migrations

| Environment | Location |
| --- | --- |
| Development | `backend/MarketApp.Api/marketapp.db` |
| Store laptop | `C:\ProgramData\MarketApp\marketapp.db` |

Pending migrations are applied when the app starts (`Data/DatabaseStartup.cs`).
On the store laptop, the database is first backed up to
`C:\ProgramData\MarketApp\backups\`.

## Postponed on purpose

Unit of measure, price history, purchase orders, a stored stock field and an
adjustment movement type.
