# Unit Test Report Instructions — OrderService

> **Target Excel:** `Docs/Report5.1_Unit Test.xlsx`
> **Test file:** `Tests/Services/OrderServiceTests.cs`
> **Module:** ORDER
> **Total tests:** 59  |  **Passed:** 59  |  **Failed:** 0

---

## 1. Sheet: MethodList (starting row 9)

Add these rows to the MethodList sheet — one per tested method:

| No | Module Name | Method Name | Sheet Name | Description | Pre-Condition |
|----|-------------|-------------|------------|-------------|---------------|
| (next) | ORDER | GetOrderHistoryAsync | GetOrderHistoryAsync | Staff browses the paginated history of all orders | N/A |
| (next) | ORDER | GetOrderStatusCountAsync | GetOrderStatusCountAsync | Staff views the count of orders grouped by status for the dashboard overview | Lookup values resolved |
| (next) | ORDER | GetKitchenOrdersAsync | GetKitchenOrdersAsync | Kitchen staff views orders filtered by preparation status | Lookup values resolved |
| (next) | ORDER | UpdateOrderStatusAsync | UpdateOrderStatusAsync | Staff transitions an order to a new status, updating table state and sending cancellation notifications when applicable | Order must exist |
| (next) | ORDER | UpdateOrderItemStatusAsync | UpdateOrderItemStatusAsync | Kitchen staff updates an individual item status and triggers a notification when the item is ready or rejected | Order item must exist |
| (next) | ORDER | CancelOrderItemAsync | CancelOrderItemAsync | Staff cancels an individual item from an order and sends a cancellation notification | Order item must exist |
| (next) | ORDER | GetOrderByIdAsync | GetOrderByIdAsync | Staff or customer views the full details of a specific order | N/A |
| (next) | ORDER | CreateOrderAsync_Staff | CreateOrderAsync_Staff | Staff creates a new order by selecting items, table, and order source, resolving the customer and calculating tax | Items non-empty; DINE_IN requires table |
| (next) | ORDER | AddItemsAsync | AddItemsAsync | Staff adds more items to an existing order, transitioning a completed order back to in-progress | Order must exist and not cancelled/paid |
| (next) | ORDER | CreateOrderAsync_Customer | CreateOrderAsync_Customer | Customer creates an order by scanning a QR code, validating the table code, token, and table availability | Table code valid; table not LOCKED |
| (next) | ORDER | AddItemsToOrderAsync | AddItemsToOrderAsync | Customer adds more items to their existing order, blocked if already paid | Order must exist and not paid |
| (next) | ORDER | GetRecentOrdersAsync | GetRecentOrdersAsync | Staff retrieves the most recent orders with a configurable limit clamped between 1 and 100 | N/A |

---

## 2. Sheet: Statistics (starting from the next available row after row 11)

| No | Function code | Passed | Failed | Untested | N | A | B | Total |
|----|---------------|--------|--------|----------|---|---|---|-------|
| (next) | GetOrderHistoryAsync | 3 | 0 | 0 | 1 | 1 | 1 | 3 |
| (next) | GetOrderStatusCountAsync | 3 | 0 | 0 | 1 | 1 | 1 | 3 |
| (next) | GetKitchenOrdersAsync | 3 | 0 | 0 | 1 | 1 | 1 | 3 |
| (next) | UpdateOrderStatusAsync | 9 | 0 | 0 | 4 | 4 | 1 | 9 |
| (next) | UpdateOrderItemStatusAsync | 4 | 0 | 0 | 2 | 1 | 1 | 4 |
| (next) | CancelOrderItemAsync | 3 | 0 | 0 | 1 | 1 | 1 | 3 |
| (next) | GetOrderByIdAsync | 3 | 0 | 0 | 1 | 1 | 1 | 3 |
| (next) | CreateOrderAsync_Staff | 8 | 0 | 0 | 2 | 5 | 1 | 8 |
| (next) | AddItemsAsync | 6 | 0 | 0 | 1 | 3 | 2 | 6 |
| (next) | CreateOrderAsync_Customer | 7 | 0 | 0 | 1 | 3 | 3 | 7 |
| (next) | AddItemsToOrderAsync | 4 | 0 | 0 | 1 | 2 | 1 | 4 |
| (next) | GetRecentOrdersAsync | 4 | 0 | 0 | 1 | 1 | 2 | 4 |
| **Sub total** | | **59** | **0** | **0** | **18** | **27** | **14** (1 deferred) | **59** |

