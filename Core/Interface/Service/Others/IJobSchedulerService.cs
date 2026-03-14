using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Interface.Service.Others
{
    public interface IJobSchedulerService
    {
        // Lên lịch kiểm tra khách không đến sau một khoảng thời gian nhất định kể từ thời điểm đặt chỗ
        string ScheduleNoShowCheck(long reservationId, TimeSpan delay);

        // Lên lịch gửi nhắc nhở trước khi đến hạn đặt chỗ
        bool CancelJob(string jobId);

        string ScheduleTableLock(long reservationId, TimeSpan delay);
    }
}
