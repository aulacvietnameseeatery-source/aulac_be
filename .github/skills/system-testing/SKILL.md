---
name: system-testing
description: "Generate Markdown instruction files for filling the Report5.3_System Test.xlsx Excel template. Use when: writing end-to-end system test plans for AuLac Restaurant workflows, documenting scenario-based system test cases, creating test reports for workflow-level testing."
argument-hint: "Workflow name, e.g., 'Order Workflow', 'Reservation Workflow', 'Shift Management'"
---

# System Testing Skill — AuLac Restaurant

## What This Skill Produces

A **Markdown instruction file** at `Docs/System Test Instructions/{WorkflowName}_Report_Instructions.md` explaining how to fill the `Report5.3_System Test.xlsx` Excel template for a given end-to-end workflow.

## When to Use

- Planning system-level tests that verify complete business workflows end-to-end
- Documenting test scenarios that cross multiple features/modules
- Preparing the Excel test report for workflow-level system verification

## Difference from Integration Testing

| Aspect | Integration Test (Report 5.2) | System Test (Report 5.3) |
|--------|-------------------------------|--------------------------|
| Scope | Single feature, function-level | End-to-end workflow across features |
| Grouping | By **Function** within a Feature | By **Scenario** within a Workflow |
| Header label | `Feature` | `Workflow` |
| Test Case ID | `IT_{FEATURE}_{nn}` | `ST_{WORKFLOW}_{nn}` |
| Focus | API correctness per endpoint | Business workflow completeness |

## Procedure

### Step 1 — Identify the Workflow

1. Read relevant controllers in `Api/Controllers/` to understand the full API surface.
2. Map the business workflow from start to finish (e.g., "Customer places order → Kitchen receives → Staff serves → Payment collected").
3. Identify actors (Customer, Staff, Admin, Kitchen) and their interactions.
4. List all scenarios: happy path, edge cases, failure recovery.

### Step 2 — Design Test Scenarios

System test scenarios should cover:
- **Complete business flows**: Multi-step processes from user action to final state.
- **Cross-module interactions**: E.g., creating an order updates inventory, completing a payment triggers a notification.
- **Role-based access**: Same workflow tested from different user roles.
- **Error recovery**: What happens when a step fails midway.

### Step 3 — Write the Markdown Instruction File

Create `Docs/System Test Instructions/{WorkflowName}_Report_Instructions.md`.

## Excel Template Structure — Report5.3_System Test.xlsx

The Excel file has these sheets:

### Sheet: Cover
| Cell | Content |
|------|---------|
| B2 | `SYSTEM TEST REPORT DOCUMENT` |
| A4/B4 | Project Name: `AuLac Restaurant` |
| A5/B5 | Project Code: `AULAC_BE` |
| A6/B6 | Document Code: `AULAC_BE_SystemTest_v1.0` |
| E4 | Creator |
| E5 | Issue Date |
| E6 | Version |
| Row 9 | `Record of change` header |
| Row 10+ | Change log table: Effective Date / Version / Change Item / *A,D,M / Change description / Reference |

### Sheet: Test Cases
Row 8 is the header row (function list — index of all scenarios across all workflows):

| Column | Header |
|--------|--------|
| B | No (auto-increment) |
| C | Function Name (scenario or sub-workflow name) |
| D | Sheet Name (must match the Excel worksheet tab) |
| E | Description |

Add one row per scenario group, starting from row 9.

### Sheet: Test Statistics
Row 10 is the header row:

| Column | Header |
|--------|--------|
| B | No |
| C | Module code (Workflow name) |
| D | Passed |
| E | Failed |
| F | Pending |
| G | N/A |
| H | Number of test cases |

- **Row 14**: Sub total (SUM formulas)
- **Row 16**: Test coverage = `(Passed + Failed) / Total × 100`%
- **Row 17**: Test successful coverage = `Passed / Total × 100`%

### Sheet: Per-Workflow (one sheet per workflow, copy from `Workflow Name1` template)

**Header section (rows 2-8):**

