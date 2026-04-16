# Report5.1 — Unit Test — PublicReservationService

> **Target Excel:** `Docs/Report5.1_Unit Test.xlsx`
> **Test file:** `Tests/Services/PublicReservationServiceTests.cs`
> **Module:** RESERVATION (Public)
> **Total tests:** 16  |  **Passed:** 16  |  **Failed:** 0

---

## 1. Sheet: Cover

| Ô | Nội dung |
|---|----------|
| B2 | UNIT TEST DOCUMENT |
| B4 | Project Name: AuLac Restaurant |
| B5 | Project Code: AULAC_BE |
| B6 | Document Code: AULAC_BE_UnitTest_v1.0 |
| E4 | quantm |
| E5 | (ngày tạo) |
| E6 | 1.0 |

---

## 2. Sheet: MethodList (starting row 9)

Add these rows to the MethodList sheet — one per tested method:

| No | Module Name | Method Name | Sheet Name | Description | Pre-Condition |
|----|-------------|-------------|------------|-------------|---------------|
| (next) | RESERVATION | CheckReservationFitAsync | Public_CheckReservationFitAsync | Customer checks whether a reservation can be accommodated for the given party size and time slot | Table and reservation data exists |
| (next) | RESERVATION | GetAvailableTablesAsync | Public_GetAvailableTablesAsync | Customer retrieves a list of available tables for online reservation booking | Table data and status available |
| (next) | RESERVATION | CreateReservationAsync | Public_CreateReservationAsync | Customer creates an online reservation, handling new/returning guest identification and table conflict validation | Valid request provided |

---

## 3. Sheet: Statistics (starting from the next available row after row 11)

| No | Function code | Passed | Failed | Untested | N | A | B | Total |
|----|---------------|--------|--------|----------|---|---|---|-------|
| (next) | CheckReservationFitAsync | 5 | 0 | 0 | 2 | 2 | 1 | 5 |
| (next) | GetAvailableTablesAsync | 5 | 0 | 0 | 3 | 1 | 1 | 5 |
| (next) | CreateReservationAsync | 6 | 0 | 0 | 3 | 2 | 1 | 6 |
| **Sub total** | | **16** | **0** | **0** | **8** | **5** | **3** | **16** |

**Summary formulas (update row 16+):**
- Test coverage: `(16 + 0) / 16 × 100 = 100%`
- Test successful coverage: `16 / 16 × 100 = 100%`

---

## 4. Per-Method Sheets

Copy the `Example` template sheet for each method listed below. Fill in the header, condition matrix, confirmation, and result sections as described.

---

### Sheet: Public_CheckReservationFitAsync

**Header (rows 1-5):**

| Field | Value |
|-------|-------|
| Code Module | Core/Service/PublicReservationService.cs |
| Method | CheckReservationFitAsync |
| Created By | quantm |
| Executed By | quantm |
| Test requirement | Customer checks whether a reservation can be accommodated for the given party size and time slot |
| Passed | 5 |
| Failed | 0 |
| Untested | 0 |
| N / A / B | 2 / 2 / 1 → Total = 5 |

**Test Case IDs (row 7, cols F+):** `UTCID01` – `UTCID05`

**Condition section:**

| Condition Group | Input Value | UTCID01 | UTCID02 | UTCID03 | UTCID04 | UTCID05 |
|-----------------|-------------|---------|---------|---------|---------|---------|
| partySize | 0 (invalid — zero) | O | | | | |
| partySize | -5 (invalid — negative) | | O | | | |
| partySize | 10 (valid, large) | | | O | | |
| partySize | 2 (valid, normal) | | | | O | |
| partySize | 1 (boundary — minimum) | | | | | O |
| reservedTime | 2h in future | O | O | O | O | O |
| availableTables | — (not checked, short-circuit) | O | O | | | |
| availableTables | 0 tables returned | | | O | | |
| availableTables | 1 table (capacity=2, no conflict) | | | | O | |
| availableTables | 1 table (capacity=4, no conflict) | | | | | O |
| reservationConflict | — | O | O | O | | |
| reservationConflict | 0 conflicts | | | | O | O |

**Confirmation section:**

| Output | Expected Value | UTCID01 | UTCID02 | UTCID03 | UTCID04 | UTCID05 |
|--------|---------------|---------|---------|---------|---------|---------|
| Return | CanBookOnline == false, Message contains "invalid" | O | | | | |
| Return | CanBookOnline == false | | O | | | |
| Return | CanBookOnline == false, Message contains "No suitable" | | | O | | |
| Return | CanBookOnline == true, Message contains "can be arranged" | | | | O | |
| Return | CanBookOnline == true | | | | | O |

