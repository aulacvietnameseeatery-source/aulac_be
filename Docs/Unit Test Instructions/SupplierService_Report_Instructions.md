# Unit Test Report Instructions — SupplierService

> **Target Excel:** `Docs/Report5.1_Unit Test.xlsx`
> **Test file:** `Tests/Services/SupplierServiceTests.cs`
> **Module:** SUPPLIER
> **Total tests:** 17  |  **Passed:** 17  |  **Failed:** 0

---

## 1. Sheet: MethodList (starting row 9)

Add these rows to the MethodList sheet — one per tested method:

| No | Module Name | Method Name | Sheet Name | Description | Pre-Condition |
|----|-------------|-------------|------------|-------------|---------------|
| (next) | SUPPLIER | GetAllSuppliersAsync | GetAllSuppliersAsync | Staff browses the paginated list of all suppliers | N/A |
| (next) | SUPPLIER | GetSupplierDetailAsync | GetSupplierDetailAsync | Staff views the full details of a supplier including its linked ingredients | Supplier must exist |
| (next) | SUPPLIER | CreateSupplierAsync | CreateSupplierAsync | Staff registers a new supplier with a unique name and optionally links ingredients | Name must be unique |
| (next) | SUPPLIER | UpdateSupplierAsync | UpdateSupplierAsync | Staff updates a supplier's information and ingredient links, ensuring the name remains unique | Supplier must exist; name unique |
| (next) | SUPPLIER | DeleteSupplierAsync | DeleteSupplierAsync | Staff removes a supplier only if it has no related ingredients or inventory transactions | Supplier must exist; no dependencies |

---

## 2. Sheet: Statistics (starting from the next available row after row 11)

| No | Function code | Passed | Failed | Untested | N | A | B | Total |
|----|---------------|--------|--------|----------|---|---|---|-------|
| (next) | GetAllSuppliersAsync | 3 | 0 | 0 | 1 | 1 | 1 | 3 |
| (next) | GetSupplierDetailAsync | 3 | 0 | 0 | 1 | 1 | 1 | 3 |
| (next) | CreateSupplierAsync | 3 | 0 | 0 | 1 | 1 | 1 | 3 |
| (next) | UpdateSupplierAsync | 4 | 0 | 0 | 1 | 2 | 1 | 4 |
| (next) | DeleteSupplierAsync | 4 | 0 | 0 | 1 | 2 | 1 | 4 |
| **Sub total** | | **17** | **0** | **0** | **5** | **7** | **5** | **17** |

**Summary formulas (update row 16+):**
- Test coverage: `(17 + 0) / 17 × 100 = 100%`
- Test successful coverage: `17 / 17 × 100 = 100%`

---

## 3. Per-Method Sheets

Copy the `Example` template sheet for each method listed below.

---

### Sheet: GetAllSuppliersAsync

**Header (rows 1-5):**

| Field | Value |
|-------|-------|
| Code Module | Core/Service/SupplierService.cs |
| Method | GetAllSuppliersAsync |
| Created By | quantm |
| Executed By | quantm |
| Test requirement | Staff browses the paginated list of all suppliers |
| Passed | 3 |
| Failed | 0 |
| Untested | 0 |
| N / A / B | 1 / 1 / 1 → Total = 3 |

**Test Case IDs (row 7, cols F+):** `UTCID01`, `UTCID02`, `UTCID03`

**Condition section:**

| Condition Group | Input Value | UTCID01 | UTCID02 | UTCID03 |
|-----------------|-------------|---------|---------|---------|
| query | SupplierListQueryDTO (default) | O | O | |
| query | SupplierListQueryDTO (PageSize=0) | | | O |
| repositoryReturn | 2 SupplierDto records, TotalCount=2 | O | | |
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

### Sheet: GetSupplierDetailAsync

**Header (rows 1-5):**

| Field | Value |
|-------|-------|
| Code Module | Core/Service/SupplierService.cs |
| Method | GetSupplierDetailAsync |
| Created By | quantm |
| Executed By | quantm |
| Test requirement | Staff views the full details of a supplier including its linked ingredient list |
| Passed | 3 |
| Failed | 0 |
| Untested | 0 |
| N / A / B | 1 / 1 / 1 → Total = 3 |

**Test Case IDs (row 7, cols F+):** `UTCID01` – `UTCID03`

**Condition section:**

| Condition Group | Input Value | UTCID01 | UTCID02 | UTCID03 |
|-----------------|-------------|---------|---------|---------|
| supplierId | 1 (exists, has 2 ingredients: Salt, Sugar) | O | | |
| supplierId | 999 (not found → null) | | O | |
| supplierId | 2 (exists, 0 ingredients) | | | O |

**Confirmation section:**

