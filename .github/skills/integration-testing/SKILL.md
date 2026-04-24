---
name: integration-testing
description: "Generate a ready-to-paste Markdown instruction file for filling the Report5.2_Integration Test.xlsx template (VRMOS / AuLac Restaurant). Use when: designing integration test cases for a feature, documenting API/UI integration flows, preparing the Excel test report sheet for a new module."
argument-hint: "<FeatureName> [--endpoints=path/to/controller] [--rounds=3] [--id-prefix=FEATURE]"
version: "3.0"
---

# Integration Testing Skill v3 — VRMOS / AuLac Restaurant

---

## 0. Output Contract (MUST follow)

Produce exactly ONE Markdown file:

`Docs/Integration Test Instructions/{FeatureName}_Report_Instructions.md`

FeatureName must be in PascalCase or with spaces matching the Excel sheet tab (e.g. `OrderManagement` → sheet tab `Order Management`).

The file MUST contain, in this order:

1. Feature overview & scope
2. Test environment & pre-conditions
3. **[NEW v3]** Code Traceability table (§2.5)
4. **[NEW v3]** Test Data Seed — placeholder definitions (§2.6)
5. Function grouping plan (CRUD + special flows)
6. **4-layer coverage matrix** (Happy / Validation / Business rule / Security)
7. **Full test case table** — ready to paste into the per-feature Excel sheet (columns A–O)
8. Header block values (rows 2–4) for the sheet
9. Updates required in `Test Cases` and `Test Statistics` sheets
10. Fill checklist
11. **[NEW v3]** Gaps Requiring Confirmation (may be empty)

Nothing else. No C# code. No prose commentary outside these sections.

**Language**: 100% English. No Vietnamese words anywhere — descriptions, procedures, expected results, pre-conditions, group headers, notes. If source requirements are in Vietnamese, translate to concise technical English before writing.

---

## 0.5 Source of Truth (Evidence-Based Rule — NO FABRICATION)

Before generating any test case, the skill MUST read source code and extract facts. The following steps are MANDATORY:

### Required Source Reads

1. **Controller files** (`Api/Controllers/`) — extract per endpoint:
   - HTTP verb + route template
   - Request DTO type and field names
   - Response DTO type and field names
   - `[HasPermission(...)]` / `[Authorize(Policy="...")]` attribute values (exact strings)
   - HTTP status codes returned by each branch (`return Ok(...)`, `return NotFound(...)`, etc.)
   - Inline error messages (exact strings)

2. **Service files** (`Core/Service/`) — extract:
   - Business rule validations (exact exception messages thrown)
   - Status transition logic (exact allowed/denied transitions)
   - DB side effects (what tables are written, what fields change)
   - SignalR calls (method name, payload object type)
   - Notification calls (event type string, payload fields, `TargetPermissions` list)

3. **DTO / Entity files** (`Core/DTO/`, `Core/Entity/`) — extract:
   - Every field with its type
   - Data annotation attributes: `[Required]`, `[Range(min, max)]`, `[MaxLength(n)]`, `[MinLength(n)]`, `[RegularExpression(...)]`

4. **Enum files** (`Core/Enum/`) — extract:
   - Exact enum member names and their string representations used in code

5. **SignalR hubs / notification services** — extract:
   - Exact event/method names broadcast to clients
   - Exact payload shape (DTO type or anonymous object fields)
   - Audience: which permission or SignalR group receives the event

6. **`appsettings.json` / resource files / `ErrorMessages.cs`** — extract:
   - Exact error message strings, or the resource key + resolved English value

### Forbidden Actions

| Forbidden | Instead |
|---|---|
| Inventing endpoints not in controllers | Read controller file first |
| Guessing error messages | Use exact string from source; if not found, tag `⚠️ UNVERIFIED` |
| Using "HTTP 400 or 404" ambiguity | Read each `return` branch in controller |
| Using "401 or 403" | Read auth middleware — 401 = no token, 403 = insufficient permission; split into separate TCs |
| Guessing permission policy names | Read `[HasPermission(...)]` attribute value literally |
| Guessing enum values | Read enum definition for exact casing |
| Hardcoding test data IDs (e.g., `orderId = 5`) | Use `{{PLACEHOLDER_NAME}}` format |
| Mixed-language output | All text must be English |

### Unverified Tag Rule

If a required fact is not found in source code after reading all relevant files, the skill MUST:
1. Place `⚠️ UNVERIFIED` inline next to the field (e.g., `"Order not found" ⚠️ UNVERIFIED`).
2. Log it in the **Gaps Requiring Confirmation** section at the end of the output file.
3. Do NOT fabricate a plausible value.

### Fabrication Halt Rule

If an endpoint, permission name, or status code cannot be confirmed from code and is critical to generating TCs, HALT and ask:
> "I cannot find [X] in the source files. Please provide [controller/service/enum] file path so I can read it before continuing."

