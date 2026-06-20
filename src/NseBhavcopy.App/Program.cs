using NseBhavcopy.App.Configuration;
using NseBhavcopy.App.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<BhavcopyOptions>(builder.Configuration.GetSection(BhavcopyOptions.SectionName));
builder.Services.Configure<WeeklyVolumeExpansionOptions>(builder.Configuration.GetSection(WeeklyVolumeExpansionOptions.SectionName));
builder.Services.Configure<NextDayBiasOptions>(builder.Configuration.GetSection(NextDayBiasOptions.SectionName));
builder.Services.AddSingleton<BhavcopyStore>();
builder.Services.AddSingleton<BhavcopyParser>();
builder.Services.AddSingleton<StockUniverseCatalog>();
builder.Services.AddSingleton<EquityCompanyNameCatalog>();
builder.Services.AddSingleton<BhavcopyDownloader>();
builder.Services.AddSingleton<ThreeSameColorVolumeScanner>();
builder.Services.AddSingleton<FiveDayUnusualVolumeScanner>();
builder.Services.AddSingleton<WeeklyVolumeExpansionScanner>();
builder.Services.AddSingleton<NextDayBiasBacktestService>();
builder.Services.AddRazorPages();

var app = builder.Build();

app.Services.GetRequiredService<BhavcopyStore>().EnsureInitialized();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.MapRazorPages();

app.Run();