**Result section:**

| UTCID | Type | P/F | Date |
|-------|------|-----|------|
| UTCID01 | A | P | (execution date) |
| UTCID02 | A | P | (execution date) |
| UTCID03 | N | P | (execution date) |
| UTCID04 | N | P | (execution date) |
| UTCID05 | B | P | (execution date) |

---

### Sheet: Public_GetAvailableTablesAsync

**Header (rows 1-5):**

| Field | Value |
|-------|-------|
| Code Module | Core/Service/PublicReservationService.cs |
| Method | GetAvailableTablesAsync |
| Created By | quantm |
| Executed By | quantm |
| Test requirement | Customer retrieves a list of available tables for online booking, marking OCCUPIED or conflicting tables as unavailable |
| Passed | 5 |
| Failed | 0 |
| Untested | 0 |
| N / A / B | 3 / 1 / 1 → Total = 5 |

**Test Case IDs (row 7, cols F+):** `UTCID01` – `UTCID05`

**Condition section:**

| Condition Group | Input Value | UTCID01 | UTCID02 | UTCID03 | UTCID04 | UTCID05 |
|-----------------|-------------|---------|---------|---------|---------|---------|
| reservedTime | 2h in future | O | O | O | | O |
| reservedTime | (any) | | | | O | |
| tables | 3 tables: T001 (OCCUPIED), T002 (available), T003 (available) | O | | | | |
| tables | 1 table: T001 (available status) | | O | O | | |
| tables | 1 table: T001 (RESERVED status) | | | | | O |
| tables | (none — repo throws) | | | | O | |
| reservationConflict | 0 conflicts for all tables | O | | O | | |
| reservationConflict | 1 conflict for T001 (existing reservation overlap) | | O | | | |
| reservationConflict | 0 conflicts, but status=RESERVED within window | | | | | O |
| tableStatusLvId | T001 == TABLE_OCCUPIED_ID | O | | | | |
| tableStatusLvId | T001 == normal (not occupied) | | O | O | | |
| tableStatusLvId | T001 == TABLE_RESERVED_ID | | | | | O |
| repo behavior | Throws InvalidOperationException | | | | O | |

**Confirmation section:**

| Output | Expected Value | UTCID01 | UTCID02 | UTCID03 | UTCID04 | UTCID05 |
|--------|---------------|---------|---------|---------|---------|---------|
| Return | Count == 3 | O | | | | |
| Return | T001.IsAvailable == false (OCCUPIED) | O | | | | |
| Return | T002.IsAvailable == true, T003.IsAvailable == true | O | | | | |
| Return | Count == 1 | | O | O | | O |
| Return | T001.IsAvailable == false (reservation conflict) | | O | | | |
| Return | T001.IsAvailable == true (no conflict, not occupied) | | | O | | |
| Exception | InvalidOperationException propagated | | | | O | |
| Return | T001.IsAvailable == false (RESERVED within window) | | | | | O |

**Result section:**

| UTCID | Type | P/F | Date |
|-------|------|-----|------|
| UTCID01 | N | P | (execution date) |
| UTCID02 | N | P | (execution date) |
| UTCID03 | N | P | (execution date) |
| UTCID04 | A | P | (execution date) |
| UTCID05 | B | P | (execution date) |

---

### Sheet: Public_CreateReservationAsync

**Header (rows 1-5):**

| Field | Value |
|-------|-------|
| Code Module | Core/Service/PublicReservationService.cs |
| Method | CreateReservationAsync |
| Created By | quantm |
| Executed By | quantm |
| Test requirement | Customer creates an online reservation, validating table availability, conflict checks, new/returning guest handling, email confirmation, and rollback on failure |
| Passed | 6 |
| Failed | 0 |
| Untested | 0 |
| N / A / B | 3 / 2 / 1 → Total = 6 |

**Test Case IDs (row 7, cols F+):** `UTCID01` – `UTCID06`

**Condition section:**

| Condition Group | Input Value | 01 | 02 | 03 | 04 | 05 | 06 |
|-----------------|-------------|----|----|----|----|----|----|
| partySize | 20 (no table can fit) | O | | | | | O |
| partySize | 2 (normal) | | O | O | O | | |
| partySize | 1 (boundary — minimum) | | | | | O | |
| availableTables | 0 tables returned | O | | | | | O |
| availableTables | 1 table T001 (capacity=2) | | O | O | O | | |
| availableTables | 1 table T001 (capacity=4) | | | | | O | |
| tableValidation | — (short-circuit, no tables) | O | | | | | O |
| tableValidation | Table not found during validation (concurrency) | | O | | | | |
| tableValidation | Table found, no conflict | | | O | O | O | |
| customerId | — | O | O | | | | O |
| customerId | null (new customer → FindOrCreate returns 99) | | | O | | O | |
| customerId | 50 (existing customer found) | | | | O | | |
| email | "john@example.com" | O | O | O | O | O | O |
| transaction | BeginTransaction called | O | O | O | O | O | O |
| transaction | Commit success | | | O | O | O | |
| transaction | Rollback on failure | O | O | | | | O |

