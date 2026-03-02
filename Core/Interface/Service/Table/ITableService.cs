using Core.DTO.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Interface.Service.Table
{
    public interface ITableService
    {
        Task<(List<TableManagementDto> Items, int TotalCount)> GetTablesForManagementAsync(GetTableManagementRequest request, CancellationToken ct = default);

        Task<List<TableSelectDto>> GetTablesForSelectAsync(CancellationToken ct = default);
    }
}
