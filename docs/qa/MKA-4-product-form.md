# QA report: Nuevo producto (product entry form)

- **Date:** 2026-10-25
- **URL:** http://localhost:5130/
- **Data that already existed when I started:** The screen has no product list, so existing data could only be inferred. The first generated internal code was `TMV00001`, so no product with an internal code existed yet. None of the barcodes I used were in the database before this session (`7772106000019`, `7772106000026`, `7772106000040` … `7772106000125`, `96385074`, `036000291452`, `0036000291452`). Every product mentioned below was created during this session.

## Summary

17 findings: **4 High, 7 Medium, 6 Low.**

Most important:

- **F1 (High):** A price typed with a thousands separator, `1.234,50`, becomes `1,23` without warning and is saved as `Bs 1,23`.
- **F2 (High):** There is no plausibility limit on the price. A barcode scanned into the price field is saved as `Bs 12.345.670,00`.
- **F3 (High):** Ticking "Sin código de barras" silently throws away a barcode that was already typed or scanned. Unticking it does not bring the barcode back.
- **F4 (High):** With a scanner that ends with Tab, a single stray Space saves a product that has a real barcode under an internal code (`TMV00008`), so the product can no longer be found by scanning it.
- **F5 (Medium):** A double Enter replaces the "Producto guardado" confirmation with three red errors. The user believes the save failed, retries, and gets "Ya existe un producto con este código."

Scenarios from the web research that passed: a currency symbol typed into the price (`Bs 12,50` → `12,50`), a space as thousands separator (`1 234,50` → `1234,50`), leading zeros kept in codes, spaces around a code trimmed, and EAN-8, UPC-A and EAN-13 check digits validated.

## Findings

### F1. A price with a thousands separator ("1.234,50") is silently saved as Bs 1,23

- **Severity:** High
- **Origin:** `own testing`
- **Relevant data:** Barcode `7772106000057` was new. The price was typed key by key as `1.234,50`.
- **Steps:**
  1. In "Nombre", type `Cocina a gas 4 hornallas`.
  2. In "Precio de venta", type `1.234,50`, writing the dot as a thousands separator the way bank software uses it.
  3. In "Código", type `7772106000057`.
  4. Click "Guardar producto".
- **Expected:** The field accepts `1.234,50` as one thousand two hundred thirty-four bolivianos. At the very least, it rejects the input with a clear message.
- **Actual:** While typing, the field turns the dot into a decimal comma and cuts the extra digits, so it shows `1,23`. The save succeeds with "Producto guardado — Cocina a gas 4 hornallas · 7772106000057 · Bs 1,23". Nothing warns the user. The app itself displays prices with a dot as thousands separator (`Bs 1.000.000,00`, `Bs 12.345.670,00`), which makes users more likely to type it that way.

### F2. No plausibility limit on the price: a barcode scanned into "Precio de venta" is saved as the price

- **Severity:** High
- **Origin:** `own testing`
- **Relevant data:** Barcodes `7772106000118` and `7772106000064` were new.
- **Steps:**
  1. In "Nombre", type `Azúcar Guabirá 1kg`.
  2. In "Precio de venta", enter `12345670`, as if a barcode had been scanned into the wrong field.
  3. In "Código", enter `7772106000118` and press Enter.
  4. Do the same with `Bolsa plastica`, price `1000000`, code `7772106000064`.
- **Expected:** A price that is unreasonable for a small grocery store is blocked, or at least needs a confirmation such as "¿Está seguro? El precio es muy alto".
- **Actual:** Both are saved: "Azúcar Guabirá 1kg · 7772106000118 · Bs 12.345.670,00" and "Bolsa plastica · 7772106000064 · Bs 1.000.000,00". A full 13-digit barcode in the price field gives a different result; see F9.

### F3. Ticking "Sin código de barras" silently erases the barcode that was already entered

- **Severity:** High
- **Origin:** `own testing`
- **Relevant data:** Barcode `7772106000033` had been typed. The next internal code was `TMV00002`.
- **Steps:**
  1. Type `Queso criollo` as the name and `35` as the price.
  2. Type `7772106000033` in "Código".
  3. Tick "Sin código de barras".
  4. Untick it.
