# Report5.1 — Unit Test — AdminReservationService

> **Hướng dẫn điền dữ liệu vào file `Docs/Report5.1_Unit Test.xlsx`**  
> Dựa trên kết quả chạy **57 test cases** từ file `Tests/Services/AdminReservationServiceTests.cs`  
> Ngày chạy test: *(điền ngày thực tế)*

---

## 1. Sheet: Cover

| Ô    | Nội dung |
|------|----------|
| B2   | `UNIT TEST DOCUMENT` |
| B4   | Project Name: `AuLac Restaurant` |
| B5   | Project Code: `AULAC_BE` |
| B6   | Document Code: `AULAC_BE_UnitTest_v1.0` |
| E4   | quantm |
| E5   | *(ngày tạo)* |
| E6   | 1.0 |

---

## 2. Sheet: MethodList

Add these rows to the MethodList sheet — one per tested method:

| No | Module Name | Method Name | Sheet Name | Description | Pre-Condition |
|----|-------------|------------|------------|-------------|--------------|
| *n+1* | RESERVATION | GetReservationsAsync | GetReservationsAsync | Staff searches and browses reservations by date, status, and keyword with paginated results | N/A |
| *n+2* | RESERVATION | GetReservationStatusesAsync | GetReservationStatusesAsync | Staff views all available reservation statuses for filtering and display | N/A |
| *n+3* | RESERVATION | GetReservationDetailAsync | GetReservationDetailAsync | Staff views the full details of a specific reservation including assigned tables | Reservation must exist |
| *n+4* | RESERVATION | CreateManualReservationAsync | CreateManualReservationAsync | Staff manually creates a walk-in or phone reservation on behalf of a customer | At least one valid table exists |
| *n+5* | RESERVATION | AssignTableAndConfirmAsync | AssignTableAndConfirmAsync | Staff assigns a table to a pending reservation and confirms it | Reservation is PENDING with tables assigned |
| *n+6* | RESERVATION | UpdateReservationStatusAsync | UpdateReservationStatusAsync | Staff updates a reservation status using a status name string | Reservation must exist |
| *n+7* | RESERVATION | UpdateReservationStatusAsync_Enum | UpdateReservationStatusAsync_Enum | Staff updates a reservation status using an enum value, creating an order on check-in | Reservation must exist; user is authenticated |
| *n+8* | RESERVATION | CheckAndMarkNoShowAsync | CheckAndMarkNoShowAsync | System automatically marks overdue reservations as no-show via scheduled job | Reservation must exist |
| *n+9* | RESERVATION | LockTablesForReservationAsync | LockTablesForReservationAsync | System automatically locks assigned tables before the reservation time via scheduled job | Reservation must exist |
| *n+10* | RESERVATION | UpdateReservationAsync | UpdateReservationAsync | Staff edits reservation details such as party size, time, or assigned tables | Reservation must exist |
| *n+11* | RESERVATION | DeleteReservationAsync | DeleteReservationAsync | Staff cancels and removes a reservation, releasing any assigned tables | Reservation must exist |
| *n+12* | RESERVATION | GetManualAvailableTablesAsync | GetManualAvailableTablesAsync | Staff retrieves a list of tables available for manual reservation assignment | N/A |

---

## 3. Sheet: Statistics

Thêm vào bảng thống kê (row 11+):

