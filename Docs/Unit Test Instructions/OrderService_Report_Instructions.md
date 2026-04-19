# Unit Test Report Instructions — OrderService

## How to fill `Report5.1_Unit Test.xlsx` for OrderService

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
| 1 | ORDER | GetOrderHistoryAsync | GetOrderHistoryAsync | Staff retrieves a paginated order history list by applying optional filters such as status, date range, and search keyword | OrderRepository is available |
| 2 | ORDER | GetOrderStatusCountAsync | GetOrderStatusCountAsync | Staff views a dashboard summary showing the count of orders in each status (Pending, In Progress, Completed, Cancelled) | Lookup values for order statuses are configured |
| 3 | ORDER | GetKitchenOrdersAsync | GetKitchenOrdersAsync | Kitchen staff views the list of active orders with their items, statuses, and table codes to manage food preparation | Lookup values for order statuses are configured |
| 4 | ORDER | UpdateOrderStatusAsync | UpdateOrderStatusAsync | Staff transitions an order from one status to another following allowed transition rules, updating the table status accordingly | Order exists; lookup values for order and table statuses are configured |
| 5 | ORDER | CancelOrderItemAsync | CancelOrderItemAsync | Staff cancels a specific order item, updating its status to CANCELLED and sending a notification to the kitchen with dish and table details | Order item exists; lookup values for item statuses are configured |
| 6 | ORDER | GetOrderByIdAsync | GetOrderByIdAsync | Staff retrieves the full detail of a specific order including items, promotions, coupons, and payments by providing the order ID | OrderRepository is available |
| 7 | ORDER | CreateOrderAsync_Staff | CreateOrderAsync_Staff | Staff creates a new order by selecting items, table (for dine-in), and source, resolving the customer and calculating item totals and tax | Dishes exist; table is available (for dine-in); lookup values configured |
| 8 | ORDER | AddItemsAsync | AddItemsAsync | Staff adds more items to an existing active order, updating the total amount and optionally resolving a new customer | Order exists and is not cancelled/paid; dishes exist |
| 9 | ORDER | CreateOrderAsync_Customer | CreateOrderAsync_Customer | Customer creates a new order by scanning a table QR code, selecting menu items, and submitting from the customer-facing menu interface | Table exists and is not locked/deleted; lookup values configured |
| 10 | ORDER | AddItemsToOrderAsync | AddItemsToOrderAsync | Customer adds more items to their existing order on the same table through the customer-facing menu, with notification sent to kitchen staff | Order exists and is not paid; lookup values configured |
| 11 | ORDER | GetRecentOrdersAsync | GetRecentOrdersAsync | Staff views the most recent orders on the dashboard, with the limit automatically capped between 1 and 100 (defaults to 20 if out of range) | OrderRepository is available |

---

## 3. Sheet: Statistics

Starting from **row 12**, one row per method:

| No | Function Code | Passed | Failed | Untested | N | A | B | Total |
|----|--------------|--------|--------|----------|---|---|---|-------|
| 1 | GetOrderHistoryAsync | 4 | 0 | 0 | 3 | 0 | 1 | 4 |
| 2 | GetOrderStatusCountAsync | 3 | 0 | 0 | 2 | 0 | 1 | 3 |
| 3 | GetKitchenOrdersAsync | 3 | 0 | 0 | 2 | 0 | 1 | 3 |
| 4 | UpdateOrderStatusAsync | 12 | 0 | 0 | 5 | 4 | 3 | 12 |
| 5 | CancelOrderItemAsync | 3 | 0 | 0 | 2 | 0 | 1 | 3 |
| 6 | GetOrderByIdAsync | 5 | 0 | 0 | 2 | 1 | 2 | 5 |
| 7 | CreateOrderAsync_Staff | 8 | 0 | 0 | 3 | 5 | 0 | 8 |
| 8 | AddItemsAsync | 7 | 0 | 0 | 3 | 4 | 0 | 7 |
| 9 | CreateOrderAsync_Customer | 7 | 0 | 0 | 3 | 3 | 1 | 7 |
| 10 | AddItemsToOrderAsync | 5 | 0 | 0 | 2 | 3 | 0 | 5 |
| 11 | GetRecentOrdersAsync | 7 | 0 | 0 | 2 | 0 | 5 | 7 |
| **Sub Total** | | **64** | **0** | **0** | **29** | **20** | **15** | **64** |

- **Test Coverage**: (64 + 0) / 64 × 100 = **100%**
- **Test Success Rate**: 64 / 64 × 100 = **100%**

---

## 4. Per-Method Sheets

For each method below, **copy the `Example` sheet template**, rename the tab to the method name, and fill in the data.

---

### Sheet: GetOrderHistoryAsync

**Header (rows 1–5):**

| Row | Col A | Col C | Col F | Col L |
|-----|-------|-------|-------|-------|
| 1 | Code Module | Core/Service/OrderService.cs | Method | GetOrderHistoryAsync |
| 2 | Created By | quantm | Executed By | quantm |
| 3 | Test requirement | Staff retrieves a paginated order history list by applying optional filters such as status, date range, and search keyword | | |
| 4 | Passed: 4 | Failed: 0 | Untested: 0 | N: 3 / A: 0 / B: 1 |

**Test Case IDs (row 7, cols F+):** `UTCID01` | `UTCID02` | `UTCID03` | `UTCID04`

**Condition Matrix:**

