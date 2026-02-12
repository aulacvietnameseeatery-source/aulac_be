using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.DTO.General
{
	public class PagedResultDTO<T>
	{
		public List<T> PageData { get; set; } = new List<T>();
        public int PageIndex { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }

		public int TotalPage => (int)Math.Ceiling((double)TotalCount / PageSize);

	}
}