- **Expected:** Either the checkbox cannot be ticked while a barcode is present, or the app asks first. Unticking brings back the barcode that was typed.
- **Actual:** At step 3, "Código" is replaced by `TMV00002` without any message. At step 4, "Código" is empty and the scanned barcode is gone. If the user saves after step 3, a product that has a real barcode gets an internal code. F4 shows how this happens by accident.

### F4. With a scanner that ends with Tab, one stray Space saves a barcoded product with an internal code

- **Severity:** High
- **Origin:** `web research — scanner prefix/suffix configuration (Tab instead of Enter)`
- **Relevant data:** Barcode `7772106000125` was new. The next internal code was `TMV00008`.
- **Steps:**
  1. Type `Café Copacabana 250g` as the name and `25` as the price.
  2. In "Código", enter `7772106000125` followed by Tab, which is what a scanner with a Tab suffix sends.
  3. Focus is now on the "Sin código de barras" checkbox. Press Space, as a user would when starting to type the next product or by accident.
  4. Press Enter.
- **Expected:** The scanned barcode is kept, and the product is saved with `7772106000125`.
- **Actual:** The Space ticks the checkbox and replaces the barcode with `TMV00008`. Enter saves "Café Copacabana 250g · TMV00008 · Bs 25,00". The product can no longer be found by scanning its package.

### F5. A double Enter hides the "Producto guardado" confirmation and shows three errors

- **Severity:** Medium
- **Origin:** `own testing`
- **Relevant data:** Barcode `7772106000101` was new.
- **Steps:**
  1. Enter `Sal Cumbre 1kg`, price `2,5` and code `7772106000101`.
  2. Press Enter twice in quick succession, for example the scanner's Enter followed by the user's own Enter.
- **Expected:** The product is saved once, the confirmation stays visible, and the second Enter on the empty form is ignored or does not hide the confirmation.
- **Actual:** The product is saved, but the confirmation disappears. In its place the empty form shows "Ingrese el nombre del producto.", "Ingrese el precio de venta." and "Ingrese el código del producto." The user believes the product was not saved. When they enter it again, they get "Ya existe un producto con este código." without being told that it is the product they just saved.

### F6. "Ya existe un producto con este código." does not say which product has the code

- **Severity:** Medium
- **Origin:** `own testing`
- **Relevant data:** `7772106000019` already existed as "Leche PIL entera 1L" at Bs 7,50.
- **Steps:**
  1. Enter `Leche PIL deslactosada`, price `8`, code `7772106000019`.
  2. Press Enter.
- **Expected:** The message names the existing product, for example "Ya existe «Leche PIL entera 1L» (Bs 7,50) con este código", so the user can tell whether it is the same item or a scanning mistake.
- **Actual:** The message is only "Ya existe un producto con este código." This screen offers no way to find out which product it is.

### F7. A saved mistake cannot be reviewed or corrected from this screen

- **Severity:** Medium
- **Origin:** `own testing`
- **Relevant data:** The products saved with wrong prices in F1 and F2, for example "Cocina a gas 4 hornallas" at Bs 1,23.
- **Steps:**
  1. Save a product with a wrong price, as in F1.
  2. Try to see or correct what was just loaded.
- **Expected:** When loading hundreds of products, the user can see the list of products loaded in the session and fix or undo the last one.
- **Actual:** Only the last confirmation is shown, and it disappears on the next save, on a double Enter (F5) or on a reload. There is no list, no edit and no undo. The user can't fix a mistake noticed one product later.

### F8. The same product can be saved twice under its UPC-A and EAN-13 forms

- **Severity:** Medium
- **Origin:** `web research — duplicate EAN/UPC validation`
- **Relevant data:** `036000291452` (UPC-A) and `0036000291452` (the same code in EAN-13 form) were both new.
- **Steps:**
  1. Save `Pañuelos UPC`, price `3`, code `036000291452`.
  2. Save `Pañuelos EAN13`, price `3`, code `0036000291452`.
