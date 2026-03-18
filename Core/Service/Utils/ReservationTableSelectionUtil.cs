using Core.DTO.Reservation;

namespace Core.Service.Utils;

/// <summary>
/// Utility methods for selecting table combinations for reservations.
/// </summary>
public static class ReservationTableSelectionUtil
{
    /// <summary>
    /// Finds the best-fit combination in a zone.
    /// Best-fit means: minimal extra capacity, then fewer tables, then stable table-code order.
    /// </summary>
    public static List<TableAvailabilityDto> SelectBestFitTablesWithinZone(
        List<TableAvailabilityDto> availableTablesInZone,
        int partySize)
    {
        if (partySize <= 0 || availableTablesInZone.Count == 0)
        {
            return new List<TableAvailabilityDto>();
        }

        var tablesSortedByCapacityThenCode = availableTablesInZone
            .OrderBy(t => t.Capacity)
            .ThenBy(t => t.TableCode, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var remainingCapacityFromIndex = new int[tablesSortedByCapacityThenCode.Count + 1];
        for (var i = tablesSortedByCapacityThenCode.Count - 1; i >= 0; i--)
        {
            remainingCapacityFromIndex[i] = remainingCapacityFromIndex[i + 1] + tablesSortedByCapacityThenCode[i].Capacity;
        }

        List<TableAvailabilityDto>? bestCombination = null;
        var bestExcessCapacity = int.MaxValue;
        var currentCombination = new List<TableAvailabilityDto>();

        void SearchCombination(int startIndex, int accumulatedCapacity)
        {
            if (accumulatedCapacity >= partySize)
            {
                var excessCapacity = accumulatedCapacity - partySize;
                if (IsPreferredCombination(currentCombination, excessCapacity, bestCombination, bestExcessCapacity))
                {
                    bestCombination = new List<TableAvailabilityDto>(currentCombination);
                    bestExcessCapacity = excessCapacity;
                }

                return;
            }

            if (startIndex >= tablesSortedByCapacityThenCode.Count)
            {
                return;
            }

            if (accumulatedCapacity + remainingCapacityFromIndex[startIndex] < partySize)
            {
                return;
            }

            currentCombination.Add(tablesSortedByCapacityThenCode[startIndex]);
            SearchCombination(startIndex + 1, accumulatedCapacity + tablesSortedByCapacityThenCode[startIndex].Capacity);
            currentCombination.RemoveAt(currentCombination.Count - 1);

            SearchCombination(startIndex + 1, accumulatedCapacity);
        }

        SearchCombination(0, 0);

        return bestCombination?
            .OrderBy(t => t.TableCode, StringComparer.OrdinalIgnoreCase)
            .ToList() ?? new List<TableAvailabilityDto>();
    }

    private static bool IsPreferredCombination(
        List<TableAvailabilityDto> candidateCombination,
        int candidateExcessCapacity,
        List<TableAvailabilityDto>? currentBestCombination,
        int currentBestExcessCapacity)
    {
        if (currentBestCombination == null)
        {
            return true;
        }

        if (candidateExcessCapacity < currentBestExcessCapacity)
        {
            return true;
        }

        if (candidateExcessCapacity > currentBestExcessCapacity)
        {
            return false;
        }

        if (candidateCombination.Count < currentBestCombination.Count)
        {
            return true;
        }

        if (candidateCombination.Count > currentBestCombination.Count)
        {
            return false;
        }

        var candidateTableCodeSignature = string.Join("|", candidateCombination
            .Select(t => t.TableCode)
            .OrderBy(code => code, StringComparer.OrdinalIgnoreCase));

        var bestTableCodeSignature = string.Join("|", currentBestCombination
            .Select(t => t.TableCode)
            .OrderBy(code => code, StringComparer.OrdinalIgnoreCase));

        return string.Compare(candidateTableCodeSignature, bestTableCodeSignature, StringComparison.OrdinalIgnoreCase) < 0;
    }
}
