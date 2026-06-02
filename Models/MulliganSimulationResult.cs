namespace CardGameCalculator.Models;

/// <summary>
/// Holds the output of a mulligan + looting simulation run.
/// All arrays are indexed by turn number, where index 0 represents the opening hand
/// after mulligans are resolved, and index t represents the state after turn t's draw
/// and looting effects have been applied.
/// </summary>
/// <param name="CumulativeProbabilityByTurn">P(assembled by turn t) for each index t.</param>
/// <param name="ConfidenceIntervalLow">Lower bound of the Wilson score interval at each turn.</param>
/// <param name="ConfidenceIntervalHigh">Upper bound of the Wilson score interval at each turn.</param>
/// <param name="ConfidenceLevel">The confidence level used to compute the intervals, e.g. 0.95 for 95%.</param>
/// <param name="Iterations">Total number of simulation iterations run.</param>
public record MulliganSimulationResult(
    double[] CumulativeProbabilityByTurn,
    double[] ConfidenceIntervalLow,
    double[] ConfidenceIntervalHigh,
    double ConfidenceLevel,
    int Iterations
);
