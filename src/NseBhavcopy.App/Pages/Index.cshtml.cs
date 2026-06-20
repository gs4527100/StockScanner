using NseBhavcopy.App.Models;
using NseBhavcopy.App.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace NseBhavcopy.App.Pages;

public class IndexModel : PageModel
{
    private readonly BhavcopyStore _store;
    private readonly StockUniverseCatalog _catalog;

    public IndexModel(BhavcopyStore store, StockUniverseCatalog catalog)
    {
        _store = store;
        _catalog = catalog;
    }

    public BhavcopyStoreStats Stats { get; private set; } = new();

    public IReadOnlyList<StockCategory> Universes { get; private set; } = Array.Empty<StockCategory>();

    public void OnGet()
    {
        Stats = _store.GetStats();
        Universes = _catalog.GetAll();
    }
}
