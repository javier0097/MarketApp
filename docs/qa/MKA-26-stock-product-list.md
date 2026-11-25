# QA report: Product list (`Productos`), new `Stock` column

- **Date:** 2026-11-24 (session 22:31–22:37, local time)
- **URL:** http://localhost:5130/productos (stock source: http://localhost:5130/inventario/movimientos; ingresos created at http://localhost:5130/inventario/ingresar)
- **Focus:** the new `Stock` column only (the rest of the list was tested before).
- **Data that already existed when I started:**
  - `102 productos`. 14 had stock > 0: Arroz Grano de Oro 1kg `7772106000026` (137), Huevo unidad `TMV00003` (400), Bolsa plastica `7772106000064` (100), Pan marraqueta `TMV00001` (74), Fideo Lazzaroni 400g `7772106000071` (70), Tomate kg `TMV00005` (41), Cebolla kg `TMV00004` (20), Sal Cumbre 1kg `TMV00089` (17), Papa kg `TMV00006` (12), Leche PIL entera 1L `7772106000040` (12), Leche PIL entera 1L `7772106000019` (9), Café Copacabana 250g `TMV00008` (9), Leche PIL entera 1L `TMV00090` (6), Galletas María… `7772106000088` (5). All the others showed `0`.
  - 29 inventory movements (ids 1–29), all of type `Ingreso`, all whole-number quantities. One of them has `Fecha del movimiento` `01/01/1900` (Sal Cumbre 1kg). There were no `Salida` movements.
  - Several products share a name: three `Leche PIL entera 1L` (`TMV00090`, `7772106000040`, `7772106000019`) and two `Arroz Grano de Oro 1kg` (`TMV00082`, `7772106000026`).
  - At the start, the stock of every product matched the sum of its movements.
- **Data I created or changed during the session:**
  - Ingresos: Zanahoria kg +10 (then edited to 4), +1, +7; Tomate kg +3; Bolsa plastica +1.000; Huevo unidad +30; QA vol 01 `TMV00041` +99.999 three times.
  - Product Zanahoria kg (id 12): its code changed from `TMV00007` to `7779990000040`, then back to "Sin código de barras". It now has the code **`TMV00091`**, not its original `TMV00007`.

## Summary

7 findings: **1 High, 3 Medium (+1 Medium outside this screen; the High is also outside this screen), 2 Low.**

The stock calculation itself is correct. For every product, the `Stock` column matched the sum of its movements. It updated after new ingresos, after editing a movement's quantity, after a double click on save (no duplicate), after changing the product's name and code, and on browser Back. Thousands are shown as `1.100` and `299.997`.

Most important:

- **F1 (High, outside this screen, `Ingresar stock`):** `Cantidad` silently drops the decimal separator. `2,5` becomes `25`, and `2.5` becomes `2`. For products sold by kg, the stock in the list ends up wrong with no warning.
- **F2 (Medium):** products with the same name each show their own stock, and nothing marks them as the same item. Searching "Arroz" shows `0` in the first row, while 137 units are in stock under the other code.
- **F3 (Medium):** a list left open never updates its stock, and nothing says when it was loaded. Searching filters the old figures.
- **F4 (Medium):** checking stock by scanning products one after another does not work. The second scan is appended to the first one, and the list says no product matches.
- **F7 (Medium, outside this screen, `Ingresar stock`):** after scanning a product, the scanner's Enter shows red errors on the fields the user has not reached yet.

Also checked, no problem found: a movement dated `01/01/1900` counts in the stock. A future date is rejected (`La fecha no puede ser posterior a hoy.`). Quantity 0 is rejected (`La cantidad debe estar entre 1 y 99.999.`). A minus sign cannot be typed, so stock cannot go negative from the UI. Three ingresos of 99.999 add up correctly. The column stays visible at a 1024 px wide window.

## Findings

### F1. Outside this screen, form at `/inventario/ingresar`: `Cantidad` silently turns `2,5` into `25` and `2.5` into `2`, so the stock in the list is wrong

- **Severity:** High
- **Origin:** `own testing`
- **Relevant data:** Tomate kg `TMV00005`, stock 41. Papa kg `TMV00006`, stock 12. Several products are sold by kilo (`Papa kg`, `Tomate kg`, `Cebolla kg`, `Zanahoria kg`).
- **Steps:**
  1. Open `/inventario/ingresar`.
  2. In `Producto`, type `TMV00005` and press Enter.
  3. In `Cantidad`, type `2,5` (key by key or pasted; both behave the same).
  4. Variation: with product `TMV00006`, paste `2.5` into `Cantidad`.
