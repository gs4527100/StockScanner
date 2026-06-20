using NseBhavcopy.App.Models;
using NseBhavcopy.App.Services;
using Microsoft.AspNetCore.Mvc;

namespace NseBhavcopy.App.Pages;

public class FiveDayUnusualVolumeModel : ScanPageModelBase
{
    private readonly FiveDayUnusualVolumeScanner _scanner;

    public FiveDayUnusualVolumeModel(
        FiveDayUnusualVolumeScanner scanner,
        StockUniverseCatalog catalog,
        BhavcopyStore store) : base(catalog, store)
    {
        _scanner = scanner;
    }

    [BindProperty]
    public int WindowDays { get; set; } = 5;

    [BindProperty]
    public int BaselineLookbackDays { get; set; } = 20;

    [BindProperty]
    public decimal MinVolumeMultiplier { get; set; } = 2.0m;

    public FiveDayUnusualVolumeScanReport? Report { get; private set; }

    public void OnGet()
    {
        LoadUniverseOptions();
        LoadDefaultScanDate();
    }

    public IActionResult OnPost()
    {
        LoadUniverseOptions();
        Report = _scanner.Scan(new FiveDayUnusualVolumeScanCriteria
        {
            ScanDate = ScanDate,
            WindowDays = WindowDays,
            BaselineLookbackDays = BaselineLookbackDays,
            MinVolumeMultiplier = MinVolumeMultiplier
        }, ParsedUniverse);
        return Page();
    }
}
