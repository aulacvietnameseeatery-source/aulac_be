# Unit Test Report Instructions — CouponService

> **Target Excel:** `Docs/Report5.1_Unit Test.xlsx`
> **Test file:** `Tests/Services/CouponServiceTests.cs`
> **Module:** COUPON
> **Total tests:** 21  |  **Passed:** 21  |  **Failed:** 0

---

## 1. Sheet: MethodList (starting row 9)

Add these rows to the MethodList sheet — one per tested method:

| No | Module Name | Method Name | Sheet Name | Description | Pre-Condition |
|----|-------------|-------------|------------|-------------|---------------|
| (next) | COUPON | GetCouponsAsync | GetCouponsAsync | Staff retrieves a list of active coupons, filtering by customer-specific or global visibility | Active coupon data exists |
| (next) | COUPON | GetCouponDetailAsync | GetCouponDetailAsync | Staff views the details of a specific coupon by its ID | Coupon ID provided |
| (next) | COUPON | CreateCouponAsync | CreateCouponAsync | Staff creates a new coupon with code normalization, uniqueness validation, and date range checks | Valid create request |
| (next) | COUPON | UpdateCouponAsync | UpdateCouponAsync | Staff updates an existing coupon with code uniqueness and discount percentage validation | Coupon must exist |
| (next) | COUPON | DeleteCouponAsync | DeleteCouponAsync | Staff soft-deletes a coupon only if it has not been used by any order | Coupon must exist |

---

## 2. Sheet: Statistics (starting from the next available row after row 11)

| No | Function code | Passed | Failed | Untested | N | A | B | Total |
|----|---------------|--------|--------|----------|---|---|---|-------|
| (next) | GetCouponsAsync | 4 | 0 | 0 | 2 | 1 | 1 | 4 |
| (next) | GetCouponDetailAsync | 3 | 0 | 0 | 1 | 1 | 1 | 3 |
| (next) | CreateCouponAsync | 5 | 0 | 0 | 1 | 3 | 1 | 5 |
| (next) | UpdateCouponAsync | 5 | 0 | 0 | 1 | 3 | 1 | 5 |
| (next) | DeleteCouponAsync | 4 | 0 | 0 | 1 | 2 | 1 | 4 |
| **Sub total** | | **21** | **0** | **0** | **6** | **10** | **5** | **21** |

**Summary formulas (update row 16+):**
- Test coverage: `(21 + 0) / 21 × 100 = 100%`
- Test successful coverage: `21 / 21 × 100 = 100%`

---

## 3. Per-Method Sheets

Copy the `Example` template sheet for each method listed below. Fill in the header, condition matrix, confirmation, and result sections as described.

---

### Sheet: GetCouponsAsync

**Header (rows 1-5):**

| Field | Value |
|-------|-------|
| Code Module | Core/Service/CouponService.cs |
| Method | GetCouponsAsync |
| Created By | quantm |
| Executed By | quantm |
| Test requirement | Staff retrieves a list of active coupons, filtering by customer-specific or global visibility |
| Passed | 2 |
| Failed | 0 |
| Untested | 0 |
| N / A / B | 2 / 1 / 1 → Total = 4 |

**Test Case IDs (row 7, cols F+):** `UTCID01` – `UTCID04`

**Condition section:**

| Condition Group | Input Value | UTCID01 | UTCID02 |
|-----------------|-------------|---------|---------|
| customerId | null (no filter) | O | |
| customerId | 100 (specific customer) | | O |
| repositoryData | 2 active coupons (1 global + 1 customer=100) | O | |
| repositoryData | 3 active coupons (1 global + 1 customer=999 + 1 customer=100) | | O |

**Confirmation section:**

| Output | Expected Value | UTCID01 | UTCID02 |
|--------|---------------|---------|---------|
| Return | Count == 2 (all active coupons returned) | O | |
| Return | Count == 2 (global + customer=100 only, excludes customer=999) | | O |
| Return | Contains CouponId 1 and 3 | | O |

**Result section:**

| UTCID | Type | P/F | Date |
|-------|------|-----|------|
| UTCID01 | N | P | (execution date) |
| UTCID02 | N | P | (execution date) |
| UTCID03 | A | P | (execution date) |
| UTCID04 | B | P | (execution date) |