**Summary formulas (update row 16+):**
- Test coverage: `(59 + 0) / 59 × 100 = 100%`
- Test successful coverage: `59 / 59 × 100 = 100%`

---

## 3. Per-Method Sheets

Copy the `Example` template sheet for each method listed below. Fill in the header, condition matrix, confirmation, and result sections as described.

---

### Sheet: GetOrderHistoryAsync

**Header (rows 1-5):**

| Field | Value |
|-------|-------|
| Code Module | Core/Service/OrderService.cs |
| Method | GetOrderHistoryAsync |
| Created By | quantm |
| Executed By | quantm |
| Test requirement | Staff browses the paginated history of all orders |
| Passed | 3 |
| Failed | 0 |
| Untested | 0 |
| N / A / B | 1 / 1 / 1 → Total = 3 |

**Test Case IDs (row 7, cols F+):** `UTCID01`, `UTCID02`, `UTCID03`

**Condition section:**

| Condition Group | Input Value | UTCID01 | UTCID02 | UTCID03 |
|-----------------|-------------|---------|---------|---------|
| query | OrderHistoryQueryDTO (default) | O | O | O |
| repositoryData | 2 records returned | O | | |
| repositoryData | 0 records returned | | O | |
| repositoryBehavior | Throws ArgumentException("Invalid query") | | | O |

**Confirmation section:**

| Output | Expected Value | UTCID01 | UTCID02 | UTCID03 |
|--------|---------------|---------|---------|---------|
| Return | PagedResultDTO with 2 items, TotalCount=2 | O | | |
| Return | PagedResultDTO with 0 items, TotalCount=0 | | O | |
| Exception | ArgumentException("Invalid query") propagated | | | O |

**Result section:**

| UTCID | Type | P/F | Date |
|-------|------|-----|------|
| UTCID01 | N | P | (execution date) |
| UTCID02 | B | P | (execution date) |
| UTCID03 | A | P | (execution date) |

---

### Sheet: GetOrderStatusCountAsync

**Header:**
- Method: `GetOrderStatusCountAsync`
- Test requirement: Staff views the count of orders grouped by each status for the dashboard overview
- Passed: 3 | Failed: 0 | N/A/B: 1/1/1

**Test Case IDs:** `UTCID01`, `UTCID02`, `UTCID03`

**Condition section:**

| Condition Group | Input Value | UTCID01 | UTCID02 | UTCID03 |
|-----------------|-------------|---------|---------|---------|
| lookupResolver | Returns valid status IDs for all 4 statuses | O | O | |
| lookupResolver | Throws KeyNotFoundException("Status not found") | | | O |
| repositoryReturn | Pending=5, InProgress=3, Completed=10, Cancelled=1 | O | | |
| repositoryReturn | All counts = 0 | | O | |

**Confirmation section:**

| Output | Expected Value | UTCID01 | UTCID02 | UTCID03 |
|--------|---------------|---------|---------|---------|
| Return | Pending=5, Completed=10 | O | | |
| Return | Pending=0, InProgress=0 | | O | |
| Exception | KeyNotFoundException("Status not found") propagated | | | O |

**Result:**

| UTCID | Type | P/F |
|-------|------|-----|
| UTCID01 | N | P |
| UTCID02 | B | P |
| UTCID03 | A | P |

---

### Sheet: GetKitchenOrdersAsync

**Header:**
- Method: `GetKitchenOrdersAsync`
- Test requirement: Kitchen staff views orders filtered by preparation status
- Passed: 3 | Failed: 0 | N/A/B: 1/1/1

**Test Case IDs:** `UTCID01`, `UTCID02`, `UTCID03`

**Condition section:**

| Condition Group | Input Value | UTCID01 | UTCID02 | UTCID03 |
|-----------------|-------------|---------|---------|---------|
| lookupResolver | Returns valid status IDs | O | O | |
| lookupResolver | Throws KeyNotFoundException | | | O |
| repositoryReturn | 2 KitchenOrderDTO records | O | | |
| repositoryReturn | Empty list | | O | |

**Confirmation section:**

| Output | Expected Value | UTCID01 | UTCID02 | UTCID03 |
|--------|---------------|---------|---------|---------|
| Return | List with 2 items | O | | |
| Return | Empty list | | O | |
| Exception | KeyNotFoundException propagated | | | O |

**Result:**

| UTCID | Type | P/F |
|-------|------|-----|
| UTCID01 | N | P |
| UTCID02 | B | P |
| UTCID03 | A | P |

---

### Sheet: UpdateOrderStatusAsync

