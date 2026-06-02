namespace CardGameCalculator.Models;

public class CardGroup
{
    public string Name { get; set; } = string.Empty;
    public int CopiesInDeck { get; set; } = 4;
    public int DesiredCopies { get; set; } = 1;
}