| Col A | Col B | Col D | UTCID01 | UTCID02 | UTCID03 | UTCID04 |
|-------|-------|-------|---------|---------|---------|---------|
| Condition | Precondition | | | | | |
| | query | | | | | |
| | | PageIndex=1, PageSize=10 (repo returns 2 orders: [{OrderId=1, TB-001, PENDING, 100000}, {OrderId=2, TB-002, COMPLETED, 200000}]) | O | | | |
| | | PageIndex=1, PageSize=10, OrderStatusCode=COMPLETED (repo returns 1 filtered order: [{OrderId=3, COMPLETED, 150000}]) | | O | | |
| | | PageIndex=1, PageSize=10 (repo returns empty page, TotalCount=0) | | | O | |
| | | PageIndex=1, PageSize=5, Search="TB-001" (repo returns 1 matching order: [{OrderId=10, TB-001, 80000}]) | | | | O |

**Confirmation:**

| Col A | Col B | Col D | UTCID01 | UTCID02 | UTCID03 | UTCID04 |
|-------|-------|-------|---------|---------|---------|---------|
| Confirm | Return | | | | | |
| | | PageData.Count==2, TotalCount==2, PageData[0].OrderId==1, PageData[0].TableCode=="TB-001", PageData[0].TotalAmount==100000, PageData[0].OrderStatus=="PENDING", PageData[1].OrderId==2, PageData[1].OrderStatus=="COMPLETED" | O | | | |
| | | PageData.Count==1, TotalCount==1, PageData[0].OrderId==3, PageData[0].OrderStatus=="COMPLETED", PageData[0].TotalAmount==150000 | | O | | |
| | | PageData.Count==0, TotalCount==0, TotalPage==0 | | | O | |
| | | PageData.Count==1, PageData[0].OrderId==10, PageData[0].TableCode=="TB-001", PageData[0].TotalAmount==80000 | | | | O |

**Result:**

| Col B | UTCID01 | UTCID02 | UTCID03 | UTCID04 |
|-------|---------|---------|---------|---------|
| Type | N | N | B | N |
| Passed/Failed | P | P | P | P |
| Executed Date | *(date)* | *(date)* | *(date)* | *(date)* |

---

### Sheet: GetOrderStatusCountAsync

**Header:**

| Row | Col C | Col L |
|-----|-------|-------|
| 1 | Core/Service/OrderService.cs | GetOrderStatusCountAsync |
| 3 | Staff views a dashboard summary showing the count of orders in each status (Pending, In Progress, Completed, Cancelled) | |

**Test Case IDs:** `UTCID01` | `UTCID02` | `UTCID03`

**Condition Matrix:** *(No input parameters — each test case varies only by mock repository response)*

| Col B | Col D | UTCID01 | UTCID02 | UTCID03 |
|-------|-------|---------|---------|---------|
| (no input) | Method called with CancellationToken only | O | O | O |

**Confirmation:**

| Col B | Col D | UTCID01 | UTCID02 | UTCID03 |
|-------|-------|---------|---------|---------|
| Return | All==25, Pending==5, InProgress==8, Completed==10, Cancelled==2 | O | | |
| Return | All==0, Pending==0, InProgress==0, Completed==0, Cancelled==0 | | O | |
| Return | Any valid counts (confirms all four status IDs 100, 101, 102, 103 are resolved correctly) | | | O |

**Result:**

| Type | N | B | N |
| Passed/Failed | P | P | P |

---

### Sheet: GetKitchenOrdersAsync

**Header:**

| Row | Col C | Col L |
|-----|-------|-------|
| 1 | Core/Service/OrderService.cs | GetKitchenOrdersAsync |
| 3 | Kitchen staff views the list of active orders with their items, statuses, and table codes to manage food preparation | |

**Test Case IDs:** `UTCID01` | `UTCID02` | `UTCID03`

**Condition Matrix:** *(No input parameters — each test case varies only by mock repository response)*

| Col B | Col D | UTCID01 | UTCID02 | UTCID03 |
|-------|-------|---------|---------|---------|
| (no input) | Method called with CancellationToken only | O | O | O |

**Confirmation:**

| Col B | Col D | UTCID01 | UTCID02 | UTCID03 |
|-------|-------|---------|---------|---------|
| Return | Count==2, [0].OrderId==1, [0].TableCode=="TB-001", [0].OrderStatus=="PENDING", [0].Items.Count==1, [0].Items[0].DishName=="Pho Bo", [1].OrderId==2, [1].OrderStatus=="IN_PROGRESS" | O | | |
| Return | Count==0 (empty list) | | O | |
| Return | Any valid list (confirms all four status IDs 100, 101, 102, 103 are resolved and passed to repository) | | | O |

**Result:**

| Type | N | B | N |
| Passed/Failed | P | P | P |

---

### Sheet: UpdateOrderStatusAsync

**Header:**

| Row | Col C | Col L |
|-----|-------|-------|
| 1 | Core/Service/OrderService.cs | UpdateOrderStatusAsync |
| 3 | Staff transitions an order from one status to another following allowed transition rules, updating the table status accordingly | |
| 4 | Passed: 12 | Failed: 0 | Untested: 0 | N: 5 / A: 4 / B: 3 |

**Test Case IDs:** `UTCID01` – `UTCID12`

**Condition Matrix:**

