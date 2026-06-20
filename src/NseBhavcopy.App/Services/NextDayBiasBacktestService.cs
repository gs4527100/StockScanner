using NseBhavcopy.App.Models;

namespace NseBhavcopy.App.Services;

public class NextDayBiasBacktestService
{
    private const int FiveDayHistoryBufferDays = 45;
    private const int WeeklyHistoryBufferWeeks = 8;

    private readonly BhavcopyStore _store;
    private readonly StockUniverseCatalog _catalog;

    public NextDayBiasBacktestService(BhavcopyStore store, StockUniverseCatalog catalog)
    {
        _store = store;
        _catalog = catalog;
    }

    public NextDayBiasBacktestReport Run(
        NextDayBiasBacktestRequest request,
        StockUniverse universe = StockUniverse.Nifty50)
    {
        var symbols = _catalog.GetSymbols(universe);
        var scanner = request.Scanner;
        var fromDate = request.FromDate.Date;
        var toDate = request.ToDate.Date;
        var report = new NextDayBiasBacktestReport
        {
            FromDate = fromDate,
            ToDate = toDate,
            SelectedScanner = scanner,
            SelectedScannerLabel = GetRunLabel(scanner, request.Pattern),
            TotalSymbols = symbols.Count
        };

        if (symbols.Count == 0)
        {
            report.Message = $"No symbols found for {StockUniverses.GetLabel(universe)}.";
            return report;
        }

        if (fromDate > toDate)
        {
            report.Message = "From date must be on or before to date.";
            return report;
        }

        var stats = _store.GetStats();
        if (!stats.MaxDate.HasValue)
        {
            report.Message = "No bhavcopy data in the database. Download data first.";
            return report;
        }

        if (toDate >= stats.MaxDate.Value)
        {
            report.Message =
                $"To date must be before the latest bhavcopy date ({stats.MaxDate:dd MMM yyyy}) so next-day outcomes are available.";
            return report;
        }

        var historyStart = fromDate.AddDays(-Math.Max(
            FiveDayHistoryBufferDays,
            WeeklyHistoryBufferWeeks * 7));
        var historyEnd = toDate.AddDays(7);

        var barsBySymbol = _store.GetBarsGroupedBySymbol(symbols, historyStart, historyEnd);
        var allSignals = new List<NextDayBiasSignal>();
        var scanned = 0;

        foreach (var symbol in symbols)
        {
            scanned++;

            if (!barsBySymbol.TryGetValue(symbol, out var symbolBars) || symbolBars.Count == 0)
            {
                continue;
            }

            var tradingDays = DailyBarHelper.GetTradingDaysInRange(symbolBars, fromDate, toDate);
            foreach (var scanDate in tradingDays)
            {
                var scanDay = DailyBarHelper.GetBarOnDate(symbolBars, scanDate);
                var nextDay = DailyBarHelper.GetNextTradingDayBar(symbolBars, scanDate);

                if (scanDay is null || nextDay is null)
                {
                    continue;
                }

                var biasResult = TryGetBias(scanner, request, scanDate, symbolBars);
                if (biasResult is null)
                {
                    continue;
                }

                var outcome = DailyBarHelper.EvaluateNextDayBias(scanDay, nextDay, biasResult.Bias);
                var nextDayChangePercent = scanDay.Close > 0
                    ? (nextDay.Close - scanDay.Close) / scanDay.Close * 100m
                    : 0m;

                allSignals.Add(new NextDayBiasSignal
                {
                    Scanner = scanner,
                    PatternDetail = biasResult.PatternDetail,
                    Symbol = symbol,
                    ScanDate = scanDate,
                    ExpectedBias = biasResult.Bias,
                    ScanDayClose = scanDay.Close,
                    NextDayClose = nextDay.Close,
                    NextTradingDate = nextDay.TradeDate,
                    NextDayChangePercent = nextDayChangePercent,
                    Outcome = outcome
                });
            }
        }

        report.Signals = allSignals
            .OrderByDescending(s => s.ScanDate)
            .ThenBy(s => s.Symbol, StringComparer.OrdinalIgnoreCase)
            .ToList();
        report.Summary = BuildSummary(scanner, request.Pattern, allSignals);
        report.ScannedCount = scanned;
        report.Message =
            $"Backtested {report.SelectedScannerLabel} on {scanned}/{symbols.Count} symbols " +
            $"({fromDate:dd MMM yyyy} – {toDate:dd MMM yyyy}). " +
            "Win = next trading day close moved in the expected direction vs scan-day close.";
        return report;
    }

