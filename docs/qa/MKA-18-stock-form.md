# QA report: Ingresar stock

- **Date:** 2026-11-10
- **URL:** http://localhost:5130/stock/ingresar (products created at http://localhost:5130/)
- **Data that already existed when I started:**
  - About 97 products from earlier sessions: "QA vol 01" to "QA vol 47", "QA lista 001" to "QA lista 027", "Leche PIL entera 1L" (twice: `7772106000019` and `7772106000040`), "Arroz Grano de Oro 1kg" (twice: `TMV00082` and `7772106000026`), "Pañuelos UPC" `036000291452` and "Pañuelos EAN13 resave" `0036000291452`, products sold by weight ("Tomate kg" `TMV00005`, "Papa kg" `TMV00006`, "Cebolla kg" `TMV00004`, "Zanahoria kg" `TMV00007`), products with no barcode ("Pan marraqueta" `TMV00001`, "Huevo unidad" `TMV00003`), and two products with names much longer than 200 characters ("Nombre largo 0123…" `7772106000095`, "Galletas María "Clásica" 200g <b>&amp;</b>…" `7772106000088`).
  - Every product I opened had "Stock actual: 0" and "Último costo unitario: Sin ingresos anteriores", so no earlier inbound movements seemed to exist.
- **What the screen does:** it records one inbound lot. "Cantidad" is the number of units received, "Costo total" is what the whole lot cost, and "Fecha" is the purchase date. A list of saved inbound movements does not exist yet (next feature), so I checked saved data through the "Resumen" block ("Stock actual", "Último costo unitario") and the "Ingreso guardado" message.

## Summary

20 findings: **4 High**, **10 Medium**, **6 Low**.

The most important ones:

- **F1 (High):** After saving with Enter from "Costo total", the cost stays in the form, and the next product is saved with the previous product's cost.
- **F2 (High):** "Cantidad" silently drops the decimal comma: "2,5" kg of tomatoes is saved as "25 u.".
- **F3 (High):** If you type a cost with a thousands separator, it shrinks: "1.250,50" becomes "1,25".
- **F4 (High):** If you paste a cost with a decimal point, it grows: "12.5" is saved as "Bs 125,00".
- **F5 (Medium):** The scanner's Enter submits the whole form. Every scan shows red errors on "Cantidad" and "Costo total" before the user has typed anything.
- **F6 (Medium):** A second Enter after saving replaces "Ingreso guardado" with three errors, so the user may think the save failed and enter the lot again.

## Findings

### F1. The cost of the previous entry stays in "Costo total" and is saved with the next product

- **Severity:** High
- **Origin:** `own testing`
- **Relevant data:** "Leche PIL entera 1L" `7772106000040` and "Fideo Lazzaroni 400g" `7772106000071`, both with stock 0 and no earlier inbound movements.
- **Steps:**
  1. In "Producto", type `7772106000040` and press Enter.
  2. In "Cantidad", type `12`, press Tab, type `96,50` in "Costo total" and press Enter (focus is still in "Costo total").
  3. "Ingreso guardado — Leche PIL entera 1L · 12 u. · Bs 96,50 · martes, 10 de noviembre de 2026" appears. "Producto" and "Cantidad" are emptied, but "Costo total" still shows "Bs 96,50".
  4. In "Producto", type `7772106000071` and press Enter. In "Cantidad", type `20` and press Enter.
- **Expected:** After a save, the form is fully cleared (or at least the cost is cleared), so one product's cost can never be reused for another product without the user noticing.
- **Actual:** The second lot is saved as "Fideo Lazzaroni 400g · 20 u. · Bs 96,50". Choosing Fideo again shows "Último costo unitario: Bs 4,83", which is wrong.
  - Reproduced with costs `48,30` (Leche PIL `7772106000019`), `25,50` (Huevo unidad) and `8,50` typed key by key (Huevo unidad).
  - It did **not** happen when the cost had no decimals (`15`, `40`, `3`), or when saving with a click on "Guardar ingreso" (`17,50`, Pan marraqueta).
  - Typing a decimal cost and pressing Enter is the fastest keyboard path, which is exactly how the owners load stock, so this will happen often. It is also inconsistent: sometimes the cost is cleared and sometimes it is not.

