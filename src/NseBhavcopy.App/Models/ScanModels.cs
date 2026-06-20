namespace NseBhavcopy.App.Models;

public class DailyVolumeBar
{
    public DateTime Date { get; init; }

    public decimal Open { get; init; }

    public decimal High { get; init; }

    public decimal Low { get; init; }

    public decimal Close { get; init; }

    public long Volume { get; init; }

    public static DailyVolumeBar FromDailyBar(DailyBar bar) =>
        new()
        {
            Date = bar.TradeDate,
            Open = bar.Open,
            High = bar.High,
            Low = bar.Low,
            Close = bar.Close,
            Volume = bar.Volume
        };
}

public enum CandlePatternColor
{
    Green,
    Red
}

public enum VolumeTrend
{
    Increasing,
    Decreasing
}

public enum VolumeTrendFilter
{
    All,
    Increasing,
    Decreasing
}

public static class VolumeTrendFilters
{
    public static IReadOnlyList<(VolumeTrendFilter Value, string Label)> Options { get; } =
    [
        (VolumeTrendFilter.All, "All (increasing & decreasing)"),
        (VolumeTrendFilter.Increasing, "Increasing only"),
        (VolumeTrendFilter.Decreasing, "Decreasing only")
    ];

    public static string GetLabel(VolumeTrendFilter filter) =>
        Options.First(o => o.Value == filter).Label;
}

public enum SameColorVolumePattern
{
    GreenIncreasing,
    GreenDecreasing,
    RedIncreasing,
    RedDecreasing
}

public static class SameColorVolumePatterns
{
    public static string GetLabel(SameColorVolumePattern pattern) => pattern switch
    {
        SameColorVolumePattern.GreenIncreasing => "3 Green Increasing",
        SameColorVolumePattern.GreenDecreasing => "3 Green Decreasing",
        SameColorVolumePattern.RedIncreasing => "3 Red Increasing",
        SameColorVolumePattern.RedDecreasing => "3 Red Decreasing",
        _ => pattern.ToString()
    };

    public static SameColorVolumePattern From(bool isBullish, VolumeTrend trend) =>
        (isBullish, trend) switch
        {
            (true, VolumeTrend.Increasing) => SameColorVolumePattern.GreenIncreasing,
            (true, VolumeTrend.Decreasing) => SameColorVolumePattern.GreenDecreasing,
            (false, VolumeTrend.Increasing) => SameColorVolumePattern.RedIncreasing,
            _ => SameColorVolumePattern.RedDecreasing
        };
}

public class ThreeSameColorVolumeScanCriteria
{
    public DateTime ScanDate { get; init; }

    public VolumeTrendFilter VolumeTrendFilter { get; init; } = VolumeTrendFilter.All;
}

public class ThreeSameColorVolumeResult
{
    public string Symbol { get; set; } = string.Empty;

    public DateTime ScanDate { get; set; }

    public SameColorVolumePattern Pattern { get; set; }

    public IReadOnlyList<DailyVolumeBar> Bars { get; set; } = Array.Empty<DailyVolumeBar>();

    public long OldestVolume => Bars.Count > 0 ? Bars[0].Volume : 0;

    public long NewestVolume => Bars.Count > 0 ? Bars[^1].Volume : 0;

    public string PatternLabel => SameColorVolumePatterns.GetLabel(Pattern);

    public bool IsGreen => Pattern is SameColorVolumePattern.GreenIncreasing or SameColorVolumePattern.GreenDecreasing;

    public bool IsVolumeIncreasing =>
        Pattern is SameColorVolumePattern.GreenIncreasing or SameColorVolumePattern.RedIncreasing;

    public string VolumeTrendArrows => IsVolumeIncreasing ? "↑ ↑" : "↓ ↓";

    public string CandleTrendArrow => IsGreen ? "▲" : "▼";

    public string VolumeTrendTitle => IsVolumeIncreasing
        ? "Volume increasing each day"
        : "Volume decreasing each day";
}

public class ThreeSameColorVolumeScanReport
{
    public DateTime ScanDate { get; set; }

    public VolumeTrendFilter VolumeTrendFilter { get; set; } = VolumeTrendFilter.All;

    public int TotalSymbols { get; set; }

