using Core.DTO.General;
using Core.Interface.Repo;
using Core.Interface.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Core.Service
{
    public abstract class BaseService<T, TDto> : IBaseService<TDto>
    where T : class

    {

        private readonly IBaseRepo<T> _repo;

        /**
         * Khởi tạo service cơ sở và nhận repository tương ứng thông qua DI.
         * Điều kiện: `repo` không được null.
         * Created By DatND (15/1/2026)
         */
        public BaseService(IBaseRepo<T> repo)
        {
            // Lưu repo để thao tác dữ liệu
            _repo = repo ?? throw new ArgumentNullException(nameof(repo));
        }
        protected abstract TDto MapToDto(T entity);
        protected abstract T MapToEntity(TDto dto);

        /**
         * Validate QueryDTO cho phân trang và filter.
         * Điều kiện: PageIndex > 0, PageSize > 0
         * Created By DatND (18/1/2026)
        */
        protected virtual void ValidateQueryDTO(QueryDTO queryDto)
        {
            if (queryDto == null)
                throw new ArgumentNullException(nameof(queryDto), "Tham số truy vấn không được để trống");

            if (queryDto.PageSize <= 0)
                throw new ArgumentException("Số bản ghi mỗi trang phải lớn hơn 0");

            if (queryDto.PageIndex <= 0)
                throw new ArgumentException("Số trang phải lớn hơn 0");

            if (queryDto.PageSize > 1000)
                throw new ArgumentException("Số bản ghi mỗi trang không được vượt quá 1000");
        }

        /**
         * Validate SaveDTO trước khi lưu.
         * Điều kiện: EntityDTO không được null
         * Created By DatND (18/1/2026)
         */
        protected virtual Task ValidateSaveDTO(TDto dto, int mode, int state)
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto), "Dữ liệu không được để trống");

            // Validate state values (1=Create, 2=Update, 4=Duplicate)
            if (state != 1 && state != 2 && state != 4)
                throw new ArgumentException("Trạng thái lưu không hợp lệ. Giá trị hợp lệ: 1 (Tạo mới), 2 (Cập nhật), 4 (Nhân bản)");

            return Task.CompletedTask;
        }

        /**
         * Validate danh sách entities trước khi xoá.
         * Điều kiện: Danh sách không được null hoặc rỗng
         * Created By DatND (18/1/2026)
        */
        protected virtual void ValidateDeleteList(List<TDto> entities)
        {
            if (entities == null)
                throw new ArgumentNullException(nameof(entities), "Danh sách xoá không được để trống");

            if (!entities.Any())
                throw new ArgumentException("Danh sách xoá không được rỗng");

            if (entities.Count > 100)
                throw new ArgumentException("Không thể xoá quá 100 bản ghi cùng lúc");
        }

        /**
         * Validate danh sách IDs.
         * Điều kiện: Danh sách không được null hoặc rỗng
         * Created By DatND (18/1/2026)
         */
        protected virtual void ValidateIdList(List<Guid> ids, string operationName = "thao tác")
        {
            if (ids == null)
                throw new ArgumentNullException(nameof(ids), $"Danh sách ID cho {operationName} không được để trống");

            if (!ids.Any())
                throw new ArgumentException($"Danh sách ID cho {operationName} không được rỗng");

            if (ids.Count > 100)
                throw new ArgumentException($"Không thể thực hiện {operationName} trên quá 100 bản ghi cùng lúc");

            // Check for duplicate IDs
            var duplicates = ids.GroupBy(x => x).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
            if (duplicates.Any())
                throw new ArgumentException($"Danh sách ID có giá trị trùng lặp");
        }

        /**
         * Validate dữ liệu bắt buộc dựa trên attribute `MISARequire`.
         * Điều kiện: property có `MISARequire` phải có giá trị khác null/rỗng, nếu sai sẽ throw exception.
         * Created By DatND (15/1/2026)
        */
        private void ValidateData(T entity)
        {
            // Kiểm tra đầu vào
            if (entity == null) throw new ArgumentNullException(nameof(entity));

            var validationErrors = new List<string>();

            

            // Nếu có lỗi validation, throw exception với tất cả lỗi
            if (validationErrors.Any())
            {
                var mainMessage = validationErrors.Count == 1
                  ? validationErrors[0]
                 : $"Dữ liệu không hợp lệ: {validationErrors.Count} lỗi được tìm thấy";

                throw new ArgumentException(mainMessage);
            }
        }


        /**         
         * Nghiệp vụ trước khi save. 
         * Điều kiện: không bắt buộc; nếu sai business rule thì throw exception tương ứng.
         * Created By DatND (15/1/2026)
        */
        protected virtual Task BeforeSaveAsync(T entity, TDto dto, int mode, int state)
        {
            // Mặc định không làm gì, service con tự implement
            return Task.CompletedTask;
        }

        /**        
         * Nghiệp vụ sau khi save. 
         * Điều kiện: không bắt buộc.
         * Created By DatND (15/1/2026)
         */
        protected virtual Task AfterSaveAsync(T entity, TDto? dto, int mode, int state)
        {
            // Mặc định không làm gì, service con tự implement
            return Task.CompletedTask;
        }

        /**
         * Validate nghiệp vụ custom cho từng entity (service con có thể override).
         * Điều kiện: không bắt buộc; nếu sai business rule thì throw exception tương ứng.
         * Created By DatND (15/1/2026)
         */
        protected virtual Task ValidateCustom(T entity, TDto? dto, int mode, int state)
        {
            return Task.CompletedTask;
            // Mặc định không làm gì, service con tự implement
        }

        /**
         * Lấy danh sách tất cả bản ghi.
         * Điều kiện: repository phải triển khai `GetAllAsync`.
         * Created By DatND (15/1/2026)
        */
        public virtual async Task<List<TDto>> GetAllAsync()
        {
            // Gọi repo để lấy dữ liệu
            var data = await _repo.GetAllAsync();

            // Chuẩn hoá kiểu trả về List
            return data?.Select(MapToDto).ToList() ?? new List<TDto>();
        }

        /**
         * Lấy1 bản ghi theo id.
         * Điều kiện: id hợp lệ; trả về null nếu không tồn tại (tuỳ repo xử lý).
         * Created By DatND (15/1/2026)
        */
        public virtual async Task<TDto> GetByIdAsync(Guid id)
        {
            // Validate ID
            if (id == Guid.Empty)
                throw new ArgumentException("ID không hợp lệ");

            // Gọi repo để lấy theo id
            var entity = await _repo.GetByIdAsync(id);

            if (entity == null)
                throw new ArgumentException($"Không tìm thấy {typeof(T).Name} với ID: {id}");

            return MapToDto(entity);
        }

        /**
         * Xoá dữ liệu theo danh sách entities.
         * Điều kiện: danh sách hợp lệ.
         * Created By DatND (15/1/2026)
        */
        public virtual async Task DeleteAsync(List<TDto> entities)
        {
            // Validate danh sách
            ValidateDeleteList(entities);

            var entityList = entities.Select(e => MapToEntity(e)).ToList();

            // Gọi repo để xoá dữ liệu
            await _repo.DeleteAsync(entityList);
        }

        /**
         * Lấy danh sách bản ghi theo điều kiện.
         * Điều kiện: QueryDTO hợp lệ.
         * Created By DatND (15/1/2026)
         */
        public virtual async Task<List<TDto>> GetByConditionAsync(QueryDTO queryDto)
        {
            // Validate QueryDTO
            ValidateQueryDTO(queryDto);

            // Gọi repo để lấy dữ liệu theo điều kiện
            var data = await _repo.GetByConditionAsync(queryDto);

            // Chuẩn hoá kiểu trả về List
            return data?.Select(MapToDto).ToList() ?? new List<TDto>();
        }

        /**
         * Đếm số bản ghi theo điều kiện.
         * Điều kiện: QueryDTO hợp lệ.
         * Created By DatND (15/1/2026)
         */
        public virtual async Task<int> CountByConditionAsync(QueryDTO queryDto)
        {
            // Validate QueryDTO
            ValidateQueryDTO(queryDto);

            // Gọi repo để đếm dữ liệu theo điều kiện
            return await _repo.CountByConditionAsync(queryDto);
        }

        /**
         * Lưu dữ liệu: tự động insert hoặc update dựa vào khoá chính.
         * - Validate bắt buộc + validate custom trước khi lưu
         * - Gọi repository SaveAsync để xác định INSERT/UPDATE
         * Điều kiện: DTO hợp lệ theo validate.
         * Created By DatND (17/1/2026)
         */
        public virtual async Task<TDto> SaveAsync(TDto dto, int mode, int state)
        {
            // Validate SaveDTO
            await ValidateSaveDTO(dto, mode, state);

            // Map DTO sang entity
            var entity = MapToEntity(dto);

            // Xử lý nghiệp vụ trước khi save
            await BeforeSaveAsync(entity, dto, mode, state);

            // Validate dữ liệu chung theo attribute
            ValidateData(entity);

            // Validate nghiệp vụ riêng của service con
            await ValidateCustom(entity,dto, mode, state);

            // Gọi repo để save (tự động INSERT hoặc UPDATE)
            var savedEntity = await _repo.SaveAsync(entity);

            // Xử lý nghiệp vụ sau khi save
            await AfterSaveAsync(savedEntity, dto, mode, state);

            return MapToDto(savedEntity);
        }

        public Task<TDto> InsertAsync(TDto entity)
        {
            throw new NotImplementedException();
        }

        public Task<TDto> UpdateAsync(TDto entity)
        {
            throw new NotImplementedException();
        }
    }
}