| Row | Col A | Col B | Col R |
|-----|-------|-------|-------|
| 2 | `Workflow` | Workflow name | `Passed` (legend color) |
| 3 | `Test requirement` | Description of what this workflow tests | `Failed` (legend color) |
| 4 | `Number of TCs` | Total test case count | `Pending` (legend color) |
| 5 | `Testing Round` | `Passed` / `Failed` / `Pending` / `N/A` headers | `N/A` (legend color) |
| 6 | `Round 1` | Passed count | Failed count | Pending count | N/A count |
| 7 | `Round 2` | (same structure) |
| 8 | `Round 3` | (same structure) |

**Test case table (row 10 = header, row 11+ = data):**

| Column | Header | Description |
|--------|--------|-------------|
| A | Test Case ID | Format: `ST_{WORKFLOW}_{nn}` (e.g., `ST_ORDER_01`) |
| B | Test Case Description | Brief description of the scenario being tested |
| C | Test Case Procedure | Detailed step-by-step instructions (from user perspective) |
| D | Expected Results | Full expected outcome at each step |
| E | Pre-conditions | Required system state before running |
| F | Round 1 | Result: `Passed` / `Failed` / `Pending` / `N/A` |
| G | Test date | Date of Round 1 execution |
| H | Tester | Who executed Round 1 |
| I | Round 2 | Result |
| J | Test date | Date of Round 2 |
| K | Tester | Who executed Round 2 |
| L | Round 3 | Result |
| M | Test date | Date of Round 3 |
| N | Tester | Who executed Round 3 |
| O | Note | Additional observations, bug IDs |

**Scenario grouping rows:**
- Insert a row with only Col A filled as the scenario group header (e.g., `Scenario A: Place Order`, `Scenario B: Cancel Order`).
- Below it, list individual test cases for that scenario.

Example layout:
```
Row 11: [Scenario A: Customer Places Order]                ← group header
Row 12: ST_ORDER_01 | Place order with valid items         | 1. Login as customer...  | Order created, status=Pending    | Customer account exists | Pending | ...
Row 13: ST_ORDER_02 | Place order with out-of-stock item   | 1. Login as customer...  | Error: item unavailable          | Item has 0 stock        | Pending | ...
Row 14: [Scenario B: Kitchen Processes Order]              ← next scenario
Row 15: ST_ORDER_03 | Kitchen accepts pending order        | 1. Login as kitchen staff... | Order status → In Progress   | Order exists, status=Pending | Pending | ...
```

### Step 4 — Design End-to-End Scenario Flows

For each workflow, map the complete flow:

```
Actor A → Action 1 → System Response → Actor B → Action 2 → ... → Final State
```

Example for Order Workflow:
```
Customer → Browse Menu → Select Items → Place Order → [Order Created, status=Pending]
Kitchen Staff → View Pending Orders → Accept Order → [status=In Progress]
Kitchen Staff → Mark as Ready → [status=Ready]
Waiter → Serve Order → [status=Served]
Cashier → Process Payment → [status=Completed, Payment recorded]
```

Each step in this flow becomes one or more test cases.

### Step 5 — Update Summary Sheets

After filling workflow sheets, update:
1. **Test Cases sheet**: Add scenario entries pointing to the workflow sheet.
2. **Test Statistics sheet**: Update counts per workflow.
3. **Round summaries** in each workflow sheet header.

## Output Format

The Markdown instruction file should contain:
1. **Workflow overview**: Business process description with actors and steps.
2. **Test environment**: Required setup (server, database, test accounts, seed data).
3. **Pre-populated test case table**: Full scenario data ready to paste into Excel.
4. **Filling instructions**: Step-by-step guide for each sheet.
5. **Workflow diagram** (optional): Mermaid sequence diagram showing the end-to-end flow.

## Constraints

- DO NOT write actual automated test code — this skill produces documentation only.
- Test Case IDs must follow `ST_{WORKFLOW}_{nn}` convention.
- Procedures must describe full user-facing steps (not raw API calls like integration tests).
- Expected results must describe business outcomes, not just HTTP codes.
- Scenarios must span multiple actors/modules to qualify as system-level tests.
- Each test case must have clear pre-conditions describing required system state.
