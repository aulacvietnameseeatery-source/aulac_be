using Core.DTO.LookUpValue;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Interface.Service.LookUp
{
    public interface ILookupService
    {
        Task<List<LookupValueI18nDto>> GetAllActiveByTypeAsync(
            ushort typeId,
            CancellationToken ct
        );
    }
}
