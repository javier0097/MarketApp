# QA report: Editar ingreso (`/inventario/movimientos/{id}/editar`)

- **Date:** 2026-11-22
- **URL:** http://localhost:5130/inventario/movimientos/{id}/editar, opened with the "Editar" button in http://localhost:5130/inventario/movimientos. Test inbounds were created in http://localhost:5130/inventario/ingresar. The URL given in the task, `/stock/ingresar`, opens a "Nuevo producto" form (see F13).
- **Data that already existed when I started:**
  - About 90 products, including "Arroz Grano de Oro 1kg" (7772106000026), "Papa kg" (TMV00006), "Huevo unidad" (TMV00003), "Fideo Lazzaroni 400g" (7772106000071), and a product with special characters and a very long name (7772106000088).
  - The list had no movements registered today (22/11/2026). Movements 1–22 existed from earlier sessions. 16 of them were registered on 10/11/2026, and all were "Ingreso". These included:
    - movement 11, "Sal Cumbre 1kg", with "Fecha del movimiento" **01/01/1900**;
    - movement 8, "Tomate kg", with cost **Bs 0,00**;
    - movement 14, "Cebolla kg", dated 15/01/2026.
  - I saw no outbound movements ("salidas") anywhere, so editing them could not be tested.
- **Data I created for the test (all on 22/11/2026):**

  | Movement | Product | Quantity | Cost | Notes |
  | --- | --- | --- | --- | --- |
  | 23 | Arroz | 10 u. | Bs 100,50 | |
  | 24 | Arroz | 24 u. | Bs 250,00 | note "Proveedor Don Lucho" |
  | 25 | Papa kg | 12 | Bs 36,00 | dated 18/11/2026 |
  | 26 | Fideo | 48 | Bs 345,60 | |
  | 27 | Huevo | 360 | Bs 324,00 | |
  | 28 | Galletas (long name) | 5 | Bs 17,50 | |
  | (new) | Arroz | 100 | Bs 1.100,00 | for the two-tab test |

  Movements 23–27 were edited several times during the test.

## Summary

**14 findings: 4 High, 5 Medium, 5 Low.**

Most important:

- **F1 (High):** the form opens with the cursor at the end of "Cantidad", without selecting it. Typing the new quantity adds digits to the old one: 12 + "15" was saved as **1.215**.
- **F2 (High):** in "Costo total", a typed thousands point is read as a decimal comma. Typing "1.500" becomes Bs 1,50, and that was saved. Pasting gives the opposite: pasting "100.5" becomes Bs 1005.
- **F3 (High):** in "Cantidad", the comma is silently removed. "12,5" kg becomes 125.
- **F4 (High):** with the same ingreso open in two tabs, saving in one tab silently undoes the change saved in the other.
- **F5 (Medium):** after saving there is no confirmation, and the list returns to today with the search cleared. The user cannot see the change they just made.
- **F7 (Medium):** an ingreso loaded with the wrong product cannot be corrected or removed.
- **F14 (Medium, outside this screen):** in "Ingresar stock", scanning a code shows errors on the empty fields before the user has typed anything.

## Findings

### F1. On opening, "Cantidad" has the cursor at the end of the old value: typing a new quantity adds to it and saves a wrong number

- **Severity:** High
- **Origin:** `own testing`
- **Relevant data:** Movement 25, "Papa kg · TMV00006", quantity 12, Bs 36,00.
- **Steps:**
  1. In `/inventario/movimientos`, click "Editar" on the "Papa kg" row.
  2. Without clicking anything, type `15` (focus is already in "Cantidad").
  3. Press Enter.
- **Expected:** "Cantidad" opens with its value selected, so that typing replaces it. This is how Tab already behaves when it moves into "Costo total" (that value is selected). The saved quantity would be 15.
- **Actual:** The field shows "1215" and the form is saved. The list shows "Cantidad" **1.215** for Papa kg, and the stock rises by 1.203 kg. Nothing warns the user, and the list gives no confirmation.
  - The "Resumen" does show "Stock: 12 → 1.215", but someone loading fast and pressing Enter does not read it.
  - With a large original quantity the 99.999 limit stops it (Huevo 360 + "300" = "360300" gives "La cantidad debe estar entre 1 y 99.999."). With small quantities, the most common case, a wrong value is saved.
  - A scanner read made by mistake on this screen would also be appended to the quantity.

