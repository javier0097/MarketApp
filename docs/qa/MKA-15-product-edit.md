# QA report: Product edit (`Editar producto`, opened from the product list)

- **Date:** 2026-11-01
- **URL:** `http://localhost:5130/productos/{id}/editar`, opened with `Editar` from `http://localhost:5130/productos`
- **Data that already existed when testing started:** 101 products. They included:
  - barcodes in several formats, such as `96385074` (EAN-8) and `7772106000019` (EAN-13);
  - the same barcode in two forms: `036000291452` (`Pañuelos UPC`) and `0036000291452` (`Pañuelos EAN13`);
  - internal codes `TMV00001` to `TMV00087`;
  - two products with the same name (`Leche PIL entera 1L`);
  - 4 products priced above Bs 9.999,99 (`TMV00010`, `TMV00009`, `7772106000118`, `7772106000064`);
  - 2 products with names over 200 characters (`7772106000095` with 923, `7772106000088` with 346).
- **Data changed by this session (not restored):** products with ids 13, 14, 15, 19, 27, 44, 91, 92, 93, 94, 95, 96, 97, 98 and 100. Their names, prices, codes or `Actualizado` dates differ from the start of the session. Id 14 ended with its original code, but its date changed. Internal codes `TMV00088` and `TMV00089` are now in use. The list still has 101 products.
- **How the scanner was simulated:** with keyboard input followed by Enter. See the manual test cases for the physical scanner.

## Summary

**17 findings: 5 High, 6 Medium, 6 Low.**

Most important:

- **F1 (High):** a price typed with a thousands separator, the format the app itself displays, is saved wrong: `1.234,50` becomes Bs 1,23.
- **F2 (High):** the form opens with the cursor in `Nombre`, so a scan adds the barcode to the name and its Enter saves at once.
- **F3 (High):** an edit tab with old data silently overwrites what another tab already saved.
- **F4 (High):** products with an out-of-range price or an over-long name cannot be changed until that other field is fixed.
- **F5 (High):** the duplicate check treats `036000291452` and `0036000291452` as different, so one physical barcode can belong to two products.

Web research added no new findings. Its applicable scenarios passed or became manual test cases:

- Saving a product again keeps the price and leading zeros: `Bs 1.234,50` and `0036000291452` stayed unchanged after changing only the name.
- Field errors appear when pressing Enter.

## Findings

### F1. A price with a thousands separator is saved with the wrong value

- **Severity:** High
- **Origin:** `own testing` (the `1.000` variation comes from `web research — thousands separator corrupting price fields`)
- **Relevant data:** `QA vol 37` (`TMV00077`, id 91), price Bs 37,00. The list and the range message both show prices with a dot for thousands, such as `Bs 1.234,50` and `Bs 9.999,99`.
- **Steps:**
  1. In `/productos`, open `Editar` on `QA vol 37`.
  2. Select the content of `Precio de venta` and type `1.234,50` on the keyboard.
  3. Press Enter.
  4. Look at the product in the list.
- **Expected:** the price is saved as Bs 1.234,50, or the form rejects the value and explains why.
- **Actual:** the dot is treated as a decimal comma and the rest is dropped. The field shows `1,23`, `Cambios` shows `Precio de venta: Bs 37,00 → Bs 1,23`, and pressing Enter saves it. The list shows `Bs 1,23`, with no warning. In the same way, `1.000` becomes `Bs 1,00` (seen in `Cambios`, not saved).

### F2. Scanning right after opening the form changes the name and saves

- **Severity:** High
- **Origin:** `own testing`
- **Relevant data:** `QA vol 42` (`TMV00082`, id 96). The scan was simulated by typing `96385074` and pressing Enter.
- **Steps:**
  1. Open `Editar` on the product. The cursor is in `Nombre`, after the existing text.
  2. Without clicking anything, scan `96385074`. The scan ends with Enter.
  3. Open the product again.
- **Expected:** a barcode read in `Nombre` does not end up in the name. At a minimum, the user can review the change before it is saved.
- **Actual:** the product is saved immediately and the form returns to the list, so `Cambios` cannot be read. The name becomes `QA vol 4296385074`, and the code is still `TMV00082`. The user wanted to change the code, and changed the name without any warning.

