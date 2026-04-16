# Unit Test Report Instructions — SaleInvoiceService

> **Target Excel:** `Docs/Report5.1_Unit Test.xlsx`
> **Test file:** `Tests/Services/SaleInvoiceServiceTests.cs`
> **Module:** INVOICE
> **Total tests:** 11  |  **Passed:** 11  |  **Failed:** 0

---

## 1. Sheet: MethodList (starting row 9)

Add these rows to the MethodList sheet — one per tested method:

| No | Module Name | Method Name | Sheet Name | Description | Pre-Condition |
|----|-------------|-------------|------------|-------------|---------------|
| (next) | INVOICE | GetSaleInvoiceDetailAsync | GetSaleInvoiceDetailAsync | Staff views a sale invoice generated from an order, with item mapping, total calculation, and filtering of rejected/cancelled items | Order must exist |
| (next) | INVOICE | GetSaleInvoiceListAsync | GetSaleInvoiceListAsync | Staff browses the paginated list of all sale invoices | N/A |

---

## 2. Sheet: Statistics (starting from the next available row after row 11)

| No | Function code | Passed | Failed | Untested | N | A | B | Total |
|----|---------------|--------|--------|----------|---|---|---|-------|
| (next) | GetSaleInvoiceDetailAsync | 8 | 0 | 0 | 4 | 1 | 3 | 8 |
| (next) | GetSaleInvoiceListAsync | 3 | 0 | 0 | 1 | 1 | 1 | 3 |
| **Sub total** | | **11** | **0** | **0** | **5** | **2** | **4** | **11** |

**Summary formulas (update row 16+):**
- Test coverage: `(11 + 0) / 11 × 100 = 100%`
- Test successful coverage: `11 / 11 × 100 = 100%`

---

## 3. Per-Method Sheets

Copy the `Example` template sheet for each method listed below.

---

### Sheet: GetSaleInvoiceDetailAsync

**Header (rows 1-5):**

| Field | Value |
|-------|-------|
| Code Module | Core/Service/SaleInvoiceService.cs |
| Method | GetSaleInvoiceDetailAsync |
| Created By | quantm |
| Executed By | quantm |
| Test requirement | Staff views a sale invoice generated from an order, with item mapping, total calculation, and filtering of rejected or cancelled items |
| Passed | 8 |
| Failed | 0 |
| Untested | 0 |
| N / A / B | 4 / 1 / 3 → Total = 8 |

**Test Case IDs (row 7, cols F+):** `UTCID01` – `UTCID08`

**Condition section:**

| Condition Group | Input Value | 01 | 02 | 03 | 04 | 05 | 06 | 07 | 08 |
|-----------------|-------------|----|----|----|----|----|----|----|----|
| orderId | 1 (exists, 2 SERVED items, paid CASH) | O | | | | | | | |
| orderId | 999 (not found → null) | | O | | | | | | |
| orderId | 2 (exists, unpaid) | | | O | | | | | |
| orderId | 3 (exists, mixed item statuses) | | | | O | | | | |
| orderId | 4 (exists, 0 items) | | | | | O | | | |
| orderId | 5 (exists, SubTotalAmount=0) | | | | | | O | | |
| orderId | 6 (exists, null navigations) | | | | | | | O | |
| orderId | 42 (exists, verify code padding) | | | | | | | | O |
| payments | Has CASH payment | O | | | | | | | O |
| payments | Empty (unpaid) | | | O | O | O | O | O | |
| itemStatuses | All SERVED | O | | O | | | | | O |
| itemStatuses | Mix: 1 SERVED + 1 REJECTED + 1 CANCELLED | | | | O | | | | |
| itemStatuses | No items | | | | | O | | O | |
| subTotalAmount | 200000 (> 0) | O | | O | O | | | | O |
| subTotalAmount | 0 (fallback to item sum) | | | | | O | O | | |
| navigations | All populated (SourceLv, Table, Staff, Customer) | O | | O | O | O | O | | O |
| navigations | null SourceLv, null Table, null Staff, null Customer | | | | | | | O | |
| discountAmount | 5000 | O | | | | | | | |
| discountAmount | 0 | | | O | O | O | O | O | O |
| tipAmount | 10000 | O | | | | | | | |
| tipAmount | null or 0 | | | O | O | O | O | O | O |

**Confirmation section:**

| Output | Expected Value | 01 | 02 | 03 | 04 | 05 | 06 | 07 | 08 |
|--------|---------------|----|----|----|----|----|----|----|----|
| Return | OrderId==1, InvoiceCode=="#INV0001" | O | | | | | | | |
| Return | OrderType=="DINE_IN", TableCode=="T001" | O | | | | | | | |
| Return | StaffName=="Staff A", CustomerName=="Customer A" | O | | | | | | | |
| Return | IsPaid==true, PaymentMethod=="CASH" | O | | | | | | | |
| Return | Items.Count==2, SubTotal==200000, Discount==5000, Tip==10000 | O | | | | | | | |
| Exception | NotFoundException("*999*") | | O | | | | | | |
| Return | IsPaid==false, PaymentMethod=="-" | | | O | | | | | |
| Return | Items.Count==1 (only SERVED "Phở") | | | | O | | | | |
| Return | Items empty | | | | | O | | | |
| Return | SubTotal==0 (no items) | | | | | O | | | |
| Return | SubTotal==200000 (fallback: sum of items 50000×2×2) | | | | | | O | | |
| Return | OrderType=="Unknown", TableCode=="", StaffName=="", CustomerName=="" | | | | | | | O | |
| Return | TipAmount==0 (null → 0) | | | | | | | O | |
| Return | InvoiceCode=="#INV0042" | | | | | | | | O |