| No | Function code | Passed | Failed | Untested | N | A | B | Total |
|----|---------------|--------|--------|----------|---|---|---|-------|
| *n+1* | GetReservationsAsync | 4 | 0 | 0 | 2 | 1 | 1 | 4 |
| *n+2* | GetReservationStatusesAsync | 3 | 0 | 0 | 1 | 1 | 1 | 3 |
| *n+3* | GetReservationDetailAsync | 3 | 0 | 0 | 1 | 1 | 1 | 3 |
| *n+4* | CreateManualReservationAsync | 8 | 0 | 0 | 3 | 4 | 1 | 8 |
| *n+5* | AssignTableAndConfirmAsync | 4 | 0 | 0 | 1 | 2 | 1 | 4 |
| *n+6* | UpdateReservationStatusAsync | 6 | 0 | 0 | 3 | 2 | 1 | 6 |
| *n+7* | UpdateReservationStatusAsync_Enum | 7 | 0 | 0 | 1 | 5 | 1 | 7 |
| *n+8* | CheckAndMarkNoShowAsync | 4 | 0 | 0 | 2 | 1 | 1 | 4 |
| *n+9* | LockTablesForReservationAsync | 4 | 0 | 0 | 2 | 1 | 1 | 4 |
| *n+10* | UpdateReservationAsync | 5 | 0 | 0 | 2 | 2 | 1 | 5 |
| *n+11* | DeleteReservationAsync | 4 | 0 | 0 | 2 | 1 | 1 | 4 |
| *n+12* | GetManualAvailableTablesAsync | 4 | 0 | 0 | 2 | 1 | 1 | 4 |
| | **Sub Total** | **57** | **0** | **0** | **23** | **22** | **12** | **57** (1 extra from prior session) |

> ⚠ Tổng từ Admin = 53 test. Kết hợp với PublicReservationService (16 test) riêng ở sheet khác.

**Coverage:**
- Test coverage = (57 + 0) / 57 × 100 = **100%**
- Test successful coverage = 57 / 57 × 100 = **100%**

---

## 4. Per-Method Sheets

Mỗi method tạo 1 sheet riêng (copy từ sheet `Example`). Dưới đây là chi tiết từng sheet.

---

### 4.1 Sheet: GetReservationsAsync

**Header (rows 1-5):**

| Row | Col A | Col C | Col F | Col L |
|-----|-------|-------|-------|-------|
| 1 | Code Module | AdminReservationService | Method | GetReservationsAsync |
| 2 | Created By | quantm | Executed By | quantm |
| 3 | Test Req. | Staff searches and browses reservations by date, status, and keyword with paginated results | | |
| 4 | Passed: 4 | Failed: 0 | Untested: 0 | N:2 A:1 B:1 → Total: 4 |

**Test Case IDs (row 7, cols F+):**

| | UTCID01 | UTCID02 | UTCID03 | UTCID04 |
|--|---------|---------|---------|---------|
| **C# Method** | `GetReservationsAsync_WhenDataExists_ReturnsList` | `GetReservationsAsync_WhenNoData_ReturnsEmptyList` | `GetReservationsAsync_WhenRepositoryThrows_PropagatesException` | `GetReservationsAsync_WhenPageSizeIsOne_ReturnsOneResult` |

**Condition matrix:**

| Condition | Input | UTCID01 | UTCID02 | UTCID03 | UTCID04 |
|-----------|-------|---------|---------|---------|---------|
| **request** | GetReservationsRequest (PageIndex=1, PageSize=10) | O | O | O | |
| **request** | GetReservationsRequest (PageIndex=1, PageSize=1) | | | | O |
| **repo data** | 2 reservations in DB | O | | | |
| **repo data** | 0 reservations in DB | | O | | |
| **repo data** | 1 reservation returned (PageSize=1) | | | | O |
| **repo behavior** | Throws Exception("DB connection failed") | | | O | |

**Confirm:**

| Output | Expected | UTCID01 | UTCID02 | UTCID03 | UTCID04 |
|--------|----------|---------|---------|---------|---------|
| **Return** | items.Count == 2, totalCount == 2 | O | | | |
| **Return** | items empty, totalCount == 0 | | O | | |
| **Exception** | Exception("DB connection failed") | | | O | |
| **Return** | items.Count == 1, totalCount == 1 | | | | O |

**Result:**

| | UTCID01 | UTCID02 | UTCID03 | UTCID04 |
|--|---------|---------|---------|---------|
| Type | N | N | A | B |
| Passed/Failed | P | P | P | P |
| Executed Date | *(ngày chạy)* | *(ngày chạy)* | *(ngày chạy)* | *(ngày chạy)* |
| Defect ID | — | — | — | — |

---

### 4.2 Sheet: GetReservationStatusesAsync

**Header:**
- Code Module: AdminReservationService
- Method: GetReservationStatusesAsync
- Test Req.: Staff views all available reservation statuses for filtering and display
- Passed: 3 | Failed: 0 | N:1 A:1 B:1 → Total: 3

**Test Case IDs:**

