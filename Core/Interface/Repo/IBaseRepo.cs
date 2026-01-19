using Core.DTO.General;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Interface.Repo
{
    public interface IBaseRepo<T>
    {
        Task<T?> GetByIdAsync(Guid id);
        Task<IEnumerable<T>> GetAllAsync();

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
        /// Lưu entity (tự động insert hoặc update dựa vào khoá chính).
        /// - Nếu key = Guid.Empty hoặc null: thực hiện INSERT
        /// - Nếu key có giá trị: thực hiện UPDATE
        /// </summary>
        /// <param name="entity">Entity cần lưu</param>
        /// <returns>Entity sau khi lưu</returns>
        /// Created by: DatND (17/01/2026)
        Task<T> SaveAsync(T entity);

        Task<T> InsertAsync(T entity);
        Task<T> UpdateAsync(T entity);
        Task DeleteAsync(List<T> ids);
    }
}