| Col B | Col D | 01 | 02 | 03 | 04 | 05 | 06 | 07 | 08 | 09 | 10 | 11 | 12 |
|-------|-------|----|----|----|----|----|----|----|----|----|----|----|----|
| orderId | 1 (StatusLvId=PENDING, TableId=10, Payments=[]) | O | | | | | | | | | | | |
| | 2 (StatusLvId=PENDING, TableId=10, Payments=[]) | | O | | | | | | | | | | |
| | 3 (StatusLvId=IN_PROGRESS, TableId=10, Payments=[]) | | | O | | | | | | | | | |
| | 4 (StatusLvId=CANCELLED, TableId=10, Payments=[]) | | | | O | | | | | | | | |
| | 5 (StatusLvId=COMPLETED, TableId=null, Payments=[]) | | | | | O | | | | | | | |
| | 999 (not found — repo returns null) | | | | | | O | | | | | | |
| | 6 (StatusLvId=PENDING, TableId=null, Payments=[]) | | | | | | | O | | | | | |
| | 7 (StatusLvId=PENDING, TableId=null, Payments=[]) | | | | | | | | O | | | | |
| | 8 (StatusLvId=PENDING, TableId=10, Payments=[{PaymentId=1}]) | | | | | | | | | O | | | |
| | 11 (StatusLvId=PENDING, TableId=null, Payments=[]) | | | | | | | | | | O | | |
| | -1 (not found — repo returns null) | | | | | | | | | | | O | |
| | long.MaxValue (not found — repo returns null) | | | | | | | | | | | | O |
| newStatus | IN_PROGRESS | O | | | | O | O | O | | | | O | |
| | CANCELLED | | O | | | | | | O | O | O | | O |
| | COMPLETED | | | O | | | | | | | | | |
| | PENDING | | | | O | | | | | | | | |

**Confirmation:**

| Col B | Col D | 01 | 02 | 03 | 04 | 05 | 06 | 07 | 08 | 09 | 10 | 11 | 12 |
|-------|-------|----|----|----|----|----|----|----|----|----|----|----|----|
| Return | No return value (void method executes successfully) | O | O | O | O | O | | O | O | | O | | |
| Exception | NotFoundException("Order not found.") | | | | | | O | | | | | O | O |
| Exception | InvalidOperationException("Order is already in this status.") | | | | | | | O | | | | | |
| Exception | InvalidOperationException("Invalid status transition: PENDING -> COMPLETED.") | | | | | | | | O | | | | |
| Exception | InvalidOperationException("Cannot cancel a paid order.") | | | | | | | | | O | | | |

**Abnormal cases summary:**
- **UTCID06, 11, 12**: orderId does not exist (999, -1, long.MaxValue) → `NotFoundException`
- **UTCID07**: orderId=6, newStatus=IN_PROGRESS but order is already PENDING→IN_PROGRESS same status → `InvalidOperationException`
- **UTCID08**: orderId=7, PENDING→COMPLETED is not an allowed transition → `InvalidOperationException`
- **UTCID09**: orderId=8, cancel a paid order (Payments not empty) → `InvalidOperationException`

**Result:**

| Type | N | N | N | N | N | A | A | A | A | N | B | B |
| Passed/Failed | P | P | P | P | P | P | P | P | P | P | P | P |

---

### Sheet: CancelOrderItemAsync

**Header:**

| Row | Col C | Col L |
|-----|-------|-------|
| 1 | Core/Service/OrderService.cs | CancelOrderItemAsync |
| 3 | Staff cancels a specific order item, updating its status to CANCELLED and sending a notification to the kitchen with dish and table details | |

**Test Case IDs:** `UTCID01` | `UTCID02` | `UTCID03`

**Condition Matrix:**

| Col B | Col D | UTCID01 | UTCID02 | UTCID03 |
|-------|-------|---------|---------|---------|
| orderItemId | 50 (Dish="Pho Bo", Table="TB-001") | O | | |
| | 51 (Dish="Pho Bo", Table="TB-001") | | O | |
| | 52 (Dish=null, Table=null — item has no dish or table reference) | | | O |

**Confirmation:**

| Col B | Col D | UTCID01 | UTCID02 | UTCID03 |
|-------|-------|---------|---------|---------|
| Return | No return value (void method executes successfully) | O | O | O |

**Result:**

| Type | N | N | B |
| Passed/Failed | P | P | P |

---

### Sheet: GetOrderByIdAsync

**Header:**

| Row | Col C | Col L |
|-----|-------|-------|
| 1 | Core/Service/OrderService.cs | GetOrderByIdAsync |
| 3 | Staff retrieves the full detail of a specific order including items, promotions, coupons, and payments by providing the order ID | |

**Test Case IDs:** `UTCID01` | `UTCID02` | `UTCID03`

**Condition Matrix:**

| Col B | Col D | UTCID01 | UTCID02 | UTCID03 | UTCID04 | UTCID05 |
|-------|-------|---------|---------|---------|---------|---------|
| orderId | 1 (repo returns OrderDetailDTO: TableCode="TB-001", StaffName="Staff A", TotalAmount=150000, OrderStatus="PENDING", Source="DINE_IN") | O | | | | |
| | 42 (repo returns OrderDetailDTO: OrderId=42) | | O | | | |
| | 999 (repo returns null — order not found) | | | O | | |
| | long.MaxValue (repo returns null — boundary id) | | | | O | |
| | 500 (repository throws InvalidOperationException("Repository failure")) | | | | | O |

**Confirmation:**

| Col B | Col D | UTCID01 | UTCID02 | UTCID03 | UTCID04 | UTCID05 |
|-------|-------|---------|---------|---------|---------|---------|
| Return | OrderId==1, TableCode=="TB-001", StaffName=="Staff A", TotalAmount==150000, OrderStatus=="PENDING", Source=="DINE_IN" | O | | | | |
| Return | OrderId==42 | | O | | | |
| Return | null (service returns null when repo returns null) | | | O | O | |
| Exception | InvalidOperationException("Repository failure") | | | | | O |