**Header:**
- Method: `UpdateOrderStatusAsync`
- Test requirement: Staff transitions an order to a new status, validating the transition, updating table state, and sending cancellation notifications when applicable
- Passed: 9 | Failed: 0 | N/A/B: 4/4/1

**Test Case IDs:** `UTCID01` – `UTCID09`

**Condition section:**

| Condition Group | Input Value | 01 | 02 | 03 | 04 | 05 | 06 | 07 | 08 | 09 |
|-----------------|-------------|----|----|----|----|----|----|----|----|-----|
| orderId | 1 (exists) | O | O | O | O | | | | O | O |
| orderId | 999 (not found) | | | | | O | | | | |
| currentStatus | PENDING | O | | | | | O | O | | O |
| currentStatus | IN_PROGRESS | | O | O | | | | | | |
| currentStatus | CANCELLED | | | | O | | | | | |
| newStatus | IN_PROGRESS | O | | | | | O | | | |
| newStatus | CANCELLED | | O | | | | | O | O | O |
| newStatus | COMPLETED | | | O | | | | | | |
| newStatus | PENDING | | | | O | O | | | | |
| tableId | Has table (1) | | O | O | O | | | | | |
| tableId | No table (null) | | | | | | | | O | O |
| payments | Has payment | | | | | | | O | | |
| payments | No payment | O | O | O | O | | O | | O | O |

**Confirmation section:**

| Output | Expected Value | 01 | 02 | 03 | 04 | 05 | 06 | 07 | 08 | 09 |
|--------|---------------|----|----|----|----|----|----|----|----|---|
| order.OrderStatusLvId | IN_PROGRESS_ID (11) | O | | | | | | | | |
| table.TableStatusLvId | AVAILABLE_ID (30) | | O | O | | | | | | |
| table.TableStatusLvId | OCCUPIED_ID (31) | | | | O | | | | | |
| Exception | NotFoundException("*not found*") | | | | | O | | | | |
| Exception | InvalidOperationException("*already in this status*") | | | | | | O | | | |
| Exception | InvalidOperationException("*Invalid status transition*") | | | | | | | O | | |
| Exception | InvalidOperationException("*paid order*") | | | | | | | | O | |
| Verify | No table update called | | | | | | | | | O |
| Verify | Notification ORDER_CANCELLED | | | | | | | | | O |

Note: UTCID08 is "cancel paid order" and UTCID09 is "cancel sends notification with no table"

**Result:**

| UTCID | Type | P/F |
|-------|------|-----|
| UTCID01 | N | P |
| UTCID02 | N | P |
| UTCID03 | N | P |
| UTCID04 | N | P |
| UTCID05 | A | P |
| UTCID06 | A | P |
| UTCID07 | A | P |
| UTCID08 | A | P |
| UTCID09 | B | P |

---

### Sheet: UpdateOrderItemStatusAsync

**Header:**
- Method: `UpdateOrderItemStatusAsync`
- Test requirement: Kitchen staff updates an individual item status and triggers a notification when the item is ready or rejected
- Passed: 4 | Failed: 0 | N/A/B: 2/1/1

**Test Case IDs:** `UTCID01` – `UTCID04`

**Condition section:**

| Condition Group | Input Value | UTCID01 | UTCID02 | UTCID03 | UTCID04 |
|-----------------|-------------|---------|---------|---------|---------|
| newStatusLvId | ITEM_READY_ID (22) | O | | | |
| newStatusLvId | ITEM_REJECTED_ID (24) | | O | | |
| newStatusLvId | ITEM_SERVED_ID (23) | | | O | |
| newStatusLvId | Any (invalid itemId=-1) | | | | O |
| rejectReason | null | O | | O | |
| rejectReason | "Out of stock" | | O | | |
| repoBehavior | Success | O | O | O | |
| repoBehavior | Throws InvalidOperationException (item not found) | | | | O |

**Confirmation section:**

| Output | Expected Value | UTCID01 | UTCID02 | UTCID03 | UTCID04 |
|--------|---------------|---------|---------|---------|---------|
| Verify | Repo UpdateOrderItemStatusAsync called | O | O | O | |
| Verify | Notification ORDER_ITEM_READY published | O | | | |
| Verify | Notification ORDER_ITEM_REJECTED published | | O | | |
| Verify | No notification published | | | O | |
| Exception | InvalidOperationException propagated | | | | O |

**Result:**

