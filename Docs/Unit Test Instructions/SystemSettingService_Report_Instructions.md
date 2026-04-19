# Unit Test Report Instructions — SystemSettingService

> **Target Excel:** `Docs/Report5.1_Unit Test.xlsx`
> **Test file:** `Tests/Services/SystemSettingServiceTests.cs`
> **Module:** SYSTEM_SETTING
> **Scope:** Methods requested — CreateSettingAsync, DeleteAsync, GetAllGroupedAsync, GetGroupAsync, GetPublicGroupAsync, UploadStoreLogoAsync, UploadStoreFileAsync

---

## 1. Sheet: MethodList (starting row 9)

| No | Module Name | Method Name | Sheet Name | Description | Pre-Condition |
|----|-------------|-------------|------------|-------------|---------------|
| (next) | SYSTEM_SETTING | CreateSettingAsync | CreateSettingAsync | Admin creates a new system configuration setting with a typed value (STRING, INT, DECIMAL, BOOL, or JSON) and persists it to the repository | N/A |
| (next) | SYSTEM_SETTING | DeleteAsync | DeleteAsync | Admin deletes a system setting by key; on successful deletion the cache entry for that key is also cleared | N/A |
| (next) | SYSTEM_SETTING | GetAllGroupedAsync | GetAllGroupedAsync | Admin retrieves all system settings grouped by their key prefix (e.g., "store", "reservation", "general" for top-level keys) | N/A |
| (next) | SYSTEM_SETTING | GetGroupAsync | GetGroupAsync | Admin retrieves all settings under a given group prefix; for the "store" group the response also includes a resolved public URL for media keys | N/A |
| (next) | SYSTEM_SETTING | GetPublicGroupAsync | GetPublicGroupAsync | Public caller retrieves only non-sensitive settings for a group prefix; sensitive settings are filtered out from the result | N/A |
| (next) | SYSTEM_SETTING | UploadStoreLogoAsync | UploadStoreLogoAsync | Admin uploads a store logo image; the file is validated as an image and saved to the "store-logo" folder via the file storage service | N/A |
| (next) | SYSTEM_SETTING | UploadStoreFileAsync | UploadStoreFileAsync | Admin uploads a store media file; the service detects whether it is a video (mp4) or image and routes it to the appropriate folder with the matching validation options | N/A |

---

## 2. Sheet: Statistics

> Only the 7 user-requested methods are listed. The test file also contains tests for other methods (GetStringAsync, GetIntAsync, SetStringAsync, BulkUpdateGroupAsync, etc.).

| No | Function code | Passed | Failed | Untested | N | A | B | Total |
|----|---------------|--------|--------|----------|---|---|---|-------|
| (next) | CreateSettingAsync | 1 | 0 | 0 | 1 | 0 | 0 | 1 |
| (next) | DeleteAsync | 2 | 0 | 0 | 1 | 0 | 1 | 2 |
| (next) | GetAllGroupedAsync | 1 | 0 | 0 | 1 | 0 | 0 | 1 |
| (next) | GetGroupAsync | 1 | 0 | 0 | 1 | 0 | 0 | 1 |
| (next) | GetPublicGroupAsync | 1 | 0 | 0 | 1 | 0 | 0 | 1 |
| (next) | UploadStoreLogoAsync | 1 | 0 | 0 | 1 | 0 | 0 | 1 |
| (next) | UploadStoreFileAsync | 1 | 0 | 0 | 1 | 0 | 0 | 1 |
| **Sub total** | | **8** | **0** | **0** | **7** | **0** | **1** | **8** |

**Summary formulas:**
- Test coverage: `(8 + 0) / 8 × 100 = 100%`
- Test successful coverage: `8 / 8 × 100 = 100%`

---

## 3. Per-Method Sheets

---

### Sheet: CreateSettingAsync

**Header:**
- Method: `CreateSettingAsync`
- Test requirement: Admin creates a new typed system setting; the service parses the value string according to the given ValueType, saves it to the repository, and clears the cache for that key
- Passed: 1 | Failed: 0 | N/A/B: 1/0/0

**Test Case IDs:** `UTCID01`

**Condition section:**

| Condition Group | Input Value | UTCID01 |
|-----------------|-------------|---------|
| key | `"reservation.max"` | O |
| settingName | `"Max"` | O |
| valueType | `"INT"` | O |
| value | `"50"` (parseable as long) | O |

**Confirmation section:**