**Abnormal cases summary:**
- **UTCID05**: orderId=500, repository throws failure while loading order detail → `InvalidOperationException("Repository failure")`

**Result:**

| Type | N | N | B | B | A |
| Passed/Failed | P | P | P | P | P |

---

### Sheet: CreateOrderAsync_Staff

**Header:**

| Row | Col C | Col L |
|-----|-------|-------|
| 1 | Core/Service/OrderService.cs | CreateOrderAsync (Staff) |
| 3 | Staff creates a new order by selecting items, table (for dine-in), and source, resolving the customer and calculating item totals and tax | |

**Test Case IDs:** `UTCID01` – `UTCID08`

**Condition Matrix:**

| Col B | Col D | 01 | 02 | 03 | 04 | 05 | 06 | 07 | 08 |
|-------|-------|----|----|----|----|----|----|----|
| source | DINE_IN | O | | | O | | O | O | O |
| | TAKEAWAY | | O | | | O | | | |
| tableId | 10 (table found, AVAILABLE status) | O | | | | | | | |
| | null (no table) | | O | O | | | | | |
| | null (DINE_IN requires table — missing) | | | | O | | | | |
| | 10 (TAKEAWAY should not have table) | | | | | O | | | |
| | 10 (table not found, repo returns null) | | | | | | O | | |
| | 10 (table found, AVAILABLE status) | | | | | | | O | |
| | 10 (table found, LOCKED status — TableStatusLvId=203) | | | | | | | | O |
| items | 2 items: [{DishId=1, Qty=2}, {DishId=2, Qty=1}] (all dishes found: Pho Bo 50000, Bun Cha 45000) | O | | | | | | | |
| | 1 item: [{DishId=1, Qty=3}] (dish found: Pho Bo 50000) | | O | | | | | | |
| | empty list: [] (violates "at least one item" rule) | | | O | | | | | |
| | 1 item: [{DishId=1, Qty=1}] | | | | O | O | O | | O |
| | 2 items: [{DishId=1, Qty=1}, {DishId=999, Qty=1}] (DishId=999 not found — partial match) | | | | | | | O | |
| | 1 item: [{DishId=1, Qty=1}] (dish found: Pho Bo 50000) | | | | | | | | O |

**Confirmation:**

| Col B | Col D | 01 | 02 | 03 | 04 | 05 | 06 | 07 | 08 |
|-------|-------|----|----|----|----|----|----|----|
| Return | OrderId==1001, Table.TableStatusLvId→OccupiedTableStatusId(201), StaffId==5, CustomerId==100, TableId==10, TotalAmount==145000, SourceLvId==DineInSourceId(400), OrderStatusLvId==PendingStatusId(100), OrderItems.Count==2 | O | | | | | | | |
| Return | OrderId==1002, TableId==null, SourceLvId==TakeawaySourceId(401), TotalAmount==150000 (3×50000) | | O | | | | | | |
| Exception | InvalidOperationException("Order must contain at least one item.") | | | O | | | | | |
| Exception | InvalidOperationException("DINE_IN order requires table.") | | | | O | | | | |
| Exception | InvalidOperationException("TAKEAWAY cannot have table.") | | | | | O | | | |
| Exception | NotFoundException("Table not found.") | | | | | | O | | |
| Exception | NotFoundException("One or more dishes not found.") — requested [1,999], only Dish{1} returned | | | | | | | O | |
| Exception | InvalidOperationException("Table is not available.") — table LOCKED(203) | | | | | | | | O |

**Abnormal cases summary:**
- **UTCID03**: empty items list → `InvalidOperationException("Order must contain at least one item.")`
- **UTCID04**: DINE_IN source but tableId=null → `InvalidOperationException("DINE_IN order requires table.")`
- **UTCID05**: TAKEAWAY source but tableId provided → `InvalidOperationException("TAKEAWAY cannot have table.")`
- **UTCID06**: tableId=10 but table not found in DB → `NotFoundException("Table not found.")`
- **UTCID07**: DishId=999 does not exist → `NotFoundException("One or more dishes not found.")`
- **UTCID08**: table is LOCKED (maintenance) → `InvalidOperationException("Table is not available.")`

**Result:**

| Type | N | N | A | A | A | A | A | N |
| Passed/Failed | P | P | P | P | P | P | P | P |

---

### Sheet: AddItemsAsync

**Header:**

| Row | Col C | Col L |
|-----|-------|-------|
| 1 | Core/Service/OrderService.cs | AddItemsAsync |
| 3 | Staff adds more items to an existing active order, updating the total amount and optionally resolving a new customer | |

**Test Case IDs:** `UTCID01` – `UTCID07`

**Condition Matrix:**

| Col B | Col D | 01 | 02 | 03 | 04 | 05 | 06 | 07 |
|-------|-------|----|----|----|----|----|----|----|
| orderId | 20 (order exists, PENDING status, TotalAmount=100000, Payments=[]) | O | | | | | | |
| | 999 (order not found — repo returns null) | | O | | | | | |
| | 21 (order exists, CANCELLED status — StatusLvId=103) | | | O | | | | |
| | 22 (order exists, PENDING status, Payments=[{PaymentId=1}] — already paid) | | | | O | | | |
| | 23 (order exists, IN_PROGRESS status, Payments=[]) | | | | | O | | |
| | 24 (order exists, COMPLETED status — StatusLvId=102, Payments=[]) | | | | | | O | |
| | 25 (order exists, PENDING status, CustomerId=100, Payments=[]) | | | | | | | O |
| items | [{DishId=3, Qty=2}] (dish found: Com Tam 40000) | O | | | | | | |
| | (any — method throws before item processing) | | O | O | O | | | |
| | [{DishId=1, Qty=1}, {DishId=888, Qty=1}] (DishId=888 not found — partial match) | | | | | O | | |
| | [{DishId=1, Qty=1}] (dish found: Pho Bo 50000) | | | | | | O | |
| | [] (no item added; used for customer-only update path) | | | | | | | O |
| customer | null | O | O | O | O | O | O | |
| | {Phone="0909999999", FullName="New Customer"} (ResolveCustomerAsync returns 200) | | | | | | | O |

