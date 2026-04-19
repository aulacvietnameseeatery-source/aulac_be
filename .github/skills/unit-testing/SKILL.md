---
name: unit-testing
description: "Write xUnit unit tests for AuLac Restaurant BE services AND generate an instruction Markdown file explaining how to fill the Report5.1_Unit Test.xlsx Excel template. Use when: creating unit tests for a service method, generating unit test report instructions, testing Core/Service layer."
argument-hint: "Service and method name, e.g., 'AuthService.LoginAsync' or 'DishService.CreateDishAsync'"
---

# Unit Testing Skill — AuLac Restaurant BE

## What This Skill Produces

1. **C# unit test file** in `Tests/Services/{ServiceName}Tests.cs` using xUnit + Moq + FluentAssertions.
2. **Markdown instruction file** in `Docs/Unit Test Instructions/{MethodName}_Report_Instructions.md` explaining how to fill the `Report5.1_Unit Test.xlsx` Excel template with the generated test data.

## When to Use

- Creating new unit tests for a service method in `Core/Service/`
- Expanding test coverage for an existing service
- Generating reporting documentation for the unit test Excel template

## Prerequisites

Read the full unit test guide before writing tests: [UNIT_TEST_GUIDE.md](../../../Tests/UNIT_TEST_GUIDE.md)

## Procedure

### Step 1 — Analyze the Target Method

1. Read `Core/Service/{ServiceName}.cs` to find the target method.
2. Identify the constructor dependencies (interfaces to mock).
3. Read the interface contracts in `Core/Interface/`.
4. Read related DTOs in `Core/DTO/`.
5. Read related entities in `Core/Entity/`.
6. Identify all code paths: happy path, error handling, edge cases.

### Step 2 — Classify Test Cases

For every input parameter and precondition, classify values into three categories:

| Type | Description | Example |
|------|-------------|---------|
| **Normal (N)** | Valid, typical input — happy path | Correct username + password |
| **Boundary (B)** | Edge values at limits | String at max length, ID = 0 |
| **Abnormal (A)** | Invalid, unexpected input | null, empty string, non-existent ID |

### Step 3 — Write the Test File

Follow this structure exactly:

```csharp
using Core.DTO.{Module};
using Core.Entity;
using Core.Interface.Repo;
using Core.Interface.Service;
using Core.Service;
using Microsoft.Extensions.Logging;
using Moq;
using FluentAssertions;

namespace Tests.Services;

/// <summary>
/// Unit Test — {ServiceName}.{MethodName}
/// Code Module : Core/Service/{ServiceName}.cs
/// Method      : {MethodName}({parameter list})
/// Created By  : {Developer Name}
/// Executed By : {Tester Name}
/// Test Req.   : {Brief description of what is tested}
/// </summary>
public class {ServiceName}Tests
{
    // ── Mocks (field-level, one per dependency) ──
    private readonly Mock<IRepo> _repoMock = new();
    // ... all dependencies from constructor

    // ── Factory method ──
    private {ServiceName} CreateService() => new(
        _repoMock.Object
        // ... all mock .Object
    );

    // ── Test Data Helpers ──
    private static Entity MakeValid{Entity}() => new() { /* ... */ };

    // ══════ TEST CASES ══════

    #region {MethodName}

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "{MethodName}")]
    public async Task {MethodName}_WhenValidInput_ReturnsExpected()
    {
        // Arrange
        // Act
        // Assert
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "{MethodName}")]
    public async Task {MethodName}_WhenBoundaryValue_ReturnsExpected()
    {
        // Arrange
        // Act
        // Assert
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "{MethodName}")]
    public async Task {MethodName}_WhenInvalidInput_ReturnsError()
    {
        // Arrange
        // Act
        // Assert
    }

    #endregion
}
```

**Naming convention:** `{MethodName}_{Scenario}_{ExpectedResult}`