### F2. "Cantidad" ignores the decimal comma, so 2,5 kg is saved as 25 units

- **Severity:** High
- **Origin:** `own testing`
- **Relevant data:** "Tomate kg" `TMV00005`, stock 0. Other products sold by weight also exist: "Papa kg", "Cebolla kg", "Zanahoria kg".
- **Steps:**
  1. In "Producto", type `TMV00005` and press Enter.
  2. In "Cantidad", type `2,5`.
  3. In "Costo total", type `12,50` and press Enter.
- **Expected:** Either the field accepts 2,5 (products sold by kg), or it rejects the comma with a clear message such as "La cantidad debe ser un número entero". It must never silently turn the number into a different one.
- **Actual:** As you type, the comma is dropped and the field shows "25". If the value is pasted, it is saved straight away as 25. The summary shows "Stock: 25 → 50" and the saved message says "Tomate kg · 25 u. · …". There is no warning. The saved message also uses "u." (units) for products whose name says they are sold by kg ("Cebolla kg · 10 u.").

### F3. Typing a cost with a thousands separator: "1.250,50" becomes "1,25"

- **Severity:** High
- **Origin:** `own testing`
- **Relevant data:** "Tomate kg" `TMV00005` selected, "Cantidad" 25.
- **Steps:**
  1. Select a product and type a quantity.
  2. In "Costo total", type `1.250,50` key by key, the way a former bank employee writes amounts.
- **Expected:** Bs 1.250,50. Or, if thousands separators are not supported, a clear message, never a different amount.
- **Actual:** The field shows "1,25" (Bs 1,25). The typed "." is taken as the decimal comma, and everything after the second decimal digit is dropped without a word. The only hint is the summary line "Costo unitario: Bs 0,05". Pasting `1.234,56` instead is read correctly as "1234,56", so the result depends on whether the user types or pastes.

### F4. Pasting a cost with a decimal point: "12.5" is saved as Bs 125,00

- **Severity:** High
- **Origin:** `own testing`
- **Relevant data:** "Tomate kg" `TMV00005`.
- **Steps:**
  1. Select "Tomate kg" and set "Cantidad" to 25.
  2. Paste `12.5` into "Costo total" (for example, copied from the Windows calculator) and press Enter.
- **Expected:** Bs 12,50, which is how the field reads the same text when it is typed, or a clear message.
- **Actual:** The field shows "125,00" and the lot is saved as "Tomate kg · 25 u. · Bs 125,00". The summary afterwards shows "Último costo unitario: Bs 5,00" instead of Bs 0,50. Typing `12.5` key by key gives "12,50", so typing and pasting give results that differ by a factor of ten.

### F5. The scanner's Enter submits the whole form, so every scan shows errors

- **Severity:** Medium
- **Origin:** `own testing`
- **Relevant data:** Any existing barcode, for example `7772106000040`.
- **Steps:**
  1. Open the screen. Focus is in "Producto".
  2. Type (or scan) `7772106000040` followed by Enter, as a USB scanner does.
- **Expected:** The product is chosen and focus moves to "Cantidad", with no errors, since the user has not tried to save yet.
- **Actual:** The product is chosen ("Elegido: Leche PIL entera 1L · 7772106000040 · precio de venta Bs 9,00") and focus moves to "Cantidad". But the form is also submitted: "Cantidad" and "Costo total" turn red with "Ingrese la cantidad de unidades del lote." and "Ingrese el costo total del lote.". This happens on every scan, so loading hundreds of items means seeing hundreds of false errors, and users learn to ignore red messages. With an unknown code (`7790000000001`), three errors appear at once.

### F6. A second Enter after saving hides "Ingreso guardado" behind three errors

- **Severity:** Medium
- **Origin:** `own testing`
- **Relevant data:** "Sal Cumbre 1kg" `TMV00089`, stock 10 at that moment.
- **Steps:**
  1. Choose `TMV00089`, type "Cantidad" `5` and "Costo total" `15`.
  2. Press Enter twice quickly (a double Enter).
