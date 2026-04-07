using Core.Interface.Service;
using Core.Interface.Service.Others;
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
            return _backgroundJobClient.Schedule<AdminReservationJobRunner>(
                runner => runner.CheckAndMarkNoShowAsync(reservationId),
                delay);
        }

        //  (Auto-Lock)
        public string ScheduleTableLock(long reservationId, TimeSpan delay)
        {
            return _backgroundJobClient.Schedule<AdminReservationJobRunner>(
                runner => runner.LockTablesForReservationAsync(reservationId),
                delay);
        }

        public bool CancelJob(string jobId)
        {
            return _backgroundJobClient.Delete(jobId);
        }

        public string EnqueueReservationCustomerEmail(
            long reservationId,
            string toEmail,
            string customerName,
            DateTime reservedTime,
            int partySize,
            string tableCodes)
        {
            return _backgroundJobClient.Enqueue<EmailJobRunner>(
                runner => runner.SendReservationCustomerEmailAsync(
                    reservationId, toEmail, customerName, reservedTime, partySize, tableCodes));
        }

        public string EnqueueReservationAdminEmail(
            long reservationId,
            string customerName,
            DateTime reservedTime,
            int partySize,
            string tableCodes)
        {
            return _backgroundJobClient.Enqueue<EmailJobRunner>(
                runner => runner.SendReservationAdminEmailAsync(
                    reservationId, customerName, reservedTime, partySize, tableCodes));
        }

        public string EnqueueReservationEmails(
            long reservationId,
            string? customerEmail,
            string customerName,
            DateTime reservedTime,
            int partySize,
            string tableCodes)
        {
            return _backgroundJobClient.Enqueue<EmailJobRunner>(
                runner => runner.SendReservationBothEmailsAsync(
                    reservationId, customerEmail, customerName, reservedTime, partySize, tableCodes));
        }
    }
}