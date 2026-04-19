# Unit Test Report Instructions — InventoryService

> **Target Excel:** `Docs/Report5.1_Unit Test.xlsx`
> **Test file:** `Tests/Services/InventoryServiceTests.cs`
> **Module:** INVENTORY
> **Total tests:** 22  |  **Passed:** 22  |  **Failed:** 0

---

## 1. Sheet: MethodList (starting row 9)

| No | Module Name | Method Name | Sheet Name | Description | Pre-Condition |
|----|-------------|-------------|------------|-------------|---------------|
| (next) | INVENTORY | GetItemsAsync | GetItemsAsync | Staff browses the paginated list of inventory items filtered by category, type, or low-stock status | N/A |
| (next) | INVENTORY | CreateTransactionAsync | CreateTransactionAsync | Staff creates a new inventory transaction in DRAFT status, auto-generates a transaction code, and optionally attaches evidence files | At least one item required |
| (next) | INVENTORY | GetTransactionDetailAsync | GetTransactionDetailAsync | Staff views the full detail of a single inventory transaction including items and media | N/A |
| (next) | INVENTORY | GetTransactionsAsync | GetTransactionsAsync | Staff browses the paginated list of inventory transactions filtered by type, status, or date | N/A |
| (next) | INVENTORY | SubmitTransactionAsync | SubmitTransactionAsync | Staff submits a DRAFT transaction for approval, changing status to PENDING_APPROVAL and notifying approvers | Transaction must be in DRAFT status |
| (next) | INVENTORY | ApproveTransactionAsync | ApproveTransactionAsync | Manager approves or rejects a PENDING_APPROVAL transaction; approval updates stock levels and notifies the creator | Transaction must be in PENDING_APPROVAL status; rejection note required when rejecting |
| (next) | INVENTORY | GetStockCardAsync | GetStockCardAsync | Staff views the paginated movement history for a specific ingredient as a stock ledger card | N/A |
| (next) | INVENTORY | GetDashboardAsync | GetDashboardAsync | Manager views the inventory dashboard summary including total items, low-stock alerts, and recent transactions | N/A |

---

## 2. Sheet: Statistics

| No | Function code | Passed | Failed | Untested | N | A | B | Total |
|----|---------------|--------|--------|----------|---|---|---|-------|
| (next) | GetItemsAsync | 2 | 0 | 0 | 1 | 0 | 1 | 2 |
| (next) | CreateTransactionAsync | 3 | 0 | 0 | 1 | 1 | 1 | 3 |
| (next) | GetTransactionDetailAsync | 3 | 0 | 0 | 1 | 1 | 1 | 3 |
| (next) | GetTransactionsAsync | 2 | 0 | 0 | 1 | 0 | 1 | 2 |
| (next) | SubmitTransactionAsync | 4 | 0 | 0 | 1 | 2 | 1 | 4 |
| (next) | ApproveTransactionAsync | 4 | 0 | 0 | 2 | 2 | 0 | 4 |
| (next) | GetStockCardAsync | 2 | 0 | 0 | 1 | 0 | 1 | 2 |
| (next) | GetDashboardAsync | 2 | 0 | 0 | 1 | 0 | 1 | 2 |
| **Sub total** | | **22** | **0** | **0** | **9** | **6** | **7** | **22** |

**Summary formulas:**
- Test coverage: `(22 + 0) / 22 × 100 = 100%`
- Test successful coverage: `22 / 22 × 100 = 100%`

---

## 3. Per-Method Sheets

---

### Sheet: GetItemsAsync

**Header:**
- Method: `GetItemsAsync`
- Test requirement: Staff retrieves a paginated list of inventory items with optional filters for category, type, and low-stock flag, delegating directly to the repository
- Passed: 2 | Failed: 0 | N/A/B: 1/0/1

**Test Case IDs:** `UTCID01`, `UTCID02`

**Condition section:**