**Assertion rules:**
- Use FluentAssertions: `.Should().Be()`, `.Should().NotBeNull()`, etc.
- Verify mock calls with `_mock.Verify(...)` for behavior testing.

### Step 4 — Run and Validate Tests

```powershell
cd Tests
dotnet test --filter "FullyQualifiedName~{ServiceName}Tests"
```

Ensure all tests pass before proceeding.

### Step 5 — Generate Excel Report Instructions

Create a Markdown file at `Docs/Unit Test Instructions/{MethodName}_Report_Instructions.md` with the following content mapped to the Excel template structure.

## Excel Template Structure — Report5.1_Unit Test.xlsx

The Excel file has these sheets:

### Sheet: Cover
| Cell | Content |
|------|---------|
| B2 | `UNIT TEST DOCUMENT` |
| B4 | Project Name: `AuLac Restaurant` |
| B5 | Project Code: `AULAC_BE` |
| B6 | Document Code: `AULAC_BE_UnitTest_v1.0` |
| E4 | Creator name |
| E5 | Issue Date |
| E6 | Version |

### Sheet: MethodList
Row 8 is the header row:

| Column | Header |
|--------|--------|
| A | No (auto-increment) |
| B | Module Name (e.g., `AUTHENTICATION`, `DISH`, `ORDER`) |
| C | Method Name (e.g., `LoginAsync`) |
| D | Sheet Name (must match the sheet tab name for this method) |
| E | Description — **must be an English natural-language use case** describing who performs the action and what they accomplish (e.g., "Staff creates a new order by selecting items, table, and source, resolving the customer and calculating tax"). Do NOT use technical summaries like "Returns paged list" or Vietnamese text. |
| F | Pre-Condition |

Add one row per tested method, starting from row 9.

### Sheet: Statistics
Row 11 is the header row:

| Column | Header |
|--------|--------|
| A | No |
| B | Function code (method name or sheet name) |
| C | Passed count |
| D | Failed count |
| E | Untested count |
| F | N (Normal test case count) |
| G | A (Abnormal test case count) |
| H | B (Boundary test case count) |
| I | Total Test Cases |

- **Row 16**: Sub total (SUM formulas)
- **Row 18**: Test coverage = `(Passed + Failed) / Total × 100`%
- **Row 19**: Test successful coverage = `Passed / Total × 100`%
- **Row 20**: Normal case = `Normal_Passed / Normal_Total × 100`%

### Sheet: Per-Method (one sheet per method, copy from `Example` template)

**Header section (rows 1-5):**

| Row | Col A | Col C | Col F | Col L |
|-----|-------|-------|-------|-------|
| 1 | `Code Module` | Module name | `Method` | Method name |
| 2 | `Created By` | Developer name | `Executed By` | Tester name |
| 3 | `Test requirement` | English natural-language use case description (merged C3:K3). Same style as the MethodList Description column — describe who performs the action and what they accomplish. | | |
| 4 | `Passed` | `Failed` | `Untested` | `N/A/B` counts |
| 5 | Passed count | Failed count | Untested count | N, A, B counts → Total |

**Test Case ID row (row 7):**
- Columns F onward: `UTCID01`, `UTCID02`, ..., `UTCID{n}`
- Each column = one test case

**Condition matrix (rows 8+):**

| Col A | Col B | Col C | Col D | Col F+ |
|-------|-------|-------|-------|--------|
| `Condition` | `Precondition` | | | |
| | {parameter name} | | | |
| | | | {input value} | `O` marks which test case uses this value |