    private static string GetRunLabel(BhavcopyScannerKind scanner, SameColorVolumePattern pattern) =>
        scanner == BhavcopyScannerKind.ThreeSameColorVolume
            ? SameColorVolumePatterns.GetLabel(pattern)
            : BhavcopyScannerKinds.GetLabel(scanner);

    private sealed record BiasResult(BiasDirection Bias, string? PatternDetail);

    private static BiasResult? TryGetBias(
        BhavcopyScannerKind scanner,
        NextDayBiasBacktestRequest request,
        DateTime scanDate,
        IReadOnlyList<DailyBar> symbolBars)
    {
        return scanner switch
        {
            BhavcopyScannerKind.ThreeSameColorVolume =>
                TryGetThreeSameColorBias(request.Pattern, scanDate, symbolBars),
            BhavcopyScannerKind.FiveDayUnusualVolume =>
                TryGetFiveDayUnusualVolumeBias(request, scanDate, symbolBars),
            BhavcopyScannerKind.WeeklyVolumeExpansion =>
                TryGetWeeklyVolumeExpansionBias(request, scanDate, symbolBars),
            _ => null
        };
    }

    private static BiasResult? TryGetThreeSameColorBias(
        SameColorVolumePattern pattern,
        DateTime scanDate,
        IReadOnlyList<DailyBar> symbolBars)
    {
        var bars = DailyBarHelper.GetLastTradingDayBars(symbolBars, scanDate, 3);
        if (bars.Count < 3 || bars[^1].TradeDate != scanDate)
        {
            return null;
        }

        var expectBullish = pattern is SameColorVolumePattern.GreenIncreasing
            or SameColorVolumePattern.GreenDecreasing;
        var increasing = pattern is SameColorVolumePattern.GreenIncreasing
            or SameColorVolumePattern.RedIncreasing;

        var matched = increasing
            ? DailyBarHelper.MatchesThreeIncreasingSameColorVolume(bars, out var isBullish)
              && isBullish == expectBullish
            : DailyBarHelper.MatchesThreeDecreasingSameColorVolume(bars, out isBullish)
              && isBullish == expectBullish;

        if (!matched)
        {
            return null;
        }

        return new BiasResult(
            expectBullish ? BiasDirection.Bullish : BiasDirection.Bearish,
            SameColorVolumePatterns.GetLabel(pattern));
    }

    private static BiasResult? TryGetFiveDayUnusualVolumeBias(
        NextDayBiasBacktestRequest request,
        DateTime scanDate,
        IReadOnlyList<DailyBar> symbolBars)
    {
        var fromDate = scanDate.AddDays(-FiveDayHistoryBufferDays);
        var dailyBars = DailyBarHelper.GetDailyBarsUpTo(
            symbolBars.Where(b => b.TradeDate >= fromDate),
            scanDate);

        var windowBars = dailyBars.TakeLast(request.WindowDays).ToList();
        if (windowBars.Count < request.WindowDays || windowBars[^1].TradeDate != scanDate)
        {
            return null;
        }

        var windowStart = windowBars[0].TradeDate;
        var averageVolume = DailyBarHelper.CalculateAverageDailyVolumeBefore(
            dailyBars,
            windowStart,
            request.BaselineLookbackDays);

        var sampleDays = DailyBarHelper.CountBaselineTradingDays(
            dailyBars,
            windowStart,
            request.BaselineLookbackDays);

        if (sampleDays == 0)
        {
            return null;
        }

        var criteria = new FiveDayUnusualVolumeScanCriteria
        {
            ScanDate = scanDate,
            WindowDays = request.WindowDays,
            BaselineLookbackDays = request.BaselineLookbackDays,
            MinVolumeMultiplier = request.MinVolumeMultiplier,
            MaxVolumeMultiplier = request.MaxVolumeMultiplier
        };

        var hasUnusualDay = windowBars.Any(day => criteria.IsUnusual(day.Volume, averageVolume, out _));
        if (!hasUnusualDay)
        {
            return null;
        }

        return new BiasResult(
            DailyBarHelper.DirectionFromBar(windowBars[^1]),
            null);
    }