- **Expected:** One save, and the confirmation stays visible.
- **Actual:** The lot is saved once ("Stock actual: 15" afterwards, which is correct). But the green "Ingreso guardado" box disappears and is replaced by "Falta el producto. Búsquelo por nombre o código, o escanee su código de barras.", "Ingrese la cantidad de unidades del lote." and "Ingrese el costo total del lote.". The user sees errors instead of a confirmation and will probably enter the lot again, which duplicates the stock. (A double click on "Guardar ingreso" did not cause this: one save, confirmation visible.)

### F7. A partly typed name plus Tab does not choose the product, and the error says the product does not exist

- **Severity:** Medium
- **Origin:** `own testing`
- **Relevant data:** "Fideo Lazzaroni 400g" `7772106000071` is the only product matching "Fideo".
- **Steps:**
  1. In "Producto", type `Fideo`. The list shows a single option, "Fideo Lazzaroni 400g".
  2. Press Tab, type `10` in "Cantidad" and `50` in "Costo total", and press Enter.
- **Expected:** Tab chooses the only match, or the error explains that a product must be chosen from the list.
- **Actual:** Nothing is chosen and no "Elegido" line appears. On save, the error is "«Fideo» no es el nombre ni el código de ningún producto. Elija uno en la lista que aparece al escribir o escanee el código de barras del producto." This is misleading: a product whose name starts with "Fideo" does exist. ArrowDown and then Enter does choose it.

### F8. Unknown barcode: no way to reach product creation from this screen

- **Severity:** Medium
- **Origin:** `own testing`
- **Relevant data:** No product has code `7790000000001`.
- **Steps:**
  1. In "Producto", type `7790000000001` and press Enter.
- **Expected:** A message telling the user the product is not registered yet, and a way to create it (a link or button to "Nuevo producto"), then come back.
- **Actual:** "«7790000000001» no es el nombre ni el código de ningún producto. Elija uno en la lista que aparece al escribir o escanee el código de barras del producto." The message asks the user to scan the barcode, which they just did. The screen has no menu or link to "Nuevo producto" (and "Nuevo producto" has none back here), so the only way is to type the other URL. In the middle of loading a delivery, the user cannot go on.

### F9. "Fecha" accepts absurd past dates (1 January 1900)

- **Severity:** Medium
- **Origin:** `own testing`
- **Relevant data:** "Sal Cumbre 1kg" `TMV00089`.
- **Steps:**
  1. Choose `TMV00089`, "Cantidad" `1`, "Costo total" `3`.
  2. Set "Fecha" to 01/01/1900 and click "Guardar ingreso".
- **Expected:** A date in the future is already rejected ("La fecha no puede ser posterior a hoy."). An obviously mistyped past date (wrong year) should also be rejected or confirmed.
- **Actual:** Saved: "Sal Cumbre 1kg · 1 u. · Bs 3,00 · lunes, 1 de enero de 1900".

### F10. "Fecha" goes back to today after each save, so a whole delivery from another day needs the date re-entered every time

- **Severity:** Medium
- **Origin:** `own testing`
- **Relevant data:** "Sal Cumbre 1kg" saved with date 09/11/2026.
- **Steps:**
  1. Save a lot with "Fecha" set to yesterday (09/11/2026).
  2. Look at "Fecha" for the next entry.
- **Expected:** When loading many products from the same purchase, the purchase date chosen once is kept for the next entry (or the user is clearly reminded of it).
- **Actual:** "Fecha" returns to today (2026-11-10). If the user loads yesterday's delivery and forgets to change the date for every item, all lots except the first are saved with the wrong date, and nothing warns them. The summary line "(no es hoy)" only appears when the date is *not* today, so it cannot help here.

### F11. A pasted cost over the limit empties the field, and the error says to enter the cost

- **Severity:** Medium
- **Origin:** `own testing`
- **Relevant data:** "Papa kg" `TMV00006`.
- **Steps:**
  1. Choose `TMV00006` and type a quantity.
  2. Paste `99999999999999` into "Costo total" and click "Guardar ingreso".
