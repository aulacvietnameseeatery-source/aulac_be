using Core.DTO.General;
using Core.Interface.Service.FileStorage;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Core.Service
{
    public class LocalFileStorage : IFileStorage
    {
        private readonly string _rootPath;

        public LocalFileStorage(IOptions<FileStorageOptions> options)
        {
            _rootPath = options.Value.RootPath;

            // Ensure the root path is configured, otherwise throw an exception
            if (string.IsNullOrWhiteSpace(_rootPath))
                throw new ArgumentException("FileStorage RootPath is not configured");
        }

        public async Task<string> SaveAsync(
            Stream content,
            string fileName,
            string folder,
            CancellationToken ct = default)
        {
            // Validate that the file content is not empty
            if (content == null || content.Length == 0)
                throw new ArgumentException("File content is empty");

            var safeFolder = NormalizeFolder(folder);
            var directory = Path.Combine(_rootPath, safeFolder);

            // Create the directory if it does not exist
            Directory.CreateDirectory(directory);

            var extension = Path.GetExtension(fileName);
            // Generate a unique file name using a GUID
            var generatedName = $"{Guid.NewGuid():N}{extension}";
            var fullPath = Path.Combine(directory, generatedName);

            // Create a new file stream for writing the file asynchronously
            await using var fs = new FileStream(
                fullPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 81920,
                useAsync: true
            );

            // Copy the content stream to the file stream
            await content.CopyToAsync(fs, ct);

            // Return the relative path of the saved file (using forward slashes)
            return Path.Combine(safeFolder, generatedName).Replace("\\", "/");
        }

        public Task DeleteAsync(string relativePath)
        {
            // If the relative path is empty, do nothing
            if (string.IsNullOrWhiteSpace(relativePath))
                return Task.CompletedTask;

            var fullPath = Path.Combine(_rootPath, relativePath);

            // Delete the file if it exists
            if (File.Exists(fullPath))
                File.Delete(fullPath);

            return Task.CompletedTask;
        }

        private static string NormalizeFolder(string folder)
        {
            // Normalize the folder path by trimming and replacing backslashes with forward slashes
            return folder
                .Trim()
                .TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                .Replace("\\", "/");
        }
    }

}
