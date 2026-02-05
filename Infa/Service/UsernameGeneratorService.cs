using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Core.Interface.Repo;
using Core.Interface.Service;

namespace Infa.Service;

/// <summary>
/// Implementation of username generator that creates usernames from full names.
/// Pattern: firstname.lastname (with collision handling via incremental numbers).
/// </summary>
public partial class UsernameGeneratorService : IUsernameGenerator
{
    private readonly IAccountRepository _accountRepository;

    public UsernameGeneratorService(IAccountRepository accountRepository)
    {
        _accountRepository = accountRepository;
    }

    /// <inheritdoc />
    public async Task<string> GenerateUniqueUsernameAsync(
        string fullName,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            throw new ArgumentException("Full name cannot be empty", nameof(fullName));

        // Step 1: Normalize the full name (remove Vietnamese diacritics)
        var normalized = RemoveDiacritics(fullName);

        // Step 2: Generate base username pattern
        var baseUsername = GenerateBaseUsername(normalized);

        // Step 3: Check for collisions and append number if needed
        var username = baseUsername;
        var counter = 2;

        while (await _accountRepository.UsernameExistsAsync(username, cancellationToken))
        {
            username = $"{baseUsername}{counter}";
            counter++;

            // Safety limit to prevent infinite loop
            if (counter > 1000)
                throw new InvalidOperationException("Unable to generate unique username after 1000 attempts");
        }

        return username;
    }

    /// <summary>
    /// Generates the base username pattern from normalized name.
    /// Pattern: firstname.lastname (or just firstname if single name).
    /// </summary>
    private static string GenerateBaseUsername(string normalizedName)
    {
        // Split into words and filter empty/whitespace
        var words = normalizedName
            .Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(w => w.Trim().ToLowerInvariant())
            .Where(w => !string.IsNullOrWhiteSpace(w))
            .ToArray();

        if (words.Length == 0)
            return "user"; // Fallback

        if (words.Length == 1)
            return SanitizeForUsername(words[0]);

        // Vietnamese naming convention: [Family] [Middle] [Given]
        // For username: use first word (family) and last word (given)
        var firstName = words[0];
        var lastName = words[^1];

        var username = $"{firstName}.{lastName}";
        return SanitizeForUsername(username);
    }

    /// <summary>
    /// Removes Vietnamese diacritics and converts to ASCII.
    /// Example: "Nguyễn Văn An" -> "Nguyen Van An"
    /// </summary>
    private static string RemoveDiacritics(string text)
    {
        // Normalize to decomposed form (separate base characters from diacritics)
        var normalizedString = text.Normalize(NormalizationForm.FormD);
        var stringBuilder = new StringBuilder();

        foreach (var c in normalizedString)
        {
            var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
            // Keep only non-spacing marks (diacritics are classified as such)
            if (unicodeCategory != UnicodeCategory.NonSpacingMark)
            {
                stringBuilder.Append(c);
            }
        }

        // Handle Vietnamese-specific characters that don't decompose properly
        return stringBuilder.ToString()
            .Normalize(NormalizationForm.FormC)
            .Replace("Đ", "D")
            .Replace("đ", "d");
    }

    /// <summary>
    /// Sanitizes username to only contain allowed characters.
    /// Allowed: lowercase letters, numbers, dots, and underscores.
    /// </summary>
    private static string SanitizeForUsername(string input)
    {
        // Replace spaces with dots
        var sanitized = input.Replace(' ', '.');

        // Remove any characters that are not alphanumeric, dot, or underscore
        sanitized = UsernameCleanupRegex().Replace(sanitized, "");

        // Remove leading/trailing dots
        sanitized = sanitized.Trim('.');

        // Collapse multiple consecutive dots into one
        sanitized = MultipleDotsRegex().Replace(sanitized, ".");

        return string.IsNullOrWhiteSpace(sanitized) ? "user" : sanitized;
    }

    [GeneratedRegex(@"[^a-z0-9._]")]
    private static partial Regex UsernameCleanupRegex();

    [GeneratedRegex(@"\.{2,}")]
    private static partial Regex MultipleDotsRegex();
}