    private static BiasResult? TryGetWeeklyVolumeExpansionBias(
        NextDayBiasBacktestRequest request,
        DateTime scanDate,
        IReadOnlyList<DailyBar> symbolBars)
    {
        var fromDate = scanDate.AddDays(-(WeeklyHistoryBufferWeeks * 7));
        var dailyBars = symbolBars.Where(b => b.TradeDate >= fromDate).ToList();
        if (dailyBars.Count == 0)
        {
            return null;
        }

        var weeks = WeeklyBarHelper.AggregateCompletedWeeks(dailyBars, scanDate);
        if (!WeeklyBarHelper.TryGetLatestTwoCompletedWeeks(weeks, out var currentWeek, out var previousWeek))
        {
            return null;
        }

        if (currentWeek.Volume <= previousWeek.Volume)
        {
            return null;
        }

        var expansionPercent = WeeklyBarHelper.CalculateVolumeExpansionPercent(
            currentWeek.Volume,
            previousWeek.Volume);

        if (expansionPercent < request.MinVolumeExpansionPercent)
        {
            return null;
        }

        if (!WeeklyCandleMetrics.MatchesColorFilter(previousWeek.Color, request.PreviousWeekColorFilter)
            || !WeeklyCandleMetrics.MatchesColorFilter(currentWeek.Color, request.CurrentWeekColorFilter))
        {
            return null;
        }

        if (currentWeek.Color == WeeklyCandleColor.Doji)
        {
            return null;
        }

        return new BiasResult(
            DailyBarHelper.DirectionFromWeeklyColor(currentWeek.Color),
            $"{WeeklyCandleMetrics.GetColorLabel(currentWeek.Color)} week (+{expansionPercent:0.#}% vol)");
    }

    private static NextDayBiasScannerSummary BuildSummary(
        BhavcopyScannerKind scanner,
        SameColorVolumePattern pattern,
        IReadOnlyList<NextDayBiasSignal> signals)
    {
        var wins = signals.Count(s => s.Outcome == BiasOutcome.Win);
        var losses = signals.Count(s => s.Outcome == BiasOutcome.Loss);
        var flats = signals.Count(s => s.Outcome == BiasOutcome.Flat);
        var evaluated = wins + losses;
        var avgMove = evaluated == 0
            ? 0m
            : Math.Round(
                signals
                    .Where(s => s.Outcome != BiasOutcome.Flat)
                    .Average(s => s.ExpectedBias == BiasDirection.Bullish
                        ? s.NextDayChangePercent
                        : -s.NextDayChangePercent),
                2);

        var patternDetail = scanner == BhavcopyScannerKind.ThreeSameColorVolume
            ? SameColorVolumePatterns.GetLabel(pattern)
            : null;

        return new NextDayBiasScannerSummary
        {
            Scanner = scanner,
            PatternDetail = patternDetail,
            TotalSignals = signals.Count,
            Wins = wins,
            Losses = losses,
            Flats = flats,
            AverageNextDayMovePercent = avgMove,
            BiasRule = BhavcopyScannerKinds.All.First(o => o.Kind == scanner).BiasDescription
        };
    }
}
