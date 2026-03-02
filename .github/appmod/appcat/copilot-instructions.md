// At the end of the file, before the last line, add:

---

## 16. File Storage Service

The application uses a generic `IFileStorage` abstraction with built-in validation for all file upload operations.

### Key Types

| Type | Location | Purpose |
|------|---------|---------|
| `IFileStorage` | `Core/Interface/Service/FileStorage/` | Storage abstraction with validation |
| `LocalFileStorage` | `Infa/Service/` | Local disk implementation |
| `FileStorageOptions` | `Core/DTO/General/` | Global config (root path, limits, allowed types) |
| `FileUploadRequest` | `Core/DTO/General/` | Input DTO for file upload |
| `FileUploadResult` | `Core/DTO/General/` | Output DTO with relative path + public URL |
| `FileValidationOptions` | `Core/DTO/General/` | Per-call validation overrides |

### Configuration (`appsettings.json`)

```json
{
  "FileStorage": {
    "RootPath": "wwwroot/uploads",
    "PublicUrlPrefix": "/uploads",
    "MaxFileSizeBytes": 10485760,
    "AllowedMimeTypes": ["image/jpeg", "image/png", "image/gif", "image/webp"],
    "AllowedExtensions": [".jpg", ".jpeg", ".png", ".gif", ".webp"]
  }
}
```

### Validation Presets

Use `FileValidationOptions` presets instead of hardcoding limits in services:

```csharp
// Standard image upload: max 5 MB, max 5 files, common image types
FileValidationOptions.ImageUpload

// Use global defaults from FileStorageOptions
FileValidationOptions.Default

// Custom per-call overrides
new FileValidationOptions
{
    MaxFileSizeBytes = 2 * 1024 * 1024,
    MaxFileCount = 3,
    AllowedMimeTypes = ["image/jpeg", "image/png"]
}
```

### Usage Pattern — Single File

```csharp
var result = await _fileStorage.SaveAsync(
    new FileUploadRequest
    {
        Stream = file.OpenReadStream(),
      FileName = file.FileName,
        ContentType = file.ContentType
 },
    folder: "dishes",
    validation: FileValidationOptions.ImageUpload,
    ct);

// result.RelativePath → "dishes/abc123.jpg"  (for DB storage)
// result.PublicUrl     → "/uploads/dishes/abc123.jpg"  (for API responses)
```

### Usage Pattern — Batch Upload

```csharp
var requests = files.Select(f => new FileUploadRequest
{
    Stream = f.OpenReadStream(),
 FileName = f.FileName,
    ContentType = f.ContentType
}).ToList();

// Validates count + each file, auto-cleans on failure
var results = await _fileStorage.SaveManyAsync(
    requests, "table-media", FileValidationOptions.ImageUpload, ct);
```

### Usage Pattern — Cleanup on Transaction Rollback

```csharp
var savedPaths = new List<string>();
await _unitOfWork.BeginTransactionAsync(ct);
try
{
    foreach (var file in files)
    {
   var result = await _fileStorage.SaveAsync(...);
        savedPaths.Add(result.RelativePath);
        // ... DB operations ...
    }
    await _unitOfWork.CommitAsync(ct);
}
catch
{
    await _unitOfWork.RollbackAsync(ct);
    await _fileStorage.DeleteManyAsync(savedPaths); // best-effort cleanup
    throw;
}
```

### Rules

- **Never** hardcode file size limits in services — use `FileValidationOptions` presets.
- **Never** manually validate MIME types or extensions — `IFileStorage` handles this.
- **Always** store `result.PublicUrl` in `MediaAsset.Url` (the public-facing URL).
- **Always** use `result.RelativePath` for `DeleteAsync` / `DeleteManyAsync` operations.
- **Always** clean up saved files on transaction rollback using `DeleteManyAsync`.
- The legacy `SaveAsync(Stream, string, string, CancellationToken)` is `[Obsolete]` — migrate callers to use `FileUploadRequest`.
