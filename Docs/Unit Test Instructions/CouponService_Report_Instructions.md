# Unit Test Report Instructions — CouponService

## How to fill `Report5.1_Unit Test.xlsx` for CouponService

---

## 1. Sheet: Cover

| Cell | Value |
|------|-------|
| B2 | `UNIT TEST DOCUMENT` |
| B4 | `AuLac Restaurant` |
| B5 | `AULAC_BE` |
| B6 | `AULAC_BE_UnitTest_v1.0` |
| E4 | `quantm` |
| E5 | *(execution date)* |
| E6 | `1.0` |

---

## 2. Sheet: MethodList

Starting from **row 9**, add one row per method:

| No | Module Name | Method Name | Sheet Name | Description | Pre-Condition |
|----|-------------|-------------|------------|-------------|---------------|
| 1 | COUPON | GetCouponsAsync | GetCouponsAsync | Customer or system retrieves all currently active coupons, optionally filtered by customer ID so that customer-specific coupons are included alongside global ones | CouponRepository is available; active coupons exist in the database |
| 2 | COUPON | GetCouponDetailAsync | GetCouponDetailAsync | Staff retrieves the full detail of a specific coupon including code, name, description, dates, discount value, usage counts, type, status, and creation timestamp by providing the coupon ID | CouponRepository is available |
| 3 | COUPON | CreateCouponAsync | CreateCouponAsync | Staff creates a new coupon by specifying code, name, description, date range, discount value, max usage, and type, with the system normalizing the code to uppercase, validating uniqueness, date range, and percent bounds, then resolving lookup values and persisting the coupon | Lookup values for coupon type and status are configured |
| 4 | COUPON | UpdateCouponAsync | UpdateCouponAsync | Staff updates an existing coupon — if the coupon has been used, only description, end time, and max usage can be changed; if unused, all fields including code, name, type, and dates can be updated with full validation | CouponRepository is available; lookup values are configured |
| 5 | COUPON | DeleteCouponAsync | DeleteCouponAsync | Staff deletes a coupon that has never been used, ensuring used coupons cannot be removed from the system | CouponRepository is available |

---

## 3. Sheet: Statistics

Starting from **row 12**, one row per method:

| No | Function Code | Passed | Failed | Untested | N | A | B | Total |
|----|--------------|--------|--------|----------|---|---|---|-------|
| 1 | GetCouponsAsync | 4 | 0 | 0 | 3 | 0 | 1 | 4 |
| 2 | GetCouponDetailAsync | 2 | 0 | 0 | 1 | 1 | 0 | 2 |
| 3 | CreateCouponAsync | 8 | 0 | 0 | 2 | 4 | 2 | 8 |
| 4 | UpdateCouponAsync | 8 | 0 | 0 | 2 | 5 | 1 | 8 |
| 5 | DeleteCouponAsync | 4 | 0 | 0 | 1 | 3 | 0 | 4 |
| **Sub Total** | | **26** | **0** | **0** | **9** | **13** | **4** | **26** |

- **Test Coverage**: (26 + 0) / 26 × 100 = **100%**
- **Test Success Rate**: 26 / 26 × 100 = **100%**

---

## 4. Per-Method Sheets

For each method below, **copy the `Example` sheet template**, rename the tab to the method name, and fill in the data.

---

### Sheet: GetCouponsAsync

**Header (rows 1–5):**

| Row | Col A | Col C | Col F | Col L |
|-----|-------|-------|-------|-------|
| 1 | Code Module | Core/Service/CouponService.cs | Method | GetCouponsAsync |
| 2 | Created By | quantm | Executed By | quantm |
| 3 | Test requirement | Customer or system retrieves all currently active coupons, optionally filtered by customer ID so that customer-specific coupons are included alongside global ones | | |
| 4 | Passed: 4 | Failed: 0 | Untested: 0 | N: 3 / A: 0 / B: 1 |

**Test Case IDs (row 7, cols F+):** `UTCID01` | `UTCID02` | `UTCID03` | `UTCID04`

**Condition Matrix:**

