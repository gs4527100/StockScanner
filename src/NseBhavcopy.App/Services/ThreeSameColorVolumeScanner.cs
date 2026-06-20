using NseBhavcopy.App.Models;

namespace NseBhavcopy.App.Services;

public class ThreeSameColorVolumeScanner
{
    private const int RequiredBars = 3;
    private const int HistoryBufferDays = 14;

    private readonly BhavcopyStore _store;
    private readonly StockUniverseCatalog _catalog;

    public ThreeSameColorVolumeScanner(BhavcopyStore store, StockUniverseCatalog catalog)
    {
        _store = store;
        _catalog = catalog;
    }

    public ThreeSameColorVolumeScanReport Scan(
        ThreeSameColorVolumeScanCriteria criteria,
        StockUniverse universe = StockUniverse.Nifty50)
    {
        var symbols = _catalog.GetSymbols(universe);
        var scanDate = criteria.ScanDate.Date;
        var fromDate = scanDate.AddDays(-HistoryBufferDays);
        var matches = new List<ThreeSameColorVolumeResult>();
        var scanned = 0;

        foreach (var symbol in symbols)
        {
            var bars = DailyBarHelper.GetLastTradingDayBars(
                _store.GetBarsForSymbol(symbol, fromDate, scanDate),
                scanDate,
                RequiredBars);

            scanned++;

            if (bars.Count < RequiredBars || bars[^1].TradeDate != scanDate)
            {
                continue;
            }

            if (!TryMatchPattern(bars, criteria.VolumeTrendFilter, out var pattern))
            {
                continue;
            }

            matches.Add(new ThreeSameColorVolumeResult
            {
                Symbol = symbol,
                ScanDate = scanDate,
                Pattern = pattern,
                Bars = bars.Select(DailyVolumeBar.FromDailyBar).ToList()
            });
        }

        var trendLabel = criteria.VolumeTrendFilter switch
        {
            VolumeTrendFilter.Increasing => "increasing volume only",
            VolumeTrendFilter.Decreasing => "decreasing volume only",
            _ => "increasing or decreasing volume"
        };

        return new ThreeSameColorVolumeScanReport
        {
            ScanDate = scanDate,
            VolumeTrendFilter = criteria.VolumeTrendFilter,
            TotalSymbols = symbols.Count,
            ScannedCount = scanned,
            Matches = matches
                .OrderBy(r => r.Pattern)
                .ThenByDescending(r => r.NewestVolume)
                .ThenBy(r => r.Symbol, StringComparer.OrdinalIgnoreCase)
                .ToList(),
            Message = $"Scanned {scanned}/{symbols.Count} symbols from local bhavcopy. " +
                        $"Filter: {VolumeTrendFilters.GetLabel(criteria.VolumeTrendFilter)}. " +
                        $"Pattern: scan date and previous 2 trading days — same color with {trendLabel}."
        };
    }

    private static bool TryMatchPattern(
        IReadOnlyList<DailyBar> bars,
        VolumeTrendFilter filter,
        out SameColorVolumePattern pattern)
    {
        pattern = default;

        if (filter is VolumeTrendFilter.All or VolumeTrendFilter.Increasing
            && DailyBarHelper.MatchesThreeIncreasingSameColorVolume(bars, out var isBullish))
        {
            pattern = SameColorVolumePatterns.From(isBullish, VolumeTrend.Increasing);
            return true;
        }

        if (filter is VolumeTrendFilter.All or VolumeTrendFilter.Decreasing
            && DailyBarHelper.MatchesThreeDecreasingSameColorVolume(bars, out isBullish))
        {
            pattern = SameColorVolumePatterns.From(isBullish, VolumeTrend.Decreasing);
            return true;
        }

        return false;
    }
}
