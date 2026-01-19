using Core.DTO;

namespace API.Models
{
    public class PagedResult<T>
    {
        public List<T> PageData { get; set; } = new List<T>();
        public int PageIndex { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPage { get; set; }
    }

}
