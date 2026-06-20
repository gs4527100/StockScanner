namespace NseBhavcopy.App.Models;

public enum BhavcopyScannerKind
{
    ThreeSameColorVolume,
    FiveDayUnusualVolume,
    WeeklyVolumeExpansion
}

public enum BiasDirection
{
    Bullish,
    Bearish
}

public enum BiasOutcome
{
    Win,
    Loss,
    Flat
}

public static class BhavcopyScannerKinds
{
    public static IReadOnlyList<BhavcopyScannerKindOption> All { get; } =
    [
        new("3-same-color", "3 Same-Color Volume", BhavcopyScannerKind.ThreeSameColorVolume,
            "Bias = candle color (green → bullish, red → bearish)."),
        new("5d-unusual-volume", "5-Day Unusual Volume", BhavcopyScannerKind.FiveDayUnusualVolume,
            "Bias = direction of the scan-date daily candle."),
        new("weekly-vol-expansion", "Weekly Volume Expansion", BhavcopyScannerKind.WeeklyVolumeExpansion,
            "Bias = current completed week candle color.")
    ];

    public static BhavcopyScannerKind Parse(string? id, BhavcopyScannerKind fallback = BhavcopyScannerKind.ThreeSameColorVolume)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return fallback;
        }

        return All.FirstOrDefault(o => string.Equals(o.Id, id, StringComparison.OrdinalIgnoreCase))?.Kind
            ?? fallback;
    }

    public static string GetId(BhavcopyScannerKind kind) =>
        All.First(o => o.Kind == kind).Id;

    public static string GetLabel(BhavcopyScannerKind kind) =>
        All.First(o => o.Kind == kind).Label;

    public static string GetPatternLabel(BhavcopyScannerKind kind, string? patternDetail = null)
    {
        if (!string.IsNullOrWhiteSpace(patternDetail))
        {
            return patternDetail;
        }

        return GetLabel(kind);
    }
}

public sealed record BhavcopyScannerKindOption(
    string Id,
    string Label,
    BhavcopyScannerKind Kind,
    string BiasDescription);

public class NextDayBiasBacktestRequest
{
    public DateTime FromDate { get; init; }

    public DateTime ToDate { get; init; }

    public BhavcopyScannerKind Scanner { get; init; } = BhavcopyScannerKind.ThreeSameColorVolume;

    /// <summary>Required when <see cref="Scanner"/> is 3 Same-Color Volume — one pattern per run.</summary>
    public SameColorVolumePattern Pattern { get; init; } = SameColorVolumePattern.GreenDecreasing;

    public int WindowDays { get; init; } = 5;

    public int BaselineLookbackDays { get; init; } = 20;

    public decimal MinVolumeMultiplier { get; init; } = 2.0m;

    public decimal? MaxVolumeMultiplier { get; init; }

    public decimal MinVolumeExpansionPercent { get; init; }

    public CandleColorFilter PreviousWeekColorFilter { get; init; } = CandleColorFilter.Any;

    public CandleColorFilter CurrentWeekColorFilter { get; init; } = CandleColorFilter.Any;
}

public class NextDayBiasSignal
{
    public BhavcopyScannerKind Scanner { get; init; }

    public string? PatternDetail { get; init; }

    public string Symbol { get; init; } = string.Empty;

    public DateTime ScanDate { get; init; }

    public BiasDirection ExpectedBias { get; init; }

    public decimal ScanDayClose { get; init; }

    public decimal NextDayClose { get; init; }

    public DateTime NextTradingDate { get; init; }

    public decimal NextDayChangePercent { get; init; }

    public BiasOutcome Outcome { get; init; }

    public bool Worked => Outcome == BiasOutcome.Win;
}

public class NextDayBiasScannerSummary
{
    public BhavcopyScannerKind Scanner { get; init; }

    public string? PatternDetail { get; init; }

    public int TotalSignals { get; init; }

    public int Wins { get; init; }

    public int Losses { get; init; }

    public int Flats { get; init; }

    public int EvaluatedSignals => Wins + Losses;

    public decimal WinRatePercent => EvaluatedSignals == 0
        ? 0m
        : Math.Round(Wins * 100m / EvaluatedSignals, 1);

    public decimal AverageNextDayMovePercent { get; init; }

    public string BiasRule { get; init; } = string.Empty;

    public string Label => BhavcopyScannerKinds.GetPatternLabel(Scanner, PatternDetail);
}

public class NextDayBiasBacktestReport
{
    public DateTime FromDate { get; set; }

    public DateTime ToDate { get; set; }

    public BhavcopyScannerKind SelectedScanner { get; set; }

    public string SelectedScannerLabel { get; set; } = string.Empty;

    public int TotalSymbols { get; set; }

    public int ScannedCount { get; set; }

    public string? Message { get; set; }

    public NextDayBiasScannerSummary? Summary { get; set; }

    public IReadOnlyList<NextDayBiasSignal> Signals { get; set; } =
        Array.Empty<NextDayBiasSignal>();
}