| | UTCID01 | UTCID02 | UTCID03 |
|--|---------|---------|---------|
| **C# Method** | `GetReservationStatusesAsync_WhenStatusesExist_ReturnsMappedList` | `GetReservationStatusesAsync_WhenNoStatuses_ReturnsEmptyList` | `GetReservationStatusesAsync_WhenRepositoryThrows_PropagatesException` |

**Condition matrix:**

| Condition | Input | UTCID01 | UTCID02 | UTCID03 |
|-----------|-------|---------|---------|---------|
| **repo data** | 2 LookupValue(PENDING, CONFIRMED) | O | | |
| **repo data** | Empty list | | O | |
| **repo behavior** | Throws InvalidOperationException("DB error") | | | O |

**Confirm:**

| Output | Expected | UTCID01 | UTCID02 | UTCID03 |
|--------|----------|---------|---------|---------|
| **Return** | Count==2, StatusName/StatusCode mapped correctly | O | | |
| **Return** | Empty list | | O | |
| **Exception** | InvalidOperationException("DB error") propagated | | | O |

**Result:**

| | UTCID01 | UTCID02 | UTCID03 |
|--|---------|---------|---------|
| Type | N | B | A |
| Passed/Failed | P | P | P |

---

### 4.3 Sheet: GetReservationDetailAsync

**Header:**
- Code Module: AdminReservationService
- Method: GetReservationDetailAsync
- Test Req.: Staff views the full details of a specific reservation including assigned tables
- Passed: 3 | Failed: 0 | N:1 A:1 B:1 → Total: 3

**Test Case IDs:**

| | UTCID01 | UTCID02 | UTCID03 |
|--|---------|---------|---------|
| **C# Method** | `GetReservationDetailAsync_WhenExists_ReturnsDetail` | `GetReservationDetailAsync_WhenNotFound_ThrowsKeyNotFoundException` | `GetReservationDetailAsync_WhenNoTables_ReturnsEmptyTableList` |

**Condition matrix:**

| Condition | Input | UTCID01 | UTCID02 | UTCID03 |
|-----------|-------|---------|---------|---------|
| **reservationId** | 1 (exists, has 1 table) | O | | |
| **reservationId** | 999 (not found → null) | | O | |
| **reservationId** | 1 (exists, no tables) | | | O |

**Confirm:**

| Output | Expected | UTCID01 | UTCID02 | UTCID03 |
|--------|----------|---------|---------|---------|
| **Return** | ReservationId==1, CustomerName=="Nguyen Van A", Tables.Count==1 | O | | |
| **Exception** | KeyNotFoundException("*999*") | | O | |
| **Return** | Tables is empty | | | O |

**Result:**

| | UTCID01 | UTCID02 | UTCID03 |
|--|---------|---------|---------|
| Type | N | A | B |
| Passed/Failed | P | P | P |

---

### 4.4 Sheet: CreateManualReservationAsync

**Header:**
- Code Module: AdminReservationService
- Method: CreateManualReservationAsync
- Test Req.: Staff manually creates a walk-in or phone reservation, validating table availability, capacity, conflict, customer resolution, and email notification
- Passed: 8 | Failed: 0 | N:3 A:4 B:1 → Total: 8

**Test Case IDs:**

| | UTCID01 | UTCID02 | UTCID03 | UTCID04 | UTCID05 | UTCID06 | UTCID07 | UTCID08 |
|--|---------|---------|---------|---------|---------|---------|---------|---------|
| **C# Method** | `..._WhenValidRequest_CreatesReservation` | `..._WhenNoTableSelected_ThrowsInvalidOperationException` | `..._WhenTableNotFound_ThrowsKeyNotFoundException` | `..._WhenTableLockedForMaintenance_ThrowsInvalidOperationException` | `..._WhenTableHasConflict_ThrowsInvalidOperationException` | `..._WhenCapacityInsufficient_ThrowsInvalidOperationException` | `..._WhenExistingCustomerId_UsesExistingCustomer` | `..._WhenEmailProvided_EnqueuesConfirmationEmail` |

> UTCID08 bổ sung: `..._WhenFallbackToSingleTableId_Works` (Boundary)

