# Unit Test Report Instructions — NotificationService

> **Target Excel:** `Docs/Report5.1_Unit Test.xlsx`
> **Test file:** `Tests/Services/NotificationServiceTests.cs`
> **Module:** NOTIFICATION
> **Total tests:** 14  |  **Passed:** 14  |  **Failed:** 0

---

## 1. Sheet: MethodList (starting row 9)

| No | Module Name | Method Name | Sheet Name | Description | Pre-Condition |
|----|-------------|-------------|------------|-------------|---------------|
| (next) | NOTIFICATION | PublishAsync | PublishAsync | System publishes a notification by persisting it to the database and pushing it to real-time channels targeting specific permissions, user IDs, or all connected clients | N/A |
| (next) | NOTIFICATION | GetNotificationsAsync | GetNotificationsAsync | Staff retrieves their paginated notification history filtered by type or unread status based on their permissions | N/A |
| (next) | NOTIFICATION | GetUnreadCountAsync | GetUnreadCountAsync | Staff queries the count of unread notifications visible to them based on their role permissions | N/A |
| (next) | NOTIFICATION | GetMissedAsync | GetMissedAsync | Staff retrieves notifications that were published after a given timestamp, used to recover missed real-time events after reconnecting | N/A |
| (next) | NOTIFICATION | GetPreferencesAsync | GetPreferencesAsync | Staff views their notification preferences for all notification types, with unset types defaulting to enabled | N/A |
| (next) | NOTIFICATION | UpdatePreferencesAsync | UpdatePreferencesAsync | Staff updates their per-type notification preferences to toggle sound and enable/disable delivery for each notification type | N/A |

---

## 2. Sheet: Statistics

| No | Function code | Passed | Failed | Untested | N | A | B | Total |
|----|---------------|--------|--------|----------|---|---|---|-------|
| (next) | PublishAsync | 4 | 0 | 0 | 2 | 1 | 1 | 4 |
| (next) | GetNotificationsAsync | 2 | 0 | 0 | 1 | 0 | 1 | 2 |
| (next) | GetUnreadCountAsync | 2 | 0 | 0 | 1 | 0 | 1 | 2 |
| (next) | GetMissedAsync | 2 | 0 | 0 | 1 | 0 | 1 | 2 |
| (next) | GetPreferencesAsync | 2 | 0 | 0 | 1 | 0 | 1 | 2 |
| (next) | UpdatePreferencesAsync | 2 | 0 | 0 | 1 | 0 | 1 | 2 |
| **Sub total** | | **14** | **0** | **0** | **7** | **1** | **6** | **14** |

**Summary formulas:**
- Test coverage: `(14 + 0) / 14 × 100 = 100%`
- Test successful coverage: `14 / 14 × 100 = 100%`

---

## 3. Per-Method Sheets

---

### Sheet: PublishAsync

**Header:**
- Method: `PublishAsync`
- Test requirement: System creates a persistent notification record and pushes it to the appropriate real-time SignalR channels; real-time push failures are silently swallowed so the DB record is always saved
- Passed: 4 | Failed: 0 | N/A/B: 2/1/1

**Test Case IDs:** `UTCID01`, `UTCID02`, `UTCID03`, `UTCID04`

**Condition section:**

| Condition Group | Input Value | UTCID01 | UTCID02 | UTCID03 | UTCID04 |
|-----------------|-------------|---------|---------|---------|---------|
| request.Type | `NEW_ORDER` | O | O | O | O |
| request.TargetPermissions | `["permission.view_orders"]` (1 permission) | O | | O | |
| request.TargetPermissions | empty list | | O | | O |
| request.TargetUserIds | empty list | O | | O | O |
| request.TargetUserIds | `[101, 102]` (2 user IDs) | | O | | |
| realtimePublisher | `PublishToPermissionsAsync` succeeds | O | | | |
| realtimePublisher | `PublishToUserAsync` succeeds (×2) | | O | | |
| realtimePublisher | `PublishToPermissionsAsync` throws `Exception` | | | O | |
| realtimePublisher | `PublishToAllAsync` succeeds (no targets) | | | | O |

**Confirmation section:**

| Output | Expected Value | UTCID01 | UTCID02 | UTCID03 | UTCID04 |
|--------|----------------|---------|---------|---------|---------|
| Verify | `AddAsync` called once; `PublishToPermissionsAsync` called once | O | | | |
| Verify | `AddAsync` called once; `PublishToUserAsync` called twice (for 2 users) | | O | | |
| Verify | No exception thrown; `AddAsync` still called once (real-time failure swallowed) | | | O | |
| Verify | `PublishToAllAsync` called once; `PublishToPermissionsAsync` and `PublishToUserAsync` never called | | | | O |