- **Expected:** Either the decimal is accepted (2,5 kg) or a clear message says that only whole units are allowed. The typed value should never be changed into a different number without telling the user.
- **Actual:** The field silently shows `25` for `2,5`, and `2` for `2.5`. There is no message. The only hint is the `Resumen` line `Stock: 41 → 66` (or `Stock: 12 → 14`). If saved, the `Stock` column in `/productos` shows 66 instead of 43,5 (ten times the intended amount), with no sign that something went wrong. I did not save these values. A user loading fast with the keyboard would not read the summary.

### F2. Products with the same name show separate stock figures, and the first row can say `0` while there is stock

- **Severity:** Medium
- **Origin:** `own testing`
- **Relevant data:** Products that already existed: `Arroz Grano de Oro 1kg` `TMV00082` (Bs 42,00, stock 0, updated 01/11/2026) and `Arroz Grano de Oro 1kg` `7772106000026` (Bs 12,90, stock 137). Three `Leche PIL entera 1L`: `TMV00090` (6), `7772106000040` (12), `7772106000019` (9).
- **Steps:**
  1. Open `/productos`.
  2. In `Buscar`, type `Arroz` (and then `Leche`).
- **Expected:** To answer "how much rice do I have?" the owner can tell these rows apart, or at least sees that the item appears more than once. Otherwise the first row is enough to decide.
- **Actual:** The list shows `2 de 102 productos`. The first row (the most recently updated) is `TMV00082 · Arroz Grano de Oro 1kg · Bs 42,00 · 0`. The 137 units sit in the second row under another code. For `Leche` the stock is split 6 / 12 / 9 over three identical names, with no total. Before this column the duplicates only affected the price. Now a user who reads the first row would think the product is out of stock (or has 6 units instead of 27).

### F3. A list left open never updates its stock and does not say when it was loaded

- **Severity:** Medium
- **Origin:** `own testing` (also listed in web research as "stale data")
- **Relevant data:** Huevo unidad `TMV00003`, stock 400.
- **Steps:**
  1. Open `/productos` in a browser tab and leave it open.
  2. In another tab, open `/inventario/ingresar` and save an ingreso of 30 for `TMV00003`. It shows `Ingreso guardado` · `Huevo unidad · 30 u. · Bs 27,00 · …`.
  3. Go back to the first tab and type `Huevo` in `Buscar`.
  4. Reload the page.
- **Expected:** The list shows 430. If it can't refresh on its own, it at least shows when the figures were loaded, so the user knows to reload.
- **Actual:** Step 3 shows `1 de 102 productos` with stock `400`. Only after reloading (step 4) does it show `430`. Searching does not refresh the data. Nothing on the screen tells the user that the figure may be out of date. Keeping the list open in one tab and the ingreso form in another is a natural way to work when loading stock.

### F4. Checking stock by scanning products one after another: the second scan is appended to the first

- **Severity:** Medium
- **Origin:** `own testing`
- **Relevant data:** Arroz Grano de Oro 1kg `7772106000026` (137) and Bolsa plastica `7772106000064` (1.100).
- **Steps:**
  1. Open `/productos`.
  2. With the cursor in `Buscar`, type `7772106000026` and press Enter, as the scanner does.
  3. Without touching anything, type `7772106000064` and press Enter (the second scan).
- **Expected:** Each scan looks up the scanned product, so the owner can check the stock of several products in a row with the scanner. The `Ingresar stock` form already works like this: it clears the field after each item.
- **Actual:** Step 2 works (`1 de 102 productos`, stock `137`). In step 3 the field contains `77721060000267772106000064`, and the list shows `Ningún producto coincide con «77721060000267772106000064». Revise lo escrito o busque por otra parte del nombre o del código.` The user has to click `Limpiar búsqueda` or delete the text before each scan. The message suggests a typing mistake, which the user did not make.

### F5. The `Stock` column has no unit, and products sold by kilo only accept whole "units"

- **Severity:** Low
- **Origin:** `own testing`
- **Relevant data:** Papa kg `TMV00006` (12), Tomate kg `TMV00005` (44), Zanahoria kg (12).
- **Steps:**
  1. Open `/productos` and look at `Papa kg` → `Stock` `12`.
  2. Open `/inventario/ingresar` and save an ingreso for `TMV00005` with quantity 3. The confirmation says `Tomate kg · 3 u. · …`.