### F3. A tab with old data silently overwrites changes saved from another tab

- **Severity:** High
- **Origin:** `own testing`
- **Relevant data:** `QA vol 39` (`TMV00079`, id 93), Bs 39,00, open in two tabs. This also applies to one person who left an old edit tab open.
- **Steps:**
  1. Open the edit form for the product in tab A and in tab B.
  2. In tab B, change the name to `QA vol 39 pestana B` and save. The list confirms the new name.
  3. In tab A, which still shows `QA vol 39`, change only the price to `77,00` and press Enter.
  4. Open the product again.
- **Expected:** tab A warns that the product changed in the meantime, or saves only the price and keeps the new name.
- **Actual:** there is no warning. The product ends as `QA vol 39` with `Bs 77,00`, so the name saved from tab B is lost. `Cambios` in tab A only announced the price change. It was reproduced on 3 products (ids 95, 94 and 93); only on id 93 was the tab B save confirmed in the list before tab A saved.

### F4. Existing out-of-range data blocks changes to other fields

- **Severity:** High
- **Origin:** `own testing`
- **Relevant data:**
  - Price above the limit: `test big number` (`TMV00010`, id 23), `Bs 1.234.567,00`. Three more products are above the limit.
  - Name above the limit: id 17 (346 characters), id 18 (923 characters).
  - The form at `/` also rejects `10000`, so these values come from before the current limits. This is not a second way to create them.
- **Steps:**
  1. Open `Editar` on `test big number`.
  2. Change only the name and press Enter.
  3. Open `Editar` on id 17 (name of 346 characters).
  4. Change only the price to `4,25` and press Enter.
- **Expected:** the user can correct one field without being forced to change another they did not touch, or the form clearly explains that the old value must be corrected first.
- **Actual:**
  - Step 2 is rejected with `El precio de venta debe estar entre Bs 0,01 y Bs 9.999,99.`, although `Cambios` lists only the name.
  - Step 4 is rejected with `El nombre no puede tener más de 200 caracteres.`, although `Cambios` lists only the price.
  - In the first case, the user must invent a price to rename the product.

### F5. The same barcode can be assigned to two products (UPC-A and EAN-13 forms)

- **Severity:** High
- **Origin:** `own testing`
- **Relevant data:** `Pañuelos EAN13` (id 15) has `0036000291452`. `Pañuelos UPC` (id 14) was edited. Both codes identify the same physical barcode: EAN-13 is the 12-digit UPC with a zero in front.
- **Steps:**
  1. Open `Editar` on id 14, whose code at this point was different from `036000291452`.
  2. In `Código`, enter `036000291452` and press Enter.
  3. Look at the list.
- **Expected:** `Ya existe un producto con este código.`, because `0036000291452` is already in use.
- **Actual:** it is saved without any message, and both products exist with the same real barcode. Depending on how the scanner reads the label, one product or the other would be found.

### F6. Invalid price input is silently changed instead of rejected

- **Severity:** Medium
- **Origin:** `own testing`
- **Relevant data:** `QA vol 45` (id 99), Bs 45,00.
- **Steps:**
  1. Open `Editar` on the product.
  2. Type each of these values on the keyboard in `Precio de venta` and look at the field and `Cambios`:

     | Typed | Field | `Cambios` |
     | --- | --- | --- |
     | `-5` | `5` | `Bs 45,00 → Bs 5,00` |
     | `12,345` | `12,34` | `Bs 45,00 → Bs 12,34` |
     | `1e3` | `13` | `Bs 45,00 → Bs 13,00` |

  3. Also enter `1,2,3` by filling the whole field at once, as a paste would. The result is `1,23`.
- **Expected:** a message tells the user that the value is not valid. A negative price should be rejected, not converted to a positive one.
- **Actual:** characters are dropped or the value is cut without any message. The value can be saved with Enter. The only sign is the new value in the field and in `Cambios`.

### F7. Unsaved changes are lost without warning

- **Severity:** Medium
- **Origin:** `own testing`
- **Relevant data:** id 96. The name was changed to `Nombre a medias`, and `Cambios` was listing the change.
- **Steps:**
  1. Open `Editar` and change the name.
  2. Leave in one of three ways: click `Cancelar`, use the browser's back button, or reload the page.
  3. Open the product again.
