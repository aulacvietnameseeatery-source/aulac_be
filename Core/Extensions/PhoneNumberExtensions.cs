using System;
using System.Collections.Generic;
using System.Linq;

namespace Core.Extensions;

public static class PhoneNumberExtensions
{
    public const string GuestPhoneSentinel = "0000000000";
    public const string SupportedPhoneValidationPattern = @"^(?:0[0-9]{9,10}|\+84[0-9]{9,10}|(?:\+41|0)[1-9][0-9]{8})$";

    public static bool IsGuestPhoneSentinel(this string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
        {
            return false;
        }

        var trimmed = phone.Trim();
        return string.Equals(trimmed, "GUEST", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(trimmed, GuestPhoneSentinel, StringComparison.Ordinal);
    }

    public static string NormalizePhoneNumber(this string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
        {
            return string.Empty;
        }

        var trimmed = phone.Trim();
        if (trimmed.IsGuestPhoneSentinel())
        {
            return GuestPhoneSentinel;
        }

        var hasLeadingPlus = trimmed.StartsWith("+", StringComparison.Ordinal);
        var digitsOnly = new string(trimmed.Where(char.IsDigit).ToArray());

        if (digitsOnly.Length == 0)
        {
            return trimmed;
        }

        if (string.Equals(digitsOnly, GuestPhoneSentinel, StringComparison.Ordinal))
        {
            return GuestPhoneSentinel;
        }

        if (hasLeadingPlus)
        {
            return $"+{digitsOnly}";
        }

        if (digitsOnly.StartsWith("84", StringComparison.Ordinal) && digitsOnly.Length is 11 or 12)
        {
            return $"+{digitsOnly}";
        }

        if (digitsOnly.StartsWith("41", StringComparison.Ordinal) && digitsOnly.Length == 11)
        {
            return $"+{digitsOnly}";
        }

        if (digitsOnly.StartsWith("0", StringComparison.Ordinal))
        {
            return digitsOnly;
        }

        return digitsOnly;
    }

    public static IReadOnlyList<string> GetPhoneLookupCandidates(this string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
        {
            return Array.Empty<string>();
        }

        var trimmed = phone.Trim();
        var normalized = phone.NormalizePhoneNumber();
        var candidates = new List<string>();

        void AddCandidate(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            if (candidates.Contains(value, StringComparer.Ordinal))
            {
                return;
            }

            candidates.Add(value);
        }

        AddCandidate(trimmed);
        AddCandidate(normalized);

        if (normalized.IsGuestPhoneSentinel())
        {
            AddCandidate("GUEST");
            AddCandidate(GuestPhoneSentinel);
            return candidates;
        }

        if (normalized.StartsWith("+84", StringComparison.Ordinal) && normalized.Length > 3)
        {
            AddCandidate($"0{normalized[3..]}");
        }

        if (normalized.StartsWith("0", StringComparison.Ordinal))
        {
            var swissLocalNumber = trimmed.StartsWith("0", StringComparison.Ordinal)
                && trimmed.Length == 10
                && trimmed[1] != '0';

            if (swissLocalNumber)
            {
                AddCandidate($"+41{normalized[1..]}");
            }

            if (normalized.Length is 10 or 11)
            {
                AddCandidate($"+84{normalized[1..]}");
            }
        }

        if (normalized.StartsWith("+41", StringComparison.Ordinal) && normalized.Length > 3)
        {
            AddCandidate($"0{normalized[3..]}");
        }

        return candidates;
    }
}