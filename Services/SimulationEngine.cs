using CardGameCalculator.Models;

namespace CardGameCalculator.Services;

public static class SimulationEngine
{
    /// <summary>
    /// Runs a Monte Carlo simulation to empirically estimate the probability of drawing at least
    /// the minimum required copies from every group in <paramref name="drawCount"/> cards.
    /// The deck is represented as an array of nullable group indices (null for non-group cards)
    /// and is shuffled in place using Fisher-Yates on each iteration. The check uses a
    /// stack-allocated count array to avoid per-iteration heap allocations.
    /// </summary>
    /// <param name="deckSize">Total number of cards in the deck (N).</param>
    /// <param name="groupSizes">Number of copies of each group in the deck (K₁, K₂, ..., K_G).</param>
    /// <param name="minimumCopies">Minimum copies required from each group (m₁, m₂, ..., m_G).</param>
    /// <param name="drawCount">Number of cards drawn per trial (n).</param>
    /// <param name="iterations">Number of simulation iterations to run.</param>
    /// <param name="confidenceLevel">
    /// Desired confidence level for the Wilson interval, e.g. 0.95 for 95%.
    /// The corresponding z-score is computed via <see cref="NormalQuantile"/>.
    /// </param>
    /// <returns>
    /// A <see cref="SimulationResult"/> containing the empirical probability, a Wilson score
    /// confidence interval at the requested level, and the raw success and iteration counts.
    /// </returns>
    public static SimulationResult Run(
        int deckSize,
        IReadOnlyList<int> groupSizes,
        IReadOnlyList<int> minimumCopies,
        int drawCount,
        int iterations,
        double confidenceLevel = 0.95)
    {
        var deck = BuildDeck(deckSize, groupSizes);
        var rng = new Random();
        int successes = 0;

        for (int i = 0; i < iterations; i++)
        {
            Shuffle(deck, rng);
            if (MeetsMinimums(deck, drawCount, minimumCopies))
                successes++;
        }

        double probability = (double)successes / iterations;
        double z = NormalQuantile(1.0 - (1.0 - confidenceLevel) / 2.0);
        var (low, high) = WilsonConfidenceInterval(successes, iterations, z);

        return new SimulationResult(probability, low, high, confidenceLevel, successes, iterations);
    }

    private static int?[] BuildDeck(int deckSize, IReadOnlyList<int> groupSizes)
    {
        var deck = new int?[deckSize];
        int position = 0;
        for (int groupIndex = 0; groupIndex < groupSizes.Count; groupIndex++)
            for (int copy = 0; copy < groupSizes[groupIndex]; copy++)
                deck[position++] = groupIndex;
        return deck;
    }

    private static void Shuffle(int?[] deck, Random rng)
    {
        for (int i = deck.Length - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (deck[i], deck[j]) = (deck[j], deck[i]);
        }
    }

    private static bool MeetsMinimums(int?[] deck, int drawCount, IReadOnlyList<int> minimumCopies)
    {
        Span<int> drawn = stackalloc int[minimumCopies.Count];
        for (int i = 0; i < drawCount; i++)
        {
            if (deck[i] is int groupIndex)
                drawn[groupIndex]++;
        }
        for (int i = 0; i < minimumCopies.Count; i++)
            if (drawn[i] < minimumCopies[i])
                return false;
        return true;
    }

    /// <summary>
    /// Computes the Wilson score confidence interval for a binomial proportion.
    /// Preferred over the normal approximation because it remains valid near p = 0 and p = 1.
    /// Formula: (p̂ + z²/2n ± z√(p̂(1−p̂)/n + z²/4n²)) / (1 + z²/n)
    /// </summary>
    private static (double low, double high) WilsonConfidenceInterval(int successes, int iterations, double z)
    {
        double p = (double)successes / iterations;
        double denominator = 1 + z * z / iterations;
        double centre = (p + z * z / (2.0 * iterations)) / denominator;
        double margin = z / denominator * Math.Sqrt(p * (1 - p) / iterations + z * z / (4.0 * iterations * iterations));
        return (Math.Max(0, centre - margin), Math.Min(1, centre + margin));
    }

    /// <summary>
    /// Computes the quantile (inverse CDF) of the standard normal distribution.
    /// Uses the Abramowitz and Stegun rational approximation (formula 26.2.17), which has
    /// a maximum absolute error of less than 4.5 × 10⁻⁴ — sufficient for confidence intervals.
    /// For a two-sided confidence level c, pass p = 1 − (1 − c) / 2, e.g. p = 0.975 for c = 0.95.
    /// </summary>
    /// <param name="probability">A probability in the range (0, 1) (p).</param>
    /// <returns>The z-score such that P(Z ≤ z) = p for Z ~ N(0, 1).</returns>
    private static double NormalQuantile(double probability)
    {
        double t = probability < 0.5
            ? Math.Sqrt(-2.0 * Math.Log(probability))
            : Math.Sqrt(-2.0 * Math.Log(1.0 - probability));

        const double c0 = 2.515517, c1 = 0.802853, c2 = 0.010328;
        const double d1 = 1.432788, d2 = 0.189269, d3 = 0.001308;

        double z = t - (c0 + c1 * t + c2 * t * t) / (1.0 + d1 * t + d2 * t * t + d3 * t * t * t);
        return probability < 0.5 ? -z : z;
    }
}