- **Expected:** The second save is rejected as a duplicate, because both codes identify the same package. Depending on its configuration, a scanner sends one form or the other.
- **Actual:** Both are saved as separate products: "Pañuelos UPC · 036000291452 · Bs 3,00" and "Pañuelos EAN13 · 0036000291452 · Bs 3,00".

### F9. A very large price gives only "Ocurrió un error inesperado." and hides the other errors

- **Severity:** Medium
- **Origin:** `own testing`
- **Relevant data:** Barcode `7772106000064` was new at the time of the first attempt.
- **Steps:**
  1. Enter `Bolsa plastica` and code `7772106000064`, and type `99999999999999,99` as the price. A 13-digit barcode such as `7772106000118` gives the same result.
  2. Press Enter.
  3. Repeat with the price `7772106000118` and "Código" left empty.
- **Expected:** A field message on "Precio de venta" explains that the price is too high. The empty "Código" is also reported.
- **Actual:** Only a banner with "Ocurrió un error inesperado." appears. It does not say which field is wrong or what to do. In step 3 the empty "Código" is not reported at all, although on its own it normally shows "Ingrese el código del producto."

### F10. With two tabs open, the app's own generated code is rejected and the user is stuck

- **Severity:** Medium
- **Origin:** `own testing`
- **Relevant data:** The next internal code was `TMV00004`.
- **Steps:**
  1. Open the screen in two browser tabs.
  2. In both tabs, tick "Sin código de barras". Both show `TMV00004`.
  3. Save `Cebolla kg`, price `5`, in tab 2. It is saved as `TMV00004`.
  4. In tab 1, save `Tomate kg`, price `6`.
- **Expected:** Tab 1 gets the next free code automatically, or the message says what to do.
- **Actual:** Tab 1 shows "Ya existe un producto con este código." under a code the user did not type and cannot edit. The only way out is to untick and tick the checkbox again, which gives `TMV00005`, and nothing tells the user to do that.

### F11. A barcode scanned while the cursor is in "Nombre" becomes the product name, and the errors don't explain why

- **Severity:** Medium
- **Origin:** `own testing`
- **Relevant data:** After every save the cursor goes back to "Nombre". Scanning the package first is the natural habit.
- **Steps:**
  1. Right after a save, with the cursor in "Nombre", scan or type `7772106000071` followed by Enter.
- **Expected:** The app recognizes that a barcode landed in "Nombre", moves it to "Código" or warns about it, and does not submit.
- **Actual:** The form is submitted, "Nombre" keeps `7772106000071`, and the errors are "Ingrese el precio de venta." and "Ingrese el código del producto." Nothing indicates that the code went into the wrong field. In general, Enter in any field submits the form instead of moving to the next field, which people used to bank software may expect.

### F12. The same name can be saved twice without warning

- **Severity:** Low
- **Origin:** `own testing`
- **Relevant data:** "Leche PIL entera 1L" already existed with code `7772106000019` at Bs 7,50.
- **Steps:**
  1. Save `Leche PIL entera 1L`, price `9`, code `7772106000040`.
- **Expected:** A warning such as "Ya existe un producto con este nombre", which still allows saving because two package sizes can share a name.
- **Actual:** It is saved silently: "Leche PIL entera 1L · 7772106000040 · Bs 9,00". There are now two products with an identical name and different prices.

### F13. A minus sign in the price is silently dropped

- **Severity:** Low
- **Origin:** `own testing`
- **Steps:**
  1. Type `-5` in "Precio de venta".
- **Expected:** The input is rejected with a message, or the minus sign is never accepted while typing.
- **Actual:** The field shows `5` without any message. Price `0`, by contrast, is rejected correctly with "El precio de venta debe ser mayor a cero."

### F14. Typing an internal code by hand is accepted or rejected inconsistently

- **Severity:** Low
- **Origin:** `own testing`
- **Relevant data:** The next internal code was `TMV00006`, and later `TMV00007`.
- **Steps:**
  1. With "Sin código de barras" unticked, save `Papa kg`, price `4`, code `TMV00006`.
  2. Then try the code `TMV00009`.
  3. Then try `TMV00007`.
