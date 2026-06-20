using NseBhavcopy.App.Models;
using NseBhavcopy.App.Services;
using Microsoft.AspNetCore.Mvc;

namespace NseBhavcopy.App.Pages;

public class ThreeSameColorVolumeModel : ScanPageModelBase
{
    private readonly ThreeSameColorVolumeScanner _scanner;

    public ThreeSameColorVolumeModel(
        ThreeSameColorVolumeScanner scanner,
        StockUniverseCatalog catalog,
        BhavcopyStore store) : base(catalog, store)
    {
        _scanner = scanner;
    }

    public ThreeSameColorVolumeScanReport? Report { get; private set; }

    [BindProperty]
    public VolumeTrendFilter VolumeTrendFilter { get; set; } = VolumeTrendFilter.All;

    public void OnGet()
    {
        LoadUniverseOptions();
        LoadDefaultScanDate();
    }

    public IActionResult OnPost()
    {
        LoadUniverseOptions();
        Report = _scanner.Scan(
            new ThreeSameColorVolumeScanCriteria
            {
                ScanDate = ScanDate,
                VolumeTrendFilter = VolumeTrendFilter
            },
            ParsedUniverse);
        return Page();
    }
}
