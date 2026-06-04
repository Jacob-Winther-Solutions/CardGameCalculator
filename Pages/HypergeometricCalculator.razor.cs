using CardGameCalculator.Models;
using CardGameCalculator.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;
using Plotly.Blazor;
using Plotly.Blazor.LayoutLib;
using Plotly.Blazor.LayoutLib.XAxisLib;
using Plotly.Blazor.Traces;
using Plotly.Blazor.Traces.BarLib;

namespace CardGameCalculator.Pages;

public partial class HypergeometricCalculator
{
    [Inject] private FormatService FormatService { get; set; } = default!;
    [Inject] private IJSRuntime JS { get; set; } = default!;

    private int _deckSize = 60;
    private int _instancesInDeck = 4;
    private int _cardsDrawn = 7;
    private int _desiredInstances = 1;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
            await JS.InvokeVoidAsync("renderMath");
    }

    protected override async Task OnInitializedAsync()
    {
        await FormatService.Initialize();
        var format = FormatService.Current;
        _deckSize = format.DeckSize;
        _cardsDrawn = format.HandSize;
        _instancesInDeck = Math.Min(_instancesInDeck, format.MaxCopiesPerCard);
    }

    private bool _calculated;
    private double _exactProb;
    private double _atLeastProb;
    private double _atMostProb;

    private Config _chartConfig = new() { Responsive = true };
    private Plotly.Blazor.Layout _chartLayout = new();
    private IList<ITrace> _chartData = new List<ITrace>();

    private void Calculate()
    {
        _instancesInDeck = Math.Clamp(_instancesInDeck, 0, _deckSize);
        _cardsDrawn = Math.Clamp(_cardsDrawn, 1, _deckSize);
        _desiredInstances = Math.Clamp(_desiredInstances, 0, Math.Min(_instancesInDeck, _cardsDrawn));

        _exactProb = ProbabilityCalculator.Hypergeometric(_deckSize, _instancesInDeck, _cardsDrawn, _desiredInstances);
        _atLeastProb = ProbabilityCalculator.HypergeometricAtLeast(_deckSize, _instancesInDeck, _cardsDrawn, _desiredInstances);
        _atMostProb = ProbabilityCalculator.HypergeometricAtMost(_deckSize, _instancesInDeck, _cardsDrawn, _desiredInstances);

        var pmf = ProbabilityCalculator.HypergeometricPmf(_deckSize, _instancesInDeck, _cardsDrawn);
        var colors = pmf.Select((_, i) => i == _desiredInstances ? "#594AE2" : "#A0A0D0").ToList();

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
                    Title = new Plotly.Blazor.LayoutLib.XAxisLib.Title { Text = "Instances Drawn" },
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
                HoverTemplate = "%{x} instances: %{y:.2f}%<extra></extra>"
            }
        };

        _calculated = true;
    }

    private int _targetProbability = 90;
    private int _advisorDesiredInHand = 1;
    private bool _advisorCalculated;
    private int? _advisorMinInstances;
    private List<AdvisorRow> _advisorRows = new();

    private void RunAdvisor()
    {
        _advisorDesiredInHand = Math.Clamp(_advisorDesiredInHand, 1, _cardsDrawn);
        double target = _targetProbability / 100.0;
        _advisorMinInstances = null;

        for (int instances = 1; instances <= _deckSize; instances++)
        {
            double prob = ProbabilityCalculator.HypergeometricAtLeast(_deckSize, instances, _cardsDrawn, _advisorDesiredInHand);
            if (prob >= target)
            {
                _advisorMinInstances = instances;
                break;
            }
        }

        int displayMax = _advisorMinInstances.HasValue
            ? Math.Min(_advisorMinInstances.Value + 2, _deckSize)
            : Math.Min(_deckSize, 20);

        _advisorRows = Enumerable.Range(1, displayMax)
            .Select(instances =>
            {
                double prob = ProbabilityCalculator.HypergeometricAtLeast(_deckSize, instances, _cardsDrawn, _advisorDesiredInHand);
                return new AdvisorRow(instances, prob, prob >= target);
            })
            .ToList();

        _advisorCalculated = true;
    }

    private void ResetToFormat()
    {
        var format = FormatService.Current;
        _deckSize = format.DeckSize;
        _cardsDrawn = format.HandSize;
        _instancesInDeck = Math.Min(4, format.MaxCopiesPerCard);
        _desiredInstances = 1;
        _calculated = false;
        _advisorCalculated = false;
    }

    private static string InstancesLabel(int n) => n == 1 ? "instance" : "instances";
}

internal record AdvisorRow(int Instances, double Probability, bool MeetsTarget);