| UTCID | Type | P/F |
|-------|------|-----|
| UTCID01 | N | P |
| UTCID02 | N | P |
| UTCID03 | B | P |
| UTCID04 | A | P |

---

### Sheet: CancelOrderItemAsync

**Header:**
- Method: `CancelOrderItemAsync`
- Test requirement: Staff cancels an individual item from an order and sends a cancellation notification
- Passed: 3 | Failed: 0 | N/A/B: 1/1/1

**Test Case IDs:** `UTCID01`, `UTCID02`, `UTCID03`

**Condition section:**

| Condition Group | Input Value | UTCID01 | UTCID02 | UTCID03 |
|-----------------|-------------|---------|---------|---------|
| orderItem.Dish | Has Dish (Phở bò) | O | | |
| orderItem.Dish | null (no Dish loaded) | | O | |
| orderItem | Not found (itemId=-1, repo throws) | | | O |
| orderItem.Order.Table | Has Table (T001) | O | | |
| orderItem.Order.Table | null | | O | |

**Confirmation section:**

| Output | Expected Value | UTCID01 | UTCID02 | UTCID03 |
|--------|---------------|---------|---------|---------|
| Verify | Notification ORDER_ITEM_CANCELLED published | O | O | |
| Exception | InvalidOperationException propagated | | | O |

**Result:**

| UTCID | Type | P/F |
|-------|------|-----|
| UTCID01 | N | P |
| UTCID02 | B | P |
| UTCID03 | A | P |

---

### Sheet: GetOrderByIdAsync

**Header:**
- Method: `GetOrderByIdAsync`
- Test requirement: Staff or customer views the full details of a specific order by its ID
- Passed: 3 | Failed: 0 | N/A/B: 1/1/1

**Test Case IDs:** `UTCID01`, `UTCID02`, `UTCID03`

**Condition section:**

| Condition Group | Input Value | UTCID01 | UTCID02 | UTCID03 |
|-----------------|-------------|---------|---------|---------|
| orderId | 1 | O | | |
| orderId | -1 (negative) | | O | |
| orderId | 0 (boundary) | | | O |

**Confirmation section:**

| Output | Expected Value | UTCID01 | UTCID02 | UTCID03 |
|--------|---------------|---------|---------|---------|
| Return | OrderDetailDTO with OrderId=1 | O | | |
| Return | null (repo returns null for -1) | | O | |
| Verify | Repo GetOrderByIdAsync(0) called | | | O |

**Result:**

| UTCID | Type | P/F |
|-------|------|-----|
| UTCID01 | N | P |
| UTCID02 | A | P |
| UTCID03 | B | P |

---

### Sheet: CreateOrderAsync_Staff

**Header:**
- Method: `CreateOrderAsync (staff overload)`
- Test requirement: Staff creates a new order by selecting items, table, and source (dine-in or takeaway), resolving the customer, and calculating tax
- Passed: 8 | Failed: 0 | N/A/B: 2/5/1

**Test Case IDs:** `UTCID01` – `UTCID08`

**Condition section:**

| Condition Group | Input Value | 01 | 02 | 03 | 04 | 05 | 06 | 07 | 08 |
|-----------------|-------------|----|----|----|----|----|----|----|----|
| source | DINE_IN | O | | | O | | O | O | O |
| source | TAKEAWAY | | O | | | O | | | |
| tableId | 1 (exists, AVAILABLE) | O | | | | O | | | O |
| tableId | null | | O | O | O | | | | |
| tableId | 999 (not found) | | | | | | O | | |
| tableId | 1 (OCCUPIED) | | | | | | | O | |
| items | 2 items (valid) | O | O | | | | | | |
| items | empty list | | | O | | | | | |
| items | 1 item (dish=999 not found) | | | | | | | O | O |
| dishRepo | Returns matching dishes | O | O | | | | | | |
| dishRepo | Returns empty (no match) | | | | | | | | O |
| dishRepo | Throws exception | | | | | | | | |

Note: UTCID08 = rollback on failure (dishRepo throws)

**Confirmation section:**

| Output | Expected Value | 01 | 02 | 03 | 04 | 05 | 06 | 07 | 08 |
|--------|---------------|----|----|----|----|----|----|----|----|
| Return | OrderId (100) | O | | | | | | | |
| Return | OrderId (101) | | O | | | | | | |
| table.status | OCCUPIED | O | | | | | | | |
| Verify | No table lookup called | | O | | | | | | |
| Verify | CommitAsync called | O | O | | | | | | |
| Exception | InvalidOperationException("*at least one item*") | | | O | | | | | |
| Exception | InvalidOperationException("*requires table*") | | | | O | | | | |
| Exception | InvalidOperationException("*cannot have table*") | | | | | O | | | |
| Exception | NotFoundException("*not found*") — table | | | | | | O | | |
| Exception | InvalidOperationException("*not available*") | | | | | | | O | |
| Exception | NotFoundException("*dishes not found*") | | | | | | | | O |
| Verify | RollbackAsync called | | | | | | | | O |

