using CardGameCalculator.Models;
using CardGameCalculator.Services;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using Plotly.Blazor;
using Plotly.Blazor.LayoutLib;
using Plotly.Blazor.Traces;
using Plotly.Blazor.Traces.ScatterLib;

namespace CardGameCalculator.Pages;

public partial class MulliganLootingCalculator
{
    [Inject] private FormatService FormatService { get; set; } = default!;

    private int _deckSize = 60;
    private int _maxMulligans = 3;

    protected override async Task OnInitializedAsync()
    {
        await FormatService.Initialize();
        _deckSize = FormatService.Current.DeckSize;
    }

    private List<CardGroup> _groups = new()
    {
        new CardGroup { Name = "Reanimation Targets", CopiesInDeck = 8, DesiredCopies = 1 },
        new CardGroup { Name = "Reanimation Spells",  CopiesInDeck = 8, DesiredCopies = 1 },
        new CardGroup { Name = "Graveyard Feeders",   CopiesInDeck = 12, DesiredCopies = 1 }
    };

    private List<LootingEffect> _lootingEffects = new()
    {
        new LootingEffect { Name = "Faithless Looting", Turn = 1, DrawCount = 2, DiscardCount = 2 }
    };

    private int _maxTurn = 5;
    private int _iterations = 100_000;
    private int _confidenceLevel = 95;

    private bool _simulated;
    private bool _simulationRunning;
    private string? _validationError;

    private MulliganSimulationResult? _baseResult;
    private MulliganSimulationResult? _lootingResult;

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

    private void ResetToFormat()
    {
        _deckSize = FormatService.Current.DeckSize;
        _simulated = false;
    }

    private void AddGroup() =>
        _groups.Add(new CardGroup { Name = $"Group {_groups.Count + 1}", CopiesInDeck = 4, DesiredCopies = 1 });

    private void RemoveGroup(CardGroup group) => _groups.Remove(group);

    private void AddLootingEffect() =>
        _lootingEffects.Add(new LootingEffect { Name = "New Effect", Turn = 1, DrawCount = 2, DiscardCount = 2 });

    private void RemoveLootingEffect(LootingEffect effect) => _lootingEffects.Remove(effect);

    private async Task RunSimulation()
    {
        if (_simulationRunning) return;

        _validationError = null;

        var groupSizes = _groups.Select(g => g.CopiesInDeck).ToList();
        var minimums   = _groups.Select(g => g.DesiredCopies).ToList();

        if (groupSizes.Sum() > _deckSize)
        {
            _validationError = $"Total copies across all groups ({groupSizes.Sum()}) exceeds deck size ({_deckSize}).";
            return;
        }
        if (_groups.Any(g => g.DesiredCopies > g.CopiesInDeck))
        {
            _validationError = "One or more groups have a desired count that exceeds their copies in deck.";
            return;
        }

        _simulationRunning = true;
        StateHasChanged();
        await Task.Delay(1);

        double confidenceLevel = _confidenceLevel / 100.0;

        _baseResult = SimulationEngine.RunWithMulligans(
            _deckSize, groupSizes, minimums, _maxMulligans,
            Array.Empty<LootingEffect>(), _maxTurn, _iterations, confidenceLevel);

        _lootingResult = _lootingEffects.Count > 0
            ? SimulationEngine.RunWithMulligans(
                _deckSize, groupSizes, minimums, _maxMulligans,
                _lootingEffects, _maxTurn, _iterations, confidenceLevel)
            : null;

        BuildChart();

        _simulated = true;
        _simulationRunning = false;
        _pendingChartUpdate = true;
    }

    private void BuildChart()
    {
        var xLabels = Enumerable.Range(0, _maxTurn + 1)
            .Select(t => t == 0 ? (object)"Opening Hand" : (object)$"Turn {t}")
            .ToList();

        _chartLayout = new Plotly.Blazor.Layout
        {
            ShowLegend = true,
            Margin = new Plotly.Blazor.LayoutLib.Margin { L = 60, R = 20, T = 20, B = 50 },
            PaperBgColor = "transparent",
            PlotBgColor = "transparent",
            XAxis = new List<XAxis>
            {
                new XAxis { Title = new Plotly.Blazor.LayoutLib.XAxisLib.Title { Text = "Game State" } }
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

        var traces = new List<ITrace>();

        if (_baseResult is not null)
        {
            traces.Add(new Scatter
            {
                X = xLabels,
                Y = _baseResult.CumulativeProbabilityByTurn.Select(p => (object)Math.Round(p * 100, 2)).ToList(),
                Mode = ModeFlag.Lines | ModeFlag.Markers,
                Name = _lootingResult is not null ? "Base (no lootings)" : "Assembled by turn",
                Line = new Line { Color = "#594AE2", Width = 2 },
                ShowLegend = true
            });
        }

        if (_lootingResult is not null)
        {
            traces.Add(new Scatter
            {
                X = xLabels,
                Y = _lootingResult.CumulativeProbabilityByTurn.Select(p => (object)Math.Round(p * 100, 2)).ToList(),
                Mode = ModeFlag.Lines | ModeFlag.Markers,
                Name = "With lootings",
                Line = new Line { Color = "#FF9800", Width = 2 },
                ShowLegend = true
            });
        }

        _chartData = traces;
    }
}
