using Core.Interface.Service.Reservation;
using Hangfire;

namespace Infa.Service;

public class AdminReservationJobRunner
{
    private readonly IAdminReservationService _adminReservationService;

    public AdminReservationJobRunner(IAdminReservationService adminReservationService)
    {
        _adminReservationService = adminReservationService;
    }

    [AutomaticRetry(Attempts = 2)]
    public Task CheckAndMarkNoShowAsync(long reservationId)
    {
        return _adminReservationService.CheckAndMarkNoShowAsync(reservationId);
    }

    [AutomaticRetry(Attempts = 2)]
    public Task LockTablesForReservationAsync(long reservationId)
    {
        return _adminReservationService.LockTablesForReservationAsync(reservationId);
    }
}