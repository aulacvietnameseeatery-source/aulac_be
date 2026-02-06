using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Interface.Service.FileStorage
{
    public interface IFileStorage
    {
        /// <summary>
        /// Asynchronously saves a file stream to the specified folder with the given file name.
        /// Returns the relative path of the saved file.
        /// </summary>
        /// <param name="fileStream">The stream containing the file data to save.</param>
        /// <param name="fileName">The name to assign to the saved file.</param>
        /// <param name="folder">The folder where the file should be saved.</param>
        /// <param name="ct">A cancellation token to observe while waiting for the task to complete.</param>
        /// <returns>A task that represents the asynchronous save operation. The task result contains the relative path of the saved file.</returns>
        Task<string> SaveAsync(
            Stream fileStream,
            string fileName,
            string folder,
            CancellationToken ct
        );

        /// <summary>
        /// Asynchronously deletes a file at the specified relative path.
        /// </summary>
        /// <param name="relativePath">The relative path of the file to delete.</param>
        /// <returns>A task that represents the asynchronous delete operation.</returns>
        Task DeleteAsync(string relativePath);
    }

}