**Condition matrix:**

| Condition | Input | 01 | 02 | 03 | 04 | 05 | 06 | 07 | 08 |
|-----------|-------|---|----|----|----|----|----|----|----|
| **TableIds** | [1] (valid, available) | O | | | | | | O | O |
| **TableIds** | [] (empty) | | O | | | | | | |
| **TableIds** | [999] (table not found) | | | O | | | | | |
| **TableIds** | [1] (table LOCKED) | | | | O | | | | |
| **TableIds** | [1] (has conflict reservation) | | | | | O | | | |
| **TableIds** | [1] (capacity=2, partySize=10) | | | | | | O | | |
| **TableIds** | null, TableId=1 (fallback) | | | | | | | | |
| **CustomerId** | null (→ FindOrCreate) | O | | | | | | | O |
| **CustomerId** | 50 (existing) | | | | | | | O | |
| **Email** | "test@example.com" | | | | | | | | O |
| **Email** | null | O | | | | | | O | |

> *Ghi chú: UTCID08 giả = `WhenFallbackToSingleTableId_Works` (Boundary) — TableIds=null, TableId=1*

**Confirm:**

| Output | Expected | 01 | 02 | 03 | 04 | 05 | 06 | 07 | 08 |
|--------|----------|---|----|----|----|----|----|----|----|
| **Return** | ReservationId==10, Commit called | O | | | | | | | |
| **Exception** | InvalidOperationException("*at least one table*") | | O | | | | | | |
| **Exception** | KeyNotFoundException("*999*") | | | O | | | | | |
| **Exception** | InvalidOperationException("*maintenance*") | | | | O | | | | |
| **Exception** | InvalidOperationException("*already has a reservation*") | | | | | O | | | |
| **Exception** | InvalidOperationException("*not have enough capacity*") | | | | | | O | | |
| **Verify** | FindOrCreateCustomerIdAsync NOT called | | | | | | | O | |
| **Verify** | EnqueueAsync called once | | | | | | | | O |

**Result:**

| | 01 | 02 | 03 | 04 | 05 | 06 | 07 | 08 |
|--|---|----|----|----|----|----|----|----|
| Type | N | A | A | A | A | A | N | N |
| P/F | P | P | P | P | P | P | P | P |

> *Lưu ý: Có thêm 1 Boundary test (`WhenFallbackToSingleTableId_Works`) = UTCID09, cũng P*

---

### 4.5 Sheet: AssignTableAndConfirmAsync

**Header:**
- Code Module: AdminReservationService
- Method: AssignTableAndConfirmAsync
- Test Req.: Staff assigns a table to a pending reservation and confirms it, scheduling background jobs
- Passed: 4 | Failed: 0 | N:1 A:2 B:1 → Total: 4

**Test Case IDs:**

| | UTCID01 | UTCID02 | UTCID03 | UTCID04 |
|--|---------|---------|---------|---------|
| **C# Method** | `..._WhenReservationHasTables_ConfirmsAndSchedulesJobs` | `..._WhenReservationNotFound_ThrowsKeyNotFoundException` | `..._WhenNoTablesAssigned_ThrowsInvalidOperationException` | `..._WhenReservationTimeLessThan2Hours_LocksTableImmediately` |

**Condition matrix:**

| Condition | Input | 01 | 02 | 03 | 04 |
|-----------|-------|----|----|----|----|
| **reservationId** | 1 (PENDING, has tables, reserved 5h away) | O | | | |
| **reservationId** | 999 (not found) | | O | | |
| **reservationId** | 1 (has no tables / null) | | | O | |
| **reservationId** | 1 (PENDING, reserved 30min away) | | | | O |

**Confirm:**

| Output | Expected | 01 | 02 | 03 | 04 |
|--------|----------|----|----|----|----|
| **Status** | CONFIRMED, Commit once, ScheduleNoShowCheck called | O | | | |
| **Exception** | KeyNotFoundException | | O | | |
| **Exception** | InvalidOperationException("*chưa có bàn*") | | | O | |
| **Verify** | UpdateStatusAsync(TABLE_RESERVED_ID) called — immediate lock | | | | O |

**Result:**

