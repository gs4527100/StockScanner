using NseBhavcopy.App.Models;

namespace NseBhavcopy.App.Services;

public class WeeklyVolumeExpansionScanner
{
    private const int HistoryBufferWeeks = 8;

    private readonly BhavcopyStore _store;
    private readonly StockUniverseCatalog _catalog;
    private readonly EquityCompanyNameCatalog _companyNames;

    public WeeklyVolumeExpansionScanner(
        BhavcopyStore store,
        StockUniverseCatalog catalog,
        EquityCompanyNameCatalog companyNames)
    {
        _store = store;
        _catalog = catalog;
        _companyNames = companyNames;
    }

    public Task<WeeklyVolumeExpansionScanReport> ScanAsync(
        WeeklyVolumeExpansionScanCriteria criteria,
        StockUniverse universe = StockUniverse.Nifty50,
        CancellationToken cancellationToken = default)
    {
        var validationErrors = criteria.Validate();
        var scanDate = criteria.ScanDate.Date;
        var symbols = _catalog.GetSymbols(universe);

        if (validationErrors.Count > 0)
        {
            return Task.FromResult(new WeeklyVolumeExpansionScanReport
            {
                ScanDate = scanDate,
                Criteria = criteria,
                TotalSymbols = symbols.Count,
                ValidationErrors = validationErrors,
                Message = "Fix validation errors and run the scan again."
            });
        }

        if (symbols.Count == 0)
        {
            return Task.FromResult(new WeeklyVolumeExpansionScanReport
            {
                ScanDate = scanDate,
                Criteria = criteria,
                Message = $"No symbols found for {StockUniverses.GetLabel(universe)}."
            });
        }

        var fromDate = scanDate.AddDays(-(HistoryBufferWeeks * 7));
        var barsBySymbol = _store.GetBarsGroupedBySymbol(symbols, fromDate, scanDate);
        var matches = new List<WeeklyVolumeExpansionResult>();
        var scanned = 0;

        Parallel.ForEach(
            symbols,
            new ParallelOptions { CancellationToken = cancellationToken },
            symbol =>
            {
                Interlocked.Increment(ref scanned);

                if (!barsBySymbol.TryGetValue(symbol, out var dailyBars) || dailyBars.Count == 0)
                {
                    return;
                }

                var weeks = WeeklyBarHelper.AggregateCompletedWeeks(dailyBars, scanDate);
                if (!WeeklyBarHelper.TryGetLatestTwoCompletedWeeks(weeks, out var currentWeek, out var previousWeek))
                {
                    return;
                }

                if (currentWeek.Volume <= previousWeek.Volume)
                {
                    return;
                }

                var expansionPercent = WeeklyBarHelper.CalculateVolumeExpansionPercent(
                    currentWeek.Volume,
                    previousWeek.Volume);

                if (expansionPercent < criteria.MinVolumeExpansionPercent)
                {
                    return;
                }

                if (!WeeklyCandleMetrics.MatchesColorFilter(previousWeek.Color, criteria.PreviousWeekColorFilter)
                    || !WeeklyCandleMetrics.MatchesColorFilter(currentWeek.Color, criteria.CurrentWeekColorFilter))
                {
                    return;
                }

                var metrics = WeeklyCandleMetrics.Calculate(
                    currentWeek.Open,
                    currentWeek.High,
                    currentWeek.Low,
                    currentWeek.Close);

                if (!criteria.PassesPercentFilter(metrics.BodyPercent, criteria.MinBodyPercent, criteria.MaxBodyPercent)
                    || !criteria.PassesPercentFilter(metrics.UpperWickPercent, criteria.MinUpperWickPercent, criteria.MaxUpperWickPercent)
                    || !criteria.PassesPercentFilter(metrics.LowerWickPercent, criteria.MinLowerWickPercent, criteria.MaxLowerWickPercent))
                {
                    return;
                }

                var result = new WeeklyVolumeExpansionResult
                {
                    Symbol = symbol,
                    CompanyName = _companyNames.GetCompanyName(symbol),
                    ScanDate = scanDate,
                    PreviousWeekOpen = previousWeek.Open,
                    PreviousWeekHigh = previousWeek.High,
                    PreviousWeekLow = previousWeek.Low,
                    PreviousWeekClose = previousWeek.Close,
                    PreviousWeekVolume = previousWeek.Volume,
                    PreviousWeekColor = previousWeek.Color,
                    CurrentWeekOpen = currentWeek.Open,
                    CurrentWeekHigh = currentWeek.High,
                    CurrentWeekLow = currentWeek.Low,
                    CurrentWeekClose = currentWeek.Close,
                    CurrentWeekVolume = currentWeek.Volume,
                    CurrentWeekColor = currentWeek.Color,
                    VolumeExpansionPercent = expansionPercent,
                    BodyPercent = metrics.BodyPercent,
                    UpperWickPercent = metrics.UpperWickPercent,
                    LowerWickPercent = metrics.LowerWickPercent
                };

                lock (matches)
                {
                    matches.Add(result);
                }
            });

        return Task.FromResult(new WeeklyVolumeExpansionScanReport
        {
            ScanDate = scanDate,
            Criteria = criteria,
            TotalSymbols = symbols.Count,
            ScannedCount = scanned,
            Matches = SortMatches(matches, criteria.SortBy),
            Message =
                $"Scanned {scanned}/{symbols.Count} symbols from local bhavcopy weekly aggregation. " +
                $"Matched {matches.Count} stock(s) with current-week volume greater than previous week."
        });
    }

    private static IReadOnlyList<WeeklyVolumeExpansionResult> SortMatches(
        List<WeeklyVolumeExpansionResult> matches,
        WeeklyVolumeExpansionSortBy sortBy) =>
        (sortBy switch
        {
            WeeklyVolumeExpansionSortBy.BodyPercent => matches
                .OrderByDescending(r => r.BodyPercent)
                .ThenByDescending(r => r.VolumeExpansionPercent),
            WeeklyVolumeExpansionSortBy.CurrentWeekVolume => matches
                .OrderByDescending(r => r.CurrentWeekVolume)
                .ThenByDescending(r => r.VolumeExpansionPercent),
            _ => matches
                .OrderByDescending(r => r.VolumeExpansionPercent)
                .ThenByDescending(r => r.CurrentWeekVolume)
        })
        .ThenBy(r => r.Symbol, StringComparer.OrdinalIgnoreCase)
        .ToList();
}
