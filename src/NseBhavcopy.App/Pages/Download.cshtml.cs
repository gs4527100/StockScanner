using NseBhavcopy.App.Models;
using NseBhavcopy.App.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace NseBhavcopy.App.Pages;

public class DownloadModel : PageModel
{
    private readonly BhavcopyDownloader _downloader;
    private readonly BhavcopyStore _store;

    public DownloadModel(BhavcopyDownloader downloader, BhavcopyStore store)
    {
        _downloader = downloader;
        _store = store;
    }

    [BindProperty]
    public DateTime FromDate { get; set; } = DateTime.Today.AddDays(-30);

    [BindProperty]
    public DateTime ToDate { get; set; } = DateTime.Today.AddDays(-1);

    public BhavcopyStoreStats Stats { get; private set; } = new();

    public BhavcopyBulkDownloadReport? Report { get; private set; }

    public string? StatusMessage { get; private set; }

    public void OnGet()
    {
        Stats = _store.GetStats();
        if (Stats.MaxDate.HasValue)
        {
            ToDate = Stats.MaxDate.Value;
            FromDate = Stats.MaxDate.Value.AddDays(-30);
        }
    }

    public async Task<IActionResult> OnPostDownloadAsync(CancellationToken cancellationToken)
    {
        Stats = _store.GetStats();
        Report = await _downloader.DownloadRangeAsync(FromDate, ToDate, cancellationToken);
        Stats = _store.GetStats();
        StatusMessage = $"Downloaded {Report.Succeeded} new file(s), skipped {Report.Skipped}, failed {Report.Failed}.";
        return Page();
    }
}