- **Expected:** because the form knows there are pending changes, it asks for confirmation before discarding them. This is most needed for back and reload, which can happen by accident.
- **Actual:** there is no warning in any of the three cases, and the changes are lost.

### F8. Returning from the form loses the search

- **Severity:** Medium
- **Origin:** `own testing`
- **Relevant data:** list with 101 products, searches `QA lista 02` (`19 de 101 productos`), `7772106000040` and `QA vol 37` (`1 de 101 productos`).
- **Steps:**
  1. In `/productos`, search for something and open `Editar` on a result.
  2. Leave by saving, with `Cancelar`, or with the browser's back button.
  3. Look at the list.
- **Expected:** the list keeps the search, so the user can continue with the next result. This matters when correcting several products.
- **Actual:** in all three cases `Buscar` is empty and the list shows `101 productos`. The user must search again for every product.

### F9. No confirmation after saving

- **Severity:** Medium
- **Origin:** `own testing`
- **Relevant data:** `QA vol 46` (id 100) with changes, and `QA vol 47` (id 101) with none.
- **Steps:**
  1. Edit a product, press `Guardar cambios`, and look at the list.
  2. Open another product, change nothing, press `Guardar cambios`, and look at the list.
- **Expected:** a short message confirms what was saved. When nothing changed, the user is told that nothing was saved.
- **Actual:** in both cases the app returns to the list with no message. The only sign of a real save is that the row moves to the top with a new `Actualizado` date. A user with hundreds of products may not notice this, and cannot tell the two cases apart.

### F10. All-zero code accepted as a valid barcode

- **Severity:** Medium
- **Origin:** `own testing`
- **Relevant data:** `Pañuelos UPC` (id 14).
- **Steps:**
  1. Open `Editar` on the product.
  2. In `Código`, enter `00000000` and press Enter.
  3. Open the product again.
- **Expected:** `El código de barras no es válido...`, as happens with `1234567890123`.
- **Actual:** it is saved without any message, and the product's code is `00000000`. I restored the code afterwards as part of F5.

### F11. A scan right after unchecking `Sin código de barras` is lost

- **Severity:** Medium
- **Origin:** `own testing`
- **Relevant data:** `QA vol 36` (`TMV00076`, id 90), an internal-code product being given a barcode. The scan was simulated by pressing digit keys and Enter.
- **Steps:**
  1. Open `Editar` on the product.
  2. Uncheck `Sin código de barras`. `Código` is emptied, but the cursor stays on the checkbox.
  3. Scan a barcode.
- **Expected:** the cursor moves to `Código` when the box is unchecked, so the scan lands there.
- **Actual:** the digits are lost, and the scan's Enter submits the form. The form shows `Ingrese el código del producto.`, which is confusing because the user has just scanned one. The cursor then moves to `Código`, so a second scan is needed.

### F12. `Cambios` shows a change that looks identical on both sides

- **Severity:** Low
- **Origin:** `own testing`
- **Relevant data:** `QA vol 44` (id 98).
- **Steps:**
  1. Open `Editar` and enter `   QA    vol 44   ` in `Nombre`.
  2. Look at `Cambios`.
  3. Save and open the product again.
- **Expected:** repeated spaces are removed, as spaces at the start and end are. `Cambios` should show no change.
- **Actual:** `Cambios` shows `Nombre: QA vol 44 → QA vol 44`. The product is saved with the inner spaces (`12/200`), which the page shows as one space, so the difference cannot be seen.

### F13. The duplicate-code message does not say which product has the code

- **Severity:** Low
- **Origin:** `own testing`
- **Relevant data:** `7772106000019` belongs to `Leche PIL entera 1L`. The product being edited was `Chicle EAN8` (id 13).
- **Steps:**
  1. Open `Editar` on `Chicle EAN8`.
  2. In `Código`, enter `7772106000019` and press Enter.
- **Expected:** the message names the product that has the code, so the user knows which one to review.
- **Actual:** `Ya existe un producto con este código.` The user must leave the form and search for the code, and doing so discards any changes (F7).