| Condition Group | Input Value | UTCID01 | UTCID02 |
|-----------------|-------------|---------|---------|
| filter.PageIndex | `1` | O | O |
| filter.PageSize | `10` | O | O |
| repositoryReturn | 2 items | O | |
| repositoryReturn | 0 items (empty) | | O |

**Confirmation section:**

| Output | Expected Value | UTCID01 | UTCID02 |
|--------|----------------|---------|---------|
| Return | `PagedResultDTO` with `TotalCount=2`, `PageData.Count=2` | O | |
| Return | `PagedResultDTO` with `TotalCount=0`, `PageData` empty | | O |

**Result:**

| UTCID | Type | P/F | Date |
|-------|------|-----|------|
| UTCID01 | N | P | 2026-04-18 |
| UTCID02 | B | P | 2026-04-18 |

---

### Sheet: CreateTransactionAsync

**Header:**
- Method: `CreateTransactionAsync`
- Test requirement: Staff creates an inventory transaction with DRAFT status; the system auto-generates a transaction code in the format `{TYPE}-{YYYYMMDD}-{SEQ}` and delegates persistence to the repository
- Passed: 3 | Failed: 0 | N/A/B: 1/1/1

**Test Case IDs:** `UTCID01`, `UTCID02`, `UTCID03`

**Condition section:**

| Condition Group | Input Value | UTCID01 | UTCID02 | UTCID03 |
|-----------------|-------------|---------|---------|---------|
| request.TypeLvId | `10` (IN type, known) | O | | O |
| request.TypeLvId | `99` (unknown type) | | O | |
| request.Items | 1 item (`IngredientId=10, Qty=5`) | O | O | O |
| todayCount | `2` (existing transactions today) | O | | |
| todayCount | `0` (first transaction of the day) | | | O |

**Confirmation section:**

| Output | Expected Value | UTCID01 | UTCID02 | UTCID03 |
|--------|----------------|---------|---------|---------|
| Return | `InventoryTransactionDetailDto` with `TransactionId=1`, `StatusLvId=DraftStatusId` | O | | |
| Exception | `InvalidOperationException("*Unknown transaction type*")` | | O | |
| Verify | `CreateTransactionAsync` called with `TransactionCode` ending in `"-001"` | | | O |

**Result:**

| UTCID | Type | P/F | Date |
|-------|------|-----|------|
| UTCID01 | N | P | 2026-04-18 |
| UTCID02 | A | P | 2026-04-18 |
| UTCID03 | B | P | 2026-04-18 |

---

### Sheet: GetTransactionDetailAsync

**Header:**
- Method: `GetTransactionDetailAsync`
- Test requirement: Staff retrieves full details of a transaction by ID; the service throws KeyNotFoundException when the transaction does not exist
- Passed: 3 | Failed: 0 | N/A/B: 1/1/1

**Test Case IDs:** `UTCID01`, `UTCID02`, `UTCID03`

**Condition section:**

| Condition Group | Input Value | UTCID01 | UTCID02 | UTCID03 |
|-----------------|-------------|---------|---------|---------|
| transactionId | `1` (exists, PENDING_APPROVAL) | O | | |
| transactionId | `999` (not found) | | O | |
| transactionId | `0` (boundary zero ID, not found) | | | O |

**Confirmation section:**

| Output | Expected Value | UTCID01 | UTCID02 | UTCID03 |
|--------|----------------|---------|---------|---------|
| Return | `InventoryTransactionDetailDto` with `TransactionId=1`, `TransactionCode="IN-20260418-001"` | O | | |
| Exception | `KeyNotFoundException("*999*")` | | O | |
| Exception | `KeyNotFoundException("*0*")` | | | O |

**Result:**

| UTCID | Type | P/F | Date |
|-------|------|-----|------|
| UTCID01 | N | P | 2026-04-18 |
| UTCID02 | A | P | 2026-04-18 |
| UTCID03 | B | P | 2026-04-18 |

---

### Sheet: GetTransactionsAsync