### F2. "Costo total": a typed thousands point becomes a decimal comma, and typing and pasting give different results

- **Severity:** High
- **Origin:** `own testing` (and `web research — typed vs pasted money amounts, ambiguous separators`)
- **Relevant data:** Movement 23, "Arroz Grano de Oro 1kg", 12 u.
- **Steps:**
  1. Open "Editar" for movement 23.
  2. In "Costo total", select everything and type `1.500` key by key.
  3. Click "Guardar cambios".
- **Expected:** In Bolivia a point separates thousands and the comma marks decimals, so "1.500" means one thousand five hundred bolivianos. The app should accept it as Bs 1.500,00, or at least show a clear message. It must not silently change the amount. Typing and pasting the same text should give the same result.
- **Actual:**
  - Typed `1.500` becomes "1,50". It was saved and the list showed "Monto total" **Bs 1,50** and "Costo unitario" Bs 0,125.
  - Typed `1.234,56` becomes "1,23".
  - Pasted `1.234,56` becomes "1234,56" (correct).
  - Pasted `100.5` becomes "1005", while typed `100.50` becomes "100,50".
  - "Cantidad" treats the point in the opposite way: typing `1.000` gives 1000.

  The only clue is the "Costo unitario: … → …" line in the "Resumen". No message appears.

### F3. "Cantidad" silently removes the decimal comma: "12,5" becomes 125

- **Severity:** High
- **Origin:** `own testing`
- **Relevant data:** Products sold by weight exist ("Papa kg", "Tomate kg", "Cebolla kg", "Zanahoria kg"). Tested on movement 23, Arroz, quantity 10.
- **Steps:**
  1. Open "Editar" for movement 23.
  2. In "Cantidad", select everything and type `12,5` key by key.
- **Expected:** If decimals are not allowed, a message such as "La cantidad debe ser un número entero" and the value is left alone. If they are allowed (kg), it is accepted as 12,5.
- **Actual:**
  - The field shows "125" with no message. The "Resumen" shows "Stock: 34 → 149".
  - Saving would store ten times the real quantity.
  - Typing `-5` likewise becomes "5" without any notice.
  - The same happens in "Ingresar stock" (`/inventario/ingresar`), seen while creating test data: "12,5" for Papa kg became "125".

### F4. Same ingreso open in two tabs: the second save silently undoes the first

- **Severity:** High
- **Origin:** `web research — concurrent edit / stale data in two tabs`
- **Relevant data:** Movement 24, Arroz, quantity 25, Bs 250,00, note "Proveedor Don Lucho".
- **Steps:**
  1. Open `/inventario/movimientos/24/editar` in tab A.
  2. Open the same address in tab B.
  3. In tab B, change "Cantidad" to 30 and save.
  4. In tab A, where "Cantidad" still shows 25, change only "Nota" to "Proveedor Don Lucho - factura 778" and save.
- **Expected:** Tab A warns that the ingreso was changed in the meantime, and asks the user to reload before saving.
- **Actual:** Both saves succeed. The list shows "Cantidad" **25** with the new note, so the quantity 30 saved in tab B was lost without notice. Only one person uses the app, but having the list open in two tabs (for example, one for each day) is realistic.

### F5. After "Guardar cambios" there is no confirmation, and the list resets to today and clears the search

- **Severity:** Medium
- **Origin:** `own testing`
- **Relevant data:**
  - Movement 11, "Sal Cumbre 1kg", registered on 10/11/2026.
  - Movements 23/24, Arroz, found with the search "Arroz".
- **Steps:**
  1. In the list, set "Fecha de registro" to 10/11/2026.
  2. Click "Editar" on movement 11, type a note ("Revisado") and press Enter.
  3. Separately, type "Arroz" in "Buscar", click "Editar" on one result, then click "Cancelar".
- **Expected:**
  - A confirmation in the same style as "Ingresar stock" (which shows "Ingreso guardado" with the details).
  - The list returns with the same date and search, ideally with the edited row highlighted, so the user can check the change.