---

### Sheet: GetCouponDetailAsync

**Header (rows 1-5):**

| Field | Value |
|-------|-------|
| Code Module | Core/Service/CouponService.cs |
| Method | GetCouponDetailAsync |
| Created By | quantm |
| Executed By | quantm |
| Test requirement | Staff views the details of a specific coupon by its ID |
| Passed | 3 |
| Failed | 0 |
| Untested | 0 |
| N / A / B | 1 / 1 / 1 → Total = 3 |

**Test Case IDs (row 7, cols F+):** `UTCID01`, `UTCID02`

**Condition section:**

| Condition Group | Input Value | UTCID01 | UTCID02 |
|-----------------|-------------|---------|---------|
| couponId | 5 (exists) | O | |
| couponId | 404 (not found → null) | | O |

Change to:
| couponId | -1 (negative — abnormal input for a primary key) | | O | |
| couponId | 0 (zero — boundary between valid and invalid IDs) | | | O |

**Confirmation section:**

| Output | Expected Value | UTCID01 | UTCID02 | UTCID03 |
|--------|---------------|---------|---------|--------|
| Return | CouponId==5, CouponCode=="C5", Type=="PERCENT", Status=="ACTIVE" | O | | |
| Exception | KeyNotFoundException | | O | |
| Exception | KeyNotFoundException | | | O |

**Result section:**

| UTCID | Type | P/F | Date |
|-------|------|-----|------|
| UTCID01 | N | P | (execution date) |
| UTCID02 | A | P | (execution date) |
| UTCID03 | B | P | (execution date) |

---

### Sheet: CreateCouponAsync

**Header (rows 1-5):**

| Field | Value |
|-------|-------|
| Code Module | Core/Service/CouponService.cs |
| Method | CreateCouponAsync |
| Created By | quantm |
| Executed By | quantm |
| Test requirement | Staff creates a new coupon with code normalization, uniqueness validation, date range checks, and discount percentage boundary |
| Passed | 4 |
| Failed | 0 |
| Untested | 0 |
| N / A / B | 1 / 3 / 1 → Total = 5 |

**Test Case IDs (row 7, cols F+):** `UTCID01` – `UTCID05`

**Condition section:**

| Condition Group | Input Value | UTCID01 | UTCID02 | UTCID03 | UTCID04 |
|-----------------|-------------|---------|---------|---------|---------|
| couponCode | " save10 " (unique, to be normalized) | O | | | |
| couponCode | " save10 " (duplicate — code already exists) | | O | | |
| couponCode | " save10 " (unique) | | | O | O |
| existingCoupon | null (no duplicate) | O | | | O |
| existingCoupon | Coupon with code "SAVE10" exists | | O | | |
| dateRange | StartTime < EndTime (valid) | O | O | | O |
| dateRange | StartTime > EndTime (invalid) | | | O | |
| discountValue | 10 (normal) | O | O | O | |
| discountValue | 100 (boundary — exactly 100%) | | | | O |
| lookupResolver | Returns valid TypeLvId + StatusLvId | O | | | O |

**Confirmation section:**

| Output | Expected Value | UTCID01 | UTCID02 | UTCID03 | UTCID04 |
|--------|---------------|---------|---------|---------|---------|
| Return | CouponId==10, Code=="SAVE10", Name=="Save 10", Type=="PERCENT", Status=="ACTIVE" | O | | | |
| Verify | CreateAsync called with normalized Code/Name/Description | O | | | |
| Exception | InvalidOperationException (code already exists) | | O | | |
| Exception | InvalidOperationException("*End time must be after start time*") | | | O | |
| Return | CouponId==11, DiscountValue==100 | | | | O |

**Result section:**

| UTCID | Type | P/F | Date |
|-------|------|-----|------|
| UTCID01 | N | P | (execution date) |
| UTCID02 | A | P | (execution date) |
| UTCID03 | A | P | (execution date) |
| UTCID04 | B | P | (execution date) |

---

### Sheet: UpdateCouponAsync

**Header (rows 1-5):**

