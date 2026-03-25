using Core.DTO.General;
using Core.Exceptions;
using Core.Interface.Service.FileStorage;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics;

namespace Infa.Service;

/// <summary>
/// Local disk implementation of <see cref="IFileStorage"/> with built-in validation.
/// </summary>
public class LocalFileStorage : IFileStorage
{
    private readonly FileStorageOptions _options;
    private readonly ILogger<LocalFileStorage> _logger;

    public LocalFileStorage(IOptions<FileStorageOptions> options, ILogger<LocalFileStorage> logger)
    {
        _options = options.Value;
        _logger = logger;

        if (string.IsNullOrWhiteSpace(_options.RootPath))
            throw new ArgumentException("FileStorage:RootPath is not configured.");
    }

    #region ── Single file ──

    /// <inheritdoc />
    public async Task<FileUploadResult> SaveAsync(
        FileUploadRequest file,
        string folder,
        FileValidationOptions? validation = null,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(file);

        ValidateFile(file, validation);

        ValidateFileCustom(file, validation);

        var relativePath = await WriteFileAsync(file.Stream, file.FileName, folder, ct);

        return new FileUploadResult
        {
            RelativePath = relativePath,
            PublicUrl = GetPublicUrl(relativePath),
            OriginalFileName = file.FileName,
            SizeBytes = file.Stream.Length
        };
    }

    #endregion

    #region ── Batch ──

    /// <inheritdoc />
    public async Task<List<FileUploadResult>> SaveManyAsync(
        IReadOnlyList<FileUploadRequest> files,
        string folder,
        FileValidationOptions? validation = null,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(files);

        if (files.Count == 0)
            throw new ValidationException("No files provided.");

        // Validate batch count
        var maxCount = validation?.MaxFileCount;
        if (maxCount.HasValue && files.Count > maxCount.Value)
            throw new ValidationException($"Too many files. Maximum {maxCount.Value} file(s) allowed, but {files.Count} were provided.");

        // Validate each file before saving any
        foreach (var file in files)
        {
            ValidateFile(file, validation);
            ValidateFileCustom(file, validation);
        }

        // Save all — rollback on failure
        var savedPaths = new List<string>(files.Count);
        var results = new List<FileUploadResult>(files.Count);

        try
        {
            foreach (var file in files)
            {
                ct.ThrowIfCancellationRequested();

                var relativePath = await WriteFileAsync(file.Stream, file.FileName, folder, ct);
                savedPaths.Add(relativePath);

                results.Add(new FileUploadResult
                {
                    RelativePath = relativePath,
                    PublicUrl = GetPublicUrl(relativePath),
                    OriginalFileName = file.FileName,
                    SizeBytes = file.Stream.Length
                });
            }
        }
        catch
        {
            // Clean up any files that were already written
            await DeleteManyAsync(savedPaths);
            throw;
        }

        return results;
    }

    #endregion

    #region ── Delete ──

