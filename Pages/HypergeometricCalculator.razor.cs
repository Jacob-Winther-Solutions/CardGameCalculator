using CardGameCalculator.Services;
using MudBlazor;
using Plotly.Blazor;
using Plotly.Blazor.LayoutLib;
using Plotly.Blazor.LayoutLib.XAxisLib;
using Plotly.Blazor.Traces;
using Plotly.Blazor.Traces.BarLib;

namespace CardGameCalculator.Pages;

public partial class HypergeometricCalculator
{
    private int _deckSize = 60;
    private int _copiesInDeck = 4;
    private int _cardsDrawn = 7;
    private int _desiredCopies = 1;

    private bool _calculated;
    private double _exactProb;
    private double _atLeastProb;
    private double _atMostProb;

    private Config _chartConfig = new() { Responsive = true };
    private Plotly.Blazor.Layout _chartLayout = new();
    private IList<ITrace> _chartData = new List<ITrace>();

    private void Calculate()
    {
        _copiesInDeck = Math.Clamp(_copiesInDeck, 0, _deckSize);
        _cardsDrawn = Math.Clamp(_cardsDrawn, 1, _deckSize);
        _desiredCopies = Math.Clamp(_desiredCopies, 0, Math.Min(_copiesInDeck, _cardsDrawn));

        _exactProb = ProbabilityCalculator.Hypergeometric(_deckSize, _copiesInDeck, _cardsDrawn, _desiredCopies);
        _atLeastProb = ProbabilityCalculator.HypergeometricAtLeast(_deckSize, _copiesInDeck, _cardsDrawn, _desiredCopies);
        _atMostProb = ProbabilityCalculator.HypergeometricAtMost(_deckSize, _copiesInDeck, _cardsDrawn, _desiredCopies);

        var pmf = ProbabilityCalculator.HypergeometricPmf(_deckSize, _copiesInDeck, _cardsDrawn);
        var colors = pmf.Select((_, i) => i == _desiredCopies ? "#594AE2" : "#A0A0D0").ToList();

        _chartLayout = new Plotly.Blazor.Layout
        {
            ShowLegend = false,
            Margin = new Plotly.Blazor.LayoutLib.Margin { L = 60, R = 20, T = 20, B = 50 },
            PaperBgColor = "transparent",
            PlotBgColor = "transparent",
            XAxis = new List<XAxis>
            {
                new XAxis
                {
                    Title = new Plotly.Blazor.LayoutLib.XAxisLib.Title { Text = "Copies Drawn" },
                    TickMode = TickModeEnum.Linear,
                    DTick = 1
                }
            },
            YAxis = new List<YAxis>
            {
                new YAxis
                {
                    Title = new Plotly.Blazor.LayoutLib.YAxisLib.Title { Text = "Probability (%)" },
                    Range = new List<object> { 0, 100 },
                    TickSuffix = "%"
                }
            }
        };

        _chartData = new List<ITrace>
        {
            new Bar
            {
                X = Enumerable.Range(0, pmf.Length).Select(i => (object)i).ToList(),
                Y = pmf.Select(p => (object)Math.Round(p * 100, 2)).ToList(),
                Marker = new Marker { Color = colors.Select(c => (object)c).ToList() },
                HoverTemplate = "%{x} copies: %{y:.2f}%<extra></extra>"
            }
        };

        _calculated = true;
    }

    private static string CopiesLabel(int n) => n == 1 ? "copy" : "copies";
}