**Header:**
- Method: `GetTransactionsAsync`
- Test requirement: Staff retrieves a paginated list of transactions filtered by type, status, or date, delegating directly to the repository
- Passed: 2 | Failed: 0 | N/A/B: 1/0/1

**Test Case IDs:** `UTCID01`, `UTCID02`

**Condition section:**

| Condition Group | Input Value | UTCID01 | UTCID02 |
|-----------------|-------------|---------|---------|
| filter.PageIndex | `1` | O | O |
| filter.PageSize | `10` | O | O |
| repositoryReturn | 2 transactions | O | |
| repositoryReturn | 0 transactions (empty) | | O |

**Confirmation section:**

| Output | Expected Value | UTCID01 | UTCID02 |
|--------|----------------|---------|---------|
| Return | `PagedResultDTO` with `TotalCount=2`, `PageData.Count=2` | O | |
| Return | `PagedResultDTO` with `TotalCount=0`, `PageData` empty | | O |

**Result:**

| UTCID | Type | P/F | Date |
|-------|------|-----|------|
| UTCID01 | N | P | 2026-04-18 |
| UTCID02 | B | P | 2026-04-18 |

---

### Sheet: SubmitTransactionAsync

**Header:**
- Method: `SubmitTransactionAsync`
- Test requirement: Staff submits a DRAFT transaction for manager approval; the system changes the status to PENDING_APPROVAL, optionally attaches media, and notifies approvers via the notification service
- Passed: 4 | Failed: 0 | N/A/B: 1/2/1

**Test Case IDs:** `UTCID01`, `UTCID02`, `UTCID03`, `UTCID04`

**Condition section:**

| Condition Group | Input Value | UTCID01 | UTCID02 | UTCID03 | UTCID04 |
|-----------------|-------------|---------|---------|---------|---------|
| transactionId | `1` (exists, DRAFT, IN type) | O | | | O |
| transactionId | `999` (not found) | | O | | |
| transactionId | `1` (exists, already PENDING) | | | O | |
| request.MediaIds | `[101]` (1 media ID) | O | | | |
| request | `null` (no media) | | O | O | O |

**Confirmation section:**

| Output | Expected Value | UTCID01 | UTCID02 | UTCID03 | UTCID04 |
|--------|----------------|---------|---------|---------|---------|
| Return | `StatusLvId=PendingStatusId`; `PublishAsync` called with Type=`INVENTORY_TX_SUBMITTED` | O | | | |
| Exception | `KeyNotFoundException("*999*")` | | O | | |
| Exception | `InvalidOperationException("*DRAFT*")` | | | O | |
| Return | `StatusLvId=PendingStatusId`; entity has no media attached | | | | O |

**Result:**

| UTCID | Type | P/F | Date |
|-------|------|-----|------|
| UTCID01 | N | P | 2026-04-18 |
| UTCID02 | A | P | 2026-04-18 |
| UTCID03 | A | P | 2026-04-18 |
| UTCID04 | B | P | 2026-04-18 |

---

### Sheet: ApproveTransactionAsync

**Header:**
- Method: `ApproveTransactionAsync`
- Test requirement: Manager approves or rejects a PENDING_APPROVAL transaction; approval sets the status to COMPLETED and applies stock changes; rejection sets the status to CANCELLED and requires a note; the creator is notified in both cases
- Passed: 4 | Failed: 0 | N/A/B: 2/2/0

**Test Case IDs:** `UTCID01`, `UTCID02`, `UTCID03`, `UTCID04`

**Condition section:**

| Condition Group | Input Value | UTCID01 | UTCID02 | UTCID03 | UTCID04 |
|-----------------|-------------|---------|---------|---------|---------|
| transactionId | `1` (exists, PENDING_APPROVAL) | O | O | | O |
| transactionId | `2` (exists, PENDING_APPROVAL, for reject) | | | | |
| transactionId | `999` (not found) | | | O | |
| transactionId | `1` (exists, still DRAFT, wrong status) | | | | O |
| request.IsApproved | `true` | O | | | |
| request.IsApproved | `false`, Note=`"Quantity mismatch"` | | O | | |
| request.IsApproved | `true` (on non-pending entity) | | | | O |

