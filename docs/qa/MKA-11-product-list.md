# QA report: Product list (Productos)

- **Date:** 2026-10-30
- **URL:** http://localhost:5130/productos
- **Data that already existed when I started:** 24 products, newest code first: `TMV00011` to `TMV00001`, plus barcodes including `7772106000040` and `7772106000019` (two products both named "Leche PIL entera 1L", at Bs 9,00 and Bs 7,50), `0036000291452` and `036000291452` (one code is contained in the other), and `96385074` (8 digits). They also included a name of 200+ characters with no spaces and a name with special characters and an emoji.
- **Data added during this session:** 77 products through the form at `/`, so the QA database now has 101. Their names start with `QA lista`, `QA teclado` or `QA vol`. The codes used are `7779990000019` and `TMV00012` to `TMV00087`.

## Summary

| Severity | Findings |
| --- | --- |
| High | 1 (on the form, outside the screen under test) |
| Medium | 11 |
| Low | 2 |

Most important findings:

- **F14 (High, form at `/`, found by accident):** after pressing Enter in "Precio de venta", the price stays in the field and the next product can be saved with it without notice.
- **F1:** a second scan is added to the first code, so the search finds nothing.
- **F2:** if the cursor is not in the search box, a scan is lost without any notice.
- **F3:** a scanned code also returns other products that contain it, and the exact match can appear second.
- **F4:** with the list open in one tab, a product saved in another tab is reported as not existing.

These worked correctly:

- Searches ignore upper/lower case and accents (`cafe`, `CAFÉ`, `azucar`, `panuelos`, `NANDU`), and spaces before or after the text.
- Leading zeros in codes are kept.
- Special characters and HTML are shown as plain text.
- Money and dates are formatted consistently.
- New products appear first when the list is opened again.
- All 101 products appear, from the newest to the oldest, and both can be found by searching.

## Findings

### F1. A second scan is added to the previous code

- **Severity:** Medium
- **Origin:** `own testing`
- **Relevant data:** products `7772106000040` and `7772106000019` existed.
- **Steps:**
  1. Open `/productos`.
  2. Enter `7772106000040` and press Enter, as the scanner would.
  3. Without touching anything, enter `7772106000019` and press Enter.
- **Expected:** the second scan replaces the first and shows product `7772106000019`.
- **Actual:** the box contains `77721060000407772106000019` and the screen says: "Ningún producto coincide con «77721060000407772106000019». Revise lo escrito o busque por otra parte del nombre o del código." The user has to clear the box by hand before each scan.

### F2. A scan is lost without notice when the cursor is outside the search box

- **Severity:** Medium
- **Origin:** `web research — scanner input depends on where the focus is (Enter/Tab suffix)`
- **Relevant data:** any list with products.
- **Steps:**
  1. Open `/productos`. The cursor starts in "Buscar".
  2. Click any row of the list.
  3. Type `7`, `7`, Enter, as the start of a scan.
- **Expected:** the characters reach the search box, or the screen makes it clear that the box must be selected first.
- **Actual:** the box stays empty, the list does not change and no message appears. The user may think the scanner failed or that the product is not being looked up.

### F3. A scanned code also returns other products, and the exact match can appear second

- **Severity:** Medium
- **Origin:** `own testing`
- **Relevant data:** `0036000291452` ("Pañuelos EAN13") and `036000291452` ("Pañuelos UPC") existed. The second code is contained in the first.
- **Steps:**
  1. Open `/productos`.
  2. Enter `036000291452` in "Buscar".
- **Expected:** the product with exactly that code appears first, or alone.
- **Actual:** both products appear, and "Pañuelos EAN13", which is not the scanned code, comes first. In this data both cost Bs 3,00. With different prices, a user reading the first row would get the wrong product's price.

### F4. A product saved in another tab is reported as not existing

- **Severity:** Medium
- **Origin:** `own testing`
- **Relevant data:** the list was open with 51 products. There is no menu between the screens, so keeping one tab per screen is a natural way to work.
- **Steps:**
  1. Open `/productos` in a tab.
  2. In a second tab, open `/`, save a product called `QA lista otra pestana` and close that tab.
  3. Back in the first tab, enter `otra pestana` in "Buscar".
- **Expected:** the new product appears, or the screen indicates that the list may be out of date.
- **Actual:** "Ningún producto coincide con «otra pestana». Revise lo escrito o busque por otra parte del nombre o del código." The product (`TMV00038`) only appears after opening the URL again. The user is told a saved product does not exist, and could try to register it again.

### F5. Searching for two words only works if they are together and in the same order as in the name

- **Severity:** Medium
- **Origin:** `own testing`
- **Relevant data:** "Leche PIL entera 1L" existed twice.
- **Steps:**
  1. Open `/productos`.
  2. Enter `leche 1l` in "Buscar", then `entera leche`.
- **Expected:** both searches find "Leche PIL entera 1L", because all the words are in the name.
- **Actual:** "Ningún producto coincide con «leche 1l». Revise lo escrito o busque por otra parte del nombre o del código." The same message appears for `entera leche`. `leche pil` does find the products, so the behaviour is hard to predict for a user.

