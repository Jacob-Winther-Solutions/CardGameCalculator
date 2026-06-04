namespace CardGameCalculator.Models;

public record FormatPreset(string Name, int DeckSize, int HandSize, int MaxCopiesPerCard, int DefaultLandCount)
{
    public static readonly FormatPreset Standard  = new("60-card Traditional",  60, 7, 4,           24);
    public static readonly FormatPreset Commander = new("100-card Singleton",   99, 7, 1,           37);
    public static readonly FormatPreset Draft     = new("40-card Limited",      40, 7, int.MaxValue, 17);

    public static readonly IReadOnlyList<FormatPreset> All = [Standard, Commander, Draft];
}