**Confirmation:**

| Col B | Col D | 01 | 02 | 03 | 04 | 05 | 06 | 07 |
|-------|-------|----|----|----|----|----|----|----|
| Return | No return value (void method executes successfully) | O | | | | | O | O |
| Return | TotalAmount==180000, SubTotalAmount==180000, OrderItems.Count==1, AddedItem(DishId=3, Quantity=2, Price=40000) | O | | | | | | |
| Return | For COMPLETED order: status transitions to IN_PROGRESS and TotalAmount==100000 | | | | | | O | |
| Return | CustomerId changes from 100 to 200 when customer info is provided | | | | | | | O |
| Exception | NotFoundException("Order not found.") | | O | | | | | |
| Exception | InvalidOperationException("Cannot add items to canceled order.") | | | O | | | | |
| Exception | InvalidOperationException("Cannot add items to paid order.") | | | | O | | | |
| Exception | NotFoundException("One or more dishes not found.") | | | | | O | | |

**Abnormal cases summary:**
- **UTCID02**: orderId=999, order does not exist → `NotFoundException`
- **UTCID03**: orderId=21, order is CANCELLED → `InvalidOperationException`
- **UTCID04**: orderId=22, order has payments (already paid) → `InvalidOperationException`
- **UTCID05**: DishId=888 does not exist in DB → `NotFoundException`

**Business rule note:**
- **UTCID06** is intentionally **Normal**: when an order is `COMPLETED`, adding items is allowed and the method reopens it by transitioning status to `IN_PROGRESS`.

**Result:**

| Type | N | A | A | A | A | N | N |
| Passed/Failed | P | P | P | P | P | P | P |

---

### Sheet: CreateOrderAsync_Customer

**Header:**

| Row | Col C | Col L |
|-----|-------|-------|
| 1 | Core/Service/OrderService.cs | CreateOrderAsync (Customer) |
| 3 | Customer creates a new order by scanning a table QR code, selecting menu items, and submitting from the customer-facing menu interface | |

**Test Case IDs:** `UTCID01` – `UTCID07`

**Condition Matrix:**

| Col B | Col D | 01 | 02 | 03 | 04 | 05 | 06 | 07 |
|-------|-------|----|----|----|----|----|----|----|
| precondition | OrderService initialized; repositories available; customer context resolved; no cancellation requested (Current customer context for this suite = CustomerId 68) | O | O | O | O | O | O | O |
| tableCode | "TB-001" (table found, AVAILABLE, QrToken="valid-qr-token") | O | | | | | | |
| | "TB-001" (table found, OCCUPIED, activeOrder: {OrderId=500, StatusLvId=100, CustomerId=68, TotalAmount=100000}) | | O | | | | | |
| | "INVALID" (table not found — repo returns null) | | | O | | | | |
| | "TB-001" (table found, AVAILABLE, QrToken="valid-qr-token") | | | | O | | | |
| | "TB-001" (table found, LOCKED — TableStatusLvId=203) | | | | | O | | |
| | "TB-001" (table found, AVAILABLE, IsDeleted=true) | | | | | | O | |
| | "TB-001" (table found, RESERVED — TableStatusLvId=202) | | | | | | | O |
| qrToken | "valid-qr-token" | O | O | | | | | |
| | "valid-qr-token" | | | | | O | O | O |
| | "wrong-token" (does not match stored QrToken) | | | | O | | | |
| items | 2 items: [{DishId=1, Qty=2, Price=50000}, {DishId=2, Qty=1, Price=45000}] | O | | | | | | |
| | 1 item: [{DishId=1, Qty=1, Price=50000}] | | O | O | O | O | O | O |

**Confirmation:**

| Col B | Col D | 01 | 02 | 03 | 04 | 05 | 06 | 07 |
|-------|-------|----|----|----|----|----|----|----|
| Return | OrderId==2001, TableId==10, TableCode=="TB-001", CustomerId==68, TotalAmount==145000 (2×50000+1×45000), OrderStatus=="PENDING", Table.TableStatusLvId→OccupiedStatusId(201) | O | | | | | | |
| Return | OrderId==500, TableId==10, TableCode=="TB-001", CustomerId==68, TotalAmount==100000, OrderStatus=="PENDING" (existing active order is reused; no new order is created) | | O | | | | | |
| Exception | KeyNotFoundException("Table 'INVALID' not found.") | | | O | | | | |
| Exception | ValidationException("Invalid QR token. Please scan the QR code on the table again.") | | | | O | | | |
| Exception | InvalidOperationException("Table 'TB-001' is under maintenance and cannot be used.") | | | | | O | | |
| Exception | InvalidOperationException("Table 'TB-001' is no longer available.") | | | | | | O | |
| Return | OrderId==2002, Table.TableStatusLvId→OccupiedStatusId(201); table was RESERVED(202)→OCCUPIED(201) | | | | | | | O |

