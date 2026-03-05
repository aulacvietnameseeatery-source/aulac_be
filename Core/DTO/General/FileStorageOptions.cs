using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.DTO.General
{
    /// <summary>
    /// Configuration options for the file storage service.
    /// Bound from the "FileStorage" section in appsettings.json.
    /// </summary>
    public class FileStorageOptions
    {
        /// <summary>
        /// Absolute root path on disk, e.g. "wwwroot/uploads".
        /// </summary>
        public string RootPath { get; set; } = null!;

        /// <summary>
        /// API base URL prepended to every public asset URL.
        /// When empty, URLs are returned as server-relative paths ("/uploads/...").
        /// </summary>
        public string BaseUrl { get; set; } = string.Empty;

        /// <summary>
        /// URL path prefix for served static files.
        /// E.g. "/uploads" → full public URL becomes "{BaseUrl}/uploads/dishes/abc.jpg".
        /// Default: "/uploads"
        /// </summary>
        public string PublicUrlPrefix { get; set; } = "/uploads";

        /// <summary>
        /// Global maximum file size in bytes. Default: 10 MB.
        /// Individual upload calls can specify a smaller limit via <see cref="FileValidationOptions"/>.
        /// </summary>
        public long MaxFileSizeBytes { get; set; } = 10 * 1024 * 1024;

        /// <summary>
        /// Allowed MIME types. Empty set = allow all.
        /// Default: common image types.
        /// </summary>
        public HashSet<string> AllowedMimeTypes { get; set; } =
        [
            "image/jpeg",
            "image/png",
            "image/gif",
            "image/webp",
            "image/svg+xml"
        ];

        /// <summary>
        /// Allowed file extensions (with dot). Empty set = allow all.
        /// Default: common image extensions.
        /// </summary>
        public HashSet<string> AllowedExtensions { get; set; } =
        [
            ".jpg",
            ".jpeg",
            ".png",
            ".gif",
            ".webp",
            ".svg"
        ];
    }
}
