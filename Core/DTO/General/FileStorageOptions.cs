using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.DTO.General
{
    public class FileStorageOptions
    {
        /// <summary>
        /// Absolute root path, eg: wwwroot/uploads
        /// </summary>
        public string RootPath { get; set; } = null!;
    }
}