| Output | Expected Value | UTCID01 | UTCID02 | UTCID03 |
|--------|---------------|---------|---------|---------|
| Return | SupplierId==1, SupplierName=="Supplier A" | O | | |
| Return | Ingredients.Count==2, [0]=="Salt", [1]=="Sugar" | O | | |
| Exception | KeyNotFoundException("*999*") | | O | |
| Return | Ingredients empty | | | O |

**Result section:**

| UTCID | Type | P/F | Date |
|-------|------|-----|------|
| UTCID01 | N | P | (execution date) |
| UTCID02 | A | P | (execution date) |
| UTCID03 | B | P | (execution date) |

---

### Sheet: CreateSupplierAsync

**Header (rows 1-5):**

| Field | Value |
|-------|-------|
| Code Module | Core/Service/SupplierService.cs |
| Method | CreateSupplierAsync |
| Created By | quantm |
| Executed By | quantm |
| Test requirement | Staff registers a new supplier with name uniqueness validation, entity creation, and ingredient linking |
| Passed | 3 |
| Failed | 0 |
| Untested | 0 |
| N / A / B | 1 / 1 / 1 → Total = 3 |

**Test Case IDs (row 7, cols F+):** `UTCID01` – `UTCID03`

**Condition section:**

| Condition Group | Input Value | UTCID01 | UTCID02 | UTCID03 |
|-----------------|-------------|---------|---------|---------|
| supplierName | "New Supplier" (unique) | O | | O |
| supplierName | "New Supplier" (already exists) | | O | |
| nameExists | false | O | | O |
| nameExists | true | | O | |
| ingredientIds | [10, 20] (has ingredients) | O | | |
| ingredientIds | [] (empty list) | | | O |

**Confirmation section:**

| Output | Expected Value | UTCID01 | UTCID02 | UTCID03 |
|--------|---------------|---------|---------|---------|
| Return | SupplierId==10, Name=="New Supplier", Phone=="0909999888" | O | | |
| Verify | UpdateSupplierIngredientsAsync called once with [10,20] | O | | |
| Exception | InvalidOperationException("*already exists*") | | O | |
| Return | SupplierId==11 | | | O |
| Verify | UpdateSupplierIngredientsAsync NOT called | | | O |

**Result section:**

| UTCID | Type | P/F | Date |
|-------|------|-----|------|
| UTCID01 | N | P | (execution date) |
| UTCID02 | A | P | (execution date) |
| UTCID03 | B | P | (execution date) |

---

### Sheet: UpdateSupplierAsync

**Header (rows 1-5):**

| Field | Value |
|-------|-------|
| Code Module | Core/Service/SupplierService.cs |
| Method | UpdateSupplierAsync |
| Created By | quantm |
| Executed By | quantm |
| Test requirement | Staff updates a supplier's information, validating existence, ensuring name uniqueness (excluding itself), and updating ingredient links |
| Passed | 4 |
| Failed | 0 |
| Untested | 0 |
| N / A / B | 1 / 2 / 1 → Total = 4 |

**Test Case IDs (row 7, cols F+):** `UTCID01` – `UTCID04`

**Condition section:**

| Condition Group | Input Value | UTCID01 | UTCID02 | UTCID03 | UTCID04 |
|-----------------|-------------|---------|---------|---------|---------|
| supplierId | 5 (exists) | O | | O | O |
| supplierId | 999 (not found → null) | | O | | |
| newName | "Updated Supplier" (unique) | O | | | O |
| newName | "Updated Supplier" (conflict — another supplier has this name) | | | O | |
| nameExists | false | O | | | O |
| nameExists | true | | | O | |
| ingredientIds | [30] | O | | | |
| ingredientIds | [] (empty list) | | | | O |

**Confirmation section:**

| Output | Expected Value | UTCID01 | UTCID02 | UTCID03 | UTCID04 |
|--------|---------------|---------|---------|---------|---------|
| Return | Name=="Updated Supplier", Phone=="0908888777", Email=="updated@test.com" | O | | | |
| Verify | UpdateSupplierIngredientsAsync called with [30] | O | | | |
| Exception | KeyNotFoundException("*999*") | | O | | |
| Exception | InvalidOperationException("*already exists*") | | | O | |
| Return | Name=="Updated Supplier" | | | | O |
| Verify | UpdateSupplierIngredientsAsync NOT called (empty list) | | | | O |

**Result section:**

| UTCID | Type | P/F | Date |
|-------|------|-----|------|
| UTCID01 | N | P | (execution date) |
| UTCID02 | A | P | (execution date) |
| UTCID03 | A | P | (execution date) |
| UTCID04 | B | P | (execution date) |

---

### Sheet: DeleteSupplierAsync

**Header (rows 1-5):**