- **Expected:** One consistent rule. The message "Los códigos internos los asigna el sistema." suggests that typing an internal code is never allowed.
- **Actual:** Steps 1 and 3 are saved ("Papa kg · TMV00006 · Bs 4,00", "Zanahoria kg · TMV00007 · Bs 5,00"). Step 2 is rejected with "Los códigos internos los asigna el sistema." Typing the next code in the sequence is apparently allowed, but nothing says so.

### F15. The name has no length limit, and the confirmation overflows

- **Severity:** Low
- **Origin:** `own testing`
- **Relevant data:** Barcodes `7772106000088` and `7772106000095` were new.
- **Steps:**
  1. Save a product whose name is about 320 characters long, containing `"`, `<b>&amp;</b>`, `ñ`, `¿` and an emoji.
  2. Save one whose name is about 1,000 characters long.
- **Expected:** A reasonable maximum with a message, for example "El nombre no puede tener más de N caracteres".
- **Actual:** Both are saved. Special characters are shown literally, which is correct. The long run of characters in the confirmation runs past the green box and is cut off on the right.

### F16. Prices are shown differently while typing and after saving

- **Severity:** Low
- **Origin:** `own testing`
- **Steps:**
  1. Type `7,50`, then tab out. Type `1234,5`, then tab out.
- **Expected:** One money format everywhere, such as `7,50` and `1.234,50`.
- **Actual:** The field shows `7,5` and `1234,5` (no trailing zero, no thousands separator). The confirmation shows `Bs 7,50` and `Bs 1.000.000,00`. This also contributes to F1.

### F17. The code is worded differently from one message to another, and half-entered data is lost without warning

- **Severity:** Low
- **Origin:** `own testing`
- **Steps:**
  1. Trigger each code error: leave it empty, enter `7772106000017`, enter `ABC-12345`.
  2. Type a name and a price, then reload the page or press the browser's Back button.
- **Expected:** The same term everywhere. A warning before half-entered data is discarded.
- **Actual:**
  - The field is called "Código". The messages say "Ingrese el código del producto.", "El código de barras no es válido. Revise que esté bien escrito o marque "Sin código de barras"." and "El código de barras solo puede tener números."
  - Reload clears the form without warning, and Back leaves the app for a blank page.

## Manual test cases

### M1. Fast loading with a real scanner (Enter suffix)

- **Preconditions:** A USB scanner in keyboard mode with Enter as suffix. 20 packages with different barcodes not yet in the database.
- **Steps:**
  1. For each package, type the name, Tab, the price, Tab, then scan the package.
  2. Go as fast as possible, without looking at the screen between items.
- **Expected:** 20 products saved, each with the complete 13 digits, with no lost or duplicated digits. Each scan saves exactly one product, and no product is saved twice.

### M2. Scanner configured with Tab suffix, no suffix, or a prefix

- **Preconditions:** The same scanner, reconfigured in turn for each suffix or prefix option.
- **Steps:**
  1. Repeat M1 with each configuration.
  2. With the Tab suffix, keep typing the next product right after the scan.
- **Expected:** The barcode is never replaced by a `TMV…` code (see F4). Prefix characters do not end up in "Código" or are rejected with a clear message.

### M3. Scanning with the cursor in the wrong field

- **Preconditions:** A real scanner.
- **Steps:**
  1. After a save, scan without first clicking "Código". The cursor is in "Nombre".
  2. Put the cursor in "Precio de venta" and scan.
- **Expected:** The barcode is not saved as a name or as a price. See F2 and F11.

### M4. Scanner keyboard layout vs. the Windows keyboard layout

- **Preconditions:** Windows keyboard set to Spanish (Latin America). The scanner left on its factory layout (US).
- **Steps:**
  1. Scan EAN-13 and EAN-8 products.
- **Expected:** The digits arrive exactly as printed under the bars.

### M5. Decimal key of the numeric keypad and pasting a whole value