**Condition matrix rules:**
1. **Input from parameters only** — Row labels (Col B) must be actual method parameters (e.g., `orderId`, `newStatus`, `query`, `items`). For DTO parameters, use the property names (e.g., `source`, `tableId`, `items` from a request DTO).
2. **No mock rows** — Do NOT add separate rows for mock setup data (e.g., `repoReturns`, `tableStatus`, `dishesExist`, `orderExists`, `hasPaid`). These are internal mock configurations, not method inputs.
3. **Describe mock data inline** — Append mock/internal state descriptions in parentheses after the parameter value. Examples:
   - `orderId | 1 (StatusLvId=PENDING, TableId=10, Payments=[])` — describes the mocked order state found by this ID
   - `orderId | 999 (order not found — repo returns null)` — describes the abnormal scenario
   - `tableId | 10 (table found, LOCKED status — TableStatusLvId=203)` — describes table mock state
   - `items | [{DishId=1, Qty=1}, {DishId=999, Qty=1}] (DishId=999 not found — partial match)` — describes dish lookup result
4. **No-parameter methods** — If the method has no input parameters (only `CancellationToken`), add a note: `*(No input parameters — each test case varies only by mock repository response)*` and use a single row: `(no input) | Method called with CancellationToken only | O | O | ...`

**Confirmation section (after conditions):**

| Col A | Col B | Col D | Col F+ |
|-------|-------|-------|--------|
| `Confirm` | {output group} | | |
| | `Return` | | |
| | | {expected value} | `O` marks which test case expects this |
| | `Exception` | | |

**Confirmation section rules:**
1. **No Verify rows** — Do NOT add `Verify` rows. Side-effect assertions (repository calls, status changes, notifications) are implied by the inline parameter descriptions in the Condition section.
2. **Return row** — For value-returning methods (`T`, `Task<T>`), fill concrete expected value(s).
3. **Void/Task Return** — For non-returning methods, use `No return value (void method executes successfully)` for success paths.
4. **Exception row** — For error paths, keep `Return` blank and fill expected exception in `Exception`.

**Return section rule by method type:**

| Method type | How to fill `Return` in Excel |
|------------|-------------------------------|
| returns value (`T`, `Task<T>`) | Fill concrete expected value(s) in `Return` |
| non-returning (`void`, `Task`) success path | `No return value (void method executes successfully)` |
| exception path | Keep `Return` blank and fill expected exception in `Exception` |

**Abnormal cases summary:**
After the Confirmation section, add an **Abnormal cases summary** block listing each abnormal test case with its UTCID, parameter scenario, and expected exception. Example:
```
**Abnormal cases summary:**
- **UTCID03**: orderId=999, order not found → `NotFoundException`
- **UTCID04**: order is CANCELLED → `InvalidOperationException`
```

**Result section (bottom rows):**

| Col A | Col B | Col F+ |
|-------|-------|--------|
| `Result` | `Type(N : Normal, A : Abnormal, B : Boundary)` | `N`, `A`, or `B` per test case |
| | `Passed/Failed` | `P` or `F` per test case |
| | `Executed Date` | Date per test case |
| | `Defect ID` | Bug ID if failed |

### Mapping: C# Test → Excel Row

For each `[Fact]` test method in the C# file:
1. **UTCID**: Assign sequential ID (`UTCID01`, `UTCID02`, ...).
2. **Condition rows**: For each input parameter, write the value used in `// Arrange` into the condition matrix with `O`.
3. **Confirmation rows**: For each `// Assert`, write the expected output.
4. **Type**: Read `[Trait("Type", "...")]` → `N`, `A`, or `B`.
5. **Result**: After running `dotnet test`, mark `P` (passed) or `F` (failed).
6. **Date**: The date tests were executed.

## Constraints

- DO NOT skip the metadata XML doc comment on the test class.
- DO NOT write tests that depend on other tests (each test must be independent).
- DO NOT use real database connections — mock all repositories.
- Every `[Fact]` MUST have `[Trait("Type", "...")]` and `[Trait("Method", "...")]`.
- Test names MUST follow `{Method}_{Scenario}_{Expected}` convention.
- Generate BOTH the C# test file AND the Markdown report instruction file.
- All Description and Test requirement text MUST be written in **English natural language** describing a use case (actor + action + context). Never use Vietnamese or terse technical summaries.