---

## 1. Inputs

| Input | Required | Default | Notes |
|---|---|---|---|
| `FeatureName` | ✅ | — | Sheet name in Excel, e.g. `Account Management` |
| `id-prefix` | ⬜ | `UPPER(FeatureName) without spaces` | Prefix for Test Case ID, e.g. `ACCOUNT`, `ORDER`, `RESERVATION` |
| `endpoints` | ⬜ | auto-discover | Path to controller file(s); if omitted, read `Api/Controllers/` to find relevant controller |
| `rounds` | ⬜ | `3` | Number of testing rounds (matches Excel template) |

If endpoints and UI flows are both unknown → STOP and ask user for one of:
- Controller file path, or
- Swagger/OpenAPI spec, or
- Plain list of endpoints/UI actions + requirements.

---

## 2. Procedure

### Step 0 — Source Read (MANDATORY before any TC generation)

Execute the §0.5 source reads in order. Populate an internal fact sheet:
- Endpoint list with verb, route, permission, request DTO, response DTO, error branches
- Business rules from service
- DTO validation constraints from annotations
- Enum values
- SignalR / notification event names and payloads

Only proceed to Step 1 after this fact sheet is populated.

### Step 1 — Collect scope
1. List all endpoints / UI actions belonging to the feature (from fact sheet, not invented).
2. Identify CRUD operations and special flows (login, refresh, reset, process, approve, etc.).
3. Identify dependencies (e.g. must create role before assigning to account).
4. Identify RBAC: which role/permission is required for each action (exact policy name from `[HasPermission]`).

### Step 2 — Build Code Traceability Table (§2.5)

For every endpoint covered, produce one row:

| Endpoint | Controller File:Line | Service Method | Request DTO | Response DTO | Permission Policy |
|---|---|---|---|---|---|
| POST /api/... | `XController.cs:NN` | `XService.MethodAsync` | `CreateXRequest` | `XResponseDTO` | `Permissions.CreateX` or `(public)` |

This table is produced first and used as the authoritative endpoint registry throughout the rest of the document.

### Step 3 — Build Test Data Seed table (§2.6)

Define every placeholder needed in TCs:

| Placeholder | Description | How to Seed |
|---|---|---|
| `{{PENDING_ORDER_ID}}` | An order with status PENDING, no payment, linked to a table | INSERT into `orders` table OR create via POST /api/orders/staff |
| ... | ... | ... |

### Step 4 — Group functions
Default grouping order (mirrors existing sheets in the file):

For non-CRUD modules, group by **business use case** (see Authentication: Login / Token Refresh / Logout / Forgot / Verify / Reset; Payment: View/Search / Process Payment).

Each group becomes a **group header row** in column A only (no ID, no result).

### Step 5 — Generate test cases applying the **4-layer matrix + new constraints**

For EACH function, design at minimum:

| Layer | Must cover | Reference TC in file |
|---|---|---|
| **Happy path** | Valid input → success + side effects verified | `ROLES_FT01`, `TABLES_FT01`, `AUTHENTICATION_FT01` |
| **Validation** | Required / length / format / range / whitespace — one TC per DTO annotation | `ROLES_FT02`, `TABLES_FT04`, `AUTHENTICATION_FT07` |
| **Business rule** | Duplicate, conflict, invalid state transition, referential integrity | `ROLES_FT04` (409), `ROLES_FT14` (assigned staff), `TABLES_FT12` (LOCKED→?), `PAY_FT16` (already paid) |
| **Security / Permission** | 401 no-token, 403 no-permission (separate TCs) | `PAY_FT17`, `AUTHENTICATION_FT03/06/10/11` |

For **list/search/filter** functions, ALWAYS include:
- View without filter
- Filter/search WITH match
- Filter/search with NO match → empty state

For **endpoints requiring authentication**, generate the shared **5-case token block** ONCE at module level (§ "Authentication & Token Edge Cases"):

| Case | TC | Token condition |
|---|---|---|
| Valid token | FTxx | Active, non-expired staff token |
| Expired token | FTxx | JWT `exp` claim in the past |
| Invalid/malformed token | FTxx | Random string as Bearer value |
| Missing token | FTxx | No `Authorization` header at all → HTTP 401 |
| Token without permission | FTxx | Valid JWT, but role lacks the required policy → HTTP 403 |

Do NOT repeat these 5 cases per endpoint — generate once and cross-reference.

For **status-transition endpoints**, produce the **full NxN transition matrix** before writing TCs:

```
From \ To | STATE_A | STATE_B | STATE_C | ...
----------|---------|---------|---------|
STATE_A   | Same    | Allowed | Rejected|
STATE_B   | Rejected| Same    | Allowed |
...
```

