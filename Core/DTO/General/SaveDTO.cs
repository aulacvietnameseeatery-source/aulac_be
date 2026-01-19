
using System;

namespace Core.DTO.General
{
    public class SaveDTO<T>
    {
        public int State { get; set; }   // 1=Create, 2=Update, 4=Duplicate
        public int Mode { get; set; }

        public required T EntityDTO { get; set; }
    }

}