**Confirmation section:**

| Output | Expected Value | UTCID01 | UTCID02 | UTCID03 | UTCID04 |
|--------|----------------|---------|---------|---------|---------|
| Return | `StatusLvId=CompletedStatusId`; `ApprovedBy=200`; `PublishAsync` called once | O | | | |
| Return | `StatusLvId=CancelledStatusId`; `Note` contains `"Rejection reason: Quantity mismatch"` | | O | | |
| Exception | `KeyNotFoundException("*999*")` | | | O | |
| Exception | `InvalidOperationException("*PENDING_APPROVAL*")` | | | | O |

**Result:**

| UTCID | Type | P/F | Date |
|-------|------|-----|------|
| UTCID01 | N | P | 2026-04-18 |
| UTCID02 | N | P | 2026-04-18 |
| UTCID03 | A | P | 2026-04-18 |
| UTCID04 | A | P | 2026-04-18 |

---

### Sheet: GetStockCardAsync

**Header:**
- Method: `GetStockCardAsync`
- Test requirement: Staff retrieves the paginated stock movement ledger for an ingredient, delegating to the repository with the given ingredient ID and pagination parameters
- Passed: 2 | Failed: 0 | N/A/B: 1/0/1

**Test Case IDs:** `UTCID01`, `UTCID02`

**Condition section:**

| Condition Group | Input Value | UTCID01 | UTCID02 |
|-----------------|-------------|---------|---------|
| ingredientId | `10` (has movements) | O | |
| ingredientId | `0` (boundary zero, no movements) | | O |
| pageIndex | `1` | O | O |
| pageSize | `10` | O | O |

**Confirmation section:**

| Output | Expected Value | UTCID01 | UTCID02 |
|--------|----------------|---------|---------|
| Return | `PagedResultDTO` with `TotalCount=2`, 2 stock card entries | O | |
| Return | `PagedResultDTO` with `TotalCount=0`, empty `PageData` | | O |

**Result:**

| UTCID | Type | P/F | Date |
|-------|------|-----|------|
| UTCID01 | N | P | 2026-04-18 |
| UTCID02 | B | P | 2026-04-18 |

---

### Sheet: GetDashboardAsync

**Header:**
- Method: `GetDashboardAsync`
- Test requirement: Manager views the inventory dashboard with counters for total items, low-stock, out-of-stock, and pending transactions, plus lists of recent transactions and low-stock items
- Passed: 2 | Failed: 0 | N/A/B: 1/0/1

**Test Case IDs:** `UTCID01`, `UTCID02`

**Condition section:**

| Condition Group | Input Value | UTCID01 | UTCID02 |
|-----------------|-------------|---------|---------|
| repositoryReturn | Dashboard with TotalItems=50, LowStockItems=5, PendingTransactions=3 | O | |
| repositoryReturn | Empty dashboard (all counters = 0, empty lists) | | O |

**Confirmation section:**

| Output | Expected Value | UTCID01 | UTCID02 |
|--------|----------------|---------|---------|
| Return | `TotalItems=50`, `LowStockItems=5`, `PendingTransactions=3`, `LowStockList.Count=1` | O | |
| Return | `TotalItems=0`, `LowStockList` empty, `RecentTransactions` empty | | O |

**Result:**

| UTCID | Type | P/F | Date |
|-------|------|-----|------|
| UTCID01 | N | P | 2026-04-18 |
| UTCID02 | B | P | 2026-04-18 |

---

## 4. Test Case ↔ C# Method Mapping