**Confirmation section:**

| Output | Expected Value | 01 | 02 | 03 | 04 | 05 | 06 |
|--------|---------------|----|----|----|----|----|----|
| Exception | InvalidOperationException("*không còn bàn*") | O | | | | | |
| Exception | InvalidOperationException("*Bàn vừa được*") | | O | | | | |
| Return | ReservationId==1, CustomerName=="John Doe", PartySize==2, Status=="PENDING" | | | O | | | |
| Verify | EnqueueReservationEmails called once | | | O | | | |
| Verify | CommitAsync called once | | | O | O | O | |
| Return | ReservationId==1 | | | | O | | |
| Verify | FindOrCreateCustomerIdAsync NOT called (existing customer used) | | | | O | | |
| Return | PartySize==1, Status=="PENDING" | | | | | O | |
| Exception | InvalidOperationException (no suitable tables) | | | | | | O |
| Verify | RollbackAsync called once, CommitAsync NOT called | | | | | | O |

**Result section:**

| UTCID | Type | P/F | Date |
|-------|------|-----|------|
| UTCID01 | A | P | (execution date) |
| UTCID02 | A | P | (execution date) |
| UTCID03 | N | P | (execution date) |
| UTCID04 | N | P | (execution date) |
| UTCID05 | B | P | (execution date) |
| UTCID06 | N | P | (execution date) |

---

## 5. Test Case ↔ C# Method Mapping

| UTCID | C# Test Method | Trait Type |
|-------|----------------|------------|
| CheckReservationFitAsync UTCID01 | `CheckReservationFitAsync_WhenPartySizeIsZero_ReturnsFalse` | A |
| CheckReservationFitAsync UTCID02 | `CheckReservationFitAsync_WhenPartySizeIsNegative_ReturnsFalse` | A |
| CheckReservationFitAsync UTCID03 | `CheckReservationFitAsync_WhenNoAvailableTables_ReturnsFalse` | N |
| CheckReservationFitAsync UTCID04 | `CheckReservationFitAsync_WhenAvailableTablesExist_ReturnsTrue` | N |
| CheckReservationFitAsync UTCID05 | `CheckReservationFitAsync_WhenPartySizeIsOne_ReturnsTrue` | B |
| GetAvailableTablesAsync UTCID01 | `GetAvailableTablesAsync_WhenTableIsOccupied_MarkAsNotAvailable` | N |
| GetAvailableTablesAsync UTCID02 | `GetAvailableTablesAsync_WhenReservationConflictExists_MarkAsNotAvailable` | N |
| GetAvailableTablesAsync UTCID03 | `GetAvailableTablesAsync_WhenTableIsAvailable_ReturnsTrue` | N |
| GetAvailableTablesAsync UTCID04 | `GetAvailableTablesAsync_WhenRepositoryThrows_PropagatesException` | A |
| GetAvailableTablesAsync UTCID05 | `GetAvailableTablesAsync_WhenTableIsReserved_MarkedAsNotAvailable` | B |
| CreateReservationAsync UTCID01 | `CreateReservationAsync_WhenNoSuitableTables_ThrowsInvalidOperationException` | A |
| CreateReservationAsync UTCID02 | `CreateReservationAsync_WhenConflictOccursDuringValidation_ThrowsInvalidOperationException` | A |
| CreateReservationAsync UTCID03 | `CreateReservationAsync_WhenNewCustomer_CreatesReservationAndQueuesEmail` | N |
| CreateReservationAsync UTCID04 | `CreateReservationAsync_WhenExistingCustomer_UsesExistingCustomerId` | N |
| CreateReservationAsync UTCID05 | `CreateReservationAsync_WhenPartySizeIsOne_CreatesReservation` | B |
| CreateReservationAsync UTCID06 | `CreateReservationAsync_OnFailure_CallsRollback` | N |

---

## 6. Kết quả chạy test (tham chiếu)

Đã chạy filter:
- `dotnet test Tests/Tests.csproj --filter FullyQualifiedName~PublicReservationServiceTests --no-build -v minimal`

Kết quả:
- Total: 16
- Passed: 16
- Failed: 0
- Duration: 1.0s