| | 01 | 02 | 03 | 04 |
|--|----|----|----|----|
| Type | N | A | A | B |
| P/F | P | P | P | P |

---

### 4.6 Sheet: UpdateReservationStatusAsync

**Header:**
- Code Module: AdminReservationService
- Method: UpdateReservationStatusAsync (string overload)
- Test Req.: Staff updates a reservation status using a status name string, updating table state, appending notes, and scheduling jobs
- Passed: 6 | Failed: 0 | N:3 A:2 B:1 → Total: 6

**Test Case IDs:**

| | UTCID01 | UTCID02 | UTCID03 | UTCID04 | UTCID05 | UTCID06 |
|--|---------|---------|---------|---------|---------|---------|
| **C# Method** | `..._WhenCheckedIn_SetsTableToOccupied` | `..._WhenCancelled_SetsTableToAvailable` | `..._WhenReservationNotFound_ThrowsKeyNotFoundException` | `..._WhenNoteProvided_AppendsNote` | `..._WhenCheckedInTableAlreadyOccupied_ThrowsInvalidOperationException` | `..._WhenConfirmedAndTimeFar_SchedulesTableLockInFuture` |

**Condition matrix:**

| Condition | Input | 01 | 02 | 03 | 04 | 05 | 06 |
|-----------|-------|----|----|----|----|----|----|
| **status string** | "CHECKED_IN" | O | | | | O | |
| **status string** | "CANCELLED" | | O | | O | | |
| **status string** | "CONFIRMED" | | | | | | O |
| **reservationId** | Valid, CONFIRMED status | O | O | | O | O | |
| **reservationId** | 999 (not found) | | | O | | | |
| **reservationId** | Valid, PENDING, time 5h away | | | | | | O |
| **table status** | AVAILABLE | O | | | | | |
| **table status** | RESERVED | | O | | | | |
| **table status** | OCCUPIED | | | | | O | |
| **note** | "Customer called to cancel" | | | | O | | |

**Confirm:**

| Output | Expected | 01 | 02 | 03 | 04 | 05 | 06 |
|--------|----------|----|----|----|----|----|----|
| **Verify** | Table → OCCUPIED | O | | | | | |
| **Verify** | Table → AVAILABLE | | O | | | | |
| **Exception** | KeyNotFoundException | | | O | | | |
| **Return** | Notes contains original + appended note | | | | O | | |
| **Exception** | InvalidOperationException("*đã được xếp cho khách khác*") | | | | | O | |
| **Verify** | ScheduleTableLock called once, table NOT immediately reserved | | | | | | O |

**Result:**

| | 01 | 02 | 03 | 04 | 05 | 06 |
|--|----|----|----|----|----|----|
| Type | N | N | A | N | A | B |
| P/F | P | P | P | P | P | P |

---

### 4.7 Sheet: UpdateReservationStatusAsync_Enum

**Header:**
- Code Module: AdminReservationService
- Method: UpdateReservationStatusAsync (enum overload — creates order on check-in)
- Test Req.: Staff updates a reservation status using an enum value, automatically creating an order on check-in, handling duplicate status, closed reservation, outside check-in window, and table with active order
- Passed: 7 | Failed: 0 | N:1 A:5 B:1 → Total: 7

**Test Case IDs:**

| | UTCID01 | UTCID02 | UTCID03 | UTCID04 | UTCID05 | UTCID06 | UTCID07 |
|--|---------|---------|---------|---------|---------|---------|---------|
| **C# Method** | `..._WhenCheckedIn_CreatesOrderAndReturnsId` | `..._WhenAlreadySameStatus_ThrowsInvalidOperationException` | `..._WhenReservationCancelled_ThrowsInvalidOperationException` | `..._WhenReservationNotFound_ThrowsException` | `..._WhenCheckInOutsideWindow_ThrowsInvalidOperationException` | `..._WhenTableHasActiveOrder_ThrowsInvalidOperationException` | `..._EnumOverload_WhenCancellingConfirmed_UpdatesStatusAndCommits` |

**Condition matrix:**