**Abnormal cases summary:**
- **UTCID03**: tableCode="INVALID", table does not exist → `KeyNotFoundException`
- **UTCID04**: qrToken="wrong-token" does not match stored token → `ValidationException`
- **UTCID05**: table is LOCKED (under maintenance), qrToken is valid to isolate table-state validation → `InvalidOperationException`
- **UTCID06**: table IsDeleted=true, qrToken is valid to isolate table-state validation → `InvalidOperationException`

**Business rule assumptions (verify with implementation):**
- **UTCID07** remains **Normal** only if RESERVED table is allowed for customer QR ordering and can transition to OCCUPIED.
- **UTCID01** `CustomerId==68` assumes `_customerService.GetGuestCustomerIdAsync(...)` resolves to 68 in the test setup.

**Result:**

| Type | N | N | A | A | A | A | N |
| Passed/Failed | P | P | P | P | P | P | P |

---

### Sheet: AddItemsToOrderAsync

**Header:**

| Row | Col C | Col L |
|-----|-------|-------|
| 1 | Core/Service/OrderService.cs | AddItemsToOrderAsync |
| 3 | Customer adds more items to their existing order on the same table through the customer-facing menu, with notification sent to kitchen staff | |

**Test Case IDs:** `UTCID01` – `UTCID05`

**Condition Matrix:**

| Col B | Col D | UTCID01 | UTCID02 | UTCID03 | UTCID04 | UTCID05 |
|-------|-------|---------|---------|---------|---------|---------|
| precondition | OrderService initialized; repositories available; customer context resolved; notification service available; no cancellation requested | O | O | O | O | O |
| orderId | 30 (order exists, PENDING status, TableId=10, TotalAmount=100000, Payments=[]) | O | | | | |
| | 999 (order not found — repo returns null) | | O | | | |
| | 31 (order exists, Payments=[{PaymentId=1}] — already paid) | | | O | | |
| | 32 (order exists, PENDING status, TableId=10, TotalAmount=100000, Payments=[]) | | | | O | |
| | 33 (order exists, PENDING status, TableId=10, TotalAmount=100000, Payments=[]) | | | | | O |
| items | [{DishId=1, Qty=2, Price=50000, Note="Extra spicy"}] (Dish{1,"Pho Bo"} found) | O | | | | |
| | (any — method throws before item processing) | | O | O | | |
| | [{DishId=1, Qty=1, Price=50000}, {DishId=2, Qty=1, Price=45000}] (all dishes found: Pho Bo, Bun Cha) | | | | O | |
| | [{DishId=1, Qty=1, Price=50000}, {DishId=999, Qty=1, Price=45000}] (DishId=999 not found — partial match) | | | | | O |

**Confirmation:**

| Col B | Col D | UTCID01 | UTCID02 | UTCID03 | UTCID04 | UTCID05 |
|-------|-------|---------|---------|---------|---------|---------|
| Return | No return value (void method executes successfully) | O | | | O | |
| Return | AddItemsToOrderAsync is called once with AddedItem(DishId=1, Qty=2, Price=50000, Note="Extra spicy", ItemStatusLvId=CreatedItemStatusId) | O | | | | |
| Return | Kitchen notification is sent once with Type="ORDER_ITEMS_ADDED" | O | | | | |
| Return | Kitchen notification metadata contains tableCode="TB-001", dishNames includes "Pho Bo" and "Bun Cha", itemCount=="2" | | | | O | |
| Exception | KeyNotFoundException("Order 999 not found.") | | O | | | |
| Exception | InvalidOperationException("This order has already been paid. Please ask staff to create a new order for your table.") | | | O | | |
| Exception | NotFoundException("One or more dishes not found.") | | | | | O |

**Abnormal cases summary:**
- **UTCID02**: orderId=999, order does not exist → `KeyNotFoundException`
- **UTCID03**: orderId=31, order already paid (Payments not empty) → `InvalidOperationException`
- **UTCID05**: items include DishId=999 but repository returns only DishId=1 → `NotFoundException`

**Business rule note:**
- **UTCID04** focuses on notification metadata correctness (dish names/table code/item count). The service delegates amount recalculation to repository/tax flow after adding items.

**Result:**

| Type | N | A | A | N | A |
| Passed/Failed | P | P | P | P | P |

---

### Sheet: GetRecentOrdersAsync

**Header:**

| Row | Col C | Col L |
|-----|-------|-------|
| 1 | Core/Service/OrderService.cs | GetRecentOrdersAsync |
| 3 | Staff views the most recent orders on the dashboard, with the limit automatically capped between 1 and 100 (defaults to 20 if out of range) | |

**Test Case IDs:** `UTCID01` – `UTCID07`

**Condition Matrix:**

| Col B | Col D | 01 | 02 | 03 | 04 | 05 | 06 | 07 |
|-------|-------|----|----|----|----|----|----|
| userId | 5 | O | O | O | O | O | O | O |
| roles | ["Admin"] | O | O | O | O | O | O | |
| | [] (empty) | | | | | | | O |
| limit | 10 (repo returns 2 orders: [{OrderId=1, "Nguyen Van A", DINE_IN, TB-001, PENDING}, {OrderId=2, "Guest", TAKEAWAY, null, COMPLETED}]) | O | | | | | | |
| | 0 (auto-corrected to 20, repo returns empty) | | O | | | | | |
| | -5 (auto-corrected to 20, repo returns empty) | | | O | | | | |
| | 150 (auto-corrected to 20, repo returns empty) | | | | O | | | |
| | 100 (exact boundary, repo returns empty) | | | | | O | | |
| | 1 (minimum valid, repo returns [{OrderId=1}]) | | | | | | O | |
| | 10 (repo returns empty) | | | | | | | O |

