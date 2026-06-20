using NseBhavcopy.App.Models;

namespace NseBhavcopy.App.Services;

public static class WeeklyBarHelper
{
    public static DateTime GetWeekStartMonday(DateTime date)
    {
        var diff = (7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7;
        return date.Date.AddDays(-diff);
    }

    public static IReadOnlyList<WeeklyBar> AggregateCompletedWeeks(
        IEnumerable<DailyBar> dailyBars,
        DateTime scanDate)
    {
        return dailyBars
            .GroupBy(b => GetWeekStartMonday(b.TradeDate))
            .Select(g => BuildWeeklyBar(g.Key, g.OrderBy(b => b.TradeDate).ToList()))
            .Where(w => w.WeekEnd <= scanDate.Date)
            .OrderBy(w => w.WeekStart)
            .ToList();
    }

    public static WeeklyBar BuildWeeklyBar(DateTime weekStart, IReadOnlyList<DailyBar> days)
    {
        if (days.Count == 0)
        {
            throw new InvalidOperationException("Cannot build weekly bar from empty days.");
        }

        var first = days[0];
        var last = days[^1];

        return new WeeklyBar
        {
            Symbol = first.Symbol,
            WeekStart = weekStart,
            WeekEnd = last.TradeDate,
            Open = first.Open,
            High = days.Max(d => d.High),
            Low = days.Min(d => d.Low),
            Close = last.Close,
            Volume = days.Sum(d => d.Volume)
        };
    }

    public static bool TryGetLatestTwoCompletedWeeks(
        IReadOnlyList<WeeklyBar> weeks,
        out WeeklyBar currentWeek,
        out WeeklyBar previousWeek)
    {
        currentWeek = null!;
        previousWeek = null!;

        if (weeks.Count < 2)
        {
            return false;
        }

        currentWeek = weeks[^1];
        previousWeek = weeks[^2];
        return true;
    }

    public static decimal CalculateVolumeExpansionPercent(long currentVolume, long previousVolume)
    {
        if (previousVolume <= 0)
        {
            return 0;
        }

        return (currentVolume - previousVolume) / (decimal)previousVolume * 100m;
    }
}