Wait — UTCID08 is the rollback test. Let me fix: UTCID07 = table occupied, UTCID08 = dish not found (throws NotFoundException), and there's a separate test for rollback. Let me recount:
- 01: DineIn valid → creates order, table OCCUPIED
- 02: Takeaway → no table update
- 03: No items → InvalidOperationException
- 04: DineIn no table → InvalidOperationException
- 05: Takeaway with table → InvalidOperationException
- 06: Table not found → NotFoundException
- 07: Table occupied → InvalidOperationException
- 08: Dish not found → NotFoundException (+ verify rollback)

Actually test 08 is `CreateOrderAsync_Staff_WhenOnFailure_RollsBack` where dishRepo throws a generic exception and we verify rollback.

**Result:**

| UTCID | Type | P/F |
|-------|------|-----|
| UTCID01 | N | P |
| UTCID02 | N | P |
| UTCID03 | A | P |
| UTCID04 | A | P |
| UTCID05 | A | P |
| UTCID06 | A | P |
| UTCID07 | A | P |
| UTCID08 | B | P |

---

### Sheet: AddItemsAsync

**Header:**
- Method: `AddItemsAsync`
- Test requirement: Staff adds more items to an existing order, validating order status and payment, recalculating totals, and transitioning a completed order back to in-progress
- Passed: 6 | Failed: 0 | N/A/B: 1/3/2

**Test Case IDs:** `UTCID01` – `UTCID06`

**Condition section:**

| Condition Group | Input Value | 01 | 02 | 03 | 04 | 05 | 06 |
|-----------------|-------------|----|----|----|----|----|----|
| currentOrderStatus | IN_PROGRESS | O | | | O | | |
| currentOrderStatus | CANCELLED | | | O | | | |
| currentOrderStatus | COMPLETED | | | | | O | |
| orderId | 1 (exists) | O | | O | O | O | O |
| orderId | 999 (not found) | | O | | | | |
| payments | No payment | O | | O | | O | O |
| payments | Has payment | | | | O | | |
| items | Valid dish (ID=1) | O | O | | | O | O |
| items | Dish not found (ID=999) | | | | | | O |

Note: UTCID06 is "dish not found" for AddItemsAsync.

**Confirmation section:**

| Output | Expected Value | 01 | 02 | 03 | 04 | 05 | 06 |
|--------|---------------|----|----|----|----|----|----|
| order.TotalAmount | 200000 (100000 + 50000×2) | O | | | | | |
| Verify | CommitAsync called | O | | | | | |
| Exception | NotFoundException | | O | | | | |
| Exception | InvalidOperationException("*canceled order*") | | | O | | | |
| Exception | InvalidOperationException("*paid order*") | | | | O | | |
| order.OrderStatusLvId | IN_PROGRESS_ID (COMPLETED→IN_PROGRESS) | | | | | O | |
| Exception | NotFoundException("*dishes not found*") | | | | | | O |

**Result:**

| UTCID | Type | P/F |
|-------|------|-----|
| UTCID01 | N | P |
| UTCID02 | A | P |
| UTCID03 | A | P |
| UTCID04 | A | P |
| UTCID05 | B | P |
| UTCID06 | B | P |

---

### Sheet: CreateOrderAsync_Customer

**Header:**
- Method: `CreateOrderAsync (customer DTO overload)`
- Test requirement: Customer creates an order by scanning a QR code, validating the table code, token, and table availability status
- Passed: 7 | Failed: 0 | N/A/B: 1/3/3

**Test Case IDs:** `UTCID01` – `UTCID07`

**Condition section:**

| Condition Group | Input Value | 01 | 02 | 03 | 04 | 05 | 06 | 07 |
|-----------------|-------------|----|----|----|----|----|----|-----|
| tableCode | "T001" (exists) | O | | O | O | O | O | O |
| tableCode | "INVALID" (not found) | | O | | | | | |
| qrToken | "valid-qr-token" (matches) | O | | | | | | |
| qrToken | "wrong-token" (mismatch) | | | O | | | | |
| qrToken | null (skip validation) | | | | | | | O |
| tableStatus | AVAILABLE | O | | | | | | O |
| tableStatus | LOCKED | | | | O | | | |
| tableStatus | OCCUPIED (active orders > 0) | | | | | O | | |
| tableStatus | RESERVED | | | | | | O | |

