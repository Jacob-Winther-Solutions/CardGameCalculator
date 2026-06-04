using CardGameCalculator.Models;
using CardGameCalculator.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Plotly.Blazor;
using Plotly.Blazor.LayoutLib;
using Plotly.Blazor.LayoutLib.XAxisLib;
using Plotly.Blazor.Traces;
using Plotly.Blazor.Traces.ScatterLib;

namespace CardGameCalculator.Pages;

public partial class ManaBaseAnalyzer
{
    [Inject] private FormatService FormatService { get; set; } = default!;
    [Inject] private IJSRuntime JS { get; set; } = default!;

    private const int MaxThreshold = 5;

    private static readonly string[] TraceColors = ["#594AE2", "#3D9CD3", "#44C17E", "#E6A817", "#E25A5A"];

    private int _deckSize = 60;
    private int _landCount = 24;
    private int _handSize = 7;
    private int _maxTurns = 5;

    protected override async Task OnInitializedAsync()
    {
        await FormatService.Initialize();
        var format = FormatService.Current;
        _deckSize = format.DeckSize;
        _handSize = format.HandSize;
        _landCount = format.DefaultLandCount;
    }

    private bool _calculated;
    private bool _pendingChartUpdate;
    private List<TurnRow> _tableRows = new();

    private PlotlyChart? _chart;
    private Config _chartConfig = new() { Responsive = true };
    private Plotly.Blazor.Layout _chartLayout = new();
    private IList<ITrace> _chartData = new List<ITrace>();

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
            await JS.InvokeVoidAsync("renderMath");

        if (_pendingChartUpdate && _chart is not null)
        {
            _pendingChartUpdate = false;
            await _chart.React(CancellationToken.None);
        }
    }

    private void ResetToFormat()
    {
        var format = FormatService.Current;
        _deckSize = format.DeckSize;
        _handSize = format.HandSize;
        _landCount = format.DefaultLandCount;
        _calculated = false;
    }

    private void Calculate()
    {
        _landCount = Math.Clamp(_landCount, 0, _deckSize);
        _handSize = Math.Clamp(_handSize, 1, _deckSize);

        _tableRows = Enumerable.Range(0, _maxTurns + 1).Select(turn =>
        {
            int cardsSeen = Math.Min(_handSize + turn, _deckSize);
            double[] probabilities = Enumerable.Range(1, MaxThreshold)
                .Select(threshold => ProbabilityCalculator.HypergeometricAtLeast(_deckSize, _landCount, cardsSeen, threshold))
                .ToArray();
            double? missRate = turn == 0 ? null
                : 1 - ProbabilityCalculator.HypergeometricAtLeast(_deckSize, _landCount, cardsSeen, turn);
            return new TurnRow(turn, cardsSeen, probabilities, missRate);
        }).ToList();

        var turnLabels = _tableRows.Select(r => (object)(r.Turn == 0 ? "Hand" : $"T{r.Turn}")).ToList();

        _chartLayout = new Plotly.Blazor.Layout
        {
            ShowLegend = true,
            Margin = new Margin { L = 60, R = 20, T = 20, B = 50 },
            PaperBgColor = "transparent",
            PlotBgColor = "transparent",
            XAxis = new List<XAxis>
            {
                new XAxis { Title = new Plotly.Blazor.LayoutLib.XAxisLib.Title { Text = "Turn" } }
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

        var missLabels = _tableRows.Where(r => r.Turn > 0).Select(r => (object)$"T{r.Turn}").ToList();
        var missValues = _tableRows.Where(r => r.Turn > 0)
            .Select(r => (object)Math.Round(r.MissLandDropProbability!.Value * 100, 2)).ToList();

        var traces = Enumerable.Range(1, MaxThreshold).Select(threshold =>
            (ITrace)new Scatter
            {
                Name = $"≥{threshold} land{(threshold == 1 ? "" : "s")}",
                X = turnLabels,
                Y = _tableRows.Select(r => (object)Math.Round(r.Probabilities[threshold - 1] * 100, 2)).ToList(),
                Mode = ModeFlag.Lines | ModeFlag.Markers,
                Line = new Line { Color = TraceColors[threshold - 1], Width = 2 },
                HoverTemplate = $"≥{threshold} land{(threshold == 1 ? "" : "s")}, %{{x}}: %{{y:.1f}}%<extra></extra>"
            }
        ).ToList<ITrace>();

        traces.Add(new Scatter
        {
            Name = "Miss land drop",
            X = missLabels,
            Y = missValues,
            Mode = ModeFlag.Lines | ModeFlag.Markers,
            Line = new Line { Color = "#9E9E9E", Dash = "dash", Width = 2 },
            HoverTemplate = "Miss land drop, %{x}: %{y:.1f}%<extra></extra>"
        });

        _chartData = traces;

        _calculated = true;
        _pendingChartUpdate = true;
    }
}

internal record TurnRow(int Turn, int CardsSeen, double[] Probabilities, double? MissLandDropProbability);