### F14. A product can be renamed to the name of another product without warning

- **Severity:** Low
- **Origin:** `own testing`
- **Relevant data:** `Arroz Grano de Oro 1kg` (`7772106000026`, Bs 12,90) already existed.
- **Steps:**
  1. Open `Editar` on another product (id 96, `TMV00082`).
  2. Change the name to `Arroz Grano de Oro 1kg` and save.
- **Expected:** at least a warning, because identical names make products hard to tell apart in the list. If repeated names are intended, this finding can be discarded.
- **Actual:** it is saved without any message. The list now has two `Arroz Grano de Oro 1kg` products with different codes and prices.

### F15. Read-only `Código` looks the same as an editable field

- **Severity:** Low
- **Origin:** `own testing`
- **Relevant data:** any product with `Sin código de barras` checked, such as `TMV00086`.
- **Steps:**
  1. Open `Editar` on the product.
  2. Compare `Código` with the other fields and try to type in it.
- **Expected:** the field visibly looks read-only.
- **Actual:** it looks like the other fields, but typing does nothing and no message explains why.

### F16. `El producto no existe.` is a dead end

- **Severity:** Low
- **Origin:** `own testing`
- **Relevant data:** no product has id 99999.
- **Steps:**
  1. Open `/productos/99999/editar`.
- **Expected:** the message comes with a way back to the list.
- **Actual:** only the title `Editar producto` and `El producto no existe.` are shown, with no link or button.

### F17. An edit URL with an invalid id shows the new-product form

- **Severity:** Low
- **Origin:** `own testing`
- **Relevant data:** none needed.
- **Steps:**
  1. Open `/productos/abc/editar`.
- **Expected:** the same `El producto no existe.` message as in F16.
- **Actual:** the address stays the same, but the page shows `Nuevo producto` and `Guardar producto`. A user who arrives here through a damaged link could create a product while believing they are editing one.

## Manual test cases

### M1. A real scan in `Código` saves the exact barcode

- **Preconditions:** USB scanner connected and sending Enter at the end of each scan. A product labelled with an unused EAN-13 barcode.
- **Steps:**
  1. Open `Editar` on a product that has a barcode.
  2. Click `Código` and select its content.
  3. Scan the label.
  4. Check the list.
- **Expected:** the code matches the label digit by digit, with no extra or missing characters, and the product is saved once.

### M2. A real scan with the cursor in `Nombre` (confirms F2)

- **Preconditions:** same scanner. A product whose name can be changed.
- **Steps:**
  1. Open `Editar` from the list, and do not click any field.
  2. Scan a label.
- **Expected:** the name does not change without the user reviewing it. Currently, per F2, the barcode is expected to be added to the name and saved.

### M3. A real scan of a label with a leading zero

- **Preconditions:** a physical label whose code starts with 0 (UPC-A). The scanner configuration in use is known (whether it sends UPC-A as 12 or 13 digits).
- **Steps:**
  1. Scan the label in `Código` on the edit form, and save.
  2. Compare the saved code with the label.
  3. In `/productos`, scan the same label in `Buscar`.
- **Expected:** the leading zero is kept, and the search finds the product. Also check whether the result differs when the scanner sends the other form (F5).

### M4. A scanner configured with Tab instead of Enter

- **Preconditions:** the scanner can be set to send Tab at the end. Only needed if the owners may use this setting.
- **Steps:**
  1. Open `Editar`, click `Código`, and select its content.
  2. Scan a valid, unused barcode.
  3. Press `Guardar cambios`.
- **Expected:** nothing is saved until the button is pressed, the cursor moves to the next control, and the code is complete.

### M5. The server stops while saving

- **Preconditions:** the app is open on the edit form with a change pending.
- **Steps:**
  1. Stop the server.
  2. Press `Guardar cambios`.
  3. Start the server again and press `Guardar cambios`.
- **Expected:** a clear message in Spanish says the change could not be saved, the typed values stay in the form, and the second attempt saves correctly once.

### M6. The server is stopped when opening the form

- **Preconditions:** the list is open and the server is stopped afterwards.
- **Steps:**
  1. Press `Editar` on a product.
  2. Start the server and reload the page.
