namespace Core.DTO.General;

/// <summary>
/// Represents a single file to be uploaded and validated by the file storage service.
/// </summary>
public class FileUploadRequest
{
    /// <summary>
    /// The file content stream. Must be seekable or at least provide Length.
    /// </summary>
    public required Stream Stream { get; init; }

    /// <summary>
    /// Original file name (used for extension extraction and logging).
    /// </summary>
    public required string FileName { get; init; }

    /// <summary>
    /// MIME content type reported by the client (e.g. "image/jpeg").
/// </summary>
    public required string ContentType { get; init; }
}

/// <summary>
/// Result returned after a successful file upload.
/// </summary>
public class FileUploadResult
{
    /// <summary>
 /// Relative storage path (e.g. "table-media/abc123.jpg").
    /// Use this for database storage.
    /// </summary>
    public required string RelativePath { get; init; }

    /// <summary>
    /// Public-facing URL (e.g. "/uploads/table-media/abc123.jpg").
    /// Use this for API responses / frontend display.
    /// </summary>
    public required string PublicUrl { get; init; }

    /// <summary>
    /// Original file name as provided by the caller.
    /// </summary>
    public required string OriginalFileName { get; init; }

  /// <summary>
    /// Actual size in bytes.
    /// </summary>
    public long SizeBytes { get; init; }
}
