using Core.DTO.General;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Interface.Service
{
    public interface IBaseService<T>
    {
        /// <summary>
        /// Get a list of all.
        /// </summary>
        /// <returns></returns>
        Task<List<T>> GetAllAsync();

        /// <summary>
        /// Get by Id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        /// Created by: DatND (06/01/2026)
        Task<T> GetByIdAsync(Guid id);

        /// <summary>
        /// Get by condition with QueryDTO
        /// </summary>
        /// <param name="queryDto"></param>
        /// <returns></returns>
        Task<List<T>> GetByConditionAsync(QueryDTO queryDto);

        /// <summary>
        /// Get count by condition with QueryDTO
        /// </summary>
        /// <param name="queryDto"></param>
        /// <returns></returns>
        Task<int> CountByConditionAsync(QueryDTO queryDto);

        /// <summary>
        /// Lưu DTO (tự động insert hoặc update).
        /// - Validates dữ liệu trước khi lưu
        /// - Tự động phân biệt INSERT/UPDATE dựa vào khoá chính
        /// </summary>
        /// <param name="dto">DTO cần lưu</param>
        /// <returns>DTO sau khi lưu</returns>
        /// Created by: DatND (17/01/2026)
        Task<T> SaveAsync(T dto, int mode, int state);

        /// <summary>
        /// Insert a new
        /// </summary>
        /// <returns></returns>
        /// Created by: DatND (06/01/2026)
        Task<T> InsertAsync(T entity);

        /// <summary>
        /// Update information
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        /// Created by: DatND (06/01/2026)
        Task<T> UpdateAsync(T entity);

        /// <summary>
        /// Xóa bản ghi
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        /// Created by: DatND (06/01/2026)
        Task DeleteAsync(List<T> entities);
    }
}
