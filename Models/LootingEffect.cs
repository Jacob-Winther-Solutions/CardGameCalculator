namespace CardGameCalculator.Models;

public class LootingEffect
{
    public string Name { get; set; } = string.Empty;
    public int Turn { get; set; } = 1;
    public int DrawCount { get; set; } = 2;
    public int DiscardCount { get; set; } = 2;
}