| Field | Value |
|-------|-------|
| Code Module | Core/Service/SupplierService.cs |
| Method | DeleteSupplierAsync |
| Created By | quantm |
| Executed By | quantm |
| Test requirement | Staff removes a supplier, validating existence, checking for related dependencies, and confirming successful or failed deletion |
| Passed | 4 |
| Failed | 0 |
| Untested | 0 |
| N / A / B | 1 / 2 / 1 → Total = 4 |

**Test Case IDs (row 7, cols F+):** `UTCID01` – `UTCID04`

**Condition section:**

| Condition Group | Input Value | UTCID01 | UTCID02 | UTCID03 | UTCID04 |
|-----------------|-------------|---------|---------|---------|---------|
| supplierId | 7 (exists, no dependencies) | O | | | |
| supplierId | 999 (not found → null) | | O | | |
| supplierId | 8 (exists, has dependencies) | | | O | |
| supplierId | 9 (exists, no dependencies, delete returns false) | | | | O |
| hasDependencies | false | O | | | O |
| hasDependencies | true | | | O | |
| deleteResult | true | O | | | |
| deleteResult | false | | | | O |

**Confirmation section:**

| Output | Expected Value | UTCID01 | UTCID02 | UTCID03 | UTCID04 |
|--------|---------------|---------|---------|---------|---------|
| Return | true | O | | | |
| Verify | DeleteAsync(7) called once | O | | | |
| Exception | KeyNotFoundException("*999*") | | O | | |
| Exception | InvalidOperationException("*related ingredients or inventory*") | | | O | |
| Return | false | | | | O |

**Result section:**

| UTCID | Type | P/F | Date |
|-------|------|-----|------|
| UTCID01 | N | P | (execution date) |
| UTCID02 | A | P | (execution date) |
| UTCID03 | A | P | (execution date) |
| UTCID04 | B | P | (execution date) |

---

## 4. Test Case ↔ C# Method Mapping

| UTCID | C# Test Method | Trait Type |
|-------|----------------|------------|
| GetAllSuppliersAsync UTCID01 | `GetAllSuppliersAsync_WhenDataExists_ReturnsPagedResult` | N |
| GetAllSuppliersAsync UTCID02 | `GetAllSuppliersAsync_WhenNoData_ReturnsEmptyResult` | B |
| GetAllSuppliersAsync UTCID03 | `GetAllSuppliersAsync_WhenQueryHasZeroPageSize_StillDelegatesToRepository` | A |
| GetSupplierDetailAsync UTCID01 | `GetSupplierDetailAsync_WhenExists_ReturnsDetailWithIngredients` | N |
| GetSupplierDetailAsync UTCID02 | `GetSupplierDetailAsync_WhenNotFound_ThrowsKeyNotFoundException` | A |
| GetSupplierDetailAsync UTCID03 | `GetSupplierDetailAsync_WhenNoIngredients_ReturnsEmptyIngredientList` | B |
| CreateSupplierAsync UTCID01 | `CreateSupplierAsync_WhenValidRequest_CreatesAndReturnsDto` | N |
| CreateSupplierAsync UTCID02 | `CreateSupplierAsync_WhenNameExists_ThrowsInvalidOperationException` | A |
| CreateSupplierAsync UTCID03 | `CreateSupplierAsync_WhenNoIngredients_CreatesWithoutIngredientUpdate` | B |
| UpdateSupplierAsync UTCID01 | `UpdateSupplierAsync_WhenValidRequest_UpdatesAndReturnsDto` | N |
| UpdateSupplierAsync UTCID02 | `UpdateSupplierAsync_WhenNotFound_ThrowsKeyNotFoundException` | A |
| UpdateSupplierAsync UTCID03 | `UpdateSupplierAsync_WhenNameConflict_ThrowsInvalidOperationException` | A |
| UpdateSupplierAsync UTCID04 | `UpdateSupplierAsync_WhenEmptyIngredientIds_SkipsIngredientUpdate` | B |
| DeleteSupplierAsync UTCID01 | `DeleteSupplierAsync_WhenValid_DeletesSuccessfully` | N |
| DeleteSupplierAsync UTCID02 | `DeleteSupplierAsync_WhenNotFound_ThrowsKeyNotFoundException` | A |
| DeleteSupplierAsync UTCID03 | `DeleteSupplierAsync_WhenHasDependencies_ThrowsInvalidOperationException` | A |
| DeleteSupplierAsync UTCID04 | `DeleteSupplierAsync_WhenDeleteReturnsFalse_ReturnsFalse` | B |

---

## Notes

- `SupplierService` depends on `ISupplierRepository` and `ILogger<SupplierService>`.
- Name uniqueness is validated on both Create (no excludeId) and Update (excludeId = current supplier's ID).
- Delete is blocked when `HasDependenciesAsync` returns true (related ingredients or inventory transactions).
- The `MapToDto` private method is tested implicitly through Create/Update return values.
