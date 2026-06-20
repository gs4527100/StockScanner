namespace NseBhavcopy.App.Models;

public class WeeklyVolumeExpansionScanCriteria
{
    public DateTime ScanDate { get; init; }

    public decimal MinVolumeExpansionPercent { get; init; }

    public decimal? MinBodyPercent { get; init; }

    public decimal? MaxBodyPercent { get; init; }

    public decimal? MinUpperWickPercent { get; init; }

    public decimal? MaxUpperWickPercent { get; init; }

    public decimal? MinLowerWickPercent { get; init; }

    public decimal? MaxLowerWickPercent { get; init; }

    public CandleColorFilter PreviousWeekColorFilter { get; init; } = CandleColorFilter.Any;

    public CandleColorFilter CurrentWeekColorFilter { get; init; } = CandleColorFilter.Any;

    public WeeklyVolumeExpansionSortBy SortBy { get; init; } = WeeklyVolumeExpansionSortBy.VolumeExpansionPercent;

    public IReadOnlyList<string> Validate()
    {
        var errors = new List<string>();

        if (MinVolumeExpansionPercent < 0)
        {
            errors.Add("Minimum volume expansion % cannot be negative.");
        }

        ValidatePercentRange(errors, MinBodyPercent, MaxBodyPercent, "Body");
        ValidatePercentRange(errors, MinUpperWickPercent, MaxUpperWickPercent, "Upper wick");
        ValidatePercentRange(errors, MinLowerWickPercent, MaxLowerWickPercent, "Lower wick");

        return errors;
    }

    private static void ValidatePercentRange(
        List<string> errors,
        decimal? min,
        decimal? max,
        string label)
    {
        if (min is < 0 or > 100)
        {
            errors.Add($"{label} % minimum must be between 0 and 100.");
        }

        if (max is < 0 or > 100)
        {
            errors.Add($"{label} % maximum must be between 0 and 100.");
        }

        if (min.HasValue && max.HasValue && min.Value > max.Value)
        {
            errors.Add($"{label} % minimum cannot exceed maximum.");
        }
    }

    public bool PassesPercentFilter(decimal value, decimal? min, decimal? max)
    {
        if (min.HasValue && value < min.Value)
        {
            return false;
        }

        if (max.HasValue && value > max.Value)
        {
            return false;
        }

        return true;
    }
}

public class WeeklyVolumeExpansionResult
{
    public string Symbol { get; set; } = string.Empty;

    public string CompanyName { get; set; } = string.Empty;

    public DateTime ScanDate { get; set; }

    public decimal PreviousWeekOpen { get; set; }

    public decimal PreviousWeekHigh { get; set; }

    public decimal PreviousWeekLow { get; set; }

    public decimal PreviousWeekClose { get; set; }

    public long PreviousWeekVolume { get; set; }

    public WeeklyCandleColor PreviousWeekColor { get; set; }

    public decimal CurrentWeekOpen { get; set; }

    public decimal CurrentWeekHigh { get; set; }

    public decimal CurrentWeekLow { get; set; }

    public decimal CurrentWeekClose { get; set; }

    public long CurrentWeekVolume { get; set; }

    public WeeklyCandleColor CurrentWeekColor { get; set; }

    public decimal VolumeExpansionPercent { get; set; }

    public decimal BodyPercent { get; set; }

    public decimal UpperWickPercent { get; set; }

    public decimal LowerWickPercent { get; set; }

    public string PreviousWeekColorLabel => WeeklyCandleMetrics.GetColorLabel(PreviousWeekColor);

    public string CurrentWeekColorLabel => WeeklyCandleMetrics.GetColorLabel(CurrentWeekColor);
}

public class WeeklyVolumeExpansionScanReport
{
    public DateTime ScanDate { get; set; }

    public WeeklyVolumeExpansionScanCriteria Criteria { get; set; } = new() { ScanDate = DateTime.Today };

    public int TotalSymbols { get; set; }

    public int ScannedCount { get; set; }

    public string? Message { get; set; }

    public IReadOnlyList<string> ValidationErrors { get; set; } = Array.Empty<string>();

    public IReadOnlyList<WeeklyVolumeExpansionResult> Matches { get; set; } =
        Array.Empty<WeeklyVolumeExpansionResult>();
}
