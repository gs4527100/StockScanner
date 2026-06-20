using NseBhavcopy.App.Configuration;
using NseBhavcopy.App.Models;
using NseBhavcopy.App.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace NseBhavcopy.App.Pages;

public class WeeklyVolumeExpansionModel : ScanPageModelBase
{
    private readonly WeeklyVolumeExpansionScanner _scanner;

    public WeeklyVolumeExpansionModel(
        WeeklyVolumeExpansionScanner scanner,
        StockUniverseCatalog catalog,
        BhavcopyStore store,
        IOptions<WeeklyVolumeExpansionOptions> options) : base(catalog, store)
    {
        _scanner = scanner;
        _options = options.Value;
    }

    private readonly WeeklyVolumeExpansionOptions _options;

    [BindProperty]
    public decimal MinVolumeExpansionPercent { get; set; }

    [BindProperty]
    public decimal? MinBodyPercent { get; set; }

    [BindProperty]
    public decimal? MaxBodyPercent { get; set; }

    [BindProperty]
    public decimal? MinUpperWickPercent { get; set; }

    [BindProperty]
    public decimal? MaxUpperWickPercent { get; set; }

    [BindProperty]
    public decimal? MinLowerWickPercent { get; set; }

    [BindProperty]
    public decimal? MaxLowerWickPercent { get; set; }

    [BindProperty]
    public CandleColorFilter PreviousWeekColorFilter { get; set; } = CandleColorFilter.Any;

    [BindProperty]
    public CandleColorFilter CurrentWeekColorFilter { get; set; } = CandleColorFilter.Any;

    [BindProperty]
    public WeeklyVolumeExpansionSortBy SortBy { get; set; } = WeeklyVolumeExpansionSortBy.VolumeExpansionPercent;

    public WeeklyVolumeExpansionScanReport? Report { get; private set; }

    public void OnGet()
    {
        LoadUniverseOptions();
        LoadDefaultScanDate();
        MinVolumeExpansionPercent = _options.DefaultMinVolumeExpansionPercent;
    }

    public async Task<IActionResult> OnPostScanAsync(CancellationToken cancellationToken)
    {
        LoadUniverseOptions();
        Report = await RunScanAsync(cancellationToken);
        return Page();
    }

    public async Task<IActionResult> OnPostExportAsync(CancellationToken cancellationToken)
    {
        LoadUniverseOptions();
        var report = await RunScanAsync(cancellationToken);

        if (report.ValidationErrors.Count > 0 || report.Matches.Count == 0)
        {
            Report = report;
            return Page();
        }

        var bytes = WeeklyVolumeExpansionCsvExporter.Export(report);
        return File(bytes, "text/csv", WeeklyVolumeExpansionCsvExporter.BuildFileName(report));
    }

    private Task<WeeklyVolumeExpansionScanReport> RunScanAsync(CancellationToken cancellationToken) =>
        _scanner.ScanAsync(BuildCriteria(), ParsedUniverse, cancellationToken);

    private WeeklyVolumeExpansionScanCriteria BuildCriteria() =>
        new()
        {
            ScanDate = ScanDate,
            MinVolumeExpansionPercent = MinVolumeExpansionPercent,
            MinBodyPercent = MinBodyPercent,
            MaxBodyPercent = MaxBodyPercent,
            MinUpperWickPercent = MinUpperWickPercent,
            MaxUpperWickPercent = MaxUpperWickPercent,
            MinLowerWickPercent = MinLowerWickPercent,
            MaxLowerWickPercent = MaxLowerWickPercent,
            PreviousWeekColorFilter = PreviousWeekColorFilter,
            CurrentWeekColorFilter = CurrentWeekColorFilter,
            SortBy = SortBy
        };
}