### F6. Two spaces between words give no results, and the message hides why

- **Severity:** Medium
- **Origin:** `own testing`
- **Relevant data:** "Leche PIL entera 1L" existed.
- **Steps:**
  1. Open `/productos`.
  2. Enter `leche  pil` in "Buscar", with two spaces.
- **Expected:** the same result as `leche pil`, with one space.
- **Actual:** "Ningún producto coincide con «leche pil». Revise lo escrito o busque por otra parte del nombre o del código." The message shows only one space, so the text looks correct and the user cannot see what to fix. Spaces before and after the text are ignored correctly.

### F7. There is no quick way to clear the search

- **Severity:** Medium
- **Origin:** `own testing`
- **Relevant data:** any search with text in the box.
- **Steps:**
  1. Open `/productos`.
  2. Enter any text in "Buscar".
  3. Press Esc and look for a clear button.
- **Expected:** Esc or a visible button clears the search and shows the full list again.
- **Actual:** Esc does nothing and there is no clear button. The user has to select the text and delete it. This adds a step to every search and makes F1 worse.

### F8. The search is lost when returning with the browser's back button

- **Severity:** Medium
- **Origin:** `own testing`
- **Relevant data:** "Leche PIL entera 1L" existed.
- **Steps:**
  1. Open `/productos` and enter `leche` in "Buscar".
  2. Open `/` in the same tab.
  3. Press the browser's back button.
- **Expected:** the list returns with `leche` still applied.
- **Actual:** the box is empty and the full list is shown.

### F9. The search box and column titles disappear when scrolling a long list

- **Severity:** Medium
- **Origin:** `own testing`
- **Relevant data:** 52 products, in a 1024 x 600 window.
- **Steps:**
  1. Open `/productos`.
  2. Scroll down the list.
- **Expected:** "Buscar" and the column titles stay available, or there is an easy way to get back to them.
- **Actual:** both scroll out of view. Rows then show only values, without titles, and the user has to go back to the top to search. This gets worse with hundreds of products.

### F10. The screen does not show how many products there are or were found

- **Severity:** Medium
- **Origin:** `own testing`
- **Relevant data:** 101 products.
- **Steps:**
  1. Open `/productos`.
  2. Look for a total, then search for `QA vol`.
- **Expected:** a total and a number of results, to check a stock load against what was received.
- **Actual:** no number appears, with or without a search. The only way to know is to count rows by hand.

### F11. The list can only be seen newest first

- **Severity:** Medium
- **Origin:** `own testing`
- **Relevant data:** 52 products.
- **Steps:**
  1. Open `/productos`.
  2. Click the column title "Precio de venta".
- **Expected:** some way to order the list, for example by name or price, to browse or review it.
- **Actual:** nothing changes and no other order is available. Reviewing products by name requires searching for them one at a time. If this order is intentional for now, this finding can be discarded in triage.

### F12. A long search with no results makes the page scroll sideways

- **Severity:** Low
- **Origin:** `own testing`
- **Relevant data:** a search text of about 270 characters with no spaces, in a 1024 x 600 window.
- **Steps:**
  1. Open `/productos`.
  2. Paste a long text with no spaces that matches nothing.
- **Expected:** the message wraps inside the page.
- **Actual:** the text in "Ningún producto coincide con «…»" runs off the right edge and a horizontal scrollbar appears.

### F13. Both screens have the same tab title

- **Severity:** Low
- **Origin:** `own testing`
- **Relevant data:** `/` and `/productos` open in two tabs.
- **Steps:**
  1. Open each screen in its own tab.
  2. Compare the tab titles.
- **Expected:** each tab shows which screen it is.
- **Actual:** both say "MarketApp", so the tabs can only be told apart by opening them.

### F14. Out of scope, form at `/`: the price stays after saving and is reused without notice

The form was not under test. I found this while creating test data and did not investigate it further. It is included because it saves wrong prices.

- **Severity:** High
- **Origin:** `own testing`
- **Relevant data:** products saved with "Sin código de barras" checked. The result is visible in the list as `TMV00016`, "QA lista 006 sin precio escrito", Bs 5,00.
- **Steps:**
  1. Open `/` and check "Sin código de barras".
  2. Enter a name, press Tab, enter a price (`5`) and press Enter while in "Precio de venta".
  3. See "Producto guardado". The name is cleared, but the price is still there.
  4. Check "Sin código de barras" again, enter another name and press Enter, without touching the price.
- **Expected:** all fields are cleared after saving, and a product without a price is rejected.
- **Actual:** the first product leaves its price in the field, and the second is saved with it: "Producto guardado — QA lista 006 sin precio escrito · TMV00016 · Bs 5,00". The price also stayed when typing with real keystrokes (`QA teclado 2`, Bs 8,00). It was cleared when Enter was pressed in the other fields tried: "Código" and the checkbox.

## Manual test cases