| Col A | Col B | Col D | UTCID01 | UTCID02 | UTCID03 | UTCID04 |
|-------|-------|-------|---------|---------|---------|---------|
| Condition | Precondition | | | | | |
| | customerId | | | | | |
| | | null (repo returns 2 coupons: [{CouponId=1, Code="COUPON1", CustomerId=null}, {CouponId=2, Code="COUPON2", CustomerId=10}]) | O | | | |
| | | 10 (repo returns 3 coupons: [{CouponId=1, Code="GLOBAL", CustomerId=null}, {CouponId=2, Code="CUST10", CustomerId=10}, {CouponId=3, Code="CUST20", CustomerId=20}]) | | O | | |
| | | null (repo returns empty list — no active coupons) | | | O | |
| | | null (repo returns 1 coupon: [{CouponId=1, Code="MAPPED", CustomerId=10, FullName="Test Customer", Discount=50000, MaxUsage=100, UsedCount=0, Type="FIXED_AMOUNT", Status="ACTIVE"}]) | | | | O |

**Confirmation:**

| Col A | Col B | Col D | UTCID01 | UTCID02 | UTCID03 | UTCID04 |
|-------|-------|-------|---------|---------|---------|---------|
| Confirm | Return | | | | | |
| | | Count==2, [0].CouponCode=="COUPON1", [1].CouponCode=="COUPON2" | O | | | |
| | | Count==2, codes contain "GLOBAL" and "CUST10", codes do not contain "CUST20" (CustomerId=20 filtered out) | | O | | |
| | | Count==0 (empty list) | | | O | |
| | | Count==1, CouponId==1, CouponCode=="MAPPED", CouponName=="Summer Discount", CustomerId==10, CustomerName=="Test Customer", DiscountValue==50000, MaxUsage==100, UsedCount==0, Type=="FIXED_AMOUNT", CouponStatus=="ACTIVE" | | | | O |

**Result:**

| Col B | UTCID01 | UTCID02 | UTCID03 | UTCID04 |
|-------|---------|---------|---------|---------|
| Type | N | N | B | N |
| Passed/Failed | P | P | P | P |
| Executed Date | *(date)* | *(date)* | *(date)* | *(date)* |

---

### Sheet: GetCouponDetailAsync

**Header (rows 1–5):**

| Row | Col A | Col C | Col F | Col L |
|-----|-------|-------|-------|-------|
| 1 | Code Module | Core/Service/CouponService.cs | Method | GetCouponDetailAsync |
| 2 | Created By | quantm | Executed By | quantm |
| 3 | Test requirement | Staff retrieves the full detail of a specific coupon including code, name, description, dates, discount value, usage counts, type, status, and creation timestamp by providing the coupon ID | | |
| 4 | Passed: 2 | Failed: 0 | Untested: 0 | N: 1 / A: 1 / B: 0 |

**Test Case IDs (row 7, cols F+):** `UTCID01` | `UTCID02`

**Condition Matrix:**

| Col A | Col B | Col D | UTCID01 | UTCID02 |
|-------|-------|-------|---------|---------|
| Condition | Precondition | | | |
| | id | | | |
| | | 5 (coupon found: {CouponId=5, Code="DETAIL01", Name="Summer Discount", Description="Test coupon", Discount=50000, MaxUsage=100, UsedCount=0, Type="FIXED_AMOUNT", Status="ACTIVE", CreatedAt=set}) | O | |
| | | 999 (coupon not found — repo returns null) | | O |

**Confirmation:**

| Col A | Col B | Col D | UTCID01 | UTCID02 |
|-------|-------|-------|---------|---------|
| Confirm | Return | | | |
| | | CouponId==5, CouponCode=="DETAIL01", CouponName=="Summer Discount", Description=="Test coupon", DiscountValue==50000, MaxUsage==100, UsedCount==0, Type=="FIXED_AMOUNT", CouponStatus=="ACTIVE", CreatedAt is not null | O | |
| | Exception | | | |
| | | KeyNotFoundException: "Coupon with ID 999 not found." | | O |

**Abnormal cases summary:**
- **UTCID02**: id=999, coupon not found → `KeyNotFoundException`

**Result:**

| Col B | UTCID01 | UTCID02 |
|-------|---------|---------|
| Type | N | A |
| Passed/Failed | P | P |
| Executed Date | *(date)* | *(date)* |

---

### Sheet: CreateCouponAsync

**Header (rows 1–5):**