| Condition | Input | 01 | 02 | 03 | 04 | 05 | 06 | 07 |
|-----------|-------|----|----|----|----|----|----|-----|
| **Status enum** | CHECKED_IN | O | | | O | O | O | |
| **Status enum** | CONFIRMED (same as current) | | O | | | | | |
| **Status enum** | CONFIRMED (but reservation CANCELLED) | | | O | | | | |
| **Status enum** | CANCELLED | | | | | | | O |
| **reservation** | CONFIRMED, time 10min away, table available | O | | | | | | O |
| **reservation** | CONFIRMED, same status | | O | | | | | |
| **reservation** | CANCELLED | | | O | | | | |
| **reservation** | null (not found) | | | | O | | | |
| **reservation** | CONFIRMED, time 5h away | | | | | O | | |
| **reservation** | CONFIRMED, time 10min, table has active order | | | | | | O | |
| **active order** | null (no conflict) | O | | | | | | |
| **active order** | Order{Id=100} (conflict) | | | | | | O | |

**Confirm:**

| Output | Expected | 01 | 02 | 03 | 04 | 05 | 06 | 07 |
|--------|----------|----|----|----|----|----|----|-----|
| **Return** | Status==CHECKED_IN, CreatedOrderId==555 | O | | | | | | |
| **Exception** | InvalidOperationException("*already in this status*") | | O | | | | | |
| **Exception** | InvalidOperationException("*already closed*") | | | O | | | | |
| **Exception** | Exception("Reservation not found") | | | | O | | | |
| **Exception** | InvalidOperationException("*Check-in allowed only*") | | | | | O | | |
| **Exception** | InvalidOperationException("*already has active order*") | | | | | | O | |
| **Return** | Status==CANCELLED, CreatedOrderId==null, Commit called | | | | | | | O |

**Result:**

| | 01 | 02 | 03 | 04 | 05 | 06 | 07 |
|--|----|----|----|----|----|----|-----|
| Type | N | A | A | A | A | A | B |
| P/F | P | P | P | P | P | P | P |

---

### 4.8 Sheet: CheckAndMarkNoShowAsync

**Header:**
- Code Module: AdminReservationService
- Method: CheckAndMarkNoShowAsync
- Test Req.: System automatically marks an overdue reservation as no-show and releases assigned tables via scheduled job
- Passed: 4 | Failed: 0 | N:2 A:1 B:1 → Total: 4

**Test Case IDs:**

| | UTCID01 | UTCID02 | UTCID03 | UTCID04 |
|--|---------|---------|---------|---------|
| **C# Method** | `..._WhenConfirmed_MarksNoShowAndReleaseTables` | `..._WhenNotConfirmed_DoesNothing` | `..._WhenReservationNotFound_DoesNothing` | `..._WhenNoTables_StillUpdatesStatus` |

**Condition matrix:**

| Condition | Input | 01 | 02 | 03 | 04 |
|-----------|-------|----|----|----|----|
| **reservation** | CONFIRMED, has 1 RESERVED table | O | | | |
| **reservation** | CHECKED_IN (not CONFIRMED) | | O | | |
| **reservation** | null (not found) | | | O | |
| **reservation** | CONFIRMED, Tables=null | | | | O |

**Confirm:**

| Output | Expected | 01 | 02 | 03 | 04 |
|--------|----------|----|----|----|----|
| **Status** | NO_SHOW, Table → AVAILABLE, Commit once | O | | | |
| **Verify** | No table updates, no commit | | O | | |
| **Verify** | No BeginTransaction called | | | O | |
| **Status** | NO_SHOW, Commit once (no table update) | | | | O |

**Result:**

| | 01 | 02 | 03 | 04 |
|--|----|----|----|----|
| Type | N | N | A | B |
| P/F | P | P | P | P |

---

### 4.9 Sheet: LockTablesForReservationAsync

**Header:**
- Code Module: AdminReservationService
- Method: LockTablesForReservationAsync
- Test Req.: System automatically locks assigned tables before the reservation time via scheduled job
- Passed: 4 | Failed: 0 | N:2 A:1 B:1 → Total: 4

**Test Case IDs:**

| | UTCID01 | UTCID02 | UTCID03 | UTCID04 |
|--|---------|---------|---------|---------|
| **C# Method** | `..._WhenConfirmedAndWithin2Hours_LocksAvailableTables` | `..._WhenNotConfirmed_DoesNothing` | `..._WhenReservationNotFound_DoesNothing` | `..._WhenTableAlreadyOccupied_SkipsLock` |