- **Actual:**
  - The page goes back to the list with no message.
  - "Fecha de registro" is back to 22/11/2026, showing "6 movimientos registrados el 22/11/2026". The edited movement is not visible.
  - After "Cancelar" the search "Arroz" is gone too.
  - To check an edit, the user has to find the date or search again.

### F6. Leaving the form with unsaved changes (Back, reload) discards them without a warning

- **Severity:** Low
- **Origin:** `own testing`
- **Relevant data:** Movements 26 (Fideo, 48) and 23 (Arroz).
- **Steps:**
  1. Open "Editar" for Fideo and change "Cantidad" to 50.
  2. Press the browser's Back button.
  3. Separately, open "Editar" for Arroz (movement 23), change "Costo total" to 132 and reload the page (F5).
- **Expected:** A warning such as "Tiene cambios sin guardar. ¿Salir de todos modos?"
- **Actual:** The changes are lost silently. The list still shows 48 and the form reloads with "120,00". "Cancelar" discarding changes is expected, but Back and F5 are often pressed by accident.

### F7. An ingreso loaded with the wrong product cannot be corrected or removed

- **Severity:** Medium
- **Origin:** `own testing`
- **Relevant data:** Any ingreso, for example movement 23.
- **Steps:**
  1. Open "Editar" for any ingreso.
  2. Try to change the product, or to cancel the ingreso.
- **Expected:** Since a wrong scan is the most likely mistake when loading fast, there is a way to fix it: change the product, delete or cancel the ingreso, or at least a message explaining what to do.
- **Actual:**
  - "Producto" is plain text ("Arroz Grano de Oro 1kg · 7772106000026").
  - There is no delete or cancel option.
  - "Cantidad" cannot be 0: "La cantidad debe estar entre 1 y 99.999."
  - The stock of the wrong product stays inflated with no way to undo it from the interface, and the screen does not explain why.

### F8. "Último costo unitario" in the "Resumen" is ambiguous

- **Severity:** Medium
- **Origin:** `own testing`
- **Relevant data:** Arroz movements 23 (Bs 100,50 / 10 u., later 120,00 / 12) and 24 (Bs 250,00 / 24 u.).
- **Steps:**
  1. Open "Editar" for movement 23 and change "Cantidad" to 12.
  2. Read the "Resumen".
- **Expected:** A label that says what it is compared with, for example "Costo unitario del último otro ingreso (22/11/2026): Bs 10,417".
- **Actual:**
  - The "Resumen" shows "Costo unitario: Bs 10,050 → Bs 8,375" and right below it "Último costo unitario: Bs 10,417".
  - Right under "Costo unitario: X → Y", "Último" reads like this movement's previous cost. It is actually another ingreso, with no date or reference.
  - After further edits it showed "Costo unitario: Bs 10,000 → Bs 11,000" next to "Último costo unitario: Bs 10,000". This looks like the same number repeated.
  - In "Ingresar stock", the same label means "the last ingreso of the product", which is a different meaning.

### F9. The "Resumen" uses the stock from when the form was opened

- **Severity:** Low
- **Origin:** `web research — stock changes while editing`
- **Relevant data:** Arroz, total stock 37 before the test.
- **Steps:**
  1. Open "Editar" for movement 24 (Arroz) in tab A.
  2. In tab B, in "Ingresar stock", load Arroz 100 u. for Bs 1.100,00. This shows "Ingreso guardado".
  3. Back in tab A, change "Cantidad" to 26.
- **Expected:** "Stock: 137 → 138" and "Último costo unitario: Bs 11,000", or a notice that the data changed.
- **Actual:** "Stock: 37 → 38" and "Último costo unitario: Bs 10,000". The preview is out of date, though the data actually saved is not affected.

### F10. Nonexistent or invalid movement: a dead end, or a "Nuevo producto" form

- **Severity:** Low
- **Origin:** `own testing`
- **Relevant data:** No movement 99999 exists.
- **Steps:**
  1. Open `/inventario/movimientos/99999/editar`, for example from an old bookmark or a movement that no longer exists.
  2. Open `/inventario/movimientos/abc/editar`.
- **Expected:** A clear message with a link back to the list.
- **Actual:**
  - Step 1 shows only "Editar ingreso" and "El movimiento no existe.", with no button or link to go back.
  - Step 2 shows the "Nuevo producto" form (page title "Nuevo producto - MarketApp") with no explanation. The user could end up creating a product thinking they were somewhere else (see also F13).

