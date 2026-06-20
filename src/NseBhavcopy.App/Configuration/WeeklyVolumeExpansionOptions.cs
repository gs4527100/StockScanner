namespace NseBhavcopy.App.Configuration;

public class WeeklyVolumeExpansionOptions
{
    public const string SectionName = "WeeklyVolumeExpansion";

    public decimal DefaultMinVolumeExpansionPercent { get; set; } = 0;
}