**Result section:**

| UTCID | Type | P/F | Date |
|-------|------|-----|------|
| UTCID01 | N | P | (execution date) |
| UTCID02 | A | P | (execution date) |
| UTCID03 | N | P | (execution date) |
| UTCID04 | N | P | (execution date) |
| UTCID05 | B | P | (execution date) |
| UTCID06 | B | P | (execution date) |
| UTCID07 | N | P | (execution date) |
| UTCID08 | B | P | (execution date) |

---

### Sheet: GetSaleInvoiceListAsync

**Header (rows 1-5):**

| Field | Value |
|-------|-------|
| Code Module | Core/Service/SaleInvoiceService.cs |
| Method | GetSaleInvoiceListAsync |
| Created By | quantm |
| Executed By | quantm |
| Test requirement | Staff browses the paginated list of all sale invoices |
| Passed | 3 |
| Failed | 0 |
| Untested | 0 |
| N / A / B | 1 / 1 / 1 → Total = 3 |

**Test Case IDs (row 7, cols F+):** `UTCID01`, `UTCID02`, `UTCID03`

**Condition section:**

| Condition Group | Input Value | UTCID01 | UTCID02 | UTCID03 |
|-----------------|-------------|---------|---------|---------|
| query | SaleInvoiceListQueryDTO (PageIndex=1, PageSize=10) | O | O | |
| query | SaleInvoiceListQueryDTO (PageIndex=1, PageSize=0) | | | O |
| repositoryReturn | 2 SaleInvoiceListDTO records, TotalCount=2 | O | | |
| repositoryReturn | Empty list, TotalCount=0 | | O | O |

**Confirmation section:**

| Output | Expected Value | UTCID01 | UTCID02 | UTCID03 |
|--------|---------------|---------|---------|---------|
| Return | PageData.Count==2, TotalCount==2 | O | | |
| Return | PageData empty, TotalCount==0 | | O | |
| Verify | Repo called with PageSize=0, returns empty | | | O |

**Result section:**

| UTCID | Type | P/F | Date |
|-------|------|-----|------|
| UTCID01 | N | P | (execution date) |
| UTCID02 | B | P | (execution date) |
| UTCID03 | A | P | (execution date) |

---

## 4. Test Case ↔ C# Method Mapping

| UTCID | C# Test Method | Trait Type |
|-------|----------------|------------|
| GetSaleInvoiceDetailAsync UTCID01 | `GetSaleInvoiceDetailAsync_WhenOrderExists_ReturnsInvoiceWithMappedItems` | N |
| GetSaleInvoiceDetailAsync UTCID02 | `GetSaleInvoiceDetailAsync_WhenOrderNotFound_ThrowsNotFoundException` | A |
| GetSaleInvoiceDetailAsync UTCID03 | `GetSaleInvoiceDetailAsync_WhenUnpaid_ReturnsFalseIsPaidAndDash` | N |
| GetSaleInvoiceDetailAsync UTCID04 | `GetSaleInvoiceDetailAsync_WhenItemsHaveRejectedOrCancelled_FiltersThemOut` | N |
| GetSaleInvoiceDetailAsync UTCID05 | `GetSaleInvoiceDetailAsync_WhenNoItems_ReturnsEmptyItemsList` | B |
| GetSaleInvoiceDetailAsync UTCID06 | `GetSaleInvoiceDetailAsync_WhenSubTotalIsZero_FallsBackToItemSum` | B |
| GetSaleInvoiceDetailAsync UTCID07 | `GetSaleInvoiceDetailAsync_WhenNullNavigations_UsesDefaults` | N |
| GetSaleInvoiceDetailAsync UTCID08 | `GetSaleInvoiceDetailAsync_WhenInvoiceCodeFormatting_PadsOrderIdTo4Digits` | B |
| GetSaleInvoiceListAsync UTCID01 | `GetSaleInvoiceListAsync_WhenDataExists_ReturnsPagedResult` | N |
| GetSaleInvoiceListAsync UTCID02 | `GetSaleInvoiceListAsync_WhenNoData_ReturnsEmptyResult` | B |
| GetSaleInvoiceListAsync UTCID03 | `GetSaleInvoiceListAsync_WhenQueryHasZeroPageSize_StillDelegatesToRepository` | A |

---

## Notes

- `SaleInvoiceService` has only 1 dependency: `ISaleInvoiceRepository` (lightweight service).
- `GetSaleInvoiceDetailAsync` contains the most business logic: item filtering (excludes REJECTED/CANCELLED), SubTotal fallback (uses item sum when SubTotalAmount is 0), and null-safe navigation property mapping.
- `GetSaleInvoiceListAsync` is a direct pass-through to the repository.