**Result:**

| UTCID | Type | P/F | Date |
|-------|------|-----|------|
| UTCID01 | N | P | 2026-04-18 |
| UTCID02 | N | P | 2026-04-18 |
| UTCID03 | A | P | 2026-04-18 |
| UTCID04 | B | P | 2026-04-18 |

---

### Sheet: GetNotificationsAsync

**Header:**
- Method: `GetNotificationsAsync`
- Test requirement: Staff retrieves their notification history by delegating to the repository with their permissions and user ID; returns an empty list when there are no notifications
- Passed: 2 | Failed: 0 | N/A/B: 1/0/1

**Test Case IDs:** `UTCID01`, `UTCID02`

**Condition section:**

| Condition Group | Input Value | UTCID01 | UTCID02 |
|-----------------|-------------|---------|---------|
| query.Skip | `0` | O | O |
| query.Take | `20` | O | O |
| userPermissions | `["permission.view_orders"]` | O | |
| userPermissions | `[]` (empty) | | O |
| repositoryReturn | 2 notification items | O | |
| repositoryReturn | empty list | | O |

**Confirmation section:**

| Output | Expected Value | UTCID01 | UTCID02 |
|--------|----------------|---------|---------|
| Return | `List` with 2 items; `result[0].Type = "NEW_ORDER"` | O | |
| Return | Empty `List` | | O |

**Result:**

| UTCID | Type | P/F | Date |
|-------|------|-----|------|
| UTCID01 | N | P | 2026-04-18 |
| UTCID02 | B | P | 2026-04-18 |

---

### Sheet: GetUnreadCountAsync

**Header:**
- Method: `GetUnreadCountAsync`
- Test requirement: Staff queries the number of unread notifications they can see based on their permission set; the service delegates directly to the repository
- Passed: 2 | Failed: 0 | N/A/B: 1/0/1

**Test Case IDs:** `UTCID01`, `UTCID02`

**Condition section:**

| Condition Group | Input Value | UTCID01 | UTCID02 |
|-----------------|-------------|---------|---------|
| userPermissions | `["permission.view_orders"]` | O | |
| userPermissions | `[]` (empty) | | O |
| repositoryReturn | `7` (unread count) | O | |
| repositoryReturn | `0` (no unread) | | O |

**Confirmation section:**

| Output | Expected Value | UTCID01 | UTCID02 |
|--------|----------------|---------|---------|
| Return | `7` | O | |
| Return | `0` | | O |

**Result:**

| UTCID | Type | P/F | Date |
|-------|------|-----|------|
| UTCID01 | N | P | 2026-04-18 |
| UTCID02 | B | P | 2026-04-18 |

---

### Sheet: GetMissedAsync

**Header:**
- Method: `GetMissedAsync`
- Test requirement: Staff retrieves notifications missed since a given UTC timestamp after reconnecting; passing null for `afterUtc` returns all missed notifications
- Passed: 2 | Failed: 0 | N/A/B: 1/0/1

**Test Case IDs:** `UTCID01`, `UTCID02`

**Condition section:**

| Condition Group | Input Value | UTCID01 | UTCID02 |
|-----------------|-------------|---------|---------|
| userPermissions | `["permission.view_orders"]` | O | O |
| afterUtc | `2026-04-18T00:00:00Z` (specific timestamp) | O | |
| afterUtc | `null` (no lower bound) | | O |
| repositoryReturn | 1 notification (`SHIFT_ASSIGNED`) | O | |
| repositoryReturn | 2 notifications | | O |

**Confirmation section:**

| Output | Expected Value | UTCID01 | UTCID02 |
|--------|----------------|---------|---------|
| Return | `List` with 1 item; `result[0].Type = "SHIFT_ASSIGNED"` | O | |
| Return | `List` with 2 items | | O |

**Result:**

| UTCID | Type | P/F | Date |
|-------|------|-----|------|
| UTCID01 | N | P | 2026-04-18 |
| UTCID02 | B | P | 2026-04-18 |

---

### Sheet: GetPreferencesAsync

**Header:**
- Method: `GetPreferencesAsync`
- Test requirement: Staff views notification preferences for all known types; persisted preferences override the defaults, while types without a stored record default to `IsEnabled=true` and `SoundEnabled=true`
- Passed: 2 | Failed: 0 | N/A/B: 1/0/1

**Test Case IDs:** `UTCID01`, `UTCID02`

**Condition section:**

| Condition Group | Input Value | UTCID01 | UTCID02 |
|-----------------|-------------|---------|---------|
| userId | `100` | O | |
| userId | `200` | | O |
| storedPreferences | 1 preference (`NEW_ORDER`, IsEnabled=false) | O | |
| storedPreferences | empty list (no preferences stored) | | O |

