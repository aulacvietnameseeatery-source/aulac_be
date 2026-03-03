using Core.DTO.General;

namespace Core.Interface.Service.FileStorage;

/// <summary>
/// Abstraction for file storage operations with built-in validation.
/// Implementations handle physical storage (local disk, blob, S3, etc.).
/// </summary>
public interface IFileStorage
{
    // ── Single file operations ────────────────────────────────────────

    /// <summary>
    /// Validates, saves a single file, and returns the result with both relative and public paths.
    /// </summary>
    /// <param name="file">The file to upload.</param>
    /// <param name="folder">Target sub-folder (e.g. "dishes", "table-media").</param>
    /// <param name="validation">Per-call validation overrides. Null = use global defaults.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Upload result containing relative path and public URL.</returns>
    /// <exception cref="Core.Exceptions.ValidationException">
    /// Thrown when the file fails validation (size, type, extension).
    /// </exception>
    Task<FileUploadResult> SaveAsync(
        FileUploadRequest file,
      string folder,
        FileValidationOptions? validation = null,
 CancellationToken ct = default);

    // ── Batch operations ──────────────────────────────────────────────

    /// <summary>
    /// Validates and saves multiple files. Validates batch count first, then each file individually.
    /// If any file fails, previously saved files in this batch are cleaned up.
    /// </summary>
    /// <param name="files">The files to upload.</param>
    /// <param name="folder">Target sub-folder.</param>
    /// <param name="validation">Per-call validation overrides. Null = use global defaults.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of upload results for all saved files.</returns>
    Task<List<FileUploadResult>> SaveManyAsync(
        IReadOnlyList<FileUploadRequest> files,
     string folder,
        FileValidationOptions? validation = null,
     CancellationToken ct = default);

    // ── Delete operations ─────────────────────────────────────────────

    /// <summary>
    /// Deletes a file by its relative path. No-op if the file does not exist.
    /// </summary>
    /// <param name="relativePath">Relative path as returned by <see cref="SaveAsync"/>.</param>
    Task DeleteAsync(string relativePath);

    /// <summary>
    /// Best-effort deletion of multiple files. Logs but does not throw on individual failures.
    /// Useful for cleanup after transaction rollback.
    /// </summary>
    /// <param name="relativePaths">Relative paths to delete.</param>
    Task DeleteManyAsync(IEnumerable<string> relativePaths);

    // ── Utility ───────────────────────────────────────────────────────

    /// <summary>
    /// Checks whether a file exists at the given relative path.
    /// </summary>
    bool Exists(string relativePath);

    /// <summary>
    /// Builds the public URL for a given relative storage path.
    /// E.g. "table-media/abc.jpg" → "/uploads/table-media/abc.jpg".
    /// </summary>
    string GetPublicUrl(string relativePath);

    // ── Legacy compatibility ──────────────────────────────────────────

    /// <summary>
    /// Low-level save without validation. Prefer <see cref="SaveAsync(FileUploadRequest, string, FileValidationOptions?, CancellationToken)"/>.
    /// Kept for backward compatibility with existing callers.
    /// </summary>
    [Obsolete("Use SaveAsync(FileUploadRequest, ...) with built-in validation instead.")]
    Task<string> SaveAsync(Stream fileStream, string fileName, string folder, CancellationToken ct);
}