| Row | Col A | Col C | Col F | Col L |
|-----|-------|-------|-------|-------|
| 1 | Code Module | Core/Service/CouponService.cs | Method | CreateCouponAsync |
| 2 | Created By | quantm | Executed By | quantm |
| 3 | Test requirement | Staff creates a new coupon by specifying code, name, description, date range, discount value, max usage, and type, with the system normalizing the code to uppercase, validating uniqueness, date range, and percent bounds, then resolving lookup values and persisting the coupon | | |
| 4 | Passed: 8 | Failed: 0 | Untested: 0 | N: 2 / A: 4 / B: 2 |

**Test Case IDs (row 7, cols F+):** `UTCID01` | `UTCID02` | `UTCID03` | `UTCID04` | `UTCID05` | `UTCID06` | `UTCID07` | `UTCID08`

**Condition Matrix:**

| Col A | Col B | Col D | UTCID01 | UTCID02 | UTCID03 | UTCID04 | UTCID05 | UTCID06 | UTCID07 | UTCID08 |
|-------|-------|-------|---------|---------|---------|---------|---------|---------|---------|---------|
| Condition | Precondition | | | | | | | | | |
| | CouponCode | | | | | | | | | |
| | | "NEWYEAR2025" (no duplicate — repo GetByCode returns null) | O | | | | | | | |
| | | " new year 2025 " (whitespace, normalized to "NEWYEAR2025", no duplicate) | | O | | | | | | |
| | | "AB" (after normalization, length < 3) | | | O | | | | | |
| | | "NEWYEAR2025" (duplicate exists — repo GetByCode returns existing coupon) | | | | O | | | | |
| | | "NEWYEAR2025" (no duplicate) | | | | | O | | | |
| | | "NEWYEAR2025" (Type="PERCENT", DiscountValue=150) | | | | | | O | | |
| | | "NEWYEAR2025" (Type="PERCENT", DiscountValue=100, no duplicate) | | | | | | | O | |
| | | "ABC" (exactly 3 chars after normalization, no duplicate) | | | | | | | | O |
| | CouponName | | | | | | | | | |
| | | "New Year Discount" | O | O | O | O | O | O | O | O |
| | StartTime | | | | | | | | | |
| | | now-1d | O | O | O | O | | O | O | O |
| | | now+10d (EndTime=now+5d, invalid range) | | | | | O | | | |
| | EndTime | | | | | | | | | |
| | | now+30d | O | O | O | O | | O | O | O |
| | | now+5d (before StartTime=now+10d) | | | | | O | | | |
| | DiscountValue | | | | | | | | | |
| | | 20000 (Type="FIXED_AMOUNT") | O | O | O | O | O | | | O |
| | | 150 (Type="PERCENT", over 100) | | | | | | O | | |
| | | 100 (Type="PERCENT", boundary at 100) | | | | | | | O | |
| | Type | | | | | | | | | |
| | | "FIXED_AMOUNT" | O | O | O | O | O | | | O |
| | | "PERCENT" | | | | | | O | O | |

**Confirmation:**

| Col A | Col B | Col D | UTCID01 | UTCID02 | UTCID03 | UTCID04 | UTCID05 | UTCID06 | UTCID07 | UTCID08 |
|-------|-------|-------|---------|---------|---------|---------|---------|---------|---------|---------|
| Confirm | Return | | | | | | | | | |
| | | CouponId==100, CouponCode=="NEWYEAR2025", CouponName=="New Year Discount", DiscountValue==20000, UsedCount==0, Type=="FIXED_AMOUNT", CouponStatus=="ACTIVE". Repo.CreateAsync called once | O | | | | | | | |
| | | CouponCode=="NEWYEAR2025" (normalized from " new year 2025 ") | | O | | | | | | |
| | | DiscountValue==100, Type=="PERCENT" | | | | | | | O | |
| | | CouponCode=="ABC" | | | | | | | | O |
| | Exception | | | | | | | | | |
| | | InvalidOperationException: "Coupon code must be at least 3 characters." | | | O | | | | | |
| | | InvalidOperationException: "Coupon with code 'NEWYEAR2025' already exists." | | | | O | | | | |
| | | InvalidOperationException: "End time must be after start time." | | | | | O | | | |
| | | InvalidOperationException: "Discount percentage must be between 0 and 100%." | | | | | | O | | |