**Confirmation:**

| Col B | Col D | 01 | 02 | 03 | 04 | 05 | 06 | 07 |
|-------|-------|----|----|----|----|----|----|
| Return | Count==2, [0].OrderId==1, [0].CustomerName=="Nguyen Van A", [0].Source=="DINE_IN", [0].TableCode=="TB-001", [0].Status=="PENDING", [1].OrderId==2, [1].CustomerName=="Guest", [1].Source=="TAKEAWAY", [1].TableCode==null, [1].Status=="COMPLETED" | O | | | | | | |
| Return | Count==0 (empty — limit auto-corrected from 0 to 20) | | O | | | | | |
| Return | Count==0 (empty — limit auto-corrected from -5 to 20) | | | O | | | | |
| Return | Count==0 (empty — limit auto-corrected from 150 to 20) | | | | O | | | |
| Return | Count==0 (empty — limit=100 exact boundary, no auto-correction) | | | | | O | | |
| Return | Count==1, [0].OrderId==1 | | | | | | O | |
| Return | Count==0 (empty — roles=[], limit=10 passed through unchanged) | | | | | | | O |

**Result:**

| Type | N | B | B | B | B | B | N |
| Passed/Failed | P | P | P | P | P | P | P |

---

## 5. Test Case ↔ C# Method Mapping