Generate exactly one TC per non-trivial cell (Allowed: verify success + side effects; Rejected: verify 400 + exact error message from code).

For **public endpoints accepting numeric client input** (price, quantity, discount):
- Include a "server-side authority" TC: submit a manipulated value; verify server uses DB value, not client value.

For **public endpoints exposing resources by ID**:
- Include an IDOR TC: verify the endpoint exposes data to any anonymous caller and document the business risk.

For **endpoints emitting SignalR/notification events**, the Expected Result MUST state:
- Exact event name (from SignalR hub method or notification type string in code)
- Exact payload fields (from DTO or notification metadata object)
- Target audience (the `TargetPermissions` list or SignalR group name from code)

### Step 6 — Write test cases in the exact Excel schema (see §3)

### Step 7 — Produce summary sheet updates (see §5)

### Step 8 — Run all Quality Gates (§6). Fix any failure before emitting.

### Step 9 — Emit the Markdown file per §0 contract

---

## 3. Excel Template — exact cell schema to target

### 3.1 Per-feature sheet header (rows 2–8)

| Row | A | B | C | D | E | R (legend) |
|---|---|---|---|---|---|---|
| 2 | `Feature` | **{FeatureName}** | — | — | — | `Passed` |
| 3 | `Test requirement` | 1-sentence scope | — | — | — | `Failed` |
| 4 | `Number of TCs` | `=COUNTA(A12:A998)` | — | — | — | `Pending` |
| 5 | `Testing Round` | `Passed` | `Failed` | `Pending` | `N/A` | `N/A` |
| 6 | `Round 1` | `=COUNTIF( $F$10:$F$1000,B$5)` | `=COUNTIF( $F$10:$F$1000,C$5)` | `=COUNTIF( $F$10:$F$1000,D$5)` | `=COUNTIF( $F$10:$F$1000,E$5)` | |
| 7 | `Round 2` | `=COUNTIF( $I$10:$I$1000,B$5)` | `=COUNTIF( $I$10:$I$1000,C$5)` | `=COUNTIF( $I$10:$I$1000,D$5)` | `=COUNTIF( $I$10:$I$1000,E$5)` | |
| 8 | `Round 3` | `=COUNTIF( $L$10:$L$1000,B$5)` | `=COUNTIF( $L$10:$L$1000,C$5)` | `=COUNTIF( $L$10:$L$1000,D$5)` | `=COUNTIF( $L$10:$L$1000,E$5)` | |

> ⚠️ The original file has inconsistent COUNTIF ranges (e.g. `F12:F200` vs `$F10:$F993`). **Always normalize to `$F$10:$F$1000`** (and I/L for rounds 2/3) as above.

### 3.2 Test case table (row 10 = header, row 11+ = data)

| Col | Header | Rule |
|---|---|---|
| A | Test Case ID | `{ID_PREFIX}_FT{NN}`, NN = 2 digits, zero-padded, continuous across groups |
| B | Test Case Description | 1 action-oriented sentence, English only |
| C | Test Case Procedure | Numbered steps `1. ...\n2. ...`, English only |
| D | Expected Results | Numbered, MUST include HTTP status + **exact** message from source + side effect; no paraphrasing |
| E | Pre-conditions | Bullet list: role, data (use `{{PLACEHOLDER}}`), network, permission |
| F | Round 1 | `Pending` by default |
| G | Test date | empty |
| H | Tester | empty |
| I | Round 2 | `Pending` |
| J / K | Test date / Tester | empty |
| L | Round 3 | `Pending` |
| M / N | Test date / Tester | empty |
| O | Note | empty |

**Group header rows**: only column A filled with the function name (e.g. `Create Account`). No ID, no result.

### 3.3 ID prefix convention (aligned with existing sheets)

| Feature | Prefix used in file |
|---|---|
| Authentication | `AUTHENTICATION` |
| Roles & Permissions | `ROLES` |
| Payment | `PAY` |
| Table | `TABLES` |
| Account | `ACCOUNT` (recommended) |
| Dish | `DISH` (recommended) |
| Order | `ORDER` (recommended) |
| Reservation | `RES` (recommended) |

> Do NOT use `IT_{FEATURE}_{nn}` — the file uses `{MODULE}_FT{NN}`. Match the file.

---

## 4. Hard Constraints (ALL must hold)

