namespace Core.DTO.General;

/// <summary>
/// Per-call validation overrides for file uploads.
/// When null/unset, the global <see cref="FileStorageOptions"/> defaults are used.
/// </summary>
public class FileValidationOptions
{
    /// <summary>
    /// Maximum file size in bytes for this specific upload.
    /// When null, uses <see cref="FileStorageOptions.MaxFileSizeBytes"/>.
    /// </summary>
    public long? MaxFileSizeBytes { get; init; }

 /// <summary>
    /// Maximum number of files allowed in a batch upload.
    /// When null, no batch limit is enforced.
    /// </summary>
    public int? MaxFileCount { get; init; }

 /// <summary>
    /// Allowed MIME types for this specific upload.
  /// When null, uses <see cref="FileStorageOptions.AllowedMimeTypes"/>.
    /// </summary>
    public HashSet<string>? AllowedMimeTypes { get; init; }

    /// <summary>
    /// Allowed file extensions (with dot) for this specific upload.
    /// When null, uses <see cref="FileStorageOptions.AllowedExtensions"/>.
    /// </summary>
    public HashSet<string>? AllowedExtensions { get; init; }

    public int? MaxVideoDurationSeconds { get; init; }

    // ?? Predefined presets ??????????????????????????????????????????????

    /// <summary>
    /// Standard image upload preset: max 5 MB, max 5 files, common image types only.
    /// </summary>
    public static readonly FileValidationOptions ImageUpload = new()
    {
      MaxFileSizeBytes = 5 * 1024 * 1024,
        MaxFileCount = 5,
      AllowedMimeTypes = ["image/jpeg", "image/png", "image/gif", "image/webp"],
        AllowedExtensions = [".jpg", ".jpeg", ".png", ".gif", ".webp"]
    };

    public static readonly FileValidationOptions DishVideo = new()
    {
        MaxFileSizeBytes = 20 * 1024 * 1024,
        MaxFileCount = 1,
        AllowedMimeTypes = ["video/mp4"],
        AllowedExtensions = [".mp4"],
        MaxVideoDurationSeconds = 15
    };

    /// <summary>
    /// Relaxed preset: uses global defaults for everything.
    /// </summary>
    public static readonly FileValidationOptions Default = new();
}
