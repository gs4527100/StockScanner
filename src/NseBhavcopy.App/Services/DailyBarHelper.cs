using NseBhavcopy.App.Models;

namespace NseBhavcopy.App.Services;

public static class DailyBarHelper
{
    public static IReadOnlyList<DailyBar> GetLastTradingDayBars(
        IEnumerable<DailyBar> bars,
        DateTime scanDate,
        int count)
    {
        return bars
            .Where(b => b.TradeDate <= scanDate.Date)
            .OrderBy(b => b.TradeDate)
            .TakeLast(count)
            .ToList();
    }

    public static bool MatchesThreeDecreasingSameColorVolume(IReadOnlyList<DailyBar> bars, out bool isBullish)
    {
        isBullish = false;

        if (bars.Count != 3)
        {
            return false;
        }

        var allBullish = bars.All(b => b.IsBullish);
        var allBearish = bars.All(b => b.IsBearish);

        if (!allBullish && !allBearish)
        {
            return false;
        }

        if (bars[0].Volume <= bars[1].Volume || bars[1].Volume <= bars[2].Volume)
        {
            return false;
        }

        isBullish = allBullish;
        return true;
    }

    public static bool MatchesThreeIncreasingSameColorVolume(IReadOnlyList<DailyBar> bars, out bool isBullish)
    {
        isBullish = false;

        if (bars.Count != 3)
        {
            return false;
        }

        var allBullish = bars.All(b => b.IsBullish);
        var allBearish = bars.All(b => b.IsBearish);

        if (!allBullish && !allBearish)
        {
            return false;
        }

        if (bars[0].Volume >= bars[1].Volume || bars[1].Volume >= bars[2].Volume)
        {
            return false;
        }

        isBullish = allBullish;
        return true;
    }

    public static IReadOnlyList<DailyBar> GetDailyBarsUpTo(IEnumerable<DailyBar> bars, DateTime scanDate)
    {
        return bars
            .Where(b => b.TradeDate <= scanDate.Date)
            .OrderBy(b => b.TradeDate)
            .ToList();
    }

    public static decimal CalculateAverageDailyVolumeBefore(
        IReadOnlyList<DailyBar> dailyBars,
        DateTime beforeDate,
        int tradingDayCount)
    {
        var volumes = dailyBars
            .Where(b => b.TradeDate < beforeDate.Date)
            .TakeLast(tradingDayCount)
            .Select(b => (decimal)b.Volume)
            .ToList();

        return volumes.Count == 0 ? 0m : volumes.Average();
    }

    public static int CountBaselineTradingDays(
        IReadOnlyList<DailyBar> dailyBars,
        DateTime beforeDate,
        int maxTradingDays)
    {
        return dailyBars
            .Where(b => b.TradeDate < beforeDate.Date)
            .TakeLast(maxTradingDays)
            .Select(b => b.TradeDate)
            .Distinct()
            .Count();
    }

    public static IReadOnlyList<DateTime> GetTradingDaysInRange(
        IEnumerable<DailyBar> dailyBars,
        DateTime fromDate,
        DateTime toDate)
    {
        return dailyBars
            .Where(b => b.TradeDate >= fromDate.Date && b.TradeDate <= toDate.Date)
            .Select(b => b.TradeDate)
            .Distinct()
            .OrderBy(d => d)
            .ToList();
    }

    public static DailyBar? GetBarOnDate(IEnumerable<DailyBar> dailyBars, DateTime date) =>
        dailyBars.FirstOrDefault(b => b.TradeDate == date.Date);

    public static DailyBar? GetNextTradingDayBar(IEnumerable<DailyBar> dailyBars, DateTime afterDate) =>
        dailyBars
            .Where(b => b.TradeDate > afterDate.Date)
            .OrderBy(b => b.TradeDate)
            .FirstOrDefault();

    public static BiasDirection DirectionFromBar(DailyBar bar) =>
        bar.Close >= bar.Open ? BiasDirection.Bullish : BiasDirection.Bearish;

    public static BiasDirection DirectionFromWeeklyColor(WeeklyCandleColor color) => color switch
    {
        WeeklyCandleColor.Green => BiasDirection.Bullish,
        WeeklyCandleColor.Red => BiasDirection.Bearish,
        _ => BiasDirection.Bullish
    };

    public static BiasOutcome EvaluateNextDayBias(
        DailyBar scanDay,
        DailyBar nextDay,
        BiasDirection expectedBias)
    {
        if (nextDay.Close > scanDay.Close)
        {
            return expectedBias == BiasDirection.Bullish ? BiasOutcome.Win : BiasOutcome.Loss;
        }

        if (nextDay.Close < scanDay.Close)
        {
            return expectedBias == BiasDirection.Bearish ? BiasOutcome.Win : BiasOutcome.Loss;
        }

        return BiasOutcome.Flat;
    }
}