1. Every TC has a unique `{PREFIX}_FT{NN}` ID, zero-padded, sequential across all groups.
2. Group header rows occupy column A only — no ID, no status, no result.
3. **Expected Results MUST quote error messages exactly from source code.** If the message comes from a resource key, cite the key and its resolved English value. No paraphrasing allowed.
4. Every happy-path TC must verify at least one DB side effect (status change, new row, updated field) beyond the HTTP response.
5. `Pending` is the default value for all Round columns (F, I, L).
6. Pre-conditions use `{{PLACEHOLDER}}` format for IDs — never hardcoded numbers.
7. No Vietnamese characters anywhere in the file.
8. No ambiguous HTTP code ranges — use exact single code only.
9. All Section headings, group names, and notes in English.
10. No invented endpoints, error messages, permissions, or enum values.
11. **401 (no token) and 403 (no permission) are SEPARATE test cases.** Never combine as "401 or 403".
12. For every module with authenticated endpoints, include exactly ONE shared block of 5 token-edge-case TCs (valid / expired / invalid / missing / no-permission). Do not repeat per endpoint.
13. For every input field defined in the request DTO, generate at least one boundary / validation TC driven by the DTO annotation (`[Required]`, `[Range]`, `[MaxLength]`, etc.). Do not skip annotated fields.
14. For every status-transition endpoint, produce the full NxN transition matrix (all states × all states) and generate one TC per non-trivial cell (Allowed and Rejected).
15. For every public endpoint that accepts a numeric field from the client (price, quantity, discount), include a "server-side authority" TC verifying the server uses its own DB/computed value, not the client-submitted value.
16. For every public endpoint that exposes a resource by ID (no auth), include an IDOR TC documenting the exposure and business risk.
17. For every endpoint that emits a SignalR event or notification, the Expected Result column MUST state: exact event name, exact payload field list, target permission/group — all traced from source code.
18. Use `{{PLACEHOLDER_NAME}}` format for all test data IDs and include a §2.6 "Test Data Seed" table with every placeholder defined and seeding instructions.

---

## 5. Output Markdown — strict template

The generated `.md` MUST use this exact structure:

````markdown
# Integration Test Instructions — {{FeatureName}}

## 1. Overview
- **Module**: {{FeatureName}}
- **Sheet name**: `{{FeatureName}}`
- **ID prefix**: `{{ID_PREFIX}}`
- **Scope**: {{1-sentence description}}
- **Endpoints / UI flows covered**:
  - {{METHOD}} {{path}} — {{purpose}}
  - ...

## 2. Test Environment
- VRMOS Server running
- Database: MySQL (seeded test data)
- Browser: Microsoft Edge / Google Chrome
- Accounts required: {{list with roles}}

## 2.5 Code Traceability

| Endpoint | Controller File:Line | Service Method | Request DTO | Response DTO | Permission Policy |
|---|---|---|---|---|---|
| {{METHOD}} {{path}} | `{{File.cs:NN}}` | `{{ServiceClass.MethodAsync}}` | `{{RequestDTO}}` | `{{ResponseDTO}}` | `{{Permissions.Xxx}}` or `(public)` |
| ... | | | | | |

## 2.6 Test Data Seed

> Substitute these placeholders in all Procedure and Pre-condition columns before executing tests.

| Placeholder | Description | Seed Instructions |
|---|---|---|
| `{{PLACEHOLDER_1}}` | {{what it represents}} | {{SQL INSERT or API call to create it}} |
| ... | | |

## 3. Sheet Header Values (paste into rows 2–4)

| Cell | Value |
|---|---|
| B2 | `{{FeatureName}}` |
| B3 | `{{test requirement sentence}}` |
| B4 | `=COUNTA(A12:A998)` |

_(Rows 5–8: keep template COUNTIF formulas — normalized to `$F$10:$F$1000`, `$I$10:$I$1000`, `$L$10:$L$1000`.)_

## 4. Function Grouping
1. {{Group 1}}
2. {{Group 2}}
...
N. Authentication & Token Edge Cases (Cross-Cutting)
N+1. Public Endpoint Security (if public endpoints exist)

## 5. 4-Layer Coverage Matrix

| Layer | Test Case IDs |
|---|---|
| Happy path | {{IDs}} |
| Validation | {{IDs}} |
| Business rule | {{IDs}} |
| Security / Permission | {{IDs}} |

## 6. Status Transition Matrix (if applicable)

> Source: `{{ServiceFile.cs}}` — `{{TransitionLogic method/lines}}`

| From \ To | {{STATE_A}} | {{STATE_B}} | {{STATE_C}} | {{STATE_D}} |
|---|---|---|---|---|
| {{STATE_A}} | Same | Allowed → FTxx | Rejected → FTxx | Allowed → FTxx |
| {{STATE_B}} | Rejected → FTxx | Same | Allowed → FTxx | Rejected → FTxx |
| ... | | | | |

## 7. Test Cases (paste into row 11+)

> Columns: A (ID) | B (Description) | C (Procedure) | D (Expected Results) | E (Pre-conditions) | F (Round 1) | I (Round 2) | L (Round 3)
> Group header rows occupy **column A only**.
> Error messages in column D are quoted exactly from source code.

### 🟦 {{Group 1}}

