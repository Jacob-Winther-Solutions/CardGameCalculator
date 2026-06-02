using CardGameCalculator.Models;
using CardGameCalculator.Services;
using MudBlazor;
using Plotly.Blazor;
using Plotly.Blazor.LayoutLib;
using Plotly.Blazor.Traces;
using Plotly.Blazor.Traces.ScatterLib;

namespace CardGameCalculator.Pages;

public partial class MultivariateCalculator
{
    private int _deckSize = 60;
    private int _drawCount = 7;

    private List<CardGroup> _groups = new()
    {
        new CardGroup { Name = "Reanimation Targets", CopiesInDeck = 8, DesiredCopies = 1 },
        new CardGroup { Name = "Reanimation Spells",  CopiesInDeck = 8, DesiredCopies = 1 },
        new CardGroup { Name = "Graveyard Feeders",   CopiesInDeck = 12, DesiredCopies = 1 }
    };

    private bool _calculated;
    private string? _validationError;
    private double _exactProb;
    private double _atLeastProb;

    private int _simulationIterations = 100_000;
    private int _confidenceLevel = 95;
    private SimulationResult? _simulationResult;
    private bool _simulationRunning; // disabled while running to prevent double-clicks

    private PlotlyChart? _chart;
    private bool _pendingChartUpdate;

    private Config _chartConfig = new() { Responsive = true };
    private Plotly.Blazor.Layout _chartLayout = new();
    private IList<ITrace> _chartData = new List<ITrace>();

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_pendingChartUpdate && _chart is not null)
        {
            _pendingChartUpdate = false;
            await _chart.React(CancellationToken.None);
        }
    }

    private void AddGroup() =>
        _groups.Add(new CardGroup { Name = $"Group {_groups.Count + 1}", CopiesInDeck = 4, DesiredCopies = 1 });

    private void RemoveGroup(CardGroup group) => _groups.Remove(group);

    private void Calculate()
    {
        _validationError = null;
        _simulationResult = null;

        var groupSizes = _groups.Select(g => g.CopiesInDeck).ToList();
        var minimums   = _groups.Select(g => g.DesiredCopies).ToList();

        int totalGroupCards = groupSizes.Sum();
        int totalDesired    = minimums.Sum();

        if (totalGroupCards > _deckSize)
        {
            _validationError = $"Total copies across all groups ({totalGroupCards}) exceeds deck size ({_deckSize}).";
            return;
        }
        if (totalDesired > _drawCount)
        {
            _validationError = $"Total desired copies ({totalDesired}) exceeds draw count ({_drawCount}).";
            return;
        }
        if (_groups.Any(g => g.DesiredCopies > g.CopiesInDeck))
        {
            _validationError = "One or more groups have a desired count that exceeds their copies in deck.";
            return;
        }

        _exactProb   = ProbabilityCalculator.MultivariateHypergeometric(_deckSize, groupSizes, minimums, _drawCount);
        _atLeastProb = ProbabilityCalculator.MultivariateHypergeometricAtLeast(_deckSize, groupSizes, minimums, _drawCount);

        BuildChart(groupSizes, minimums);

        _calculated = true;
        _pendingChartUpdate = true;
    }

    private void RunSimulation()
    {
        if (!_calculated || _simulationRunning) return;

        _simulationRunning = true;
        _simulationResult = null;

        var groupSizes = _groups.Select(g => g.CopiesInDeck).ToList();
        var minimums   = _groups.Select(g => g.DesiredCopies).ToList();

        _simulationResult = SimulationEngine.Run(
            _deckSize, groupSizes, minimums, _drawCount,
            _simulationIterations, _confidenceLevel / 100.0);

        _simulationRunning = false;
    }

    private void BuildChart(List<int> groupSizes, List<int> minimums)
    {
        int maxDraw = Math.Min(_deckSize, 40);

        var xValues = Enumerable.Range(1, maxDraw).Select(x => (object)x).ToList();
        var yValues = Enumerable.Range(1, maxDraw)
            .Select(draw => (object)Math.Round(
                ProbabilityCalculator.MultivariateHypergeometricAtLeast(_deckSize, groupSizes, minimums, draw) * 100, 2))
            .ToList();

        _chartLayout = new Plotly.Blazor.Layout
        {
            ShowLegend = true,
            Margin = new Plotly.Blazor.LayoutLib.Margin { L = 60, R = 20, T = 20, B = 50 },
            PaperBgColor = "transparent",
            PlotBgColor = "transparent",
            XAxis = new List<XAxis>
            {
                new XAxis
                {
                    Title = new Plotly.Blazor.LayoutLib.XAxisLib.Title { Text = "Cards Drawn (n)" }
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
            new Scatter
            {
                X = xValues,
                Y = yValues,
                Mode = ModeFlag.Lines,
                Name = "At least minimum of each group",
                Line = new Line { Color = "#594AE2", Width = 2 },
                ShowLegend = true
            },
            new Scatter
            {
                X = new List<object> { _drawCount },
                Y = new List<object> { Math.Round(_atLeastProb * 100, 2) },
                Mode = ModeFlag.Markers,
                Name = $"Current ({_drawCount} drawn)",
                Marker = new Marker { Size = 10, Color = "#594AE2" },
                ShowLegend = true
            },
            ReferenceLine(maxDraw, 90, "#F44336", "90%"),
            ReferenceLine(maxDraw, 75, "#FF9800", "75%"),
            ReferenceLine(maxDraw, 50, "#4CAF50", "50%")
        };
    }

    private static Scatter ReferenceLine(int maxX, double threshold, string color, string label) =>
        new Scatter
        {
            X = new List<object> { 1, maxX },
            Y = new List<object> { threshold, threshold },
            Mode = ModeFlag.Lines,
            Name = label,
            Line = new Line { Color = color, Dash = "dash", Width = 1 },
            ShowLegend = true
        };
}
