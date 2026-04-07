namespace Core.Helpers;

/// <summary>
/// Strongly-typed options read from appsettings "Restaurant" section.
/// </summary>
public sealed class RestaurantOptions
{
    public const string SectionName = "Restaurant";

    /// <summary>
    /// IANA timezone ID for the restaurant (e.g. "Europe/Zurich").
    /// </summary>
    public string TimeZoneId { get; set; } = "Europe/Zurich";

    private TimeZoneInfo? _tz;

    /// <summary>
    /// Resolved <see cref="TimeZoneInfo"/> with a safe fallback to UTC if the configured ID is invalid.
    /// </summary>
    public TimeZoneInfo TimeZone
    {
        get
        {
            if (_tz is not null) return _tz;
            try
            {
                _tz = TimeZoneInfo.FindSystemTimeZoneById(TimeZoneId);
            }
            catch
            {
                // Fallback: if the ID is invalid, use UTC so the app still starts.
                _tz = TimeZoneInfo.Utc;
            }
            return _tz;
        }
    }
}