| A (ID) | B (Description) | C (Procedure) | D (Expected Results) | E (Pre-conditions) | F | I | L |
|---|---|---|---|---|---|---|---|
| {{PREFIX}}_FT01 | {{desc}} | 1. ...<br>2. ... | 1. HTTP {{code}}.<br>2. Response: `{{exact message from code}}`.<br>3. {{DB side effect}}.<br>4. {{SignalR event name}}: payload `{{fields}}` sent to `{{TargetPermissions}}`. | - {{role}}<br>- {{data using placeholder}} | Pending | Pending | Pending |

...

### 🟦 Authentication & Token Edge Cases (Cross-Cutting)

> Generated once. All authenticated endpoints in this module are subject to these conditions.

| A (ID) | B (Description) | C (Procedure) | D (Expected Results) | E (Pre-conditions) | F | I | L |
|---|---|---|---|---|---|---|---|
| {{PREFIX}}_FTxx | Valid token — access granted | 1. Obtain valid JWT for Staff account with all required permissions.<br>2. Call any authenticated endpoint with valid body.<br>3. Observe response. | 1. HTTP 200 (or 201).<br>2. Expected resource returned. | - Valid JWT, non-expired.<br>- Account has required permission. | Pending | Pending | Pending |
| {{PREFIX}}_FTxx | Expired JWT returns 401 | 1. Obtain an expired JWT token.<br>2. Send request with `Authorization: Bearer {expired_token}`.<br>3. Observe response. | 1. HTTP 401.<br>2. Response: `{ success: false }`. | - Expired JWT available. | Pending | Pending | Pending |
| {{PREFIX}}_FTxx | Malformed JWT returns 401 | 1. Send request with `Authorization: Bearer INVALID_STRING`.<br>2. Observe response. | 1. HTTP 401.<br>2. Response: `{ success: false }`. | - Server JWT validation enabled. | Pending | Pending | Pending |
| {{PREFIX}}_FTxx | Missing Authorization header returns 401 | 1. Send request with no `Authorization` header.<br>2. Observe response. | 1. HTTP 401.<br>2. Response: `{ success: false }`. | - No token provided. | Pending | Pending | Pending |
| {{PREFIX}}_FTxx | Valid JWT without required permission returns 403 | 1. Obtain valid JWT for an account whose role lacks the required permission.<br>2. Send request with valid body.<br>3. Observe response. | 1. HTTP 403.<br>2. Response: `{ success: false }`. | - Valid JWT.<br>- Account role lacks `{{PermissionPolicy}}` permission. | Pending | Pending | Pending |

### 🟦 Public Endpoint Security — IDOR (if applicable)

| A (ID) | B (Description) | C (Procedure) | D (Expected Results) | E (Pre-conditions) | F | I | L |
|---|---|---|---|---|---|---|---|
| {{PREFIX}}_FTxx | IDOR — public endpoint exposes resource to any caller by sequential ID | 1. No authentication required.<br>2. Send `{{METHOD}} {{/api/resource/{id}}}` with a guessable ID belonging to another user/session.<br>3. Observe response and returned data. | 1. HTTP 200 — full resource data returned with no auth check.<br>2. **IDOR risk documented**: any anonymous caller can enumerate resources by incrementing `{id}`. Business owner must decide whether to accept, mitigate, or convert IDs to UUIDs. | - Target resource ID exists in DB. | Pending | Pending | Pending |

## 8. Summary Sheet Updates

### 8.1 `Test Cases` sheet — append row

| No | Function Name | Sheet Name | Description | Pre-Condition |
|---|---|---|---|---|
| {{next No}} | {{FeatureName}} | {{FeatureName}} | {{short desc}} | {{main pre-condition}} |

### 8.2 `Test Statistics` sheet — append row (row N = next available)

| Cell | Formula |
|---|---|
| B_N_ | {{next No}} |
| C_N_ | `='{{FeatureName}}'!B2` |
| D_N_ | `='{{FeatureName}}'!B6` |
| E_N_ | `='{{FeatureName}}'!C6` |
| F_N_ | `='{{FeatureName}}'!D6` |
| G_N_ | `='{{FeatureName}}'!E6` |
| H_N_ | `='{{FeatureName}}'!B4` |