**Abnormal cases summary:**
- **UTCID03**: CouponCode="AB" (too short after normalization) → `InvalidOperationException`
- **UTCID04**: CouponCode="NEWYEAR2025" (duplicate exists) → `InvalidOperationException`
- **UTCID05**: EndTime before StartTime → `InvalidOperationException`
- **UTCID06**: Type="PERCENT", DiscountValue=150 (over 100) → `InvalidOperationException`

**Result:**

| Col B | UTCID01 | UTCID02 | UTCID03 | UTCID04 | UTCID05 | UTCID06 | UTCID07 | UTCID08 |
|-------|---------|---------|---------|---------|---------|---------|---------|---------|
| Type | N | N | A | A | A | A | B | B |
| Passed/Failed | P | P | P | P | P | P | P | P |
| Executed Date | *(date)* | *(date)* | *(date)* | *(date)* | *(date)* | *(date)* | *(date)* | *(date)* |

---

### Sheet: UpdateCouponAsync

**Header (rows 1–5):**

| Row | Col A | Col C | Col F | Col L |
|-----|-------|-------|-------|-------|
| 1 | Code Module | Core/Service/CouponService.cs | Method | UpdateCouponAsync |
| 2 | Created By | quantm | Executed By | quantm |
| 3 | Test requirement | Staff updates an existing coupon — if the coupon has been used, only description, end time, and max usage can be changed; if unused, all fields including code, name, type, and dates can be updated with full validation | | |
| 4 | Passed: 8 | Failed: 0 | Untested: 0 | N: 2 / A: 5 / B: 1 |

**Test Case IDs (row 7, cols F+):** `UTCID01` | `UTCID02` | `UTCID03` | `UTCID04` | `UTCID05` | `UTCID06` | `UTCID07` | `UTCID08`

**Condition Matrix:**

| Col A | Col B | Col D | UTCID01 | UTCID02 | UTCID03 | UTCID04 | UTCID05 | UTCID06 | UTCID07 | UTCID08 |
|-------|-------|-------|---------|---------|---------|---------|---------|---------|---------|---------|
| Condition | Precondition | | | | | | | | | |
| | id | | | | | | | | | |
| | | 10 (coupon found: {Code="OLD_CODE", UsedCount=0, EndTime=now+30d}) | O | | | | | | | |
| | | 11 (coupon found: {Code="USED_CODE", UsedCount=5, EndTime=now+30d}) | | O | | | | | | |
| | | 999 (coupon not found — repo returns null) | | | O | | | | | |
| | | 12 (coupon found: {EndTime=now-1d, expired}) | | | | O | | | | |
| | | 13 (coupon found: {Code="ORIGINAL", UsedCount=0, EndTime=now+30d}) | | | | | O | | | |
| | | 14 (coupon found: {UsedCount=0, EndTime=now+30d}) | | | | | | O | | |
| | | 15 (coupon found: {UsedCount=3, EndTime=now+30d}) | | | | | | | O | |
| | | 16 (coupon found: {Code="SAMECODE", UsedCount=0, EndTime=now+30d}) | | | | | | | | O |
| | request.CouponCode | | | | | | | | | |
| | | "UPDATED2025" (no duplicate) | O | | | | | | | |
| | | "UPDATED2025" (used coupon — code ignored) | | O | | | | | | |
| | | "UPDATED2025" | | | O | O | | | | |
| | | "DUPLICATE" (duplicate exists — repo GetByCode returns existing) | | | | | O | | | |
| | | "UPDATED2025" (StartTime=now+10d, EndTime=now+5d — invalid range) | | | | | | O | | |
| | | "UPDATED2025" (EndTime before coupon.StartTime) | | | | | | | O | |
| | | "SAMECODE" (same as existing code — case-insensitive match) | | | | | | | | O |

**Confirmation:**

