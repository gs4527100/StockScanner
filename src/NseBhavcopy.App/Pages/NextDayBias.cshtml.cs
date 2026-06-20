using NseBhavcopy.App.Configuration;
using NseBhavcopy.App.Models;
using NseBhavcopy.App.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace NseBhavcopy.App.Pages;

public class NextDayBiasModel : ScanPageModelBase
{
    private readonly NextDayBiasBacktestService _backtest;
    private readonly NextDayBiasOptions _options;
    private readonly WeeklyVolumeExpansionOptions _weeklyOptions;

    public NextDayBiasModel(
        NextDayBiasBacktestService backtest,
        StockUniverseCatalog catalog,
        BhavcopyStore store,
        IOptions<NextDayBiasOptions> options,
        IOptions<WeeklyVolumeExpansionOptions> weeklyOptions) : base(catalog, store)
    {
        _backtest = backtest;
        _options = options.Value;
        _weeklyOptions = weeklyOptions.Value;
    }

    [BindProperty]
    public DateTime FromDate { get; set; }

    [BindProperty]
    public DateTime ToDate { get; set; }

    [BindProperty]
    public string Scanner { get; set; } = BhavcopyScannerKinds.GetId(BhavcopyScannerKind.ThreeSameColorVolume);

    [BindProperty]
    public SameColorVolumePattern Pattern { get; set; } = SameColorVolumePattern.GreenDecreasing;

    [BindProperty]
    public int WindowDays { get; set; } = 5;

    [BindProperty]
    public int BaselineLookbackDays { get; set; } = 20;

    [BindProperty]
    public decimal MinVolumeMultiplier { get; set; } = 2.0m;

    [BindProperty]
    public decimal MinVolumeExpansionPercent { get; set; }

    public NextDayBiasBacktestReport? Report { get; private set; }

    public string? ErrorMessage { get; private set; }

    public BhavcopyScannerKind ParsedScanner => BhavcopyScannerKinds.Parse(Scanner);

    public void OnGet()
    {
        LoadUniverseOptions();
        LoadDefaultBacktestDates();
        MinVolumeExpansionPercent = _weeklyOptions.DefaultMinVolumeExpansionPercent;
    }

    public IActionResult OnPost()
    {
        LoadUniverseOptions();

        if (FromDate > ToDate)
        {
            ErrorMessage = "From date must be on or before to date.";
            return Page();
        }

        if ((ToDate - FromDate).Days + 1 > _options.MaxCalendarDays)
        {
            ErrorMessage = $"Date range cannot exceed {_options.MaxCalendarDays} calendar days.";
            return Page();
        }

        Report = _backtest.Run(new NextDayBiasBacktestRequest
        {
            FromDate = FromDate,
            ToDate = ToDate,
            Scanner = ParsedScanner,
            Pattern = Pattern,
            WindowDays = WindowDays,
            BaselineLookbackDays = BaselineLookbackDays,
            MinVolumeMultiplier = MinVolumeMultiplier,
            MinVolumeExpansionPercent = MinVolumeExpansionPercent
        }, ParsedUniverse);

        if (Report.Summary is null && Report.Message?.Contains("must be", StringComparison.OrdinalIgnoreCase) == true)
        {
            ErrorMessage = Report.Message;
        }

        return Page();
    }

    private void LoadDefaultBacktestDates()
    {
        var stats = Store.GetStats();
        if (!stats.MaxDate.HasValue)
        {
            ToDate = DateTime.Today.AddDays(-1);
            FromDate = ToDate.AddDays(-_options.DefaultLookbackCalendarDays);
            return;
        }

        ToDate = stats.MaxDate.Value.AddDays(-3);
        FromDate = ToDate.AddDays(-_options.DefaultLookbackCalendarDays);
    }
}
