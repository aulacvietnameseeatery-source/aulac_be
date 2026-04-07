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

        /// <summary>Enqueues a Hangfire fire-and-forget job to send the customer reservation confirmation email.</summary>
        string EnqueueReservationCustomerEmail(long reservationId, string toEmail, string customerName, DateTime reservedTime, int partySize, string tableCodes);

        /// <summary>Enqueues a Hangfire fire-and-forget job to send the admin reservation notification email (fetches store email internally).</summary>
        string EnqueueReservationAdminEmail(long reservationId, string customerName, DateTime reservedTime, int partySize, string tableCodes);

        /// <summary>
        /// Enqueues one Hangfire job that sends customer + admin reservation emails concurrently.
        /// </summary>
        string EnqueueReservationEmails(long reservationId, string? customerEmail, string customerName, DateTime reservedTime, int partySize, string tableCodes);
    }
}