## 9. Fill Checklist
- [ ] Sheet created from `Feature 1` template (preserves formatting & dropdowns)
- [ ] Header rows 2–4 filled per §3 above
- [ ] Rounds formulas normalized to `$F$10:$F$1000` / `$I$10:$I$1000` / `$L$10:$L$1000`
- [ ] All TC IDs follow `{{PREFIX}}_FT{NN}` and are unique
- [ ] Each function has ≥1 case per applicable layer (happy / validation / business rule / security)
- [ ] Expected results include HTTP code + exact message from source + DB side effect
- [ ] No hardcoded IDs — only `{{PLACEHOLDER}}` format
- [ ] No "X or Y" HTTP codes — one exact code per TC
- [ ] Shared 5-case token block present (valid / expired / invalid / missing / no-permission)
- [ ] Status transition matrix complete with TC references per non-trivial cell
- [ ] Every error message either quoted exactly from code or tagged `⚠️ UNVERIFIED`
- [ ] Every SignalR/notification expected result has: event name + payload fields + target audience
- [ ] No Vietnamese characters in file
- [ ] `Test Cases` + `Test Statistics` rows added
- [ ] `Cover` sheet — `Record of change` row appended
- [ ] Sheet `Bản sao của Table Management` deleted if still present

## 10. Gaps Requiring Confirmation

> List any fact that could not be verified from source code. Each item blocks test execution until resolved.

| # | Gap Description | Affected TC(s) | Source File to Check |
|---|---|---|---|
| 1 | {{description of unverified fact}} | {{FTxx, FTyy}} | {{Controller/Service/DTO file path}} |

_(If empty: "No gaps — all facts traced to source code.")_
````

---

## 6. Quality Gates (run internally before emitting)

All gates must pass. If any fails, regenerate the affected section.

| Gate | Check |
|---|---|
| G01 | **100% English** — regex scan for Vietnamese characters: `[àáảãạâầấẩẫậăằắẳẵặèéẻẽẹêềếểễệìíỉĩịòóỏõọôồốổỗộơờớởỡợùúủũụưừứửữựỳýỷỹỵđÀÁẢÃẠÂẦẤẨẪẬĂẰẮẲẴẶÈÉẺẼẸÊỀẾỂỄỆÌÍỈĨỊÒÓỎÕỌÔỒỐỔỖỘƠỜỚỞỠỢÙÚỦŨỤƯỪỨỬỮỰỲÝỶỸỴĐ]` — zero matches required |
| G02 | **Every endpoint has a row** in §2.5 Code Traceability table |
| G03 | **No hardcoded numeric IDs** in Procedure or Pre-conditions columns — only `{{PLACEHOLDER}}` |
| G04 | **No "X or Y" HTTP codes** — single exact code per TC |
| G05 | **Shared 5-case token block** present in "Authentication & Token Edge Cases" group |
| G06 | **Status transition matrix** present and complete (NxN) with one TC reference per non-trivial cell |
| G07 | **Every error message** in Expected Results either quoted verbatim from source or tagged `⚠️ UNVERIFIED` |
| G08 | **§2.6 Test Data Seed** table present with all `{{PLACEHOLDER}}` values defined and seeding instructions |
| G09 | **§10 Gaps Requiring Confirmation** section present (even if no gaps) |
| G10 | **All endpoints, permissions, enum values, SignalR event names** traceable to a source file read in Step 0 |
| G11 | **IDOR TC** present for every public endpoint that exposes a resource by ID |
| G12 | **Server-side authority TC** present for every public endpoint accepting numeric client input |
| G13 | **SignalR/notification TCs** include: exact event name + payload fields + target audience |

---

## 7. Self-Check Protocol

After generating all sections but BEFORE writing the file:

```
FOR EACH quality gate G01–G13:
  IF gate fails:
    IDENTIFY the failing TC or section
    REGENERATE that section using source facts
    RE-RUN the gate
  IF gate still fails after regeneration:
    IF gate is G01 (language) or G10 (fabrication):
      HALT — emit error message to user:
      "Gate {{Gxx}} failed: [description]. Cannot proceed without [missing source file / translation]. Please provide [specific file path]."
    ELSE:
      Tag affected cells with ⚠️ and add to Gaps Requiring Confirmation
```

---

## 8. Changelog

| Version | Date | Changes |
|---|---|---|
| v1.0 | — | Initial skill |
| v2.0 | — | Added 4-layer matrix, Excel schema, ID convention |
| v3.0 | 2026-04-21 | Added §0.5 Source of Truth (no-fabrication rule), Code Traceability table (§2.5), Test Data Seed (§2.6), Hard Constraints 11–18, Quality Gates G01–G13, Self-Check Protocol, Language Lock, Status Transition Matrix template, 5-case token block, IDOR and server-side authority TC requirements |


## 0. Output Contract (MUST follow)

Produce exactly ONE Markdown file:

`Docs/Integration Test Instructions/{FeatureName}_Report_Instructions.md`

The file MUST contain, in this order:

1. Feature overview & scope
2. Test environment & pre-conditions
3. Function grouping plan (CRUD + special flows)
4. **4-layer coverage matrix** (Happy / Validation / Business rule / Security)
5. **Full test case table** — ready to paste into the per-feature Excel sheet (columns A–O)
6. Header block values (rows 2–4) for the sheet
7. Updates required in `Test Cases` and `Test Statistics` sheets
8. Fill checklist

