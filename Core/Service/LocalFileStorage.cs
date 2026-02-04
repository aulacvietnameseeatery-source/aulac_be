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

            if (string.IsNullOrWhiteSpace(_rootPath))
                throw new ArgumentException("FileStorage RootPath is not configured");
        }

        public async Task<string> SaveAsync(
            Stream content,
            string fileName,
            string folder,
            CancellationToken ct = default)
        {
            if (content == null || content.Length == 0)
                throw new ArgumentException("File content is empty");

            var safeFolder = NormalizeFolder(folder);
            var directory = Path.Combine(_rootPath, safeFolder);

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
                useAsync: true
            );

            await content.CopyToAsync(fs, ct);

            return Path.Combine(safeFolder, generatedName).Replace("\\", "/");
            //return new FileSaveResult
            //{
            //    RelativePath = Path.Combine(safeFolder, generatedName).Replace("\\", "/"),
            //    FileName = generatedName,
            //    Size = fs.Length
            //};
        }

        public Task DeleteAsync(string relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath))
                return Task.CompletedTask;

            var fullPath = Path.Combine(_rootPath, relativePath);

            if (File.Exists(fullPath))
                File.Delete(fullPath);

            return Task.CompletedTask;
        }

        private static string NormalizeFolder(string folder)
        {
            return folder
                .Trim()
                .TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                .Replace("\\", "/");
        }
    }

}