| Field | Value |
|-------|-------|
| Code Module | Core/Service/CouponService.cs |
| Method | UpdateCouponAsync |
| Created By | quantm |
| Executed By | quantm |
| Test requirement | Staff updates an existing coupon with code uniqueness validation and discount percentage upper bound check |
| Passed | 5 |
| Failed | 0 |
| Untested | 0 |
| N / A / B | 1 / 3 / 1 → Total = 5 |

**Test Case IDs (row 7, cols F+):** `UTCID01` – `UTCID05`

**Condition section:**

| Condition Group | Input Value | UTCID01 | UTCID02 | UTCID03 | UTCID04 | UTCID05 |
|-----------------|-------------|---------|---------|---------|---------|---------|
| couponId | 99 (not found → null) | O | | | | |
| couponId | 8 (exists, code="OLD10") | | O | O | O | O |
| newCode | — | O | | | | |
| newCode | "UP20" (unique) | | O | | | |
| newCode | "UP20" (unique) | | | O | | |
| couponExpired | EndTime in the past | | | | O | |
| couponUsed | UsedCount=5 (already used) | | | | | O |
| discountValue | — | O | | | | |
| discountValue | 20 (valid) | | O | | | |
| discountValue | 100.01 (exceeds 100%) | | | O | | |

**Confirmation section:**

| Output | Expected Value | UTCID01 | UTCID02 | UTCID03 | UTCID04 | UTCID05 |
|--------|---------------|---------|---------|---------|---------|---------|
| Exception | KeyNotFoundException | O | | | | |
| Return | Code=="UP20", Name=="Update 20", DiscountValue==20 | | O | | | |
| Verify | UpdateAsync called once | | O | | | |
| Exception | InvalidOperationException("*between 0 and 100%*") | | | O | | |
| Exception | InvalidOperationException("*expired*") | | | | O | |
| Verify | Only Desc/EndTime/MaxUsage updated, Code/Name/DiscountValue unchanged | | | | | O |

**Result section:**

| UTCID | Type | P/F | Date |
|-------|------|-----|------|
| UTCID01 | A | P | (execution date) |
| UTCID02 | N | P | (execution date) |
| UTCID03 | A | P | (execution date) |
| UTCID04 | A | P | (execution date) |
| UTCID05 | B | P | (execution date) |

---

### Sheet: DeleteCouponAsync

**Header (rows 1-5):**

| Field | Value |
|-------|-------|
| Code Module | Core/Service/CouponService.cs |
| Method | DeleteCouponAsync |
| Created By | quantm |
| Executed By | quantm |
| Test requirement | Staff soft-deletes a coupon, blocking deletion if the coupon has already been used by any order |
| Passed | 4 |
| Failed | 0 |
| Untested | 0 |
| N / A / B | 1 / 2 / 1 → Total = 4 |

**Test Case IDs (row 7, cols F+):** `UTCID01` – `UTCID04`

**Condition section:**

| Condition Group | Input Value | UTCID01 | UTCID02 | UTCID03 | UTCID04 |
|-----------------|-------------|---------|---------|---------|---------|
| couponId | 20 (exists, usedCount=1) | O | | | |
| couponId | 30 (exists, usedCount=0) | | O | | |
| couponId | -1 (not found — abnormal) | | | O | |
| couponId | 40 (exists, DeleteAsync returns false) | | | | O |
| usedCount | 1 (coupon has been used) | O | | | |
| usedCount | 0 (coupon never used) | | O | | |

**Confirmation section:**

| Output | Expected Value | UTCID01 | UTCID02 | UTCID03 | UTCID04 |
|--------|---------------|---------|---------|---------|---------|
| Exception | InvalidOperationException("*has been used*") | O | | | |
| Verify | DeleteAsync(30) called once | | O | | |
| Exception | KeyNotFoundException | | | O | |
| Exception | KeyNotFoundException("*Failed to delete*") | | | | O |

**Result section:**

| UTCID | Type | P/F | Date |
|-------|------|-----|------|
| UTCID01 | A | P | (execution date) |
| UTCID02 | N | P | (execution date) |
| UTCID03 | A | P | (execution date) |
| UTCID04 | B | P | (execution date) |

**Condition section:**

