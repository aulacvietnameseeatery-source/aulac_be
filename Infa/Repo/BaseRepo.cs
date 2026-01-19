using Core.DTO.General;
using Core.Enum;
using Core.Helpers;
using Core.Interface.Repo;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Infra.Repo
{
    public abstract class BaseRepo<T> : IBaseRepo<T> where T : class
    {
        private readonly string _connectionString;

        /**
         * Khởi tạo repository cơ sở và chọn chuỗi kết nối theo môi trường (Dev/Prod).
         * Điều kiện: appsettings phải có `ConnectionStrings:DevConnectionString/ProdConnectionString`.
         * Created By DatND (15/1/2026)
         */
        public BaseRepo(IConfiguration configuration, IHostEnvironment env)
        {
            if (env.IsProduction())
            {
                _connectionString = configuration.GetConnectionString("ProdConnectionString")
                    ?? throw new InvalidOperationException("Thiếu ProdConnectionString trong cấu hình.");
            }
            else
            {
                _connectionString = configuration.GetConnectionString("DevConnectionString")
                    ?? throw new InvalidOperationException("Thiếu DevConnectionString trong cấu hình.");
            }
        }


        /**
         * Lấy danh sách tất cả bản ghi của bảng tương ứng.
         * Created By DatND (15/1/2026)
         */
        public virtual async Task<IEnumerable<T>> GetAllAsync()
        {
            throw new NotImplementedException();
        }

        /**
         * Lấy 1 bản ghi theo id:
         * - Key: xác định bằng `[MISAKey]` (ưu tiên) hoặc convention.
         * - Column name: theo `[MISAColumnName]` (ưu tiên) hoặc tên property.
         * Created By DatND (16/1/2026)
         */
        public virtual async Task<T?> GetByIdAsync(Guid id)
        {
            throw new NotImplementedException();
        }

        /**
         * Thêm mới bản ghi: insert các property có `[MISAColumnName]`.
         * Created By DatND (15/1/2026)
         */
        public virtual async Task<T> InsertAsync(T entity)
        {
            throw new NotImplementedException();
        }

        /**
         * Cập nhật bản ghi theo khoá chính:
         * - Key: `[MISAKey]` (ưu tiên) hoặc convention.
         * - Update columns: các property có `[MISAColumnName]` (trừ khoá chính).
         * - WHERE: dùng tên cột theo `[MISAColumnName]` (nếu có) trên key property.
         * Created By DatND (16/1/2026)
         */
        public virtual async Task<T> UpdateAsync(T entity)
        {
            throw new NotImplementedException();
        }

        /**
         * Xoá 1 bản ghi theo id:
         * - Key: `[MISAKey]` (ưu tiên) hoặc convention.
         * - Column name: theo `[MISAColumnName]` (ưu tiên) hoặc tên property.
         * Created By DatND (16/1/2026)
         */
        public virtual async Task DeleteAsync(List<T> entities)
        {
            throw new NotImplementedException();
        }

        /**
         * Lấy danh sách bản ghi theo điều kiện phân trang, lọc, sắp xếp.
         * - Pagination: PageIndex (1-based), PageSize
         * - Filter: JSON array format [["field","operator",value],"and/or",...]
         * - Sort: JSON array format [{"Selector":"field","Desc":true/false},...]
         * - CustomParam: Custom query cho các điều kiện khác
         * - Columns: Tên các cột cần lấy, ngăn cách bởi dấu phẩy
         * Created By DatND (17/1/2026)
         */
        public virtual async Task<List<T>> GetByConditionAsync(QueryDTO queryDto)
        {
           throw new NotImplementedException();
        }

        /**
         * Đếm tổng số bản ghi theo điều kiện lọc (không tính phân trang).
         * - Filter: JSON array format [["field","operator",value],"and/or",...]
         * - CustomParam: Custom query cho các điều kiện khác
         * Created By DatND (17/1/2026)
         */
        public virtual async Task<int> CountByConditionAsync(QueryDTO queryDto)
        {
           throw new NotImplementedException();
        }

        /**
         * Lưu entity: tự động quyết định INSERT hay UPDATE dựa vào giá trị khoá chính.
         * Logic:
         * - Nếu key = Guid.Empty: INSERT (tạo mới GUID)
         * - Nếu key có giá trị và record tồn tại: UPDATE
         * - Nếu key có giá trị nhưng record không tồn tại: INSERT
         * Created By DatND (17/1/2026)
         */
        public virtual async Task<T> SaveAsync(T entity)
        {
           throw new NotImplementedException();
        }


        #region private helper methods

        #endregion
    }
}