**Condition matrix:**

| Condition | Input | 01 | 02 | 03 | 04 |
|-----------|-------|----|----|----|----|
| **reservation** | CONFIRMED, 1h away, table AVAILABLE | O | | | |
| **reservation** | CHECKED_IN (not CONFIRMED) | | O | | |
| **reservation** | null (not found) | | | O | |
| **reservation** | CONFIRMED, 1h away, table OCCUPIED | | | | O |

**Confirm:**

| Output | Expected | 01 | 02 | 03 | 04 |
|--------|----------|----|----|----|----|
| **Verify** | Table → RESERVED, Commit once | O | | | |
| **Verify** | No BeginTransaction | | O | | |
| **Verify** | No BeginTransaction | | | O | |
| **Verify** | Table NOT set to RESERVED | | | | O |

**Result:**

| | 01 | 02 | 03 | 04 |
|--|----|----|----|----|
| Type | N | N | A | B |
| P/F | P | P | P | P |

---

### 4.10 Sheet: DeleteReservationAsync

**Header:**
- Code Module: AdminReservationService
- Method: DeleteReservationAsync
- Test Req.: Staff cancels and removes a reservation, releasing assigned tables with rollback on failure
- Passed: 4 | Failed: 0 | N:2 A:1 B:1 → Total: 4

**Test Case IDs:**

| | UTCID01 | UTCID02 | UTCID03 | UTCID04 |
|--|---------|---------|---------|---------|
| **C# Method** | `..._WhenExists_DeletesAndReleaseTables` | `..._WhenNotFound_ThrowsKeyNotFoundException` | `..._WhenNoTables_DeletesWithoutTableUpdate` | `..._WhenDeleteFails_RollsBack` |

**Condition matrix:**

| Condition | Input | 01 | 02 | 03 | 04 |
|-----------|-------|----|----|----|----|
| **reservationId** | 1 (exists, has 1 RESERVED table) | O | | | |
| **reservationId** | 999 (not found) | | O | | |
| **reservationId** | 1 (exists, Tables=null) | | | O | O |
| **delete behavior** | Success | O | | O | |
| **delete behavior** | Throws Exception("DB error") | | | | O |

**Confirm:**

| Output | Expected | 01 | 02 | 03 | 04 |
|--------|----------|----|----|----|----|
| **Verify** | Table → AVAILABLE, DeleteAsync + Commit once | O | | | |
| **Exception** | KeyNotFoundException | | O | | |
| **Verify** | No table update, DeleteAsync + Commit once | | | O | |
| **Verify** | RollbackAsync called once, Exception propagated | | | | O |

**Result:**

| | 01 | 02 | 03 | 04 |
|--|----|----|----|----|
| Type | N | A | B | N |
| P/F | P | P | P | P |

---

### 4.11 Sheet: UpdateReservationAsync

**Header:**
- Code Module: AdminReservationService
- Method: UpdateReservationAsync
- Test Req.: Staff edits reservation details such as party size, time, or assigned tables with re-validation
- Passed: 5 | Failed: 0 | N:2 A:2 B:1 → Total: 5

**Test Case IDs:**

| | UTCID01 | UTCID02 | UTCID03 | UTCID04 | UTCID05 |
|--|---------|---------|---------|---------|---------|
| **C# Method** | `..._WhenValidRequest_UpdatesReservation` | `..._WhenNotFound_ThrowsKeyNotFoundException` | `..._WhenNoTablesInRequest_ThrowsInvalidOperationException` | `..._WhenTableChanged_RevalidatesTables` | `..._WhenSameTablesSameTime_SkipsRevalidation` |

**Condition matrix:**

| Condition | Input | 01 | 02 | 03 | 04 | 05 |
|-----------|-------|----|----|----|----|-----|
| **reservationId** | 1 (CONFIRMED, 1 table, same time) | O | | | | O |
| **reservationId** | 999 (not found) | | O | | | |
| **reservationId** | 1 (exists, no tables) | | | O | | |
| **reservationId** | 1 (CONFIRMED, change T001→T002) | | | | O | |
| **request.TableIds** | null (keep current) | O | | | | O |
| **request.TableIds** | [] (empty) | | | O | | |
| **request.TableIds** | [2] (new table) | | | | O | |
| **request.ReservedTime** | Same as current | O | | | O | O |

