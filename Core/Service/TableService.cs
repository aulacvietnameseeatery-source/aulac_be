using Core.DTO.Table;
using Core.Interface.Repo;
using Core.Interface.Service.Table;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Service
{
    public class TableService : ITableService
    {
        private readonly ITableRepository _tableRepository;

        public TableService(ITableRepository tableRepository)
        {
            _tableRepository = tableRepository;
        }

        public async Task<(List<TableManagementDto> Items, int TotalCount)> GetTablesForManagementAsync(GetTableManagementRequest request, CancellationToken ct = default)
        {
            var (tables, totalCount) = await _tableRepository.GetTablesForManagementAsync(request, ct);

            // Mapping Entity sang DTO để trả về Frontend
            var dtos = tables.Select(t => new TableManagementDto
            {
                TableId = t.TableId,
                TableCode = t.TableCode,
                Capacity = t.Capacity,
                IsOnline = t.IsOnline ?? false,

                // Trạng thái
                StatusId = t.TableStatusLvId,
                StatusCode = t.TableStatusLv?.ValueCode ?? "UNKNOWN",
                StatusName = t.TableStatusLv?.ValueName ?? "Unknown",

                // Loại bàn
                TypeId = t.TableTypeLvId,
                TypeName = t.TableTypeLv?.ValueName ?? "Unknown",

                // Khu vực
                ZoneId = t.ZoneLvId,
                ZoneName = t.ZoneLv?.ValueName ?? "Unknown"
            }).ToList();

            return (dtos, totalCount);
        }

        public async Task<List<TableSelectDto>> GetTablesForSelectAsync(CancellationToken ct = default)
        {
            var tables = await _tableRepository.GetTablesWithRelationsAsync(ct);

            var now = DateTime.UtcNow;

            if (tables == null || !tables.Any())
            {
                return new List<TableSelectDto>();
            }

            return tables.Select(t =>
            {
                var activeOrder = t.Orders
                    .FirstOrDefault(o =>
                        o.OrderStatusLv.ValueCode != OrderStatusCode.CANCELLED.ToString() &&
                        o.OrderStatusLv.ValueCode != OrderStatusCode.COMPLETED.ToString());

                var upcomingReservation = t.Reservations
                    .Where(r => r.ReservedTime > now)
                    .OrderBy(r => r.ReservedTime)
                    .FirstOrDefault();

                return new TableSelectDto
                {
                    TableId = t.TableId,
                    TableCode = t.TableCode,
                    Capacity = t.Capacity,
                    ZoneId = t.ZoneLvId,
                    ZoneName = t.ZoneLv.ValueName,
                    StatusCode = t.TableStatusLv.ValueCode,
                    HasActiveOrder = activeOrder != null,
                    ActiveOrderId = activeOrder?.OrderId,
                    UpcomingReservationTime = upcomingReservation?.ReservedTime
                };
            }).ToList();
        }
    }
}