### F11. The same amount is called "Monto total" in the list and "Costo total" in the form

- **Severity:** Low
- **Origin:** `own testing`
- **Relevant data:** Any ingreso.
- **Steps:**
  1. Compare the list's column headers with the edit form.
- **Expected:** The same name everywhere.
- **Actual:** The list column is "Monto total"; the form field (and "Ingresar stock") is "Costo total".

### F12. The "Resumen" shows up for some invalid quantities and not for others

- **Severity:** Low
- **Origin:** `own testing`
- **Relevant data:** Movement 23.
- **Steps:**
  1. In "Cantidad", type `0`.
  2. Then type `100000`.
- **Expected:** The same behavior for both, preferably no preview while the value is invalid.
- **Actual:**
  - With 0, the "Resumen" disappears.
  - With 100000, it shows "Stock: 34 → 100.024" and "Costo unitario: Bs 10,050 → Bs 0,001" next to the error "La cantidad debe estar entre 1 y 99.999."

### F13. Outside this screen, any unknown address: shows the "Nuevo producto" form with no notice

- **Severity:** Low
- **Origin:** `own testing` (while preparing data)
- **Relevant data:** The task gave `/stock/ingresar` as the address for creating ingresos. The link in the movement list points to `/inventario/ingresar`.
- **Steps:**
  1. Open `http://localhost:5130/stock/ingresar`.
- **Expected:** A "Página no encontrada" message with links to the main screens.
- **Actual:** The page shows the "Nuevo producto" form ("Nombre", "Precio de venta", "Código", "Guardar producto"), with page title "Nuevo producto - MarketApp". The same happens with `/inventario/movimientos/abc/editar` (F10).

### F14. Outside this screen, "Ingresar stock" (`/inventario/ingresar`): scanning a code shows errors on fields the user has not filled yet

- **Severity:** Medium
- **Origin:** `own testing` (while preparing data)
- **Relevant data:** Products with codes 7772106000026 (Arroz), 7772106000071 (Fideo), TMV00006 (Papa).
- **Steps:**
  1. Open `/inventario/ingresar`.
  2. In "Producto", type the code `7772106000071` key by key and press Enter, as a scanner does.
- **Expected:** The product is selected and the cursor moves to "Cantidad", with no errors.
- **Actual:**
  - The product is selected ("Elegido: Fideo Lazzaroni 400g · 7772106000071 · precio de venta Bs 6,50"), but "Cantidad" and "Costo total" turn red with "Ingrese la cantidad de unidades del lote." and "Ingrese el costo total del lote."
  - This happened on every scan (5 out of 5).
  - Choosing the product by mouse click, or with Arrow Down + Enter after typing "Papa", does not show the errors.
  - For users who load hundreds of items, every item starts with two error messages.

## Manual test cases

### M1. Physical barcode scanner on the edit screen

- **Preconditions:** USB scanner connected. An ingreso with a small quantity exists (for example 12).
- **Steps:**
  1. Click "Editar" on that ingreso and, without clicking anything, scan a product barcode.
  2. Repeat with focus in "Costo total", "Fecha del movimiento" and "Nota".
- **Expected:** No scan saves a wrong quantity or cost. A scan in "Cantidad" or "Costo total" is rejected with a clear message. If a scan in "Nota" saves the code as text, the user can see that clearly. Check especially step 1 together with F1: the code's Enter must not save.

### M2. Server stopped while saving

- **Preconditions:** The edit form for an ingreso is open with changes. Stop the backend.
- **Steps:**
  1. Click "Guardar cambios".
  2. Start the server again and click "Guardar cambios" once more.
- **Expected:** A clear message in Spanish (for example "No se pudo guardar. Revise que el sistema esté encendido e intente de nuevo."). The form keeps the typed values. Nothing is half-saved. The second save works and the movement and stock are correct.

### M3. Server stopped when opening "Editar"

- **Preconditions:** The movement list is open. Stop the backend.
- **Steps:**
  1. Click "Editar" on any ingreso.
- **Expected:** A clear message in Spanish with a way back to the list, not an empty form or a form with empty fields that could be saved.

### M4. Browser or laptop closed during the save

