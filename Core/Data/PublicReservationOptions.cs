namespace Core.Extensions;

public sealed class PublicReservationOptions
{
    public const string SectionName = "PublicReservation";

    public BookingTokenOptions BookingToken { get; set; } = new();
    public RateLimitOptions RateLimit { get; set; } = new();

    public sealed class BookingTokenOptions
    {
        public string Secret { get; set; } = "ChangeThisPublicReservationTokenSecretForProduction2026!";
        public int LifetimeMinutes { get; set; } = 5;
    }

    public sealed class RateLimitOptions
    {
        public FixedWindowOptions PhoneLookup { get; set; } = new();
        public SlidingWindowOptions FitCheck { get; set; } = new();
        public FixedWindowOptions CreateReservation { get; set; } = new();
    }

    public sealed class FixedWindowOptions
    {
        public int PermitLimit { get; set; } = 5;
        public int WindowMinutes { get; set; } = 1;
        public int QueueLimit { get; set; } = 0;
    }

    public sealed class SlidingWindowOptions
    {
        public int PermitLimit { get; set; } = 20;
        public int WindowMinutes { get; set; } = 1;
        public int SegmentsPerWindow { get; set; } = 4;
        public int QueueLimit { get; set; } = 0;
    }
}