    public int ScannedCount { get; set; }

    public string? Message { get; set; }

    public IReadOnlyList<ThreeSameColorVolumeResult> Matches { get; set; } =
        Array.Empty<ThreeSameColorVolumeResult>();

    public int GreenIncreasingCount => Count(SameColorVolumePattern.GreenIncreasing);

    public int GreenDecreasingCount => Count(SameColorVolumePattern.GreenDecreasing);

    public int RedIncreasingCount => Count(SameColorVolumePattern.RedIncreasing);

    public int RedDecreasingCount => Count(SameColorVolumePattern.RedDecreasing);

    private int Count(SameColorVolumePattern pattern) =>
        Matches.Count(m => m.Pattern == pattern);
}

public class FiveDayUnusualVolumeScanCriteria
{
    public DateTime ScanDate { get; init; }

    public int WindowDays { get; init; } = 5;

    public int BaselineLookbackDays { get; init; } = 20;

    public decimal MinVolumeMultiplier { get; init; } = 2.0m;

    public decimal? MaxVolumeMultiplier { get; init; }

    public bool IsUnusual(long currentVolume, decimal averageVolume, out decimal ratio)
    {
        ratio = averageVolume > 0 ? currentVolume / averageVolume : 0m;

        if (averageVolume <= 0)
        {
            return currentVolume > 0 && MinVolumeMultiplier <= 1;
        }

        if (ratio < MinVolumeMultiplier)
        {
            return false;
        }

        if (MaxVolumeMultiplier.HasValue && ratio > MaxVolumeMultiplier.Value)
        {
            return false;
        }

        return true;
    }
}

public class UnusualDayVolume
{
    public DateTime Date { get; init; }

    public long Volume { get; init; }

    public decimal VolumeRatio { get; init; }

    public decimal Open { get; init; }

    public decimal High { get; init; }

    public decimal Low { get; init; }

    public decimal Close { get; init; }

    public static UnusualDayVolume FromBar(DailyBar bar, decimal ratio) =>
        new()
        {
            Date = bar.TradeDate,
            Volume = bar.Volume,
            VolumeRatio = ratio,
            Open = bar.Open,
            High = bar.High,
            Low = bar.Low,
            Close = bar.Close
        };
}

public class FiveDayUnusualVolumeResult
{
    public string Symbol { get; set; } = string.Empty;

    public DateTime ScanDate { get; set; }

    public decimal AverageVolume { get; set; }

    public int BaselineSampleDays { get; set; }

    public IReadOnlyList<UnusualDayVolume> UnusualDays { get; set; } = Array.Empty<UnusualDayVolume>();

    public decimal PeakVolumeRatio => UnusualDays.Count > 0 ? UnusualDays.Max(d => d.VolumeRatio) : 0m;
}

public class FiveDayUnusualVolumeScanReport
{
    public DateTime ScanDate { get; set; }

    public int WindowDays { get; set; }

    public int BaselineLookbackDays { get; set; }

    public decimal MinVolumeMultiplier { get; set; }

    public decimal? MaxVolumeMultiplier { get; set; }

    public int TotalSymbols { get; set; }

    public int ScannedCount { get; set; }

    public string? Message { get; set; }

    public IReadOnlyList<FiveDayUnusualVolumeResult> Matches { get; set; } = Array.Empty<FiveDayUnusualVolumeResult>();
}

public class BhavcopyDownloadResult
{
    public DateTime Date { get; init; }

    public bool Success { get; init; }

    public int RowCount { get; init; }

    public string? Format { get; init; }

    public string? Error { get; init; }
}

public class BhavcopyBulkDownloadReport
{
    public DateTime FromDate { get; set; }

    public DateTime ToDate { get; set; }

    public int Attempted { get; set; }

    public int Succeeded { get; set; }

    public int Skipped { get; set; }

    public int Failed { get; set; }

    public IReadOnlyList<BhavcopyDownloadResult> Results { get; set; } = Array.Empty<BhavcopyDownloadResult>();
}

public class BhavcopyStoreStats
{
    public int TotalBars { get; set; }

    public int SymbolCount { get; set; }

    public DateTime? MinDate { get; set; }

    public DateTime? MaxDate { get; set; }
}