| UTCID | C# Test Method | Trait Type |
|-------|----------------|------------|
| GetItemsAsync UTCID01 | `GetItemsAsync_WhenItemsExist_ReturnsPagedResult` | N |
| GetItemsAsync UTCID02 | `GetItemsAsync_WhenNoItems_ReturnsEmptyPagedResult` | B |
| CreateTransactionAsync UTCID01 | `CreateTransactionAsync_WhenValidInRequest_CreatesDraftTransactionAndReturnsDetail` | N |
| CreateTransactionAsync UTCID02 | `CreateTransactionAsync_WhenUnknownTypeLvId_ThrowsInvalidOperationException` | A |
| CreateTransactionAsync UTCID03 | `CreateTransactionAsync_WhenFirstTransactionOfDay_GeneratesSequenceNumberOne` | B |
| GetTransactionDetailAsync UTCID01 | `GetTransactionDetailAsync_WhenTransactionExists_ReturnsDetailDto` | N |
| GetTransactionDetailAsync UTCID02 | `GetTransactionDetailAsync_WhenTransactionNotFound_ThrowsKeyNotFoundException` | A |
| GetTransactionDetailAsync UTCID03 | `GetTransactionDetailAsync_WhenIdIsZero_ThrowsKeyNotFoundException` | B |
| GetTransactionsAsync UTCID01 | `GetTransactionsAsync_WhenTransactionsExist_ReturnsPagedResult` | N |
| GetTransactionsAsync UTCID02 | `GetTransactionsAsync_WhenNoTransactions_ReturnsEmptyPagedResult` | B |
| SubmitTransactionAsync UTCID01 | `SubmitTransactionAsync_WhenDraftInTransaction_ChangeStatusToPendingAndNotifiesApprovers` | N |
| SubmitTransactionAsync UTCID02 | `SubmitTransactionAsync_WhenTransactionNotFound_ThrowsKeyNotFoundException` | A |
| SubmitTransactionAsync UTCID03 | `SubmitTransactionAsync_WhenTransactionAlreadyPending_ThrowsInvalidOperationException` | A |
| SubmitTransactionAsync UTCID04 | `SubmitTransactionAsync_WhenNullRequest_SubmitsSuccessfullyWithoutAttachingMedia` | B |
| ApproveTransactionAsync UTCID01 | `ApproveTransactionAsync_WhenApproved_MovesToCompletedAndNotifiesCreator` | N |
| ApproveTransactionAsync UTCID02 | `ApproveTransactionAsync_WhenRejectedWithNote_MovesToCancelledAndAppendNote` | N |
| ApproveTransactionAsync UTCID03 | `ApproveTransactionAsync_WhenTransactionNotFound_ThrowsKeyNotFoundException` | A |
| ApproveTransactionAsync UTCID04 | `ApproveTransactionAsync_WhenTransactionNotPendingApproval_ThrowsInvalidOperationException` | A |
| GetStockCardAsync UTCID01 | `GetStockCardAsync_WhenIngredientHasMovements_ReturnsPagedStockCard` | N |
| GetStockCardAsync UTCID02 | `GetStockCardAsync_WhenIngredientIdIsZero_ReturnsEmptyResult` | B |
| GetDashboardAsync UTCID01 | `GetDashboardAsync_WhenCalled_ReturnsDashboardSummary` | N |
| GetDashboardAsync UTCID02 | `GetDashboardAsync_WhenNoData_ReturnsEmptyDashboard` | B |

---

## Notes

- Run command: `dotnet test Tests/Tests.csproj --filter "FullyQualifiedName~InventoryServiceTests"`
- All 22 tests passed on 2026-04-18.
- `SubmitTransactionAsync` and `ApproveTransactionAsync` use `IUnitOfWork` for database transactions; tests mock `BeginTransactionAsync`, `CommitAsync`, and `RollbackAsync`.
- `ApproveTransactionAsync` tests use entities with empty `InventoryTransactionItems` so that `ApplyStockChangesAsync` and `CheckLowStockAlertsAsync` are no-ops, keeping the test focused on the lifecycle state machine.
- Lookup IDs are constants: `DraftStatusId=1`, `PendingStatusId=2`, `CompletedStatusId=3`, `CancelledStatusId=4`, `InTypeId=10`, `OutTypeId=11`, `AdjustTypeId=12`.