| UTCID | Sheet | C# Test Method | Type |
|-------|-------|---------------|------|
| UTCID01 | GetOrderHistoryAsync | `GetOrderHistoryAsync_WhenValidQuery_ReturnsPagedResult` | N |
| UTCID02 | GetOrderHistoryAsync | `GetOrderHistoryAsync_WhenFilteredByStatus_ReturnsFilteredResult` | N |
| UTCID03 | GetOrderHistoryAsync | `GetOrderHistoryAsync_WhenNoOrders_ReturnsEmptyPage` | B |
| UTCID04 | GetOrderHistoryAsync | `GetOrderHistoryAsync_WhenSearchByKeyword_DelegatesToRepo` | N |
| UTCID01 | GetOrderStatusCountAsync | `GetOrderStatusCountAsync_WhenCalled_ReturnsCorrectCounts` | N |
| UTCID02 | GetOrderStatusCountAsync | `GetOrderStatusCountAsync_WhenAllZero_ReturnsZeroCounts` | B |
| UTCID03 | GetOrderStatusCountAsync | `GetOrderStatusCountAsync_ResolvesAllFourStatusIds` | N |
| UTCID01 | GetKitchenOrdersAsync | `GetKitchenOrdersAsync_WhenCalled_ReturnsKitchenOrders` | N |
| UTCID02 | GetKitchenOrdersAsync | `GetKitchenOrdersAsync_WhenNoActiveOrders_ReturnsEmptyList` | B |
| UTCID03 | GetKitchenOrdersAsync | `GetKitchenOrdersAsync_ResolvesAllFourStatusIds` | N |
| UTCID01 | UpdateOrderStatusAsync | `UpdateOrderStatusAsync_PendingToInProgress_UpdatesStatus` | N |
| UTCID02 | UpdateOrderStatusAsync | `UpdateOrderStatusAsync_PendingToCancelled_SetsTableAvailable` | N |
| UTCID03 | UpdateOrderStatusAsync | `UpdateOrderStatusAsync_InProgressToCompleted_SetsTableAvailable` | N |
| UTCID04 | UpdateOrderStatusAsync | `UpdateOrderStatusAsync_CancelledToPending_SetsTableOccupied` | N |
| UTCID05 | UpdateOrderStatusAsync | `UpdateOrderStatusAsync_CompletedToInProgress_AllowedTransition` | N |
| UTCID06 | UpdateOrderStatusAsync | `UpdateOrderStatusAsync_WhenOrderNotFound_ThrowsNotFoundException` | A |
| UTCID07 | UpdateOrderStatusAsync | `UpdateOrderStatusAsync_WhenSameStatus_ThrowsInvalidOperationException` | A |
| UTCID08 | UpdateOrderStatusAsync | `UpdateOrderStatusAsync_InvalidTransition_PendingToCompleted_ThrowsInvalidOperationException` | A |
| UTCID09 | UpdateOrderStatusAsync | `UpdateOrderStatusAsync_CancelPaidOrder_ThrowsInvalidOperationException` | A |
| UTCID10 | UpdateOrderStatusAsync | `UpdateOrderStatusAsync_WhenCancelled_SendsNotification` | N |
| UTCID11 | UpdateOrderStatusAsync | `UpdateOrderStatusAsync_WhenOrderIdNegative_ThrowsNotFoundException` | B |
| UTCID12 | UpdateOrderStatusAsync | `UpdateOrderStatusAsync_WhenOrderIdMaxValue_ThrowsNotFoundException` | B |
| UTCID01 | CancelOrderItemAsync | `CancelOrderItemAsync_WhenValidItem_CancelsAndNotifies` | N |
| UTCID02 | CancelOrderItemAsync | `CancelOrderItemAsync_NotificationContainsDishAndTableInfo` | N |
| UTCID03 | CancelOrderItemAsync | `CancelOrderItemAsync_WhenItemHasNoDish_HandlesGracefully` | B |
| UTCID01 | GetOrderByIdAsync | `GetOrderByIdAsync_WhenOrderExists_ReturnsDetail` | N |
| UTCID02 | GetOrderByIdAsync | `GetOrderByIdAsync_DelegatesToRepository` | N |
| UTCID03 | GetOrderByIdAsync | `GetOrderByIdAsync_WhenRepoReturnsNull_ReturnsNull` | B |
| UTCID04 | GetOrderByIdAsync | `GetOrderByIdAsync_WhenOrderIdIsMaxValue_ReturnsNull` | B |
| UTCID05 | GetOrderByIdAsync | `GetOrderByIdAsync_WhenRepositoryThrows_PropagatesException` | A |
| UTCID01 | CreateOrderAsync_Staff | `CreateOrderAsync_Staff_DineIn_CreatesOrderWithTable` | N |
| UTCID02 | CreateOrderAsync_Staff | `CreateOrderAsync_Staff_Takeaway_CreatesOrderWithoutTable` | N |
| UTCID03 | CreateOrderAsync_Staff | `CreateOrderAsync_Staff_EmptyItems_ThrowsInvalidOperationException` | A |
| UTCID04 | CreateOrderAsync_Staff | `CreateOrderAsync_Staff_DineInWithoutTable_ThrowsInvalidOperationException` | A |
| UTCID05 | CreateOrderAsync_Staff | `CreateOrderAsync_Staff_TakeawayWithTable_ThrowsInvalidOperationException` | A |
| UTCID06 | CreateOrderAsync_Staff | `CreateOrderAsync_Staff_TableNotFound_ThrowsNotFoundException` | A |
| UTCID07 | CreateOrderAsync_Staff | `CreateOrderAsync_Staff_TableOccupied_ThrowsInvalidOperationException` | A |
| UTCID08 | CreateOrderAsync_Staff | `CreateOrderAsync_Staff_DishNotFound_ThrowsNotFoundException` | A |
| UTCID01 | AddItemsAsync | `AddItemsAsync_WhenValidOrder_AddsItemsAndUpdatesTotal` | N |
| UTCID02 | AddItemsAsync | `AddItemsAsync_WhenOrderNotFound_ThrowsNotFoundException` | A |
| UTCID03 | AddItemsAsync | `AddItemsAsync_WhenOrderCancelled_ThrowsInvalidOperationException` | A |
| UTCID04 | AddItemsAsync | `AddItemsAsync_WhenOrderPaid_ThrowsInvalidOperationException` | A |
| UTCID05 | AddItemsAsync | `AddItemsAsync_WhenDishNotFound_ThrowsNotFoundException` | A |
| UTCID06 | AddItemsAsync | `AddItemsAsync_WhenCompletedOrder_TransitionsToInProgress` | N |
| UTCID07 | AddItemsAsync | `AddItemsAsync_WhenCustomerProvided_ResolvesCustomer` | N |
| UTCID01 | CreateOrderAsync_Customer | `CreateOrderAsync_Customer_WhenTableAvailable_CreatesNewOrder` | N |
| UTCID02 | CreateOrderAsync_Customer | `CreateOrderAsync_Customer_WhenTableOccupied_AddsToExistingOrder` | N |
| UTCID03 | CreateOrderAsync_Customer | `CreateOrderAsync_Customer_WhenTableNotFound_ThrowsKeyNotFoundException` | A |
| UTCID04 | CreateOrderAsync_Customer | `CreateOrderAsync_Customer_WhenInvalidQrToken_ThrowsValidationException` | A |
| UTCID05 | CreateOrderAsync_Customer | `CreateOrderAsync_Customer_WhenTableLocked_ThrowsInvalidOperationException` | A |
| UTCID06 | CreateOrderAsync_Customer | `CreateOrderAsync_Customer_WhenTableDeleted_ThrowsInvalidOperationException` | A |
| UTCID07 | CreateOrderAsync_Customer | `CreateOrderAsync_Customer_WhenTableReserved_ChangesToOccupied` | N |
| UTCID01 | AddItemsToOrderAsync | `AddItemsToOrderAsync_WhenValidOrder_AddsItemsAndNotifies` | N |
| UTCID02 | AddItemsToOrderAsync | `AddItemsToOrderAsync_WhenOrderNotFound_ThrowsKeyNotFoundException` | A |
| UTCID03 | AddItemsToOrderAsync | `AddItemsToOrderAsync_WhenOrderAlreadyPaid_ThrowsInvalidOperationException` | A |
| UTCID04 | AddItemsToOrderAsync | `AddItemsToOrderAsync_NotificationContainsDishNamesAndTableCode` | N |
| UTCID05 | AddItemsToOrderAsync | `AddItemsToOrderAsync_WhenDishNotFound_ThrowsNotFoundException` | A |
| UTCID01 | GetRecentOrdersAsync | `GetRecentOrdersAsync_WhenValidInput_ReturnsRecentOrders` | N |
| UTCID02 | GetRecentOrdersAsync | `GetRecentOrdersAsync_WhenLimitZero_DefaultsTo20` | B |
| UTCID03 | GetRecentOrdersAsync | `GetRecentOrdersAsync_WhenLimitNegative_DefaultsTo20` | B |
| UTCID04 | GetRecentOrdersAsync | `GetRecentOrdersAsync_WhenLimitExceeds100_DefaultsTo20` | B |
| UTCID05 | GetRecentOrdersAsync | `GetRecentOrdersAsync_WhenLimit100_UsesExactLimit` | B |
| UTCID06 | GetRecentOrdersAsync | `GetRecentOrdersAsync_WhenLimit1_UsesExactLimit` | B |
| UTCID07 | GetRecentOrdersAsync | `GetRecentOrdersAsync_WhenEmptyRoles_PassesEmptyList` | N |

---

*Note: The total C# test count is **69** (actual `dotnet test` output: Passed 69), while this report currently documents **64** representative cases in the Excel sheets.*