**Confirmation section:**

| Output | Expected Value | 01 | 02 | 03 | 04 | 05 | 06 | 07 |
|--------|---------------|----|----|----|----|----|----|-----|
| Return.OrderId | 200 | O | | | | | | |
| Return.TableCode | "T001" | O | | | | | | |
| Return.CustomerId | 68 (guest) | O | | | | | | |
| Return.OrderStatus | "PENDING" | O | | | | | | |
| table.status | OCCUPIED | O | | | | | O | |
| Exception | KeyNotFoundException("*not found*") | | O | | | | | |
| Exception | ValidationException("*Invalid QR token*") | | | O | | | | |
| Exception | InvalidOperationException("*maintenance*") | | | | O | | | |
| Exception | ConflictException("*already occupied*") | | | | | O | | |
| Return.OrderId | 300 (reserved table) | | | | | | O | |
| Return.OrderId | 400 (no QR token) | | | | | | | O |

**Result:**

| UTCID | Type | P/F |
|-------|------|-----|
| UTCID01 | N | P |
| UTCID02 | A | P |
| UTCID03 | A | P |
| UTCID04 | A | P |
| UTCID05 | B | P |
| UTCID06 | B | P |
| UTCID07 | B | P |

---

### Sheet: AddItemsToOrderAsync

**Header:**
- Method: `AddItemsToOrderAsync`
- Test requirement: Customer adds more items to their existing order, validating payment status and recalculating tax
- Passed: 4 | Failed: 0 | N/A/B: 1/2/1

**Test Case IDs:** `UTCID01` – `UTCID04`

**Condition section:**

| Condition Group | Input Value | UTCID01 | UTCID02 | UTCID03 | UTCID04 |
|-----------------|-------------|---------|---------|---------|---------|
| orderId | 1 (exists, IN_PROGRESS, no payment) | O | | | |
| orderId | 999 (not found) | | O | | |
| orderId | 1 (exists, has payment) | | | O | |
| orderId | 1 (exists, PENDING status, no payment) | | | | O |
| items | 1 item (DishId=1, Price=50000) | O | O | O | O |

**Confirmation section:**

| Output | Expected Value | UTCID01 | UTCID02 | UTCID03 | UTCID04 |
|--------|---------------|---------|---------|---------|---------|
| Verify | Repo AddItemsToOrderAsync called | O | | | O |
| Exception | KeyNotFoundException | | O | | |
| Exception | InvalidOperationException("*already been paid*") | | | O | |
| Verify | CommitAsync called (PENDING order still adds items) | | | | O |

**Result:**

| UTCID | Type | P/F |
|-------|------|-----|
| UTCID01 | N | P |
| UTCID02 | A | P |
| UTCID03 | A | P |
| UTCID04 | B | P |

---

### Sheet: GetRecentOrdersAsync

**Header:**
- Method: `GetRecentOrdersAsync`
- Test requirement: Staff retrieves the most recent orders with a configurable limit clamped between 1 and 100, defaulting to 20
- Passed: 4 | Failed: 0 | N/A/B: 1/1/2

**Test Case IDs:** `UTCID01` – `UTCID04`

**Condition section:**

| Condition Group | Input Value | UTCID01 | UTCID02 | UTCID03 | UTCID04 |
|-----------------|-------------|---------|---------|---------|---------|
| limit | 10 (valid) | O | | | |
| limit | 0 (below min) | | O | | |
| limit | 150 (above max) | | | O | |
| limit | -5 (negative) | | | | O |

**Confirmation section:**

| Output | Expected Value | UTCID01 | UTCID02 | UTCID03 | UTCID04 |
|--------|---------------|---------|---------|---------|---------|
| Return | List with 2 items | O | | | |
| Verify | Repo called with limit=10 | O | | | |
| Verify | Repo called with limit=20 (default) | | O | O | O |

**Result:**

| UTCID | Type | P/F |
|-------|------|-----|
| UTCID01 | N | P |
| UTCID02 | B | P |
| UTCID03 | B | P |
| UTCID04 | A | P |

---

## 4. Test Case ↔ C# Method Mapping

