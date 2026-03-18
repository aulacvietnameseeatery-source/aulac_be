using Core.Interface.Service;
using Core.Interface.Service.Others;
using Core.Interface.Service.Reservation;
using Hangfire;
using System;

namespace Infa.Service
{
    public class HangfireJobScheduler : IJobSchedulerService
    {
        private readonly IBackgroundJobClient _backgroundJobClient;

        public HangfireJobScheduler(IBackgroundJobClient backgroundJobClient)
        {
            _backgroundJobClient = backgroundJobClient;
        }

        //  (No-Show)
        public string ScheduleNoShowCheck(long reservationId, TimeSpan delay)
        {
            return _backgroundJobClient.Schedule<IAdminReservationService>(
                service => service.CheckAndMarkNoShowAsync(reservationId),
                delay);
        }

        //  (Auto-Lock)
        public string ScheduleTableLock(long reservationId, TimeSpan delay)
        {
            return _backgroundJobClient.Schedule<IAdminReservationService>(
                service => service.LockTablesForReservationAsync(reservationId),
                delay);
        }

        public bool CancelJob(string jobId)
        {
            return _backgroundJobClient.Delete(jobId);
        }
    }
}