- **Preconditions:** An ingreso of a product whose stock is known.
- **Steps:**
  1. Change "Cantidad" and click "Guardar cambios".
  2. Close the browser immediately (or cut power on the laptop).
  3. Reopen and check the list and the "Stock actual" in "Ingresar stock".
- **Expected:** The movement has either all old values or all new values, and the product's stock matches the sum of its movements.

### M5. Date field on the owners' laptop

- **Preconditions:** The owners' Windows laptop with its usual regional settings (Spanish – Bolivia).
- **Steps:**
  1. Open "Editar" and look at "Fecha del movimiento".
  2. Change the date with the keyboard only.
- **Expected:** The date shows as dd/mm/aaaa, as everywhere else in the app. The test browser showed "11/22/2026" (month first). Keyboard entry is easy to do.

### M6. Date limits close to midnight

- **Preconditions:** Laptop clock set to 23:50, Bolivia time.
- **Steps:**
  1. Edit an ingreso and set "Fecha del movimiento" to today.
  2. Save.
  3. Repeat at 00:05.
- **Expected:** "Today" is accepted, and there is no false "La fecha no puede ser posterior a hoy." caused by time zone differences.

### M7. Decimal key on the numeric keypad

- **Preconditions:** Windows regional settings Spanish (Bolivia), physical keyboard with a numeric keypad.
- **Steps:**
  1. In "Costo total", type `12` + keypad decimal key + `50`.
  2. Type `1` + keypad decimal key + `500`.
- **Expected:** 12,50, and then a clear result for the thousands case (see F2), never Bs 1,50 without a warning.

### M8. Editing an ingreso of a product that already has sales

- **Preconditions:** A product with an ingreso of 10 u. and later outbound movements of 8 u. No outbound could be created in this session.
- **Steps:**
  1. Edit the ingreso and lower "Cantidad" to 5.
- **Expected:** The "Resumen" shows the resulting stock correctly. If the stock would go negative, the app warns or prevents it with a clear message.

### M9. Editing an ingreso dated more than one year ago

- **Preconditions:** An ingreso whose "Fecha del movimiento" is more than one year old (movement 11, dated 01/01/1900, already exists).
- **Steps:**
  1. Change only the quantity or the cost and save.
  2. Then change the date to another date also more than one year old.
- **Expected:** Step 1 saves; in this session, changing only the note did save. Step 2 shows "La fecha no puede ser anterior al …" with the correct date.

## Resolution

To be filled in by the developer after triage.

| Finding | Decision (fixed / ticket / discarded) | Ticket or reason |
| --- | --- | --- |
| F1 | fixed | Changed differently from the proposal: the edit form now opens with no field focused, like the product edit form, so nothing typed before choosing a field can change a value. Selecting the value on open would still let a stray scanner read replace the quantity. |
| F2 | discarded | Same as F3 of the #18 QA report (`MKA-18-stock-form.md`): the owners do not usually write a thousands separator, and the field shows the value that stays and the summary shows the unit cost change before saving. Typing and pasting differ for the same reason. |
| F3 | discarded | Same as F2 of the #18 QA report (`MKA-18-stock-form.md`) and F12 of the #23 one: quantities are whole units by decision (unit of measure postponed), products sold by weight are registered in whole grams, and the dropped comma or minus sign is visible in the field and in the summary before saving. |
| F4 | discarded | Same as F3 of the #15 QA report (`MKA-15-product-edit.md`): it needs the same inbound open in two tabs, and the app assumes one writer at a time; detecting it would need a version check for a very rare case. |
| F5 | fixed | After saving an edit, the movement list shows "Ingreso guardado" with the saved values, the same alert as "Ingresar stock", like the product list does after an edit. The list still opens on today with no search: keeping the date and search is not needed for this ticket. |
| F6 | discarded | As F7 in MKA-15 and F19 in MKA-18: leaving by accident loses at most the changes to one inbound, which is little data to type again, and the inbound stays as it was. |
| F7 | ticket | #27: an inbound recorded by mistake, for example with the wrong product, will be voided with a reason. The product stays fixed when editing. |
| F8 | fixed | Changed differently from the proposal: when editing, the summary no longer compares with another inbound and only shows how this inbound's unit cost changes, like the other fields. In "Ingresar stock" the line is now "Costo unitario anterior (dd/mm/yyyy)", with the date of the inbound it comes from. |
| F9 | discarded | As F4: it needs another tab recording stock for the same product while the edit is open, and the app assumes one writer at a time. Only the preview is stale; the saved data is right, and reloading the form updates it. |
| F10 | ticket | #20 and #16: the navigation menu (#20) will give a way back from "El movimiento no existe.", which already says what happened, and the not-found page (#16) will catch invalid URLs such as `/inventario/movimientos/abc/editar`. |
| F11 | discarded | Decided in F8 of the #23 QA report (`MKA-23-inventory-movements-list.md`): the list column also holds outbounds, where the amount is income, so it is "Monto total"; the inbound form asks for a cost, so it is "Costo total". |
| F12 | discarded | Cosmetic: the quantity error next to the field already says the value is invalid, and the API rejects it, so nothing wrong is saved whether the preview shows or not. |
| F13 | ticket | #16: every URL the frontend does not recognize will show a not-found page instead of the new-product form. |
| F14 | discarded | Same as F5 of the #18 QA report (`MKA-18-stock-form.md`) and F11 of the #23 one: nothing wrong is saved, the errors only point at the fields still empty, and the scanner sends no Enter today. |