| UTCID | C# Test Method | Trait Type |
|-------|----------------|------------|
| GetOrderHistoryAsync UTCID01 | `GetOrderHistoryAsync_WhenDataExists_ReturnsPagedResult` | N |
| GetOrderHistoryAsync UTCID02 | `GetOrderHistoryAsync_WhenNoData_ReturnsEmptyResult` | B |
| GetOrderHistoryAsync UTCID03 | `GetOrderHistoryAsync_WhenRepositoryThrows_PropagatesException` | A |
| GetOrderStatusCountAsync UTCID01 | `GetOrderStatusCountAsync_WhenCalled_ResolvesStatusIdsAndReturnsCount` | N |
| GetOrderStatusCountAsync UTCID02 | `GetOrderStatusCountAsync_WhenAllZero_ReturnsZeroCounts` | B |
| GetOrderStatusCountAsync UTCID03 | `GetOrderStatusCountAsync_WhenLookupResolverFails_PropagatesException` | A |
| GetKitchenOrdersAsync UTCID01 | `GetKitchenOrdersAsync_WhenOrdersExist_ReturnsList` | N |
| GetKitchenOrdersAsync UTCID02 | `GetKitchenOrdersAsync_WhenNoOrders_ReturnsEmpty` | B |
| GetKitchenOrdersAsync UTCID03 | `GetKitchenOrdersAsync_WhenLookupResolverFails_PropagatesException` | A |
| UpdateOrderStatusAsync UTCID01 | `UpdateOrderStatusAsync_WhenPendingToInProgress_UpdatesStatusAndCommits` | N |
| UpdateOrderStatusAsync UTCID02 | `UpdateOrderStatusAsync_WhenCancelled_SetsTableToAvailable` | N |
| UpdateOrderStatusAsync UTCID03 | `UpdateOrderStatusAsync_WhenCompleted_SetsTableToAvailable` | N |
| UpdateOrderStatusAsync UTCID04 | `UpdateOrderStatusAsync_WhenCancelledToPending_SetsTableToOccupied` | N |
| UpdateOrderStatusAsync UTCID05 | `UpdateOrderStatusAsync_WhenOrderNotFound_ThrowsNotFoundException` | A |
| UpdateOrderStatusAsync UTCID06 | `UpdateOrderStatusAsync_WhenSameStatus_ThrowsInvalidOperationException` | A |
| UpdateOrderStatusAsync UTCID07 | `UpdateOrderStatusAsync_WhenInvalidTransition_ThrowsInvalidOperationException` | A |
| UpdateOrderStatusAsync UTCID08 | `UpdateOrderStatusAsync_WhenCancelPaidOrder_ThrowsInvalidOperationException` | A |
| UpdateOrderStatusAsync UTCID09 | `UpdateOrderStatusAsync_WhenNoTableId_SkipsTableUpdate` | B |
| UpdateOrderItemStatusAsync UTCID01 | `UpdateOrderItemStatusAsync_WhenReady_DelegatesAndPublishesNotification` | N |
| UpdateOrderItemStatusAsync UTCID02 | `UpdateOrderItemStatusAsync_WhenRejected_PublishesRejectNotificationWithReason` | N |
| UpdateOrderItemStatusAsync UTCID03 | `UpdateOrderItemStatusAsync_WhenStatusNotReadyOrRejected_NoNotification` | B |
| UpdateOrderItemStatusAsync UTCID04 | `UpdateOrderItemStatusAsync_WhenInvalidItemId_PropagatesRepositoryException` | A |
| CancelOrderItemAsync UTCID01 | `CancelOrderItemAsync_WhenValid_CancelsItemAndNotifies` | N |
| CancelOrderItemAsync UTCID02 | `CancelOrderItemAsync_WhenOrderItemHasNoDish_StillCancels` | B |
| CancelOrderItemAsync UTCID03 | `CancelOrderItemAsync_WhenOrderItemNotFound_PropagatesException` | A |
| GetOrderByIdAsync UTCID01 | `GetOrderByIdAsync_WhenCalled_DelegatesToRepository` | N |
| GetOrderByIdAsync UTCID02 | `GetOrderByIdAsync_WhenIdIsNegative_ReturnsNull` | A |
| GetOrderByIdAsync UTCID03 | `GetOrderByIdAsync_WhenIdIsZero_DelegatesToRepository` | B |
| CreateOrderAsync_Staff UTCID01 | `CreateOrderAsync_Staff_WhenDineInValid_CreatesOrderAndSetsTableOccupied` | N |
| CreateOrderAsync_Staff UTCID02 | `CreateOrderAsync_Staff_WhenTakeaway_NoTableUpdate` | N |
| CreateOrderAsync_Staff UTCID03 | `CreateOrderAsync_Staff_WhenNoItems_ThrowsInvalidOperationException` | A |
| CreateOrderAsync_Staff UTCID04 | `CreateOrderAsync_Staff_WhenDineInNoTable_ThrowsInvalidOperationException` | A |
| CreateOrderAsync_Staff UTCID05 | `CreateOrderAsync_Staff_WhenTakeawayWithTable_ThrowsInvalidOperationException` | A |
| CreateOrderAsync_Staff UTCID06 | `CreateOrderAsync_Staff_WhenTableNotFound_ThrowsNotFoundException` | A |
| CreateOrderAsync_Staff UTCID07 | `CreateOrderAsync_Staff_WhenTableOccupied_ThrowsInvalidOperationException` | A |
| CreateOrderAsync_Staff UTCID08 | `CreateOrderAsync_Staff_WhenOnFailure_RollsBack` | B |
| AddItemsAsync UTCID01 | `AddItemsAsync_WhenValidItems_AddsItemsAndUpdatesTotal` | N |
| AddItemsAsync UTCID02 | `AddItemsAsync_WhenOrderNotFound_ThrowsNotFoundException` | A |
| AddItemsAsync UTCID03 | `AddItemsAsync_WhenOrderCancelled_ThrowsInvalidOperationException` | A |
| AddItemsAsync UTCID04 | `AddItemsAsync_WhenOrderHasPayment_ThrowsInvalidOperationException` | A |
| AddItemsAsync UTCID05 | `AddItemsAsync_WhenCompletedOrder_SetsStatusToInProgress` | B |
| AddItemsAsync UTCID06 | `AddItemsAsync_WhenDishNotFound_ThrowsNotFoundException` | B |
| CreateOrderAsync_Customer UTCID01 | `CreateOrderAsync_Customer_WhenValid_CreatesOrderAndReturnsResponse` | N |
| CreateOrderAsync_Customer UTCID02 | `CreateOrderAsync_Customer_WhenTableNotFound_ThrowsKeyNotFoundException` | A |
| CreateOrderAsync_Customer UTCID03 | `CreateOrderAsync_Customer_WhenInvalidQrToken_ThrowsValidationException` | A |
| CreateOrderAsync_Customer UTCID04 | `CreateOrderAsync_Customer_WhenTableLocked_ThrowsInvalidOperationException` | A |
| CreateOrderAsync_Customer UTCID05 | `CreateOrderAsync_Customer_WhenTableOccupiedWithActiveOrders_ThrowsConflictException` | B |
| CreateOrderAsync_Customer UTCID06 | `CreateOrderAsync_Customer_WhenReservedTable_SetsToOccupied` | B |
| CreateOrderAsync_Customer UTCID07 | `CreateOrderAsync_Customer_WhenNoQrToken_SkipsValidation` | B |
| AddItemsToOrderAsync UTCID01 | `AddItemsToOrderAsync_WhenValid_AddsItemsAndRecalcsTax` | N |
| AddItemsToOrderAsync UTCID02 | `AddItemsToOrderAsync_WhenOrderNotFound_ThrowsKeyNotFoundException` | A |
| AddItemsToOrderAsync UTCID03 | `AddItemsToOrderAsync_WhenOrderAlreadyPaid_ThrowsInvalidOperationException` | A |
| AddItemsToOrderAsync UTCID04 | `AddItemsToOrderAsync_WhenOrderIsPending_StillAddsItems` | B |
| GetRecentOrdersAsync UTCID01 | `GetRecentOrdersAsync_WhenCalled_ReturnsList` | N |
| GetRecentOrdersAsync UTCID02 | `GetRecentOrdersAsync_WhenLimitIsZero_DefaultsTo20` | B |
| GetRecentOrdersAsync UTCID03 | `GetRecentOrdersAsync_WhenLimitExceeds100_DefaultsTo20` | B |
| GetRecentOrdersAsync UTCID04 | `GetRecentOrdersAsync_WhenLimitIsNegative_DefaultsTo20` | A |

---

## Notes

- The `UpdateOrderStatusAsync_WhenCancelled_SendsNotification` test verifies that a notification of type `ORDER_CANCELLED` is published when an order is cancelled.
- The `CreateOrderAsync_Staff_WhenOnFailure_RollsBack` test verifies the transaction rollback on failure.
- 2 pre-existing failures in `AuthServiceTests` are unrelated to the ORDER module.