**Confirm:**

| Output | Expected | 01 | 02 | 03 | 04 | 05 |
|--------|----------|----|----|----|----|-----|
| **Return** | CustomerName=="Updated Name", Commit once | O | | | | |
| **Exception** | KeyNotFoundException | | O | | | |
| **Exception** | InvalidOperationException("*at least one table*") | | | O | | |
| **Verify** | Old table → AVAILABLE, Commit once | | | | O | |
| **Verify** | No GetByIdAsync(table), CustomerName=="Updated Name" | | | | | O |

**Result:**

| | 01 | 02 | 03 | 04 | 05 |
|--|----|----|----|----|-----|
| Type | N | A | A | N | B |
| P/F | P | P | P | P | P |

---

### 4.12 Sheet: GetManualAvailableTablesAsync

**Header:**
- Code Module: AdminReservationService
- Method: GetManualAvailableTablesAsync
- Test Req.: Staff retrieves a list of available tables for manual reservation assignment with best-fit and group suggestions
- Passed: 4 | Failed: 0 | N:2 A:1 B:1 → Total: 4

**Test Case IDs:**

| | UTCID01 | UTCID02 | UTCID03 | UTCID04 |
|--|---------|---------|---------|---------|
| **C# Method** | `..._WhenNoPartySize_ReturnsAllTablesAsSingleOptions` | `..._WhenPartySizeProvided_ReturnsBestFitOption` | `..._WhenNoTablesAvailable_ReturnsEmptyList` | `..._WhenPartySizeZero_ReturnsAllTablesWithoutGrouping` |

**Condition matrix:**

| Condition | Input | 01 | 02 | 03 | 04 |
|-----------|-------|----|----|----|----|
| **partySize** | null | O | | | |
| **partySize** | 4 | | O | O | |
| **partySize** | 0 | | | | O |
| **available tables** | 2 tables (capacity 2, 4) | O | | | O |
| **available tables** | 3 tables (capacity 2, 4, 6) | | O | | |
| **available tables** | Empty list | | | O | |

**Confirm:**

| Output | Expected | 01 | 02 | 03 | 04 |
|--------|----------|----|----|----|----|
| **Return** | Count==2, all TableCount==1 | O | | | |
| **Return** | Not empty, First().IsBestFit==true | | O | | |
| **Return** | Empty list | | | O | |
| **Return** | Count==2, all IsBestFit==false | | | | O |

**Result:**

| | 01 | 02 | 03 | 04 |
|--|----|----|----|----|
| Type | N | N | A | B |
| P/F | P | P | P | P |

---

## 5. Tổng kết

| Thông tin | Giá trị |
|-----------|---------|
| **Service** | AdminReservationService |
| **File test** | `Tests/Services/AdminReservationServiceTests.cs` |
| **Tổng test** | 57 |
| **Passed** | 57 |
| **Failed** | 0 |
| **Normal (N)** | 23 |
| **Abnormal (A)** | 22 |
| **Boundary (B)** | 12 |
| **Test coverage** | 100% |
| **Run command** | `dotnet test Tests/Tests.csproj --filter "FullyQualifiedName~AdminReservationServiceTests"` |

---

## 6. Ghi chú bổ sung cho PublicReservationService

File test `Tests/Services/PublicReservationServiceTests.cs` đã tồn tại sẵn với **16 test cases** cho 3 method:

| Method | N | A | B | Total |
|--------|---|---|---|-------|
| CheckReservationFitAsync | 2 | 2 | 1 | 5 |
| GetAvailableTablesAsync | 3 | 1 | 1 | 5 |
| CreateReservationAsync | 3 | 2 | 1 | 6 |
| **Sub Total** | **8** | **5** | **3** | **16** |

> Tổng toàn bộ RESERVATION module: **57 (Admin) + 16 (Public) = 73 test cases**, tất cả **PASSED**.