- **Expected:** The user can tell whether `12` means 12 kg or 12 units (bags, boxes).
- **Actual:** The header is only `Stock` and the value is a bare number. The ingreso confirmation calls it `u.` (units) even for products named "kg". Together with F1, the owner cannot record or read kilos reliably.

### F6. `Actualizado` does not change when the stock changes, which suggests the stock figure is old

- **Severity:** Low
- **Origin:** `own testing`
- **Relevant data:** Huevo unidad `TMV00003`, which received an ingreso today (24/11/2026).
- **Steps:**
  1. Save an ingreso for `TMV00003` today.
  2. Open `/productos` and find `Huevo unidad`.
- **Expected:** The user can tell how current the stock is, or the column next to `Stock` makes clear that it refers to the product's data only.
- **Actual:** The row shows `430` and `Actualizado` `25/10/2026, 15:34`. The product does not move up the list (the list is ordered by `Actualizado`). Next to the new column, the date can easily be read as "stock as of 25/10", and the products whose stock just changed are not the ones at the top.

### F7. Outside this screen, form at `/inventario/ingresar`: scanning the product (code + Enter) immediately shows errors on fields the user has not reached

- **Severity:** Medium
- **Origin:** `own testing`
- **Relevant data:** Any existing product, e.g. Zanahoria kg, Papa kg `TMV00006`.
- **Steps:**
  1. Open `/inventario/ingresar`.
  2. In `Producto`, type a code and press Enter, as the barcode scanner does.
  3. Variation: type the code and press Tab instead.
- **Expected:** The product is selected and the cursor moves to `Cantidad` with no errors. Errors only appear after trying to save.
- **Actual:** With Enter, the product is selected (`Elegido: …`), but `Cantidad` and `Costo total` are immediately marked invalid with `Ingrese la cantidad de unidades del lote.` and `Ingrese el costo total del lote.`. With Tab, no errors appear. Since the scanner always sends Enter, every scanned product starts with red error messages, so the user cannot tell real errors from these. Reproduced on every scan during the session (5+ products).

## Manual test cases

### M1. Checking stock with the real scanner in the product list

- **Preconditions:** USB barcode scanner connected and configured to send Enter after the code. At least two products with a barcode and stock > 0 (e.g. `7772106000026`, `7772106000064`).
- **Steps:**
  1. Open `/productos` and click in `Buscar`.
  2. Scan the first product.
  3. Without touching the keyboard or mouse, scan the second product.
  4. Scan a product that is not registered.
- **Expected:** After step 2 only that product is shown with its correct stock. After step 3 the second product replaces the first (no concatenated codes, see F4). Step 4 shows a clear "not found" message. No step leaves the page or opens another screen.

### M2. Server stops while the product list is loading or open

- **Preconditions:** The app running with products that have stock > 0.
- **Steps:**
  1. Stop the server.
  2. Open (or reload) `/productos`.
  3. Start the server again and reload.
  4. Variation: open `/productos`, stop the server, then type in `Buscar`.
- **Expected:** With the server down, the list shows a clear message in Spanish saying that the data could not be loaded. It never shows a list with stock `0` or blank stock that could be taken for real figures. After step 3 the correct stock shows again.

### M3. Server stops while saving an ingreso, and stock in the list afterwards

- **Preconditions:** A product with a known stock (note it from `/productos`).
- **Steps:**
  1. Open `/inventario/ingresar`, scan the product, and type a quantity and cost.
  2. Stop the server and press Enter or `Guardar ingreso`.
  3. Start the server again and press `Guardar ingreso` again.
  4. Open `/productos` and `/inventario/movimientos` (search by the product).
- **Expected:** Step 2 shows a clear error and keeps the data typed. After step 3 there is exactly one new movement, and the stock in the list equals the previous stock plus the quantity, not twice the quantity.

### M4. Browser or laptop closes right after saving an ingreso

- **Preconditions:** A product with a known stock.
- **Steps:**
  1. Save an ingreso and, as soon as `Ingreso guardado` appears, close the browser (or turn off the laptop).
  2. Variation: close the browser right after pressing `Guardar ingreso`, before the confirmation appears.
  3. Open the app again, then open `/productos` and `/inventario/movimientos`.
