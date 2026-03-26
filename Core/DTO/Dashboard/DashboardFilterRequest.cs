using System;
using System.Collections.Generic;


namespace Core.DTO.Dashboard
{
    public class DashboardFilterRequest
    {
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }
}