## Manual test results

To be filled in by the developer after running the manual test cases.

| Case | Result (passed / failed / not run) | Decision (fixed / ticket / discarded) | Notes, ticket or reason |
| --- | --- | --- | --- |
| M1 | not run | discarded | Since F1 the form opens with no field focused, so a scan before clicking a field types nothing. A scan in a field is easy to see and fix before saving, and the scanner sends no Enter today, so it never saves on its own. "Cantidad" and "Costo total" also stop taking digits past their length limit, so a code ends up as a value over the limit, which the API rejects, and "Fecha del movimiento" only takes a date. |
| M2 | passed | | Run by the developer: with the server stopped, saving showed a clear message and kept the typed values; after starting it again, the second save worked. |
| M3 | failed | discarded | Run by the developer: the browser shows its own "cannot reach this page" screen instead of a message from the app. Expected: in production the same process serves the pages and the API, so with it stopped no page of the app can load at all, and no empty form that could be saved ever appears. |
| M4 | passed | | Simulated against a test database. Browser closed: three saves whose connection the client dropped right after sending them; the last one was still applied in full on the server. Power cut: the server process was killed at a random moment in the middle of a series of saves alternating two sets of values, five times. Every time the movement held exactly one complete set (quantity, cost, date and note together), the stock from the summary matched the sum of the movements, and SQLite's integrity check passed. |
| M5 | not run | | The store laptop was not available. Same check as M2 of the #23 QA report (`MKA-23-inventory-movements-list.md`), still pending: on the laptop the field should show `dd/mm/aaaa` and accept a date typed with the keyboard only. |
| M6 | passed | | Simulated at 22:46 Bolivia time, when the UTC date (24/11) was already a day ahead of the local one (23/11), which is the mismatch this case looks for. The form offered 23/11 as the latest date, saving with 23/11 was accepted, and 24/11 was rejected with "La fecha no puede ser posterior a hoy." by both the edit and the entry endpoints: browser and server take "today" from the local clock. Not run at the exact midnight crossing: the browser and the server share the laptop's clock, so a form opened at 23:59 and saved after midnight sends the previous day, which is still accepted. |
| M7 | passed | | Checked by the developer, who uses the keypad decimal key every time they record or edit an inbound: `12` + decimal key + `50` gives 12,50. The thousands case was discarded in F2. |
| M8 | not run | | Outbound movements cannot be recorded yet. The stock in the summary is covered by the unit tests of `stockAfterInboundEdit`, including a result below zero, which the form shows as is: whether to warn about a negative stock is not decided and belongs with the outbound ticket. |
| M9 | passed | | Simulated with a test inbound dated 10/06/2024. Step 1: changing only the quantity (6 → 7) saved, and the list showed "Ingreso guardado" with the date unchanged. Step 2: changing the date to 01/06/2024 showed "La fecha no puede ser anterior al 23/11/2025." next to the date field, with the focus on it, and the inbound kept 10/06/2024. |
