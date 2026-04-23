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
                runner => runner.SendReservationStatusCustomerEmailAsync(
                    reservationId, toEmail, customerName, reservedTime, partySize, tableCodes, "CONFIRM"));
        }

        public string EnqueueReservationAdminEmail(
            long reservationId,
            string customerName,
            DateTime reservedTime,
            int partySize,
            string tableCodes)
        {
            return _backgroundJobClient.Enqueue<EmailJobRunner>(
                runner => runner.SendReservationStatusAdminEmailAsync(
                    reservationId, customerName, reservedTime, partySize, tableCodes, "CONFIRM"));
        }

        public string EnqueueReservationStatusEmails(
            long reservationId,
            string? customerEmail,
            string customerName,
            DateTime reservedTime,
            int partySize,
            string tableCodes,
            string status)
        {
            return _backgroundJobClient.Enqueue<EmailJobRunner>(
                runner => runner.SendReservationStatusBothEmailsAsync(
                    reservationId, customerEmail, customerName, reservedTime, partySize, tableCodes, status));
        }
    }
}