using Core.Interface.Service.Others;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Hangfire;
using Core.Interface.Service.Entity;

namespace Infa.Service
{
    public class HangfireJobScheduler : IJobSchedulerService
    {
        private readonly IBackgroundJobClient _backgroundJobClient;

        public HangfireJobScheduler(IBackgroundJobClient backgroundJobClient)
        {
            _backgroundJobClient = backgroundJobClient;
        }

        public string ScheduleNoShowCheck(long reservationId, TimeSpan delay)
        {
            return _backgroundJobClient.Schedule<IAdminReservationService>(
                service => service.CheckAndMarkNoShowAsync(reservationId),
                delay);
        }

        public bool CancelJob(string jobId)
        {
            return _backgroundJobClient.Delete(jobId);
        }
    }
}
