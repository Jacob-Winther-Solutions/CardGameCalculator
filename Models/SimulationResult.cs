namespace CardGameCalculator.Models;

/// <summary>
/// Holds the output of a Monte Carlo simulation run, including the empirical probability
/// and a Wilson score confidence interval at the requested confidence level.
/// </summary>
/// <param name="Probability">Empirical probability: successes / iterations.</param>
/// <param name="ConfidenceIntervalLow">Lower bound of the Wilson score confidence interval.</param>
/// <param name="ConfidenceIntervalHigh">Upper bound of the Wilson score confidence interval.</param>
/// <param name="ConfidenceLevel">The confidence level used to compute the interval, e.g. 0.95 for 95%.</param>
/// <param name="Successes">Number of iterations in which all group minimums were met.</param>
/// <param name="Iterations">Total number of iterations run.</param>
public record SimulationResult(
    double Probability,
    double ConfidenceIntervalLow,
    double ConfidenceIntervalHigh,
    double ConfidenceLevel,
    int Successes,
    int Iterations
);
