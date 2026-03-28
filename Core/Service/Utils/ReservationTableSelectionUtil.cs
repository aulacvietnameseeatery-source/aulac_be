using Core.DTO.Reservation;
using System.Text.RegularExpressions;

namespace Core.Service.Utils;

/// <summary>
/// Utility methods for selecting table combinations for reservations.
/// </summary>
public static class ReservationTableSelectionUtil
{
    /// <summary>
    /// Finds all valid table combinations for a given party size across all zones.
    /// This includes single tables, contiguous tables, and best-fit combinations.
    /// </summary>
    public static List<ManualTableOptionDto> FindAllTableOptions(
        List<TableAvailabilityDto> availabilityPool,
        int partySize)
    {
        if (partySize <= 0 || availabilityPool.Count == 0)
        {
            return new List<ManualTableOptionDto>();
        }

        var optionMap = new Dictionary<string, ManualTableOptionDto>(StringComparer.Ordinal);

        void AddOption(List<TableAvailabilityDto> optionTables)
        {
            if (optionTables.Count == 0) return;

            var totalCapacity = optionTables.Sum(x => x.Capacity);
            if (totalCapacity < partySize) return;

            var sortedById = optionTables
                .Select(x => x.TableId)
                .OrderBy(x => x)
                .ToList();

            var key = string.Join("-", sortedById);
            if (optionMap.ContainsKey(key)) return;

            var distinctZones = optionTables
                .Select(x => x.Zone)
                .Where(z => !string.IsNullOrWhiteSpace(z))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            optionMap[key] = new ManualTableOptionDto
            {
                OptionId = key,
                TableIds = sortedById,
                TableCodes = string.Join(", ", optionTables
                    .Select(x => x.TableCode)
                    .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)),
                Zone = distinctZones.Count == 1 ? distinctZones[0] : "MIXED",
                TotalCapacity = totalCapacity,
                ExcessCapacity = totalCapacity - partySize,
                TableCount = optionTables.Count,
                IsBestFit = false,
            };
        }

        // 1. Single tables that fit
        foreach (var single in availabilityPool.Where(x => x.Capacity >= partySize))
        {
            AddOption(new List<TableAvailabilityDto> { single });
        }

        // 2. Zone-based combinations (Contiguous & Best-fit)
        foreach (var zoneGroup in availabilityPool.GroupBy(x => x.Zone))
        {
            var sortedByOrder = zoneGroup
                .OrderBy(x => ParseTableOrder(x.TableCode))
                .ThenBy(x => x.TableCode, StringComparer.OrdinalIgnoreCase)
                .ToList();

            // 2.1 Contiguous in Zone
            for (var i = 0; i < sortedByOrder.Count; i++)
            {
                var pick = new List<TableAvailabilityDto> { sortedByOrder[i] };
                var sum = sortedByOrder[i].Capacity;
                var prevOrder = ParseTableOrder(sortedByOrder[i].TableCode);

                if (sum >= partySize)
                {
                    AddOption(pick);
                    continue;
                }

                for (var j = i + 1; j < sortedByOrder.Count; j++)
                {
                    var currentOrder = ParseTableOrder(sortedByOrder[j].TableCode);
                    if (currentOrder - prevOrder > 1) break;

                    pick.Add(sortedByOrder[j]);
                    sum += sortedByOrder[j].Capacity;
                    prevOrder = currentOrder;

                    if (sum >= partySize)
                    {
                        AddOption(new List<TableAvailabilityDto>(pick));
                        break;
                    }
                }
            }

            // 2.2 Best-fit in Zone
            var bestFitInZone = SelectBestFitTablesWithinZone(sortedByOrder, partySize);
            if (bestFitInZone.Count > 0)
            {
                AddOption(bestFitInZone);
            }
        }

        if (optionMap.Count == 0) return new List<ManualTableOptionDto>();

        return optionMap.Values.ToList();
    }

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

    public static int ParseTableOrder(string tableCode)
    {
        var match = Regex.Match(tableCode, "(\\d+)(?!.*\\d)");
        if (!match.Success)
        {
            return int.MaxValue;
        }

        return int.TryParse(match.Value, out var value) ? value : int.MaxValue;
    }
}
