using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Interface.Service.FileStorage
{
    public interface IFileStorage
    {
        Task<string> SaveAsync(
            Stream fileStream,
            string fileName,
            string folder,
            CancellationToken ct
        );

        Task DeleteAsync(string relativePath);
    }

}