- **Expected:** The value stays visible with the limit message "El costo total debe estar entre Bs 0,00 y Bs 99.999,99." (the message that appears when the too-large value is typed).
- **Actual:** The field is silently emptied and the error is "Ingrese el costo total del lote.", as if the user had typed nothing. When typing, any digit after the seventh is silently ignored ("9999999,00").

### F12. Pressing Enter on a name shared by two products silently chooses the first one

- **Severity:** Medium
- **Origin:** `own testing`
- **Relevant data:** Two products named exactly "Arroz Grano de Oro 1kg" (`TMV00082` and `7772106000026`). The same happens with "Leche PIL entera 1L" (now three of them, see F13).
- **Steps:**
  1. In "Producto", type `Arroz Grano de Oro 1kg`. The list shows both, with the same name.
  2. Press Enter.
- **Expected:** The user has to choose clearly between two products that look the same (for example, they are marked as possible duplicates, or Enter does not choose when the name is ambiguous).
- **Actual:** `TMV00082` is chosen without a word. The only difference is the code in the small grey "Elegido" line, which is easy to miss. Stock may end up split between two "identical" products.

### F13. Outside this screen, form at `/`: a product can be created with exactly the same name as an existing one

- **Severity:** Medium
- **Origin:** `own testing`
- **Relevant data:** "Leche PIL entera 1L" already existed twice (`7772106000019` and `7772106000040`). I noticed this while choosing products in "Ingresar stock".
- **Steps:**
  1. Open http://localhost:5130/.
  2. In "Nombre", type `Leche PIL entera 1L`, in "Precio de venta" type `9`, tick "Sin código de barras" and click "Guardar producto".
- **Expected:** A warning that a product with that name already exists, or at least a confirmation.
- **Actual:** "Producto guardado — Leche PIL entera 1L · TMV00090 · Bs 9,00". There are now three products with the same name, which makes "Ingresar stock" error-prone (F12).
- **Note:** The existing names longer than 200 characters looked suspicious too, but the form at `/` now rejects them ("El nombre no puede tener más de 200 caracteres."), so they seem to be older data. I did not report that.

### F14. Pasted numbers in other common formats are misread without warning

- **Severity:** Medium
- **Origin:** `web research — pasting amounts with thousands and decimal separators into money and number fields`
- **Relevant data:** "Café Copacabana 250g" `TMV00008` selected, "Cantidad" 10.
- **Steps:**
  1. Paste `1,234.56` into "Costo total" and press Tab.
  2. Paste `1.200` into "Cantidad" and press Tab.
- **Expected:** The amount is read correctly, or a message says the format is not understood.
- **Actual:**
  - "Costo total" shows "1,23" ("Costo unitario: Bs 0,12" for 10 units).
  - "Cantidad" shows "1" (summary "Stock: 0 → 1"). Typing the same `1.200` key by key gives "1200".
  - Pasting `1.234,56` and `Bs 12,50` was read correctly ("1234,56" and "12,50").

### F15. A lot with cost Bs 0,00 is saved without any confirmation

- **Severity:** Low
- **Origin:** `own testing`
- **Relevant data:** "Tomate kg" `TMV00005`.
- **Steps:**
  1. Choose `TMV00005`, "Cantidad" `1`, "Costo total" `0`, and click "Guardar ingreso".