| Col A | Col B | Col D | UTCID01 | UTCID02 | UTCID03 | UTCID04 | UTCID05 | UTCID06 | UTCID07 | UTCID08 |
|-------|-------|-------|---------|---------|---------|---------|---------|---------|---------|---------|
| Confirm | Return | | | | | | | | | |
| | | CouponCode=="UPDATED2025", CouponName=="Updated Coupon", DiscountValue==30000. Repo.UpdateAsync called once | O | | | | | | | |
| | | CouponCode=="USED_CODE" (code NOT changed), MaxUsage==500. Repo.GetByCodeAsync never called | | O | | | | | | |
| | | CouponCode=="SAMECODE". Repo.GetByCodeAsync never called (same code skips uniqueness check) | | | | | | | | O |
| | Exception | | | | | | | | | |
| | | KeyNotFoundException: "Coupon with ID 999 not found." | | | O | | | | | |
| | | InvalidOperationException: "Cannot update an expired coupon." | | | | O | | | | |
| | | InvalidOperationException: "Coupon with code 'DUPLICATE' already exists." | | | | | O | | | |
| | | InvalidOperationException: "End time must be after start time." | | | | | | O | | |
| | | InvalidOperationException: "End time must be after start time." | | | | | | | O | |

**Abnormal cases summary:**
- **UTCID03**: id=999, coupon not found → `KeyNotFoundException`
- **UTCID04**: coupon is expired (EndTime < now) → `InvalidOperationException`
- **UTCID05**: unused coupon, duplicate code → `InvalidOperationException`
- **UTCID06**: unused coupon, EndTime before StartTime → `InvalidOperationException`
- **UTCID07**: used coupon, EndTime before coupon.StartTime → `InvalidOperationException`

**Result:**

| Col B | UTCID01 | UTCID02 | UTCID03 | UTCID04 | UTCID05 | UTCID06 | UTCID07 | UTCID08 |
|-------|---------|---------|---------|---------|---------|---------|---------|---------|
| Type | N | N | A | A | A | A | A | B |
| Passed/Failed | P | P | P | P | P | P | P | P |
| Executed Date | *(date)* | *(date)* | *(date)* | *(date)* | *(date)* | *(date)* | *(date)* | *(date)* |

---

### Sheet: DeleteCouponAsync

**Header (rows 1–5):**

| Row | Col A | Col C | Col F | Col L |
|-----|-------|-------|-------|-------|
| 1 | Code Module | Core/Service/CouponService.cs | Method | DeleteCouponAsync |
| 2 | Created By | quantm | Executed By | quantm |
| 3 | Test requirement | Staff deletes a coupon that has never been used, ensuring used coupons cannot be removed from the system | | |
| 4 | Passed: 4 | Failed: 0 | Untested: 0 | N: 1 / A: 3 / B: 0 |

**Test Case IDs (row 7, cols F+):** `UTCID01` | `UTCID02` | `UTCID03` | `UTCID04`

**Condition Matrix:**

| Col A | Col B | Col D | UTCID01 | UTCID02 | UTCID03 | UTCID04 |
|-------|-------|-------|---------|---------|---------|---------|
| Condition | Precondition | | | | | |
| | id | | | | | |
| | | 20 (coupon found: {UsedCount=0} — repo DeleteAsync returns true) | O | | | |
| | | 999 (coupon not found — repo returns null) | | O | | |
| | | 21 (coupon found: {UsedCount=3} — has been used) | | | O | |
| | | 22 (coupon found: {UsedCount=0} — repo DeleteAsync returns false) | | | | O |

**Confirmation:**

| Col A | Col B | Col D | UTCID01 | UTCID02 | UTCID03 | UTCID04 |
|-------|-------|-------|---------|---------|---------|---------|
| Confirm | Return | | | | | |
| | | No return value (void method executes successfully). Repo.DeleteAsync(20) called once | O | | | |
| | Exception | | | | | |
| | | KeyNotFoundException: "Coupon with ID 999 not found." | | O | | |
| | | InvalidOperationException: "Cannot delete coupon that has been used." | | | O | |
| | | KeyNotFoundException: "Failed to delete coupon with ID 22." | | | | O |

**Abnormal cases summary:**
- **UTCID02**: id=999, coupon not found → `KeyNotFoundException`
- **UTCID03**: id=21, coupon has been used (UsedCount=3) → `InvalidOperationException`
- **UTCID04**: id=22, repo DeleteAsync returns false → `KeyNotFoundException`

**Result:**