Nothing else. No C# code. No prose commentary outside these sections.

---

## 1. Inputs

| Input | Required | Default | Notes |
|---|---|---|---|
| `FeatureName` | ✅ | — | Sheet name in Excel, e.g. `Account Management` |
| `id-prefix` | ⬜ | `UPPER(FeatureName) without spaces` | Prefix for Test Case ID, e.g. `ACCOUNT`, `ORDER`, `RESERVATION` |
| `endpoints` | ⬜ | auto-discover | Path to controller file(s); if omitted, ask user for endpoint list or UI flow |
| `rounds` | ⬜ | `3` | Number of testing rounds (matches Excel template) |

If endpoints and UI flows are both unknown → STOP and ask user for one of:
- Controller file path, or
- Swagger/OpenAPI spec, or
- Plain list of endpoints/UI actions + requirements.

---

## 2. Procedure

### Step 1 — Collect scope
1. List all endpoints / UI actions belonging to the feature.
2. Identify CRUD operations and special flows (login, refresh, reset, process, approve, etc.).
3. Identify dependencies (e.g. must create role before assigning to account).
4. Identify RBAC: which role/permission is required for each action.

### Step 2 — Group functions
Default grouping order (mirrors existing sheets in the file):


For non-CRUD modules, group by **business use case** (see Authentication: Login / Token Refresh / Logout / Forgot / Verify / Reset; Payment: View/Search / Process Payment).

Each group becomes a **group header row** in column A only (no ID, no result).

### Step 3 — Generate test cases applying the **4-layer matrix**
For EACH function, design at minimum:

| Layer | Must cover | Reference TC in file |
|---|---|---|
| **Happy path** | Valid input → success + side effects verified | `ROLES_FT01`, `TABLES_FT01`, `AUTHENTICATION_FT01` |
| **Validation** | Required / length / format / range / whitespace | `ROLES_FT02`, `TABLES_FT04`, `AUTHENTICATION_FT07` |
| **Business rule** | Duplicate, conflict, invalid state transition, referential integrity | `ROLES_FT04` (409), `ROLES_FT14` (assigned staff), `TABLES_FT12` (LOCKED→?), `PAY_FT16` (already paid) |
| **Security / Permission** | Missing permission, expired/invalid/reused token, inactive/locked account | `PAY_FT17`, `AUTHENTICATION_FT03/06/10/11` |

For **list/search/filter** functions, ALWAYS include:
- View without filter
- Filter/search WITH match
- Filter/search with NO match → empty state

For **endpoints with tokens/sessions**, ALWAYS include: valid / expired / invalid / missing / reused.

### Step 4 — Write test cases in the exact Excel schema (see §4)

### Step 5 — Produce summary sheet updates (see §5)

### Step 6 — Emit the Markdown file per §0 contract

---

## 3. Excel Template — exact cell schema to target

### 3.1 Per-feature sheet header (rows 2–8)

| Row | A | B | C | D | E | R (legend) |
|---|---|---|---|---|---|---|
| 2 | `Feature` | **{FeatureName}** | — | — | — | `Passed` |
| 3 | `Test requirement` | 1-sentence scope | — | — | — | `Failed` |
| 4 | `Number of TCs` | `=COUNTA(A12:A998)` | — | — | — | `Pending` |
| 5 | `Testing Round` | `Passed` | `Failed` | `Pending` | `N/A` | `N/A` |
| 6 | `Round 1` | `=COUNTIF( $F$10:$F$1000,B$5)` | `=COUNTIF( $F$10:$F$1000,C$5)` | `=COUNTIF( $F$10:$F$1000,D$5)` | `=COUNTIF( $F$10:$F$1000,E$5)` | |
| 7 | `Round 2` | `=COUNTIF( $I$10:$I$1000,B$5)` | `=COUNTIF( $I$10:$I$1000,C$5)` | `=COUNTIF( $I$10:$I$1000,D$5)` | `=COUNTIF( $I$10:$I$1000,E$5)` | |
| 8 | `Round 3` | `=COUNTIF( $L$10:$L$1000,B$5)` | `=COUNTIF( $L$10:$L$1000,C$5)` | `=COUNTIF( $L$10:$L$1000,D$5)` | `=COUNTIF( $L$10:$L$1000,E$5)` | |

> ⚠️ The original file has inconsistent COUNTIF ranges (e.g. `F12:F200` vs `$F10:$F993`). **Always normalize to `$F$10:$F$1000`** (and I/L for rounds 2/3) as above.

### 3.2 Test case table (row 10 = header, row 11+ = data)

