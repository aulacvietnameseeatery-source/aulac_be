---
name: integration-testing
description: "Generate Markdown instruction files for filling the Report5.2_Integration Test.xlsx Excel template. Use when: writing integration test plans for AuLac Restaurant features, documenting API integration test cases, creating test reports for feature-level testing."
argument-hint: "Feature name, e.g., 'Authentication', 'Order Management', 'Reservation'"
---

# Integration Testing Skill — AuLac Restaurant

## What This Skill Produces

A **Markdown instruction file** at `Docs/Integration Test Instructions/{FeatureName}_Report_Instructions.md` explaining how to fill the `Report5.2_Integration Test.xlsx` Excel template for a given feature.

## When to Use

- Planning integration tests for a feature that spans multiple API endpoints
- Documenting test cases that verify interactions between controllers, services, and repositories
- Preparing the Excel test report for integration-level verification

## Procedure

### Step 1 — Identify the Feature Scope

1. Read the relevant controller(s) in `Api/Controllers/` to list all endpoints.
2. Read the service interfaces in `Core/Interface/` and implementations in `Core/Service/`.
3. List the API flows that constitute the feature (e.g., CRUD operations, multi-step workflows).
4. Identify dependencies between endpoints (e.g., "must create before updating").

### Step 2 — Design Test Cases

For each feature, design integration test cases that test:
- **Function-level groups**: Group test cases by function (API endpoint or logical operation).
- **Happy paths**: Normal request → expected response.
- **Error scenarios**: Invalid input, unauthorized access, resource not found.
- **Pre-conditions**: What state must exist before running the test (e.g., "user must be logged in", "dish must exist").
- **Step-by-step procedure**: Exact API calls or UI actions to perform.
- **Expected results**: HTTP status codes, response body structure, database state changes.

### Step 3 — Document Test Cases per Feature

Each feature becomes one sheet in the Excel file. The instructions should provide complete data for each field.

### Step 4 — Write the Markdown Instruction File

Create `Docs/Integration Test Instructions/{FeatureName}_Report_Instructions.md`.

## Excel Template Structure — Report5.2_Integration Test.xlsx

The Excel file has these sheets:

### Sheet: Cover
| Cell | Content |
|------|---------|
| B2 | `TEST REPORT DOCUMENT` |
| A4/B4 | Project Name: `AuLac Restaurant` |
| A5/B5 | Project Code: `AULAC_BE` |
| A6/B6 | Document Code: `AULAC_BE_IntegrationTest_v1.0` |
| E4 | Creator |
| E5 | Issue Date |
| E6 | Version |
| Row 10+ | Record of change table |

### Sheet: Test Cases
Row 8 is the header row (function list — index of all test cases across all features):

| Column | Header |
|--------|--------|
| B | No (auto-increment) |
| C | Function Name (e.g., `Login`, `Create Order`) |
| D | Sheet Name (must match the Excel sheet tab) |
| E | Description |
| F | Pre-Condition |

Add one row per function/API group, starting from row 9.

### Sheet: Test Statistics
Row 10 is the header row:

| Column | Header |
|--------|--------|
| B | No |
| C | Module code (Feature name) |
| D | Passed |
| E | Failed |
| F | Pending |
| G | N/A |
| H | Number of test cases |

- **Row 14**: Sub total (SUM formulas)
- **Row 16**: Test coverage = `(Passed + Failed) / Total × 100`%
- **Row 17**: Test successful coverage = `Passed / Total × 100`%

### Sheet: Per-Feature (one sheet per feature, copy from `Feature 1` template)

**Header section (rows 2-8):**

| Row | Col A | Col B | Col R |
|-----|-------|-------|-------|
| 2 | `Feature` | Feature name | `Passed` (legend color) |
| 3 | `Test requirement` | Description of what this feature tests | `Failed` (legend color) |
| 4 | `Number of TCs` | Total test case count | `Pending` (legend color) |
| 5 | `Testing Round` | `Passed` / `Failed` / `Pending` / `N/A` headers | `N/A` (legend color) |
| 6 | `Round 1` | Passed count | Failed count | Pending count | N/A count |
| 7 | `Round 2` | (same structure) |
| 8 | `Round 3` | (same structure) |

**Test case table (row 10 = header, row 11+ = data):**

| Column | Header | Description |
|--------|--------|-------------|
| A | Test Case ID | Format: `IT_{Feature}_{nn}` (e.g., `IT_AUTH_01`) |
| B | Test Case Description | Brief description of what is tested |
| C | Test Case Procedure | Step-by-step instructions to execute |
| D | Expected Results | What the correct outcome looks like |
| E | Pre-conditions | What must be true before running this test |
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

**Function grouping rows:**
- Insert a row with only Col A filled as the function group header (e.g., `Login API`, `Create Order`).
- Below it, list individual test cases for that function.

Example layout:
```
Row 11: [Function A]                          ← group header (bold, merged or just Col A)
Row 12: IT_AUTH_01 | Login with valid creds   | 1. POST /api/auth/login... | 200 OK + tokens | User exists | Pending | ...
Row 13: IT_AUTH_02 | Login with wrong password | 1. POST /api/auth/login... | 401 Unauthorized | User exists | Pending | ...
Row 14: [Function B]                          ← next group header
Row 15: IT_AUTH_03 | Refresh expired token    | ...
```

### Step 5 — Mapping API Endpoints to Test Cases

For each controller endpoint, generate test cases:

| HTTP Method | Endpoint | Test Cases to Generate |
|-------------|----------|----------------------|
| POST | `/api/{resource}` | Create with valid data, create with missing fields, create with duplicate |
| GET | `/api/{resource}` | Get all (with data), get all (empty), get with filters |
| GET | `/api/{resource}/{id}` | Get existing, get non-existent, get with invalid ID |
| PUT | `/api/{resource}/{id}` | Update valid, update non-existent, update with invalid data |
| DELETE | `/api/{resource}/{id}` | Delete existing, delete non-existent, delete referenced entity |

### Step 6 — Update Summary Sheets

After filling feature sheets, update:
1. **Test Cases sheet**: Add function entries pointing to the feature sheet.
2. **Test Statistics sheet**: Update counts per feature.
3. **Round summaries** in each feature sheet header.

## Output Format

The Markdown instruction file should contain:
1. **Feature overview**: What the feature does and which endpoints it covers.
2. **Test environment**: Required setup (server, database, auth tokens).
3. **Pre-populated test case table**: Full test data ready to paste into Excel.
4. **Filling instructions**: Step-by-step guide for each sheet.

## Constraints

- DO NOT write actual C# integration test code — this skill produces documentation only.
- Test Case IDs must follow `IT_{FEATURE}_{nn}` convention.
- Procedures must include exact API endpoint paths and HTTP methods.
- Expected results must include HTTP status codes and key response fields.
- Each test case must have clear pre-conditions.
