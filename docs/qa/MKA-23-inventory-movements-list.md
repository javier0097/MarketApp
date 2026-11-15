# QA report: Movimientos de inventario (inventory movements list)

- **Date:** 2026-11-15 (session from about 12:47 to 12:53 local time, Bolivia UTC-4)
- **URL:** `http://localhost:5130/inventario/movimientos` (entries created through `http://localhost:5130/inventario/ingresar`)
- **Browser:** Chromium driven through the UI only. Its locale shows date fields as `mm/dd/aaaa`. Viewport 1280 px wide, then 1366×768.
- **Data that already existed when I started:**
  - 18 movements, all of type `Ingreso`. 16 were registered on 10/11/2026 and 2 on 13/11/2026. None were registered today, so the screen opened on the empty state for today.
  - The existing movements include:
    - a `Fecha del movimiento` of `01/01/1900` and another of `15/01/2026`, both registered on 10/11/2026;
    - a movement of `Tomate kg` with `Bs 0,00`;
    - a note with special characters (`Proveedor "Don José" <b>&amp;</b> 'factura' Nº 123 ñandú ¿ok? 😀`), which is shown literally, as expected;
    - two notes `test` on `Leche PIL entera 1L` (TMV00090), one of them with a unit cost of `Bs 50,00`.
  - Three different products are named `Leche PIL entera 1L`: `TMV00090`, `7772106000019` and `7772106000040`.
  - Product `Queso criollo` (`TMV00002`) exists but has no movements.
- **Data I created during the session, all registered on 15/11/2026:**
  - `Pan marraqueta` (TMV00001): 24 u., `Bs 8,40`.
  - `Leche PIL entera 1L` (7772106000019): 3 u., `Bs 10,00`, `Fecha` 01/11/2026, with a 500-character note without spaces.
  - `Tomate kg` (TMV00005): saved as 15 u., `Bs 7,35`. I typed `1,5`; see F12.
  - `Café Copacabana 250g` (TMV00008): 6 u., `Bs 90,60`, saved with a double click. Only one record was created.

## Summary

12 findings in total: 1 High, 7 Medium, 4 Low. Two of them (F11, F12) are outside this screen, on `/inventario/ingresar`.

- **High, F12:** outside this screen, on `/inventario/ingresar`. `Cantidad` silently drops the decimal separator. `1,5` kg of tomato was saved as `15 u.`, and the list now shows that wrong movement.
- **Medium, F1:** searching for an existing product that has no movements says `Ningún producto coincide con «TMV00002»`. That suggests the product does not exist.
- **Medium, F2:** the date filter reads a typed date as month/day. Typing `10/11/2026` shows `No se registraron movimientos el 11/10/2026.`
- **Medium, F3:** the list does not refresh after an entry is saved in another tab. The user may think the entry was lost and load it again.
- **Medium, F4:** once a day has movements, there is no way to get from the list to `Ingresar stock`, or back.
- **Medium, F5:** a backdated entry cannot be found by the date the user typed in the entry form. The filter only uses `Fecha de registro`.
- **Medium, F6:** scanning a second barcode into `Buscar` adds it to the previous code, and the search finds nothing.

These worked and need no change:

- **Search:** it ignores accents in both directions (`cafe` finds `Café`, `plástica` finds `Bolsa plastica`) and ignores case and word order. It trims spaces, tabs and non-breaking spaces from pasted text. It treats `%`, `_`, quotes, `<script>` and `\` literally. A 400-character search does not break the layout.
- **Date filter:** it groups late-evening entries (22:40 local) under the correct local day. Clearing the search with the `Limpiar búsqueda` button or with Ctrl+A and Backspace keeps the date the user had chosen. After `Limpiar búsqueda`, the focus goes back to `Buscar`.
- **Notes:** HTML in notes is shown as plain text.
- **Double click:** a double click on `Guardar ingreso` creates a single movement.
- **Wording:** singular and plural are correct (`1 movimiento`, `2 movimientos`).

## Findings

### F1. "Ningún producto coincide" when the product exists but has no movements

- **Severity:** Medium
- **Origin:** own testing
- **Relevant data:** product `Queso criollo` / `TMV00002` exists. `/inventario/ingresar` finds it: `Elegido: Queso criollo · TMV00002 · precio de venta Bs 35,00`. It has no movements.
- **Steps:**
  1. Open `/inventario/movimientos`.
  2. Type `TMV00002` in `Buscar`.
  3. Replace it with `queso`.
- **Expected:** a message saying that the product has no movements yet, for example "Queso criollo todavía no tiene movimientos". The user should see that the product was found and simply has no stock movements.
- **Actual:** `Ningún producto coincide con «TMV00002». Revise lo escrito o busque por otra parte del nombre o del código.` The same message appears for `queso`. It tells the user that what they typed is wrong. A user who scanned a real barcode could conclude that the product is not registered and try to create it again.

### F2. The date filter reads typed dates as month/day, while the rest of the screen uses day/month

- **Severity:** Medium
- **Origin:** own testing
- **Relevant data:** movements registered on 10/11/2026 (16 rows). The date field shows `11/15/2026` and, when empty, the placeholder `mm/dd/aaaa`. The table and messages use `dd/mm/aaaa`, for example `15/11/2026, 12:49`. The format of the field depends on the browser and Windows language. See M2.
- **Steps:**
  1. Open `/inventario/movimientos`.
  2. Click the first part of `Fecha de registro` and type `10112026`, the way a Bolivian user writes 10 November 2026.
- **Expected:** the field uses the same day/month order as the rest of the app and shows the 16 movements of 10/11/2026.
- **Actual:** the field becomes 2026-10-11 and the screen says `No se registraron movimientos el 11/10/2026.` A user who does not read the date in the message closely will believe nothing was registered on 10 November.

### F3. The list does not refresh after an entry is saved in another tab

- **Severity:** Medium
- **Origin:** web research — new records appear in a filtered list (common "results update" scenario)
- **Relevant data:** 3 movements registered today before this step.
- **Steps:**
  1. Open `/inventario/movimientos`. It shows `3 movimientos registrados el 15/11/2026`.
  2. Open `/inventario/ingresar` in a new tab and save an entry: `TMV00008`, `Cantidad` 6, `Costo total` 90,60. The form shows `Ingreso guardado`.
  3. Go back to the first tab and wait a few seconds.
- **Expected:** the list shows the new movement, or at least offers a visible way to refresh it.
- **Actual:** the list still says `3 movimientos registrados el 15/11/2026`, and the new `Café Copacabana 250g` row is missing. It only appears after reloading the page, which then shows `4 movimientos registrados el 15/11/2026`. The screen has no refresh button. A user who checks the list after loading stock may think the entry was lost and enter it again, which creates duplicate stock. Going back with the browser's back button does refresh the list.

### F4. No way to go from the list to "Ingresar stock" once there are movements, or back

- **Severity:** Medium
- **Origin:** own testing
- **Relevant data:** the `Ingresar stock` link only appears in today's empty state (`Todavía no se registraron movimientos hoy...`).
- **Steps:**
  1. Open `/inventario/movimientos` on a day that already has movements (today, after 1 entry). Alternatively, choose any past date or type a search.
  2. Look for a way to register new stock.
  3. Open `/inventario/ingresar` and look for a way back to the list.
- **Expected:** a permanent way to move between the two screens, such as a button or a menu.
- **Actual:** after the first movement of the day, the `Ingresar stock` link disappears. Neither screen has a menu or a link to the other. The user has to type the address or use the browser's back button.

### F5. A backdated entry cannot be found by the date that was entered for it

- **Severity:** Medium
- **Origin:** own testing
- **Relevant data:** entry for `Leche PIL entera 1L` (7772106000019) saved today with `Fecha` 01/11/2026. The confirmation said `domingo, 1 de noviembre de 2026`. Existing data also has backdated movements: `15/01/2026` and `01/01/1900`, registered on 10/11/2026.
- **Steps:**
  1. In `/inventario/ingresar`, save an entry with `Fecha` 01/11/2026.
  2. Open `/inventario/movimientos` and choose `Fecha de registro` 01/11/2026.
- **Expected:** a way to find the entry by the date the user gave it. At least, the empty message should warn that the filter uses the registration date, not the movement date.
- **Actual:** `No se registraron movimientos el 01/11/2026.` The entry only appears under 15/11/2026. In the table its `Fecha del movimiento` (`01/11/2026`) is not highlighted in any way. The entry form calls the field just `Fecha` and the list calls it `Fecha del movimiento`, so the user has no clue that the two dates are different things.

### F6. Scanning a second barcode adds it to the previous one in "Buscar"

- **Severity:** Medium
- **Origin:** own testing
- **Relevant data:** `Fideo Lazzaroni 400g` / `7772106000071` has 1 movement.
- **Steps:**
  1. In `Buscar`, type `7772106000071` followed by Enter, as a scanner does. The list shows that product's movement.
  2. Scan or type another code. Here I typed `7`.
  3. Press `Escape` to try to clear the field.
- **Expected:** after Enter, the text is selected or replaced, so the next scan starts a new search. This is how the product field in `/inventario/ingresar` behaves. Alternatively, `Escape` clears the search.
- **Actual:** the field becomes `77721060000717` and shows `Ningún producto coincide con «77721060000717». Revise lo escrito...`. `Escape` does nothing. The user has to click `Limpiar búsqueda` or select and delete the text before every scan.

### F7. Clearing the date leaves the previous day's movements on screen

- **Severity:** Low
- **Origin:** own testing
- **Relevant data:** 2 movements registered on 13/11/2026.
- **Steps:**
  1. Choose `Fecha de registro` 13/11/2026. The screen shows `2 movimientos registrados el 13/11/2026` and the table.
  2. Clear the date field.
- **Expected:** the table is hidden, or it is clear that it belongs to another date.
- **Actual:** the field turns red and shows `Complete la fecha para ver sus movimientos.`, while `2 movimientos registrados el 13/11/2026` and its table stay on screen below the empty field.

### F8. "Monto total" in the list, "Costo total" in the entry form

- **Severity:** Low
- **Origin:** own testing
- **Relevant data:** any movement.
- **Steps:**
  1. Compare the column headers of `/inventario/movimientos` with the field labels of `/inventario/ingresar`.
- **Expected:** the same value has the same name on both screens.
- **Actual:** the form asks for `Costo total`, and the list shows that value under `Monto total`, next to `Costo unitario`. The date has the same problem: `Fecha` in the form, `Fecha del movimiento` in the list.

### F9. Cantidad × Costo unitario does not match Monto total because of rounding

- **Severity:** Low
- **Origin:** own testing
- **Relevant data:** existing rows:
  - `Fideo Lazzaroni 400g`: 20, `Bs 96,50`, `Bs 4,83`. 20 × 4,83 = 96,60.
  - `Leche PIL entera 1L` (7772106000040): 12, `Bs 96,50`, `Bs 8,04`. 12 × 8,04 = 96,48.
  - My entry: 3, `Bs 10,00`, `Bs 3,33`. 3 × 3,33 = 9,99.
- **Steps:**
  1. Choose 10/11/2026 and look at the two rows above, or today's `Leche PIL entera 1L` row.
- **Expected:** a user who checks the numbers can tell that the unit cost is rounded, for example through a hint or more decimals.
- **Actual:** the three columns do not add up and nothing explains why. Owners used to checking figures (banking background) may think the entry was saved wrong.

### F10. Reloading the page loses the chosen date and search

- **Severity:** Low
- **Origin:** own testing
- **Relevant data:** movements on 10/11/2026.
- **Steps:**
  1. Choose `Fecha de registro` 10/11/2026, or type a search.
  2. Reload the page.
- **Expected:** the same date or search is kept, since the address could carry it.
- **Actual:** the screen goes back to today with an empty search. The address stays `/inventario/movimientos` whatever filter is chosen, so a filtered view cannot be saved as a favourite or reopened.

### F11. Outside this screen, form at `/inventario/ingresar`: red errors appear as soon as a product is read

- **Severity:** Medium
- **Origin:** own testing (seen while creating data for the list)
- **Relevant data:** products `TMV00001`, `TMV00005`, `TMV00002` and `TMV00089`. It happened with all four.
- **Steps:**
  1. Open `/inventario/ingresar`.
  2. Type a product code in `Producto` and press Enter, as the scanner does.
- **Expected:** the cursor moves to `Cantidad` without errors, because the user has not had a chance to fill it in yet.
- **Actual:** `Cantidad` and `Costo total` turn red at once, with `Ingrese la cantidad de unidades del lote.` and `Ingrese el costo total del lote.` This happens on every scan, so with hundreds of products per delivery users learn to ignore red messages.

### F12. Outside this screen, form at `/inventario/ingresar`: "Cantidad" silently drops the decimal separator, so 1,5 kg is saved as 15

- **Severity:** High
- **Origin:** own testing (seen while creating data for the list)
- **Relevant data:** `Tomate kg` (TMV00005), sold by weight, stock 26 before the entry. The wrong movement is now in the list: `15/11/2026, 12:49 · Tomate kg · Ingreso · 15 · Bs 7,35 · Bs 0,49`.
- **Steps:**
  1. Open `/inventario/ingresar`, type `TMV00005` and press Enter.
  2. Type `1,5` in `Cantidad`, then `7,35` in `Costo total`.
  3. Save.
  4. Variation: type `1,5` or `1.5` key by key and look at the field before saving.
- **Expected:** 1,5 kg is accepted. If whole units are required, the field rejects the input with a clear message. Either way, a typed decimal never turns into a different number.
- **Actual:**
  - `Ingreso guardado — Tomate kg · 15 u. · Bs 7,35`. Stock went from 26 to 41.
  - When typing, the `,` and the `.` are simply ignored, so the field shows `15` and the summary shows `Stock: 41 → 56`.
  - There is no message. A user typing fast while looking at the paper invoice saves ten times the real quantity, and a unit cost of `Bs 0,49` instead of `Bs 4,90`.
  - Products sold by weight (`Tomate kg`, `Cebolla kg`) cannot be loaded with fractions at all.

## Manual test cases

### M1. Searching with the physical barcode scanner

- **What it checks:** that a real scan, typed very fast and followed by Enter, searches for the whole code. It also checks what happens with consecutive scans (see F6).
- **Preconditions:** USB scanner set to send Enter after the code. Products `7772106000019` and `7772106000071` have movements.
- **Steps:**
  1. Open `/inventario/movimientos` and click `Buscar`.
  2. Scan `Leche PIL entera 1L` (7772106000019).
  3. Without touching anything, scan `Fideo Lazzaroni 400g` (7772106000071).
  4. Scan the same product twice quickly.
- **Expected:** step 2 shows `2 movimientos de «7772106000019» en todas las fechas`, or more if more were added, and no intermediate result from a partial code stays on screen. In step 3, the second scan replaces the first and shows only the Fideo movements. No duplicated or concatenated codes appear.

### M2. Date field format on the store laptop

- **What it checks:** the order of day and month in `Fecha de registro` with the real Windows and browser language of the store (see F2).
- **Preconditions:** the Toma&Ve laptop with its usual browser and regional settings. Movements registered on 10/11/2026.
- **Steps:**
  1. Open `/inventario/movimientos` and look at the format of `Fecha de registro`.
  2. Click the field, type `10112026` and press Tab.
  3. Clear the field and look at the placeholder.
- **Expected:** the field shows `dd/mm/aaaa`, the same as the table. Step 2 shows `16 movimientos registrados el 10/11/2026`.

### M3. Movements near midnight and the page left open overnight

- **What it checks:** that movements are grouped by the local day at the day boundary, and that "today" moves to the new day.
- **Preconditions:** server running; the laptop's clock and time zone set to Bolivia (UTC-4).
- **Steps:**
  1. At 23:58, save an entry in `/inventario/ingresar`. After 00:00, save another one.
  2. Open `/inventario/movimientos` and check the previous day and the new day.
  3. Leave the list open before midnight on the default date. After midnight, without reloading, look at the date and the message, then save an entry and come back.
- **Expected:** the 23:58 entry appears only under the previous day, and the 00:0x entry only under the new day. The page left open does not keep saying `Todavía no se registraron movimientos hoy` for a day that is no longer today. Ideally it moves to the new date, or makes clear which date it shows.

### M4. Server stopped while using the list

- **What it checks:** that a stopped server is reported in plain Spanish, and that an old list is not presented as current.
- **Preconditions:** list open on a date with movements.
- **Steps:**
  1. Stop the MarketApp server.
  2. Change `Fecha de registro` to another date with movements.
  3. Type a search in `Buscar`.
  4. Reload the page.
  5. Start the server again and repeat steps 2 and 3.
- **Expected:**
  - Steps 2 and 3 show a clear message, for example "No se pudo conectar…, intente de nuevo". The table of the previous date or search does not stay on screen as if it matched the new date or search.
  - Step 4 shows a clear message instead of a blank page.
  - Step 5 works again without closing the browser.

### M5. Movements of type "Salida"

- **What it checks:** how exits are shown. Today no screen can create them, so all existing data is `Ingreso`. The empty state already promises `ingresos y salidas de stock`.
- **Preconditions:** at least one exit registered once the feature exists, for the same product as an entry on the same day.
- **Steps:**
  1. Open `/inventario/movimientos` on that day.
  2. Search for that product.
- **Expected:** the type reads `Salida` and is easy to tell apart from `Ingreso`. The quantity and the amounts make clear that stock went down (sign or wording). The count line includes both types.

### M6. List with months of real data

- **What it checks:** that the list stays usable with many movements. A delivery can bring hundreds of products, and the search covers `todas las fechas` with no paging.
- **Preconditions:** a copy of the database with several months of movements, or at least 1,000 movements on one day and 5,000 in total.
- **Steps:**
  1. Open the list on the busiest day.
  2. Type `a` in `Buscar`, then `leche`, at normal typing speed.
  3. Scroll to the end of the results.
- **Expected:** the results appear in about one second or less. Typing never shows the results of an earlier, shorter search after a later one. The page does not freeze. The column headers remain understandable while scrolling.

### M7. Laptop with the wrong time zone

- **What it checks:** which day a movement is assigned to if Windows is not set to Bolivia time.
- **Preconditions:** Windows time zone changed, for example to UTC.
- **Steps:**
  1. Save an entry at 21:00 Bolivia time.
  2. Open the list on "today".
- **Expected:** the behaviour is documented and consistent. Ideally the store's day is used. If not, the user can tell why a movement shows up under another date.

## Web research sources

- [xTuple – Inventory History](https://manuals.xtuple.com/refguide/current/dspInventoryHistory.html): today as the default filter.
- [Tekla PowerFab – Inventory transaction history](https://support.tekla.com/doc/tekla-powerfab/2026/inv_transaction_history): filter by transaction date and type.
- [Visma.net – View inventory transaction history](https://docs.vismasoftware.no/visma-net-erp/help/inventory/manage-inventory-transactions/view-inventory-transaction-history/)
- [TestMu AI – Transaction history filter](https://www.testmuai.com/use-cases/kane-cli/transaction-history-filter/): empty results and filter combinations.
- [ACCELQ – Test cases for search functionality](https://www.accelq.com/blog/test-cases-for-search-functionality/): empty query, "no results" message.
- [Yrkan – Search, pagination, sorting](https://yrkan.com/course/module-05-web-testing/search-pagination-sorting/): results update after a filter is applied or removed.
- [NashTech – Date and time testing: hidden time zone bugs](https://blog.nashtechglobal.com/date-and-time-testing-finding-hidden-time-zone-bugs/): local versus UTC midnight.
- [QASkills – Time zone handling testing](https://qaskills.sh/blog/timezone-handling-testing-storage-display): stored versus displayed date.
- [vintrace – Stock Movement Report](https://support.vintrace.com/hc/en-us/articles/32301321425044): date bounds of a stock movement report.

Discarded as not applicable to this context: cross-browser, mobile, pagination SEO, load testing of public sites, security of public websites, DST (Bolivia has no daylight saving time).

## Resolution

To be filled in by the developer after triage.

| Finding | Decision (fixed / ticket / discarded) | Ticket or reason |
| --- | --- | --- |
| F1 | fixed | The list only knows movements, so it cannot tell whether the text matches a product. The message now gives both causes: the product has no movements yet, or the text matches no registered product. |
| F2 | discarded | The date field is the browser's native one, and its day/month order follows the browser's language, which the page cannot change. The QA browser uses month/day; Chrome in Spanish on the store laptop should show `dd/mm/aaaa`. M2 checks it there. |
| F3 | discarded | Real-time updates are not needed here: one person loads stock at a time, and the list loads again when it is opened, reloaded or reached with the back button. |
| F4 | ticket | #20: the menu links every screen, so the list and "Ingresar stock" can always be reached from each other. |
| F5 | discarded | Finding movements by their movement date needs filters on the list, which this ticket does not include. The list filters by registration day, as the ticket asks. The different names of the date on the two screens are covered by F8. |
| F6 | discarded | Typing after an earlier search adds to it, as in any text field. The joined code is shown in the field and in the message, so the user sees the problem and clears the search with `Limpiar búsqueda`. |
| F7 | discarded | Chosen on purpose: the list always shows a day, so while the field is empty or invalid it keeps the last valid date. The counter says which date is shown (`2 movimientos registrados el 13/11/2026`). |
| F8 | fixed | The date field of the entry form and its summary now say "Fecha del movimiento", as in the list. "Monto total" stays in the list: the column will also hold exits, where the amount is income, not a cost. |
| F9 | fixed | Products sold by the gram, like cheese, have unit costs below one cent, and rounding them to cents confused the users. Unit costs now show 3 decimals, in the list and in the entry form summary. Quantity × unit cost still may not match the total exactly, but the gap is now under a tenth of a cent per unit. |
| F10 | discarded | Keeping the date and the search in the address is out of this ticket's scope, as in the product list. |
| F11 | discarded | Same as F5 of the #18 QA report (`MKA-18-stock-form.md`): nothing wrong is saved, the errors only point at the fields still empty, and the scanner sends no Enter today. |
| F12 | discarded | Same as F2 of the #18 QA report (`MKA-18-stock-form.md`): quantities are whole units by decision (unit of measure postponed), and the dropped comma is visible in the field and in the summary before saving. Products sold by weight are registered in whole grams, as the cheese already is. |

## Manual test results

To be filled in by the developer after running the manual test cases.

| Case | Result (passed / failed / not run) | Decision (fixed / ticket / discarded) | Notes, ticket or reason |
| --- | --- | --- | --- |
| M1 | passed | discarded | Steps 1 and 2 simulated in the browser, without the physical scanner: a fast typed code plus Enter, and a code entered with no pause between characters, both showed only the full code's movements (`3 movimientos de «7772106000040» en todas las fechas`, `1 movimiento de «7772106000071» en todas las fechas`). Steps 3 and 4 expect the second scan to replace the first, which was discarded in F6. |
| M2 | not run | | The store laptop was not available. To be checked on it before relying on F2's discard: the field should show `dd/mm/aaaa`, and typing `10112026` should show 10/11/2026. |
| M3 | passed | discarded | Simulated with the movements' times set in a test database and the browser clock moved forward. Steps 1 and 2 passed: the 23:58 entry appears only on the previous day and the 00:02 entry only on the new day. Step 3: a page left open keeps the "hoy" message until the next interaction, then says `No se registraron movimientos en la fecha elegida.` with the old date in the field; it does not move to the new day until it is reloaded or opened again. Discarded: the field shows which date is listed, and nobody leaves the list open overnight expecting it to change. |
| M4 | failed | discarded | Simulated by stopping a test server. Steps 2 and 3 show no message: the list loads every movement when it opens and filters them without calling the server, so there is no connection to fail, and what it shows is right for the loaded data. Step 4 shows the browser's own "site can't be reached" page, because the same server delivers the screen. Step 5 passed: everything works again without closing the browser. The current behaviour is acceptable. |
| M5 | not run | | No screen creates exits yet. To be checked when exits are built. |
| M6 | not run | discarded | Performance with large volumes is out of scope for now: the store will have few movements for a while. Problems with more data are expected and will be solved when they appear. |
| M7 | not run | discarded | The behaviour is defined and covered by the `localDate` tests: the day is taken from the device's time zone, so with Windows in UTC an entry at 21:00 Bolivian time appears on the next day. The store laptop is set to Bolivian time. |