- **Expected:** A free lot may be legitimate (a supplier's bonus), but a zero cost is more often a typing mistake, so a confirmation would help.
- **Actual:** Saved: "Tomate kg · 1 u. · Bs 0,00 · martes, 10 de noviembre de 2026".

### F16. Numbers are formatted differently across the screen

- **Severity:** Low
- **Origin:** `own testing`
- **Relevant data:** Any product.
- **Steps:**
  1. Type "Cantidad" `1200` (or `10000`) and a cost such as `9999999`.
- **Expected:** One format everywhere (with a thousands dot, as in the messages).
- **Actual:** The summary shows "Stock: 0 → 1200" and "Stock: 0 → 10000" with no separator, and "Costo total" shows "9999999,00". The messages use "9.999" and "Bs 99.999,99", and "Costo unitario" shows "Bs 1.554.421,20".

### F17. A third decimal in "Costo total" is silently dropped (truncated, not rounded)

- **Severity:** Low
- **Origin:** `own testing`
- **Relevant data:** "Huevo unidad" `TMV00003` selected.
- **Steps:**
  1. Type `10,999` in "Costo total" and press Tab.
- **Expected:** Bs 11,00 (rounded), or a message that only two decimals are allowed.
- **Actual:** "10,99", with no message.

### F18. Error messages are worded in different styles

- **Severity:** Low
- **Origin:** `own testing`
- **Relevant data:** None needed. Submit the form with fields empty.
- **Steps:**
  1. Open the screen, clear "Fecha" and press Enter with all fields empty.
- **Expected:** The same pattern for every required field, and a label or message that makes clear the date is the purchase date (for example, "Fecha de compra").
- **Actual:** Most messages start with "Ingrese…" ("Ingrese la cantidad de unidades del lote.", "Ingrese el costo total del lote.", "Ingrese la fecha de ingreso."), but the product one starts with "Falta el producto. Búsquelo por nombre o código, o escanee su código de barras.". Also, "Ingrese la fecha de ingreso." talks about the date of the *entry*, while the field is meant to hold the *purchase* date. That could lead users to type the day they load the data instead of the day they bought the stock.

### F19. Leaving the page discards a half-filled entry without warning

- **Severity:** Low
- **Origin:** `own testing`
- **Relevant data:** "Sal Cumbre 1kg" `TMV00089`.
- **Steps:**
  1. Choose `TMV00089`, type "Cantidad" `5` and "Costo total" `15`.
  2. Go to http://localhost:5130/ and press the browser's Back button (or reload the page).
- **Expected:** A warning before the typed data is lost, or the data still there on return.
- **Actual:** The form is empty, with no warning.

### F20. The chosen product is confirmed only in small grey text

- **Severity:** Low
- **Origin:** `own testing`
- **Relevant data:** "Leche PIL entera 1L" `7772106000040`.
- **Steps:**
  1. Scan or type `7772106000040` and press Enter.
- **Expected:** After a scan, "Producto" shows only the barcode digits, so the confirmation of *which* product was chosen should be easy to read for users in their 50s.
- **Actual:** The only place the product name appears is a small, light-grey line under the field ("Elegido: Leche PIL entera 1L · 7772106000040 · precio de venta Bs 9,00"). This gets in the way of checking that the right product was scanned (see also F12).

## Manual test cases

### M1. Loading a delivery with the real USB scanner

- **Preconditions:** The owners' laptop, their USB scanner as set up for daily use, and at least 10 products with barcodes already created.
- **Steps:**
  1. Open "Ingresar stock".
  2. For each of the 10 products: scan the barcode, type the quantity, type the cost with decimals (for example `12,50`), and press Enter, as fast as possible.
  3. After the 10 saves, choose each product again and check "Stock actual" and "Último costo unitario".
- **Expected:** Each scan chooses the right product (check the "Elegido" line) and shows no errors before saving. Each lot is saved with its own cost (no cost carried over, see F1). Stock and unit costs match what was entered.

### M2. The scanner fires while the cursor is in the wrong field

- **Preconditions:** Same as M1.
- **Steps:**
  1. Choose a product, click in "Cantidad" and scan a barcode.
  2. Repeat with the cursor in "Costo total", "Fecha" and "Nota".
- **Expected:** Nothing is saved with the barcode as the quantity, cost, date or note. A clear message appears, or the scan is redirected to "Producto". (With typed input, "Cantidad" rejects 13 digits and "Costo total" rejects "7772106" with "El costo total debe estar entre Bs 0,00 y Bs 99.999,99.", but "Nota" would accept the digits.)

### M3. A scanner set up with a different suffix (none, Tab, or CR+LF)

- **Origin:** `web research — keyboard-wedge scanner suffix configuration`
- **Preconditions:** A scanner that can be reconfigured, or a second scanner model.
- **Steps:**
  1. Set the suffix to "none", scan a barcode and check that the product is chosen.
  2. Set the suffix to Tab and repeat.
  3. Set it to CR+LF and repeat.
- **Expected:** In every case the product is chosen, focus ends in "Cantidad", no empty lot is saved and no false errors appear.

### M4. How the date looks and is typed with the owners' Windows language

- **Preconditions:** The owners' laptop with its usual Windows and browser language (Spanish).
- **Steps:**
  1. Open "Ingresar stock" and look at how "Fecha" shows today (10 November 2026).
  2. Type a past date with the keyboard only, for example 5 November 2026.
- **Expected:** The date shows as day/month/year (10/11/2026), and typing day, then month, then year gives 5 November. (In my test browser, which is set to English, it showed "11/10/2026", month first, which a Bolivian user would read as 11 October.)

### M5. The server stops in the middle of a save

- **Preconditions:** The app running on the laptop. The developer can stop the server.
- **Steps:**
  1. Fill a full entry (product, quantity, cost with decimals, note).
  2. Stop the server and click "Guardar ingreso".
  3. Start the server again and click "Guardar ingreso" once more.
- **Expected:** On the first try, a clear Spanish message says it could not be saved and the typed data stays in the form. After the retry, the lot is saved exactly once ("Stock actual" goes up only once).

### M6. The browser or laptop closes right after "Guardar ingreso"

- **Preconditions:** The owners' laptop.
- **Steps:**
  1. Fill a full entry and click "Guardar ingreso", then immediately close the browser (or turn off the laptop).
  2. Open the app again, choose the same product and look at "Stock actual".
- **Expected:** The lot is either saved once or not at all, never partly saved and never twice. The user can tell which from "Stock actual".

### M7. The page stays open past midnight

- **Origin:** `web research — receive date defaults to today`
- **Preconditions:** "Ingresar stock" open on the laptop late in the evening.
- **Steps:**
  1. Leave the page open (no reload) until after midnight.
  2. Fill an entry without touching "Fecha" and save.
- **Expected:** "Fecha" shows the new day (or the user is warned). The lot is not saved with yesterday's date without the user noticing.

### M8. The decimal key on the numeric keypad

- **Preconditions:** The owners' keyboard (Spanish/Latin American layout) with NumLock on.
- **Steps:**
  1. In "Costo total", type `12`, press the keypad's decimal key, then type `50`.
  2. In "Cantidad", do the same with `2`, decimal key, `5`.
- **Expected:** "Costo total" shows "12,50". "Cantidad" either accepts 2,5 or shows a clear message, and never silently becomes 25 (see F2).

### M9. Browser autofill suggestions while scanning

- **Origin:** `web research — autocomplete intercepting the scanner's Enter`
- **Preconditions:** The owners' browser after some days of use, so it has a history of typed values.
- **Steps:**
  1. Scan barcodes into "Producto" and type values into "Cantidad", "Costo total" and "Nota" for several entries.
  2. Watch whether the browser's own suggestion list appears over the app's product list.
- **Expected:** Only the app's product list appears. The scanner's Enter never picks a browser suggestion instead of the scanned product.

## Resolution

To be filled in by the developer after triage.

| Finding | Decision (fixed / ticket / discarded) | Ticket or reason                                                                                                                                                                       |
| ------- | ------------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| F1      | fixed                                 | Leaving the cost field made it emit its old value after the form was cleared; the cursor now moves before the clearing. Same fix in `Nuevo producto`, which had the bug with the price |
| F2      | discarded                             | Quantities are whole units by decision (unit of measure postponed); the dropped comma is visible in the field and in the summary before saving                                         |
| F3 | discarded | As F1 in MKA-4 and MKA-15: the owners do not usually write a thousands separator, and the field shows `1,25` and the summary the unit cost before the lot is saved |
| F4 | discarded | Not reproducible with a real paste: Mantine normalizes pasted text in the `paste` event (`12.5` → `12,5`); the QA browser inserted the text without that event |
| F5 | discarded | Nothing wrong is saved: the product is chosen and the errors only point at the fields still empty. The scanner sends no Enter today |
| F6 | discarded | A double Enter right after saving is rare, and choosing the product again to re-enter the lot shows the stock that already includes it |
| F7 | fixed | When the typed text matches products but none was chosen, the message now asks to choose it from the list instead of saying the product does not exist. Tab still does not choose |
| F8 | ticket | #20 (navigation menu) and #21 (create a product from "Ingresar stock" when its barcode is unknown) |
| F9 | fixed | The date can be at most one year before today; an older one is rejected with "La fecha no puede ser anterior al {date}.", and the date picker does not offer it |
| F10 | discarded | Going back to today after each save was decided in the ticket: most lots are entered the day they arrive, and loading from paper is occasional. A way to keep the date can come later if that changes |
| F11 | discarded | Pasting a cost of more than 7 integer digits is very rare and nothing wrong is saved: the field stays visibly empty. The digit limit keeps the cost in cents within the API's integer |
| F12 | discarded | The list shows both products with their codes, and the "Elegido" line shows which one was chosen before saving. The cause is the duplicate names of F13 |
| F13 | ticket | #12 (show similar products while typing the name of a new product) |
| F14 | discarded | The cost cases, as F4; the quantity `1.200`, as F3 |
| F15 | discarded | A cost of Bs 0 is accepted by decision of the ticket (a supplier's bonus), and the summary shows the unit cost of Bs 0,00 before saving |
| F16 | fixed | Quantities shown on screen (stock in the summary, units in "Ingreso guardado") now have a thousands dot, like money. Input fields keep no separator, as decided in F16 of MKA-4 |
| F17 | discarded | The system should not round an amount the user typed: the field takes no third decimal, the user sees the value that stays and adjusts it if needed |
| F18 | discarded | The product is searched or scanned, not typed in, so its message does not start with "Ingrese". "Ingreso" is the stock coming in, the same word as in "Ingresar stock", "Ingreso guardado" and "Fecha del ingreso" |
| F19 | discarded | As F17 in MKA-4 and F7 in MKA-15: leaving by accident loses at most one entry, which is little data to type again |
| F20 | discarded | The "Elegido" line is visible enough right under the field, and the users handle dense information well |

## Manual test results

To be filled in by the developer after running the manual test cases.

| Case | Result (passed / failed / not run) | Decision (fixed / ticket / discarded) | Notes, ticket or reason |
| --- | --- | --- | --- |
| M1 | not run | | Its parts are already covered: choosing the product by scanning, the cost not carrying over (F1, fixed and checked) and the stock and unit cost in the summary |
| M2 | not run | discarded | Redirecting a scan to "Producto" or warning about it was never part of the ticket. A barcode in "Cantidad" or "Costo total" is rejected by the range check, and one in "Nota" changes neither stock nor cost |
| M3 | passed | | With Enter (or CR+LF) the errors of F5 appear, which is expected: that suffix is not used today, and it saves nothing wrong |
| M4 | not run | | Needs the owners' laptop, which was not at hand. Worth running there: type 05, 11, 2026 and check that the summary says "5 de noviembre" |
| M5 | passed | | |
| M6 | passed | | Closing the browser right after saving: the lot was saved once. Turning off the laptop was not tried |
| M7 | not run | | By the code, "Fecha" keeps the day the page was opened, but the summary compares it with today on every render and shows "(no es hoy)" in bold, so the user is warned. After the first save the date goes back to the new day |
| M8 | passed | | The cost shows "12,50". In "Cantidad" the decimal key is ignored and 2,5 becomes 25, as in F2 (discarded) |
| M9 | not run | | Needs days of use in the owners' browser. By the code, "Producto", where the scanner types, has `autoComplete="off"` (Mantine sets it on every Combobox field), so no browser suggestion can take the scanner's Enter. The other fields may show suggestions, but nothing is scanned there and a suggestion only applies if the user picks it |