### M1. Physical scanner configured to send Enter after the code

- **Preconditions:** USB scanner set to send Enter. Products with barcodes exist, including one that starts with zero.
- **Steps:**
  1. Open `/productos` and do not click anything.
  2. Scan a product.
  3. Clear the box and scan a product whose code starts with zero.
  4. Scan another product without clearing the box.
- **Expected:** every digit arrives in order and the page does not reload. Each scan shows its product, including the leading zero. Step 4 shows the behaviour described in F1, or the new behaviour if F1 is fixed.

### M2. Physical scanner configured to send Tab after the code

- **Preconditions:** USB scanner set to send Tab, if the store may use that setting.
- **Steps:**
  1. Open `/productos`.
  2. Scan a product.
  3. Scan a second product without clicking anything.
- **Expected:** the first product is shown, and the second scan is not lost. The search box is the only field on the screen, so Tab may take the cursor out of it, as in F2.

### M3. Scan immediately after opening the screen

- **Preconditions:** the database has several hundred products.
- **Steps:**
  1. Open or reload `/productos`.
  2. Scan a code as soon as the page appears, before the list has finished loading.
- **Expected:** no digit is lost, and the correct product is shown once the list loads. The screen does not say the product does not exist while it is still loading.

### M4. The server is stopped

- **Preconditions:** the app is running with products.
- **Steps:**
  1. Stop the server and open `/productos`.
  2. Start the server and open `/productos`.
  3. With the list open, stop the server, then search and reload the page.
  4. Start the server and reload again.
- **Expected:** a clear message in Spanish says the list could not be loaded and what to do. It never says that there are no products. The list works again once the server is back.

### M5. No products at all

- **Preconditions:** an empty database. It could not be tested because the QA database has data.
- **Steps:**
  1. Open `/productos`.
  2. Enter any text in "Buscar".
- **Expected:** a clear message says there are no products yet, instead of an empty table or a message suggesting a search problem.

### M6. List with hundreds or thousands of products

- **Preconditions:** a database with at least 1.000 products, loaded in advance. This session only reached 101.
- **Steps:**
  1. Open `/productos` and note how long it takes.
  2. Type a name quickly and then scan a code.
  3. Scroll to the end and check that the oldest product is there.
- **Expected:** every product is available, with none hidden by a limit. The page opens quickly on the store's laptop, and typing does not lag or lose characters.

## Resolution

To be filled in by the developer after triage.

| Finding | Decision (fixed / ticket / discarded) | Ticket or reason |
| --- | --- | --- |
| F1 | discarded | No data is affected and both codes are visible in the message; if the scanner is set to send Enter, selecting the text on Enter would fix it |
| F2 | discarded | The search box stays empty and the list does not change, so the user sees that nothing was searched; no data is affected |
| F3 | discarded | One code containing another is very rare, and both results show their full code |
| F4 | discarded | Refreshing the page updates the list, and the planned menu will reduce working with several tabs |
| F5 | fixed | Each word must be in the name or the code, in any order |
| F6 | fixed | Fixed with F5: repeated spaces count as one separator |
| F7 | fixed | An X inside the search box clears it; Esc was left out on purpose |
| F8 | discarded | Not a frequent scenario; no data is affected and the search can be typed again in seconds |
| F9 | discarded | Search results are short, so going back to the top is no trouble, and the four columns are easy to read without their titles |
| F10 | fixed | The list shows the total and, while searching, how many match |
| F11 | discarded | The ticket asks for newest first and the search already finds a product; other orders can come in their own ticket |
| F12 | fixed | The no-results message now wraps long text without spaces |
| F13 | fixed | Each screen sets its own tab title |
| F14 | fixed | The price format on leaving the field overwrote the cleared price after saving; it now reads the latest value |

## Manual test results

To be filled in by the developer after running the manual test cases.

| Case | Result (passed / failed / not run) | Decision (fixed / ticket / discarded) | Notes, ticket or reason |
| --- | --- | --- | --- |
| M1 | passed | | Simulated by pasting the code and pressing Enter, no physical scanner; a second scan without clearing is F1, discarded |
| M2 | failed | discarded | Simulated by pasting the code and pressing Tab, no physical scanner. The case expected the second scan to work, but this behavior is accepted: Tab moves focus out of the box, and the box does not change, so the user sees that nothing was searched (same as F2) |
| M3 | not run | discarded | The list loads in milliseconds on the same laptop, so there is no time to scan before the search box appears; if it happened, it looks like F2 |
| M4 | failed | discarded | With the server stopped, opening or reloading shows the browser's own error page: the same server delivers the page, so no message from the app can appear. Fixing it would take a service worker, not worth it for a rare case on a Windows service. It never says there are no products and works again when the server is back |
| M5 | passed | | Run with a new empty database. Shows "Todavía no hay productos registrados..." and "Crear producto", which opens the form. There is no "Buscar" box in this state, so step 2 does not apply |
| M6 | not run | discarded | The app is being developed assuming there will not be many products, which is also why pagination is left out |