- **Expected:** The stock in `/productos` always equals the sum of the movements listed for that product. In the variation, the ingreso is either saved completely (movement plus stock) or not at all, and the user can tell which one by looking at `Movimientos de inventario`.

### M5. Real scanner in `Ingresar stock`: no premature errors (F7)

- **Preconditions:** Scanner configured with an Enter suffix. Also, if possible, with a Tab suffix.
- **Steps:**
  1. Open `/inventario/ingresar` and scan a product.
  2. Look at `Cantidad` and `Costo total` before typing anything.
  3. Load 10 products in a row with the scanner and the keyboard.
- **Expected:** After each scan the cursor is in `Cantidad` with no error messages. Every ingreso is saved, and the stock in `/productos` increases by exactly the quantities typed.

## Resolution

To be filled in by the developer after triage.

| Finding | Decision (fixed / ticket / discarded) | Ticket or reason |
| --- | --- | --- |
| F1 | discarded | Same as F2 of the #18 QA report (`MKA-18-stock-form.md`) and F3 of the #25 one: quantities are whole units by decision (unit of measure postponed), and products sold by weight are registered in whole grams. A comma is dropped (`2,5` → `25`) and a point cuts what follows it (`2.5` → `2`), but both results show in the field and in the summary before saving. |
| F2 | discarded | Each row is a different product with its own code and its own stock; adding up everything a search matches would mix different products. Products registered twice with the same name are a data problem, which #12 addresses by showing similar products while a new one is being created. |
| F3 | discarded | Same as F3 of the #23 QA report (`MKA-23-inventory-movements-list.md`): real-time updates are not needed, one person loads stock at a time, and the list loads again when it is opened, reloaded or reached with the back button. |
| F4 | discarded | Same as F1 of the #11 QA report (`MKA-11-product-list.md`): no data is affected and both codes are visible in the message. The scanner sends no Enter today; if it is set to send one, selecting the search text on Enter would let the next scan replace it. |
| F5 | discarded | The stock counts whatever each product is sold by, so the column cannot name one unit. Products sold by weight are already handled by registering them in whole grams. |
| F6 | discarded | The stock is always current: it is calculated from the movements every time the list is opened, so it has no date of its own. `Actualizado` was already there before the stock column and refers to changes to the product itself, which the owners make when they edit it. |
| F7 | discarded | Same as F5 of the #18 QA report (`MKA-18-stock-form.md`), F11 of the #23 one and F14 of the #25 one: nothing wrong is saved, the errors only point at the fields still empty, and the scanner sends no Enter today. |

## Manual test results

To be filled in by the developer after running the manual test cases.

| Case | Result (passed / failed / not run) | Decision (fixed / ticket / discarded) | Notes, ticket or reason |
| --- | --- | --- | --- |
| M1 | not run | discarded | As M1 of the #23 QA report (`MKA-23-inventory-movements-list.md`): step 3 expects the second scan to replace the first, which was discarded in F4, and the precondition assumes a scanner that sends Enter, which the store's does not today. A single scan, and a code that matches no product, behave like any search in this list. |
| M2 | not run | discarded | Not related to this ticket: how the list behaves with the server stopped did not change with the stock column. As in M3 of the #25 QA report, with the server stopped no page of the app loads at all, so a list with made-up stock cannot appear. |
| M3 | passed | | Simulated with the test server and Sal Cumbre 1kg (stock 15). With the server stopped, `Guardar ingreso` showed "No se pudo conectar con el sistema. Verifique que el servidor esté iniciado." and kept the product, quantity 7 and cost 63,00. After starting it again, the second save showed "Ingreso guardado"; the product has exactly one new movement (2 → 3), and the list shows stock 22 (15 + 7). |
| M4 | passed | | Simulated against a test database with Sal Cumbre 1kg. Browser closed right after saving: of three saves whose connection the client dropped at once, two were recorded in full and one never reached the server, so each ingreso was saved completely or not at all. Laptop turned off: the server process was killed at a random moment during a series of saves, three times. After every case the product had no incomplete movement, the stock in the product list equalled the sum of its movements in the database (68, 70, 90, 112, 135), and SQLite's integrity check passed. |
| M5 | not run | discarded | Not related to this ticket: it checks the scanner in `Ingresar stock`, which this ticket did not change. The premature errors it looks for were discarded in F7, and the precondition assumes a scanner that sends Enter or Tab, which the store's does not today. |