| Col B | UTCID01 | UTCID02 | UTCID03 | UTCID04 |
|-------|---------|---------|---------|---------|
| Type | N | A | A | A |
| Passed/Failed | P | P | P | P |
| Executed Date | *(date)* | *(date)* | *(date)* | *(date)* |

---

## 5. C# Test ↔ UTCID Mapping

| UTCID | Method Sheet | C# Test Name | Type |
|-------|-------------|--------------|------|
| UTCID01 | GetCouponsAsync | GetCouponsAsync_WhenNoCustomerId_ReturnsAllActiveCoupons | N |
| UTCID02 | GetCouponsAsync | GetCouponsAsync_WhenCustomerId_ReturnsFilteredCoupons | N |
| UTCID03 | GetCouponsAsync | GetCouponsAsync_WhenNoCoupons_ReturnsEmptyList | B |
| UTCID04 | GetCouponsAsync | GetCouponsAsync_MapsAllDtoFieldsCorrectly | N |
| UTCID01 | GetCouponDetailAsync | GetCouponDetailAsync_WhenCouponExists_ReturnsDetail | N |
| UTCID02 | GetCouponDetailAsync | GetCouponDetailAsync_WhenCouponNotFound_ThrowsKeyNotFoundException | A |
| UTCID01 | CreateCouponAsync | CreateCouponAsync_WhenValidRequest_CreatesCouponAndReturnsDto | N |
| UTCID02 | CreateCouponAsync | CreateCouponAsync_NormalizesCodeToUpperCase | N |
| UTCID03 | CreateCouponAsync | CreateCouponAsync_WhenCodeTooShort_ThrowsInvalidOperationException | A |
| UTCID04 | CreateCouponAsync | CreateCouponAsync_WhenDuplicateCode_ThrowsInvalidOperationException | A |
| UTCID05 | CreateCouponAsync | CreateCouponAsync_WhenEndTimeBeforeStartTime_ThrowsInvalidOperationException | A |
| UTCID06 | CreateCouponAsync | CreateCouponAsync_WhenPercentOver100_ThrowsInvalidOperationException | A |
| UTCID07 | CreateCouponAsync | CreateCouponAsync_WhenPercentExactly100_Succeeds | B |
| UTCID08 | CreateCouponAsync | CreateCouponAsync_WhenCodeExactly3Chars_Succeeds | B |
| UTCID01 | UpdateCouponAsync | UpdateCouponAsync_WhenUnusedCoupon_PerformsFullUpdate | N |
| UTCID02 | UpdateCouponAsync | UpdateCouponAsync_WhenUsedCoupon_OnlyUpdatesAllowedFields | N |
| UTCID03 | UpdateCouponAsync | UpdateCouponAsync_WhenCouponNotFound_ThrowsKeyNotFoundException | A |
| UTCID04 | UpdateCouponAsync | UpdateCouponAsync_WhenCouponExpired_ThrowsInvalidOperationException | A |
| UTCID05 | UpdateCouponAsync | UpdateCouponAsync_WhenUnusedAndDuplicateCode_ThrowsInvalidOperationException | A |
| UTCID06 | UpdateCouponAsync | UpdateCouponAsync_WhenUnusedAndEndBeforeStart_ThrowsInvalidOperationException | A |
| UTCID07 | UpdateCouponAsync | UpdateCouponAsync_WhenUsedAndEndBeforeStart_ThrowsInvalidOperationException | A |
| UTCID08 | UpdateCouponAsync | UpdateCouponAsync_WhenUnusedAndSameCode_SkipsUniquenessCheck | B |
| UTCID01 | DeleteCouponAsync | DeleteCouponAsync_WhenUnusedCoupon_DeletesSuccessfully | N |
| UTCID02 | DeleteCouponAsync | DeleteCouponAsync_WhenCouponNotFound_ThrowsKeyNotFoundException | A |
| UTCID03 | DeleteCouponAsync | DeleteCouponAsync_WhenCouponUsed_ThrowsInvalidOperationException | A |
| UTCID04 | DeleteCouponAsync | DeleteCouponAsync_WhenRepoReturnsFalse_ThrowsKeyNotFoundException | A |

*Note: The total C# test count is **26**, all mapped to **26** documented UTCIDs across 5 method sheets.*
