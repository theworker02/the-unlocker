namespace TheUnlocker.Economy;

public sealed class PublisherAccount
{
    public string PublisherId { get; init; } = "";
    public string DisplayName { get; init; } = "";
    public bool Verified { get; init; }
    public string[] CollaboratorUserIds { get; init; } = [];
    public string[] Roles { get; init; } = [];
    public string DonationUrl { get; init; } = "";
    public string DefaultLicense { get; init; } = "All Rights Reserved";
}

public sealed class PaidModListing
{
    public string ModId { get; init; } = "";
    public string Currency { get; init; } = "USD";
    public decimal Price { get; init; }
    public string LicenseId { get; init; } = "";
    public bool AllowsRefunds { get; init; } = true;
}

public sealed class PublisherRevenueReport
{
    public string PublisherId { get; init; } = "";
    public DateOnly PeriodStart { get; init; }
    public DateOnly PeriodEnd { get; init; }
    public int UnitsSold { get; init; }
    public decimal GrossRevenue { get; init; }
    public decimal PlatformFees { get; init; }
    public decimal NetRevenue => GrossRevenue - PlatformFees;
}

public sealed class PublisherEconomyService
{
    public PublisherRevenueReport EstimateRevenue(string publisherId, IEnumerable<PaidModListing> listings, IReadOnlyDictionary<string, int> salesByMod)
    {
        var gross = 0m;
        var units = 0;
        foreach (var listing in listings)
        {
            salesByMod.TryGetValue(listing.ModId, out var sold);
            units += sold;
            gross += sold * listing.Price;
        }

        return new PublisherRevenueReport
        {
            PublisherId = publisherId,
            PeriodStart = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-30)),
            PeriodEnd = DateOnly.FromDateTime(DateTime.UtcNow),
            UnitsSold = units,
            GrossRevenue = gross,
            PlatformFees = Math.Round(gross * 0.12m, 2)
        };
    }
}