| Condition Group | Input Value | UTCID01 | UTCID02 |
|-----------------|-------------|---------|---------|
| couponId | 20 (exists, usedCount=1) | O | |
| couponId | 30 (exists, usedCount=0) | | O |
| usedCount | 1 (coupon has been used) | O | |
| usedCount | 0 (coupon never used) | | O |

**Confirmation section:**

| Output | Expected Value | UTCID01 | UTCID02 |
|--------|---------------|---------|---------|
| Exception | InvalidOperationException("*has been used*") | O | |
| Verify | DeleteAsync(30) called once | | O |

**Result section:**

| UTCID | Type | P/F | Date |
|-------|------|-----|------|
| UTCID01 | A | P | (execution date) |
| UTCID02 | N | P | (execution date) |

---

## 4. Test Case ↔ C# Method Mapping

| UTCID | C# Test Method | Trait Type |
|-------|----------------|------------|
| GetCouponsAsync UTCID01 | `GetCouponsAsync_WhenNoCustomerId_ReturnsAllActiveCoupons` | N |
| GetCouponsAsync UTCID02 | `GetCouponsAsync_WhenCustomerIdProvided_FiltersByCustomerIdOrGlobal` | N |
| GetCouponsAsync UTCID03 | `GetCouponsAsync_WhenNegativeCustomerId_ReturnsOnlyGlobalCoupons` | A |
| GetCouponsAsync UTCID04 | `GetCouponsAsync_WhenRepositoryReturnsEmpty_ReturnsEmptyList` | B |
| GetCouponDetailAsync UTCID01 | `GetCouponDetailAsync_WhenCouponExists_ReturnsDetail` | N |
| GetCouponDetailAsync UTCID02 | `GetCouponDetailAsync_WhenIdIsNegative_ThrowsKeyNotFoundException` | A |
| GetCouponDetailAsync UTCID03 | `GetCouponDetailAsync_WhenIdIsZero_ThrowsKeyNotFoundException` | B |
| CreateCouponAsync UTCID01 | `CreateCouponAsync_WhenValidRequest_CreatesCouponWithNormalizedCode` | N |
| CreateCouponAsync UTCID02 | `CreateCouponAsync_WhenCodeExists_ThrowsInvalidOperationException` | A |
| CreateCouponAsync UTCID03 | `CreateCouponAsync_WhenEndTimeBeforeStartTime_ThrowsInvalidOperationException` | A |
| CreateCouponAsync UTCID04 | `CreateCouponAsync_WhenPercentIsExactly100_AllowsCreation` | B |
| CreateCouponAsync UTCID05 | `CreateCouponAsync_WhenCodeTooShort_ThrowsInvalidOperationException` | A |
| UpdateCouponAsync UTCID01 | `UpdateCouponAsync_WhenCouponNotFound_ThrowsKeyNotFoundException` | A |
| UpdateCouponAsync UTCID02 | `UpdateCouponAsync_WhenCodeChangedAndUnique_UpdatesSuccessfully` | N |
| UpdateCouponAsync UTCID03 | `UpdateCouponAsync_WhenPercentGreaterThan100_ThrowsInvalidOperationException` | A |
| UpdateCouponAsync UTCID04 | `UpdateCouponAsync_WhenCouponExpired_ThrowsInvalidOperationException` | A |
| UpdateCouponAsync UTCID05 | `UpdateCouponAsync_WhenCouponIsUsed_UpdatesOnlyLimitedFields` | B |
| DeleteCouponAsync UTCID01 | `DeleteCouponAsync_WhenCouponUsed_ThrowsInvalidOperationException` | A |
| DeleteCouponAsync UTCID02 | `DeleteCouponAsync_WhenValid_DeletesSuccessfully` | N |
| DeleteCouponAsync UTCID03 | `DeleteCouponAsync_WhenCouponNotFound_ThrowsKeyNotFoundException` | A |
| DeleteCouponAsync UTCID04 | `DeleteCouponAsync_WhenDeleteReturnsFalse_ThrowsKeyNotFoundException` | B |

---

## Notes

- All coupon codes are normalized (trimmed + uppercased) before persistence.
- CouponService depends on ICouponRepository and ILookupResolver only (lightweight service).
- Boundary test (UTCID04 of CreateCouponAsync) validates that DiscountValue == 100 is allowed for PERCENT type.