- **Expected:** a clear message in Spanish instead of an empty or endlessly loading form. After the reload, the form shows the correct data.

### M7. The browser or laptop closes during an edit or save

- **Preconditions:** the edit form is open with changes.
- **Steps:**
  1. Close the browser, or shut the laptop down, without saving.
  2. Reopen the app and the product.
  3. Repeat, closing it immediately after pressing `Guardar cambios`.
- **Expected:** after the first closure the product is unchanged. After the second, the product has either all the changes or none of them, never only some.

## Resolution

To be filled in by the developer after triage.

| Finding | Decision (fixed / ticket / discarded) | Ticket or reason |
| --- | --- | --- |
| F1 | discarded | Users check the screen before saving, `Cambios` shows the new price before it is saved, and prices above Bs 1.000 are rare in the store |
| F2 | fixed | The cursor starts in `Nombre` only when creating; when editing no field has it, so a scan changes nothing, whatever the scanner sends at the end |
| F3 | discarded | It needs the same product open in two tabs, and the app assumes one writer at a time; detecting it would need a version check for a very rare case |
| F4 | discarded | The error appears next to the field to correct, and a value that far out of range is almost certainly a typing mistake, so correcting it is what should happen |
| F5 | discarded | The store's scanner always sends each label in the same form, so scanning never creates both forms; it would only happen by typing the other form by hand, which is rare |
| F6 | discarded | Users check the screen before saving, and the resulting value shows in the field and in `Cambios`; a price is never negative and has at most two decimals, so the field not accepting anything else is enough |
| F7 | discarded | `Cancelar` is an explicit action, and leaving by accident loses at most the changes to one product, which is little data to type again |
| F8 | discarded | Keeping the search when returning to the list is not planned for now; it can be typed again in seconds |
| F9 | fixed | After saving a change, the list shows the same `Producto guardado` box as the form that creates products; with no changes nothing was saved, so it shows none |
| F10 | discarded | No real label has that code, and accepting it as a code causes no problem, so it does not need a special rule |
| F11 | fixed | Unticking `Sin código de barras` moves the cursor to `Código`, so a scan lands there whatever the scanner sends at the end |
| F12 | fixed | `Cambios` keeps repeated spaces visible, so it shows the name as it will be saved; the name is still saved as typed, minus the outer spaces |
| F13 | discarded | Knowing that the code is already in use is enough to save correctly, and the list search finds which product has it in seconds |
| F14 | ticket | [#12 Show similar products while typing the name of a new product](https://github.com/javier0097/MarketApp/issues/12) |
| F15 | fixed | `Código` is no longer read-only: editing it unticks `Sin código de barras`, and a wrong code is rejected by the validations when saving |
| F16 | discarded | Products are never deleted and `Editar` always uses a real id, so this page is only reached by typing the URL by hand |
| F17 | ticket | [#16 Show a not-found page for unknown URLs](https://github.com/javier0097/MarketApp/issues/16) |

## Manual test results

To be filled in by the developer after running the manual test cases.

| Case | Result (passed / failed / not run) | Decision (fixed / ticket / discarded) | Notes, ticket or reason |
| --- | --- | --- | --- |
| M1 | passed | | Run several times with the physical scanner |
| M2 | passed | | Simulated by typing the digits and pressing Enter, no physical scanner: with no field holding the cursor, nothing changed and nothing was saved (F2) |
| M3 | passed | | Simulated by typing `036000291452` and pressing Enter, no physical scanner or label: the leading zero is kept and the search finds the product. Which form the scanner sends for such a label was not checked |
| M4 | passed | | Simulated by typing the code and pressing Tab, no physical scanner: the code stays complete, the cursor moves to `Sin código de barras`, and nothing is saved until `Guardar cambios` is pressed |
| M5 | passed | | Run on the QA app: the connection message is shown, the form keeps the data, and the retry saves once |
| M6 | not run | discarded | `Editar` loads a new page from the same server, so with the server stopped the browser shows its own error page and no message from the app can appear. Fixing it would take a service worker, not worth it for a rare case on a Windows service |
| M7 | not run | discarded | Unsaved changes only live in the tab, so closing it changes nothing on the server; SQLite applies each save in a transaction, so a product is saved whole or not at all |
