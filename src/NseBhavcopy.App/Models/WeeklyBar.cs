namespace NseBhavcopy.App.Models;

public class WeeklyBar
{
    public string Symbol { get; init; } = string.Empty;

    public DateTime WeekStart { get; init; }

    public DateTime WeekEnd { get; init; }

    public decimal Open { get; init; }

    public decimal High { get; init; }

    public decimal Low { get; init; }

    public decimal Close { get; init; }

    public long Volume { get; init; }

    public WeeklyCandleColor Color => WeeklyCandleMetrics.GetColor(Open, Close);
}

public enum WeeklyCandleColor
{
    Green,
    Red,
    Doji
}

public enum CandleColorFilter
{
    Any,
    Green,
    Red,
    Doji
}

public enum WeeklyVolumeExpansionSortBy
{
    VolumeExpansionPercent,
    BodyPercent,
    CurrentWeekVolume
}

public sealed class WeeklyCandleMetrics
{
    public decimal TotalRange { get; init; }

    public decimal BodySize { get; init; }

    public decimal UpperWick { get; init; }

    public decimal LowerWick { get; init; }

    public decimal BodyPercent { get; init; }

    public decimal UpperWickPercent { get; init; }

    public decimal LowerWickPercent { get; init; }

    public static WeeklyCandleColor GetColor(decimal open, decimal close)
    {
        if (close > open)
        {
            return WeeklyCandleColor.Green;
        }

        if (close < open)
        {
            return WeeklyCandleColor.Red;
        }

        return WeeklyCandleColor.Doji;
    }

    public static string GetColorLabel(WeeklyCandleColor color) => color switch
    {
        WeeklyCandleColor.Green => "Green",
        WeeklyCandleColor.Red => "Red",
        _ => "Doji"
    };

    public static WeeklyCandleMetrics Calculate(decimal open, decimal high, decimal low, decimal close)
    {
        var totalRange = high - low;
        var bodySize = Math.Abs(close - open);
        var upperWick = high - Math.Max(open, close);
        var lowerWick = Math.Min(open, close) - low;

        if (totalRange <= 0)
        {
            return new WeeklyCandleMetrics
            {
                TotalRange = 0,
                BodySize = bodySize,
                UpperWick = upperWick,
                LowerWick = lowerWick,
                BodyPercent = 0,
                UpperWickPercent = 0,
                LowerWickPercent = 0
            };
        }

        return new WeeklyCandleMetrics
        {
            TotalRange = totalRange,
            BodySize = bodySize,
            UpperWick = upperWick,
            LowerWick = lowerWick,
            BodyPercent = bodySize / totalRange * 100m,
            UpperWickPercent = upperWick / totalRange * 100m,
            LowerWickPercent = lowerWick / totalRange * 100m
        };
    }

    public static bool MatchesColorFilter(WeeklyCandleColor color, CandleColorFilter filter) =>
        filter == CandleColorFilter.Any || color.ToString() == filter.ToString();
}