- **Preconditions:** The laptop's real keyboard with numeric keypad, Windows region set to Bolivia.
- **Steps:**
  1. Type `7`, the keypad decimal key, `50`.
  2. Copy `7.50`, `1.234,50` and `Bs 7,50` from Excel or WhatsApp and paste each with Ctrl+V and with right-click → Pegar.
  3. Accept a value the browser suggests from autocomplete, if one appears.
- **Expected:** The price is always `7,50` or `1.234,50`. In testing, a value inserted all at once as `7.50` ended up as `750`. Keystroke-by-keystroke typing and Ctrl+V gave `7,50`. Confirm on the real machine that no real way of entering a value produces `750`.

### M6. The server stops in the middle of a save

- **Preconditions:** The form is filled with a new product.
- **Steps:**
  1. Stop the backend.
  2. Press "Guardar producto".
  3. Restart the backend and press "Guardar producto" again.
- **Expected:** A clear Spanish message saying the product was not saved and the system is not responding, rather than "Ocurrió un error inesperado.". The form keeps the data. The retry saves the product once, and the `TMV` sequence does not skip or repeat.

### M7. Slow or lost response after a successful save

- **Preconditions:** The backend slowed down so that a save takes several seconds.
- **Steps:**
  1. Press "Guardar producto".
  2. Press Enter or click again while waiting.
- **Expected:** The button shows that it is working and cannot be pressed again. One product is saved. No "Ya existe un producto con este código." appears for the user's own product.

### M8. The browser or laptop closes in the middle of loading

- **Preconditions:** Several products saved, and a half-filled form.
- **Steps:**
  1. Close the browser, or turn off the laptop.
  2. Reopen the app.
  3. Save a new product without barcode.
- **Expected:** All products saved before the close are still there, with their data intact. The half-filled form is either restored or clearly lost, and the user knows which. The new internal code continues the sequence without repeating an existing one.

## Resolution

To be filled in by the developer after triage.

| Finding | Decision (fixed / ticket / discarded) | Ticket or reason                                                                                                                              |
| ------- | ------------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------- |
| F1      | discarded                             | Users check the screen before saving, the confirmation shows the saved price, and prices above Bs 1.000 are rare in the store                 |
| F2      | fixed                                 | The API accepts prices from Bs 0,01 to Bs 9.999,99 only                                                                                       |
| F3      | fixed                                 | Unticking "Sin código de barras" brings back the code that was there before ticking it                                                        |
| F4      | discarded                             | The scanner is configured to send nothing after the code, so no Tab moves the focus to the checkbox; unticking it brings the code back (F3)   |
| F5      | discarded                             | The scanner is configured to send nothing after the code, so it adds no Enter; when in doubt, the product list will show whether it was saved |
| F6      | discarded                             | Knowing that a product already has the code is enough; which product it is does not matter when creating one                                  |
| F7      | ticket                                | [#11 List products](https://github.com/javier0097/MarketApp/issues/11); editing comes after the MVP                                           |
| F8      | discarded                             | The store's only scanner is configured to always send a code in the same form; duplicates under another code are handled in F12              |
| F9      | fixed                                 | The price field takes at most 7 whole digits, so a too-high price gets the range message                                                      |
| F10     | fixed                                 | When the internal code is rejected, the checkbox tells the user to untick and tick it again to get a new one                                  |
| F11     | discarded                             | A code scanned into "Nombre" leaves the name to be typed elsewhere, so the user notices; the scanner sends no Enter, so nothing is submitted |
| F12     | ticket                                | [#12 Show similar products while typing the name](https://github.com/javier0097/MarketApp/issues/12); an exact name check would miss variants |
| F13     | discarded                             | A product never has a negative price, so the field never accepts the minus sign; the value left is visible on screen                         |
| F14     | discarded                             | By design a typed internal code is accepted only if it is exactly the next one, the same code the checkbox gives                             |
| F15     | fixed                                 | The name is limited to 200 characters with a message and a character counter, and the confirmation wraps long text                   |
| F16     |                                       |                                                                                                                                               |
| F17     |                                       |                                                                                                                                               |