| Output | Expected Value | UTCID01 |
|--------|----------------|---------|
| Verify | `SaveAsync` called with `SettingKey="reservation.max"`, `ValueType="INT"`, `ValueInt=50` | O |
| Verify | `RemoveAsync("system_setting:reservation.max")` called once (cache cleared) | O |

**Result:**

| UTCID | Type | P/F | Date |
|-------|------|-----|------|
| UTCID01 | N | P | 2026-04-18 |

---

### Sheet: DeleteAsync

**Header:**
- Method: `DeleteAsync`
- Test requirement: Admin deletes a system setting by key; if the repository confirms deletion, the cache entry is cleared; if the key does not exist, the method returns false without touching the cache
- Passed: 2 | Failed: 0 | N/A/B: 1/0/1

**Test Case IDs:** `UTCID01`, `UTCID02`

**Condition section:**

| Condition Group | Input Value | UTCID01 | UTCID02 |
|-----------------|-------------|---------|---------|
| key | `"store.logoUrl"` (exists) | O | |
| key | `"missing.key"` (not found) | | O |
| repositoryReturn | `true` (deleted) | O | |
| repositoryReturn | `false` (not found) | | O |

**Confirmation section:**

| Output | Expected Value | UTCID01 | UTCID02 |
|--------|----------------|---------|---------|
| Return | `true`; `RemoveAsync("system_setting:store.logoUrl")` called once | O | |
| Return | `false`; `RemoveAsync` never called | | O |

**Result:**

| UTCID | Type | P/F | Date |
|-------|------|-----|------|
| UTCID01 | N | P | 2026-04-18 |
| UTCID02 | B | P | 2026-04-18 |

---

### Sheet: GetAllGroupedAsync

**Header:**
- Method: `GetAllGroupedAsync`
- Test requirement: Admin retrieves all settings grouped by the first segment of each key; keys without a dot go into the "general" group; keys with a known prefix (e.g., "store.", "reservation.") are grouped accordingly
- Passed: 1 | Failed: 0 | N/A/B: 1/0/0

**Test Case IDs:** `UTCID01`

**Condition section:**

| Condition Group | Input Value | UTCID01 |
|-----------------|-------------|---------|
| settings | `["store.email", "reservation.default_duration_minutes", "timezone"]` | O |

**Confirmation section:**

| Output | Expected Value | UTCID01 |
|--------|----------------|---------|
| Return | Dictionary with keys `"store"`, `"reservation"`, `"general"`; `result["general"][0].SettingKey = "timezone"` | O |

**Result:**

| UTCID | Type | P/F | Date |
|-------|------|-----|------|
| UTCID01 | N | P | 2026-04-18 |

---

### Sheet: GetGroupAsync

**Header:**
- Method: `GetGroupAsync`
- Test requirement: Admin retrieves all settings under a group prefix; for the "store" group the service additionally calls `GetPublicUrl` on any media keys to attach a public-facing URL to the DTO
- Passed: 1 | Failed: 0 | N/A/B: 1/0/0

**Test Case IDs:** `UTCID01`

**Condition section:**

| Condition Group | Input Value | UTCID01 |
|-----------------|-------------|---------|
| group | `"store"` | O |
| settings | `["store.logoUrl"="uploads/store-logo/logo.png", "store.name"="AuLac"]` | O |
| fileStorage.GetPublicUrl | returns `"/uploads/store-logo/logo.png"` for `"store-logo/logo.png"` | O |

**Confirmation section:**

| Output | Expected Value | UTCID01 |
|--------|----------------|---------|
| Return | `store.logoUrl.PublicUrl = "/uploads/store-logo/logo.png"`; `store.name.PublicUrl = null` | O |

**Result:**

| UTCID | Type | P/F | Date |
|-------|------|-----|------|
| UTCID01 | N | P | 2026-04-18 |

---

### Sheet: GetPublicGroupAsync

**Header:**
- Method: `GetPublicGroupAsync`
- Test requirement: Public endpoint returns settings for a group prefix, excluding any setting marked as sensitive so that secrets are never exposed in public API responses
- Passed: 1 | Failed: 0 | N/A/B: 1/0/0

**Test Case IDs:** `UTCID01`

**Condition section:**

| Condition Group | Input Value | UTCID01 |
|-----------------|-------------|---------|
| group | `"store"` | O |
| settings | `["store.email" (isSensitive=false), "store.smtp.password" (isSensitive=true)]` | O |

**Confirmation section:**