**Confirmation section:**

| Output | Expected Value | UTCID01 | UTCID02 |
|--------|----------------|---------|---------|
| Return | All `NotificationType` enum values returned; `NEW_ORDER.IsEnabled=false`, `NEW_ORDER.SoundEnabled=false` | O | |
| Return | All `NotificationType` enum values returned; all have `IsEnabled=true`, `SoundEnabled=true` (defaults) | | O |

**Result:**

| UTCID | Type | P/F | Date |
|-------|------|-----|------|
| UTCID01 | N | P | 2026-04-18 |
| UTCID02 | B | P | 2026-04-18 |

---

### Sheet: UpdatePreferencesAsync

**Header:**
- Method: `UpdatePreferencesAsync`
- Test requirement: Staff saves their per-type notification preferences; the service maps the request to entity objects with the user ID and delegates to the repository's upsert operation
- Passed: 2 | Failed: 0 | N/A/B: 1/0/1

**Test Case IDs:** `UTCID01`, `UTCID02`

**Condition section:**

| Condition Group | Input Value | UTCID01 | UTCID02 |
|-----------------|-------------|---------|---------|
| userId | `100` | O | |
| userId | `200` | | O |
| request.Preferences | 2 items (`NEW_ORDER`: enabled=true; `LOW_STOCK_ALERT`: enabled=false) | O | |
| request.Preferences | empty list | | O |

**Confirmation section:**

| Output | Expected Value | UTCID01 | UTCID02 |
|--------|----------------|---------|---------|
| Verify | `UpsertPreferencesAsync(100, prefs where Count=2 and prefs[0].UserId=100)` called once | O | |
| Verify | `UpsertPreferencesAsync(200, prefs where Count=0)` called once | | O |

**Result:**

| UTCID | Type | P/F | Date |
|-------|------|-----|------|
| UTCID01 | N | P | 2026-04-18 |
| UTCID02 | B | P | 2026-04-18 |

---

## 4. Test Case ↔ C# Method Mapping

| UTCID | C# Test Method | Trait Type |
|-------|----------------|------------|
| PublishAsync UTCID01 | `PublishAsync_WhenTargetPermissions_PersistsNotificationAndPushesToPermissionGroup` | N |
| PublishAsync UTCID02 | `PublishAsync_WhenTargetUserIds_PushesToEachSpecificUser` | N |
| PublishAsync UTCID03 | `PublishAsync_WhenRealTimePushFails_StillPersistsNotificationAndDoesNotThrow` | A |
| PublishAsync UTCID04 | `PublishAsync_WhenNoTargets_BroadcastsToAllConnectedClients` | B |
| GetNotificationsAsync UTCID01 | `GetNotificationsAsync_WhenCalled_DelegatesQueryToRepositoryAndReturnsItems` | N |
| GetNotificationsAsync UTCID02 | `GetNotificationsAsync_WhenNoNotifications_ReturnsEmptyList` | B |
| GetUnreadCountAsync UTCID01 | `GetUnreadCountAsync_WhenUserHasUnreadNotifications_ReturnsCount` | N |
| GetUnreadCountAsync UTCID02 | `GetUnreadCountAsync_WhenNoUnreadNotifications_ReturnsZero` | B |
| GetMissedAsync UTCID01 | `GetMissedAsync_WhenAfterUtcProvided_ReturnsMissedNotificationsAfterDate` | N |
| GetMissedAsync UTCID02 | `GetMissedAsync_WhenAfterUtcIsNull_DelegatesNullAndReturnsAll` | B |
| GetPreferencesAsync UTCID01 | `GetPreferencesAsync_WhenUserHasSomePreferences_MergesWithDefaults` | N |
| GetPreferencesAsync UTCID02 | `GetPreferencesAsync_WhenNoPreferencesStored_ReturnsAllTypesWithDefaultEnabled` | B |
| UpdatePreferencesAsync UTCID01 | `UpdatePreferencesAsync_WhenCalled_UpsertsPreferencesForUser` | N |
| UpdatePreferencesAsync UTCID02 | `UpdatePreferencesAsync_WhenEmptyList_StillCallsUpsertWithEmptyCollection` | B |

---

## Notes

- Run command: `dotnet test Tests/Tests.csproj --filter "FullyQualifiedName~NotificationServiceTests"`
- All 14 tests passed on 2026-04-18.
- `PublishAsync` wraps the real-time push in a try/catch so DB persistence is always guaranteed regardless of SignalR availability.
- `GetPreferencesAsync` merges stored preferences with defaults for all values of the `NotificationType` enum, so the result count always equals `System.Enum.GetNames(typeof(NotificationType)).Length`.