| Col | Header | Rule |
|---|---|---|
| A | Test Case ID | `{ID_PREFIX}_FT{NN}`, NN = 2 digits, zero-padded, continuous across groups |
| B | Test Case Description | 1 action-oriented sentence |
| C | Test Case Procedure | Numbered steps `1. ...\n2. ...` |
| D | Expected Results | Numbered, MUST include HTTP status + UI message + side effect |
| E | Pre-conditions | Bullet list: role, data, network, permission |
| F | Round 1 | `Pending` by default |
| G | Test date | empty |
| H | Tester | empty |
| I | Round 2 | `Pending` |
| J / K | Test date / Tester | empty |
| L | Round 3 | `Pending` |
| M / N | Test date / Tester | empty |
| O | Note | empty |

**Group header rows**: only column A filled with the function name (e.g. `Create Account`). No ID, no result.

### 3.3 ID prefix convention (aligned with existing sheets)

| Feature | Prefix used in file |
|---|---|
| Authentication | `AUTHENTICATION` |
| Roles & Permissions | `ROLES` |
| Payment | `PAY` |
| Table | `TABLES` |
| Account | `ACCOUNT` (recommended) |
| Dish | `DISH` (recommended) |
| Order | `ORDER` (recommended) |
| Reservation | `RES` (recommended) |

> Do NOT use `IT_{FEATURE}_{nn}` — the file uses `{MODULE}_FT{NN}`. Match the file.

---

## 4. Output Markdown — strict template

The generated `.md` MUST use this exact structure (fill `{{placeholders}}`):

````markdown
# Integration Test Instructions — {{FeatureName}}

## 1. Overview
- **Module**: {{FeatureName}}
- **Sheet name**: `{{FeatureName}}`
- **ID prefix**: `{{ID_PREFIX}}`
- **Scope**: {{1-sentence description}}
- **Endpoints / UI flows covered**:
  - {{METHOD}} {{path}} — {{purpose}}
  - ...

## 2. Test Environment
- VRMOS Server running
- Database: MySQL (seeded test data)
- Browser: Microsoft Edge / Google Chrome
- Accounts required: {{list with roles}}

## 3. Sheet Header Values (paste into rows 2–4)

| Cell | Value |
|---|---|
| B2 | `{{FeatureName}}` |
| B3 | `{{test requirement sentence}}` |
| B4 | `=COUNTA(A12:A998)` |

(Rows 5–8: keep template formulas — §3.1 of skill.)

## 4. Function Grouping
1. {{Group 1}}
2. {{Group 2}}
...

## 5. 4-Layer Coverage Matrix

| Layer | Test Case IDs |
|---|---|
| Happy path | {{IDs}} |
| Validation | {{IDs}} |
| Business rule | {{IDs}} |
| Security / Permission | {{IDs}} |
| List/Search | {{IDs}} |

## 6. Test Cases (paste into row 11+)

> Columns: A | B | C | D | E | F | G | H | I | J | K | L | M | N | O
> Group headers occupy only column A.

### 🟦 {{Group 1}}

| A (ID) | B (Description) | C (Procedure) | D (Expected) | E (Pre-conditions) | F | I | L |
|---|---|---|---|---|---|---|---|
| {{PREFIX}}_FT01 | {{desc}} | 1. ...<br>2. ... | 1. HTTP 200.<br>2. Toast "...".<br>3. {{side effect}} | - {{role}}<br>- {{data}} | Pending | Pending | Pending |
| ... | | | | | | | |

### 🟦 {{Group 2}}
... (continue)

## 7. Summary Sheet Updates

### 7.1 `Test Cases` sheet — append row

| No | Function Name | Sheet Name | Description | Pre-Condition |
|---|---|---|---|---|
| {{next No}} | {{FeatureName}} | {{FeatureName}} | {{short desc}} | {{main pre-condition}} |

### 7.2 `Test Statistics` sheet — append row (e.g. row {{N}})

| Cell | Formula |
|---|---|
| B{{N}} | {{next No}} |
| C{{N}} | `='{{FeatureName}}'!B2` |
| D{{N}} | `='{{FeatureName}}'!B6` |
| E{{N}} | `='{{FeatureName}}'!C6` |
| F{{N}} | `='{{FeatureName}}'!D6` |
| G{{N}} | `='{{FeatureName}}'!E6` |
| H{{N}} | `='{{FeatureName}}'!B4` |

## 8. Fill Checklist
- [ ] Sheet created from `Feature 1` template (preserves formatting & dropdowns)
- [ ] Header rows 2–4 filled
- [ ] Rounds formulas normalized to `$F$10:$F$1000` / I / L
- [ ] All TC IDs follow `{{PREFIX}}_FT{NN}` and are unique
- [ ] Each function has ≥1 case per applicable layer
- [ ] Expected results include HTTP code + message + side effect
- [ ] `Test Cases` + `Test Statistics` rows added
- [ ] `Cover` sheet — `Record of change` row appended
- [ ] Sheet `Bản sao của Table Management` deleted if still present