| Output | Expected Value | UTCID01 |
|--------|----------------|---------|
| Return | `List` with 1 item; only `"store.email"` present; `"store.smtp.password"` filtered out | O |

**Result:**

| UTCID | Type | P/F | Date |
|-------|------|-----|------|
| UTCID01 | N | P | 2026-04-18 |

---

### Sheet: UploadStoreLogoAsync

**Header:**
- Method: `UploadStoreLogoAsync`
- Test requirement: Admin uploads a logo image; the service wraps the stream in a `FileUploadRequest` and delegates to `IFileStorage.SaveAsync` targeting the `"store-logo"` folder with image upload validation options
- Passed: 1 | Failed: 0 | N/A/B: 1/0/0

**Test Case IDs:** `UTCID01`

**Condition section:**

| Condition Group | Input Value | UTCID01 |
|-----------------|-------------|---------|
| fileName | `"logo.png"` | O |
| contentType | `"image/png"` | O |
| stream | `MemoryStream([1,2,3])` (3-byte stub) | O |

**Confirmation section:**

| Output | Expected Value | UTCID01 |
|--------|----------------|---------|
| Return | `FileUploadResult` with `RelativePath = "store-logo/logo.png"` | O |
| Verify | `SaveAsync` called with folder=`"store-logo"` | O |

**Result:**

| UTCID | Type | P/F | Date |
|-------|------|-----|------|
| UTCID01 | N | P | 2026-04-18 |

---

### Sheet: UploadStoreFileAsync

**Header:**
- Method: `UploadStoreFileAsync`
- Test requirement: Admin uploads a store media file; the service detects MP4 content type to route to the `"store-videos"` folder with video validation, and routes all other files to `"store-media"` with image validation
- Passed: 1 | Failed: 0 | N/A/B: 1/0/0

**Test Case IDs:** `UTCID01`

**Condition section:**

| Condition Group | Input Value | UTCID01 |
|-----------------|-------------|---------|
| fileName | `"intro.mp4"` | O |
| contentType | `"video/mp4"` | O |
| stream | `MemoryStream([1,2,3])` (3-byte stub) | O |

**Confirmation section:**

| Output | Expected Value | UTCID01 |
|--------|----------------|---------|
| Return | `FileUploadResult` with `RelativePath = "store-videos/intro.mp4"` | O |
| Verify | `SaveAsync` called with folder=`"store-videos"` | O |

**Result:**

| UTCID | Type | P/F | Date |
|-------|------|-----|------|
| UTCID01 | N | P | 2026-04-18 |

---

## 4. Test Case ↔ C# Method Mapping

| UTCID | C# Test Method | Trait Type |
|-------|----------------|------------|
| CreateSettingAsync UTCID01 | `CreateSettingAsync_WhenTypeIsInt_ParsesAndSavesIntValue` | N |
| DeleteAsync UTCID01 | `DeleteAsync_WhenDeleted_ClearsCacheAndReturnsTrue` | N |
| DeleteAsync UTCID02 | `DeleteAsync_WhenNotDeleted_DoesNotClearCache` | B |
| GetAllGroupedAsync UTCID01 | `GetAllGroupedAsync_WhenMixedKeys_GroupsByPrefixAndGeneral` | N |
| GetGroupAsync UTCID01 | `GetGroupAsync_WhenStoreGroup_AttachesPublicUrlForMediaKeys` | N |
| GetPublicGroupAsync UTCID01 | `GetPublicGroupAsync_WhenContainsSensitive_FiltersSensitiveOut` | N |
| UploadStoreLogoAsync UTCID01 | `UploadStoreLogoAsync_WhenImage_RoutesToStoreLogoFolder` | N |
| UploadStoreFileAsync UTCID01 | `UploadStoreFileAsync_WhenVideo_RoutesToStoreVideosFolder` | N |

---

## Notes

- Run command: `dotnet test Tests/Tests.csproj --filter "FullyQualifiedName~SystemSettingServiceTests"`
- All tests in scope passed on 2026-04-18.
- The test file contains additional tests for methods not in scope (GetStringAsync, GetIntAsync, GetJsonAsync, SetStringAsync, GetAllNonSensitiveAsync, BulkUpdateGroupAsync); those are not documented here.
- `GetGroupAsync` and `GetPublicGroupAsync` for the "store" group additionally call `IFileStorage.GetPublicUrl(relativePath)` for keys listed in the `StoreMediaKeys` set.
