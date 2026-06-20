using NseBhavcopy.App.Models;
using NseBhavcopy.App.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace NseBhavcopy.App.Pages;

public abstract class ScanPageModelBase : PageModel
{
    private readonly StockUniverseCatalog _catalog;
    private readonly BhavcopyStore _store;

    protected ScanPageModelBase(StockUniverseCatalog catalog, BhavcopyStore store)
    {
        _catalog = catalog;
        _store = store;
    }

    [BindProperty]
    public DateTime ScanDate { get; set; } = DateTime.Today.AddDays(-1);

    [BindProperty]
    public string Universe { get; set; } = StockUniverses.GetId(StockUniverse.Nifty50);

    public IReadOnlyList<StockCategory> UniverseOptions { get; private set; } = Array.Empty<StockCategory>();

    public IReadOnlyDictionary<string, int> UniverseCounts { get; private set; } =
        new Dictionary<string, int>();

    protected void LoadUniverseOptions()
    {
        UniverseOptions = _catalog.GetAll();
        UniverseCounts = UniverseOptions.ToDictionary(
            u => u.Id,
            u => _catalog.GetSymbolCount(u.Universe));
    }

    protected void LoadDefaultScanDate()
    {
        var stats = _store.GetStats();
        if (stats.MaxDate.HasValue)
        {
            ScanDate = stats.MaxDate.Value;
        }
    }

    protected StockUniverse ParsedUniverse => StockUniverses.Parse(Universe);

    protected BhavcopyStore Store => _store;
}