    /// <inheritdoc />
    public Task DeleteAsync(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
            return Task.CompletedTask;

        var fullPath = Path.Combine(_options.RootPath, NormalizePath(relativePath));

        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
            _logger.LogDebug("Deleted file: {Path}", relativePath);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task DeleteManyAsync(IEnumerable<string> relativePaths)
    {
        foreach (var path in relativePaths)
        {
            try
            {
                await DeleteAsync(path);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete file during cleanup: {Path}", path);
            }
        }
    }

    #endregion

    #region ── Utility ──

    /// <inheritdoc />
    public bool Exists(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
            return false;

        var fullPath = Path.Combine(_options.RootPath, NormalizePath(relativePath));
        return File.Exists(fullPath);
    }

    /// <inheritdoc />
    public string GetPublicUrl(string relativePath)
    {
        var prefix = _options.PublicUrlPrefix.TrimEnd('/');
        var path = relativePath.TrimStart('/');

        // Server-relative path: "/uploads/dishes/abc.jpg"
        var serverRelative = $"{prefix}/{path}";

        // Prepend the API base URL when configured, e.g. "http://localhost:5000"
        if (string.IsNullOrWhiteSpace(_options.BaseUrl))
            return serverRelative;

        var baseUrl = _options.BaseUrl.TrimEnd('/');
        return $"{baseUrl}{serverRelative}";
    }

    #endregion

    #region ── Legacy compatibility ──

    /// <inheritdoc />
    [Obsolete("Use SaveAsync(FileUploadRequest, ...) with built-in validation instead.")]
    public async Task<string> SaveAsync(Stream fileStream, string fileName, string folder, CancellationToken ct)
    {
        if (fileStream is null || fileStream.Length == 0)
            throw new ArgumentException("File content is empty.");

        return await WriteFileAsync(fileStream, fileName, folder, ct);
    }

    #endregion

    #region ── Private helpers ──
    /// <summary>
    /// Validates a single file against custom rules. Override in derived classes for specific validation logic (e.g. image dimensions, video codecs).
    /// </summary>
    /// <param name="file"></param>
    /// <param name="validation"></param>
    public virtual void ValidateFileCustom(FileUploadRequest file, FileValidationOptions? validation) { }
    /// <summary>
    /// Validates a single file against per-call and global rules.
    /// </summary>
    private void ValidateFile(FileUploadRequest file, FileValidationOptions? validation)
    {
        // ── Validate not empty ──
        if (file.Stream.Length == 0)
            throw new ValidationException($"File '{file.FileName}' is empty.");

        // ── Validate size ──
        var maxSize = validation?.MaxFileSizeBytes ?? _options.MaxFileSizeBytes;
        if (maxSize > 0 && file.Stream.Length > maxSize)
        {
            var maxMb = maxSize / (1024.0 * 1024.0);
            throw new ValidationException($"File '{file.FileName}' ({file.Stream.Length / (1024.0 * 1024.0):F1} MB) exceeds the {maxMb:F0} MB size limit.");
        }

        // ── Validate MIME type ──
        var allowedMimeTypes = validation?.AllowedMimeTypes ?? _options.AllowedMimeTypes;
        if (allowedMimeTypes is { Count: > 0 }
            && !allowedMimeTypes.Contains(file.ContentType, StringComparer.OrdinalIgnoreCase))
        {
            throw new ValidationException(
                $"File '{file.FileName}' has unsupported content type '{file.ContentType}'. " +
                $"Allowed: {string.Join(", ", allowedMimeTypes)}");
        }

        // ── Validate extension ──
        var allowedExtensions = validation?.AllowedExtensions ?? _options.AllowedExtensions;
        var extension = Path.GetExtension(file.FileName);
        if (allowedExtensions is { Count: > 0 }
            && !string.IsNullOrEmpty(extension)
            && !allowedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
        {
            throw new ValidationException(
                $"File '{file.FileName}' has unsupported extension '{extension}'. " +
                $"Allowed: {string.Join(", ", allowedExtensions)}");
        }

        // =========================
        // VIDEO EXTRA VALIDATION
        // =========================
        if (IsVideoFile(file.ContentType, extension))
        {
            var isMp4 = file.ContentType.Equals("video/mp4", StringComparison.OrdinalIgnoreCase)
                        || extension.Equals(".mp4", StringComparison.OrdinalIgnoreCase);

            // Validate MP4 file signature when this upload is expected to be MP4.
            if (isMp4 && !IsValidMp4(file.Stream))
                throw new ValidationException($"File '{file.FileName}' is not a valid MP4.");

            // Duration
            var maxDuration = validation?.MaxVideoDurationSeconds;
            if (maxDuration.HasValue)
            {
                var duration = GetVideoDurationSeconds(file.Stream, file.FileName);

                if (duration > maxDuration.Value)
                    throw new ValidationException(
                        $"Video too long ({duration}s). Max is {maxDuration.Value}s.");
            }
        }
    }

    /// <summary>
    /// Writes a stream to disk and returns the relative path.
    /// </summary>
    private async Task<string> WriteFileAsync(Stream content, string fileName, string folder, CancellationToken ct)
    {
        var safeFolder = NormalizePath(folder);
        var directory = Path.Combine(_options.RootPath, safeFolder);
        Directory.CreateDirectory(directory);

        var extension = Path.GetExtension(fileName);
        var generatedName = $"{Guid.NewGuid():N}{extension}";
        var fullPath = Path.Combine(directory, generatedName);

        await using var fs = new FileStream(
            fullPath,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 81920,
            useAsync: true);

        await content.CopyToAsync(fs, ct);

        return Path.Combine(safeFolder, generatedName).Replace('\\', '/');
    }

    /// <summary>
    /// Normalizes a path segment: trims, removes leading separators, uses forward slashes.
    /// </summary>
    private static string NormalizePath(string path)
    {
        return path
   .Trim()
    .TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            .Replace('\\', '/');
    }

    private static bool IsVideoFile(string contentType, string extension)
    {
        return (!string.IsNullOrWhiteSpace(contentType)
            && contentType.StartsWith("video/", StringComparison.OrdinalIgnoreCase))
            || extension.Equals(".mp4", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsValidMp4(Stream stream)
    {
        Span<byte> buffer = stackalloc byte[12];

        stream.Position = 0;
        stream.Read(buffer);

        var isFtyp =
            buffer[4] == 'f' &&
            buffer[5] == 't' &&
            buffer[6] == 'y' &&
            buffer[7] == 'p';

        stream.Position = 0;
        return isFtyp;
    }

    private static int GetVideoDurationSeconds(Stream stream, string fileName)
    {
        var extension = Path.GetExtension(fileName);
        var tempPath = Path.Combine(
            Path.GetTempPath(),
            $"{Guid.NewGuid():N}{extension}" 
        );

        try
        {
            stream.Position = 0;

            using (var fs = new FileStream(tempPath, FileMode.Create, FileAccess.Write))
            {
                stream.CopyTo(fs);
            }

            using var file = TagLib.File.Create(tempPath);

            return (int)file.Properties.Duration.TotalSeconds;
        }
        catch (TagLib.UnsupportedFormatException)
        {
            throw new ValidationException("Invalid or unsupported video format.");
        }
        finally
        {
            try { File.Delete(tempPath); } catch { }
            stream.Position = 0;
        }
    }

    #endregion
}
