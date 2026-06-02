namespace CardGameCalculator.Services;

public static class ProbabilityCalculator
{
    /// <summary>
    /// Computes the hypergeometric probability mass function (PMF): the probability of drawing
    /// exactly <paramref name="desiredCopies"/> copies of a card in a single draw of
    /// <paramref name="drawCount"/> cards from a deck.
    /// Formula: P(X = k) = C(K, k) * C(N - K, n - k) / C(N, n)
    /// </summary>
    /// <param name="deckSize">Total number of cards in the deck (N).</param>
    /// <param name="copiesInDeck">Number of copies of the target card in the deck (K).</param>
    /// <param name="drawCount">Number of cards drawn (n).</param>
    /// <param name="desiredCopies">Exact number of target copies to draw (k).</param>
    /// <returns>Probability in the range [0, 1], or 0 if the parameters are outside the valid support.</returns>
    public static double Hypergeometric(int deckSize, int copiesInDeck, int drawCount, int desiredCopies)
    {
        if (desiredCopies < Math.Max(0, drawCount + copiesInDeck - deckSize) || desiredCopies > Math.Min(copiesInDeck, drawCount))
            return 0.0;
        return BinomialCoefficient(copiesInDeck, desiredCopies)
            * BinomialCoefficient(deckSize - copiesInDeck, drawCount - desiredCopies)
            / BinomialCoefficient(deckSize, drawCount);
    }

    /// <summary>
    /// Computes the full hypergeometric PMF over all valid values of drawn copies,
    /// i.e. P(X = 0), P(X = 1), ..., P(X = min(K, n)).
    /// The returned array is indexed by the number of copies drawn, so index i holds P(X = i).
    /// </summary>
    /// <param name="deckSize">Total number of cards in the deck (N).</param>
    /// <param name="copiesInDeck">Number of copies of the target card in the deck (K).</param>
    /// <param name="drawCount">Number of cards drawn (n).</param>
    /// <returns>Array of probabilities of length min(K, n) + 1, summing to 1.</returns>
    public static double[] HypergeometricPmf(int deckSize, int copiesInDeck, int drawCount)
    {
        int maxDrawable = Math.Min(copiesInDeck, drawCount);
        var pmf = new double[maxDrawable + 1];
        for (int copies = 0; copies <= maxDrawable; copies++)
            pmf[copies] = Hypergeometric(deckSize, copiesInDeck, drawCount, copies);
        return pmf;
    }

    /// <summary>
    /// Computes the probability of drawing <em>at least</em> <paramref name="desiredCopies"/>
    /// copies of the target card: P(X >= k) = sum of P(X = i) for i from k to min(K, n).
    /// </summary>
    /// <param name="deckSize">Total number of cards in the deck (N).</param>
    /// <param name="copiesInDeck">Number of copies of the target card in the deck (K).</param>
    /// <param name="drawCount">Number of cards drawn (n).</param>
    /// <param name="desiredCopies">Minimum number of target copies to draw (k).</param>
    /// <returns>Cumulative probability P(X >= k) in the range [0, 1].</returns>
    public static double HypergeometricAtLeast(int deckSize, int copiesInDeck, int drawCount, int desiredCopies)
    {
        double sum = 0;
        for (int copies = desiredCopies; copies <= Math.Min(copiesInDeck, drawCount); copies++)
            sum += Hypergeometric(deckSize, copiesInDeck, drawCount, copies);
        return sum;
    }

    /// <summary>
    /// Computes the probability of drawing <em>at most</em> <paramref name="desiredCopies"/>
    /// copies of the target card: P(X <= k) = sum of P(X = i) for i from 0 to k.
    /// </summary>
    /// <param name="deckSize">Total number of cards in the deck (N).</param>
    /// <param name="copiesInDeck">Number of copies of the target card in the deck (K).</param>
    /// <param name="drawCount">Number of cards drawn (n).</param>
    /// <param name="desiredCopies">Maximum number of target copies to draw (k).</param>
    /// <returns>Cumulative probability P(X <= k) in the range [0, 1].</returns>
    public static double HypergeometricAtMost(int deckSize, int copiesInDeck, int drawCount, int desiredCopies)
    {
        double sum = 0;
        for (int copies = 0; copies <= desiredCopies; copies++)
            sum += Hypergeometric(deckSize, copiesInDeck, drawCount, copies);
        return sum;
    }

    /// <summary>
    /// Computes the multivariate hypergeometric PMF: the probability of drawing exactly
    /// <paramref name="desiredCopies"/>[i] copies from each group i simultaneously in a single
    /// draw of <paramref name="drawCount"/> cards from a deck containing multiple distinct groups.
    /// Formula: P(X₁=k₁, ..., X_G=k_G) = [∏ C(Kᵢ, kᵢ)] * C(N - ΣKᵢ, n - Σkᵢ) / C(N, n)
    /// The C(N - ΣKᵢ, n - Σkᵢ) term accounts for the remaining draws coming from cards
    /// outside any defined group.
    /// </summary>
    /// <param name="deckSize">Total number of cards in the deck (N).</param>
    /// <param name="groupSizes">Number of copies of each group in the deck (K₁, K₂, ..., K_G).</param>
    /// <param name="desiredCopies">Exact number of copies to draw from each group (k₁, k₂, ..., k_G).</param>
    /// <param name="drawCount">Total number of cards drawn (n).</param>
    /// <returns>Probability in the range [0, 1], or 0 if any parameter is outside the valid support.</returns>
    public static double MultivariateHypergeometric(
        int deckSize,
        IReadOnlyList<int> groupSizes,
        IReadOnlyList<int> desiredCopies,
        int drawCount)
    {
        if (groupSizes.Count != desiredCopies.Count) return 0;

        int totalGroupCards = groupSizes.Sum();
        int totalDesired = desiredCopies.Sum();

        if (totalGroupCards > deckSize) return 0;
        if (totalDesired > drawCount) return 0;

        double denominator = BinomialCoefficient(deckSize, drawCount);
        if (denominator == 0) return 0;

        double numerator = 1.0;
        for (int i = 0; i < groupSizes.Count; i++)
        {
            if (desiredCopies[i] < 0 || desiredCopies[i] > groupSizes[i]) return 0;
            numerator *= BinomialCoefficient(groupSizes[i], desiredCopies[i]);
        }

        numerator *= BinomialCoefficient(deckSize - totalGroupCards, drawCount - totalDesired);

        return numerator / denominator;
    }

    /// <summary>
    /// Computes the probability that every group meets its minimum in a single draw of
    /// <paramref name="drawCount"/> cards: P(X₁ >= m₁, X₂ >= m₂, ..., X_G >= m_G).
    /// Achieved by summing the multivariate PMF over all valid draw tuples (k₁, ..., k_G)
    /// where kᵢ ∈ [mᵢ, min(Kᵢ, n)] and Σkᵢ ≤ n. The enumeration is handled recursively
    /// group by group, reducing the remaining draw budget at each level.
    /// </summary>
    /// <param name="deckSize">Total number of cards in the deck (N).</param>
    /// <param name="groupSizes">Number of copies of each group in the deck (K₁, K₂, ..., K_G).</param>
    /// <param name="minimumCopies">Minimum copies required from each group (m₁, m₂, ..., m_G).</param>
    /// <param name="drawCount">Total number of cards drawn (n).</param>
    /// <returns>Probability in the range [0, 1], or 0 if the minimums are collectively impossible to satisfy.</returns>
    public static double MultivariateHypergeometricAtLeast(
        int deckSize,
        IReadOnlyList<int> groupSizes,
        IReadOnlyList<int> minimumCopies,
        int drawCount)
    {
        if (groupSizes.Count != minimumCopies.Count) return 0;

        int totalGroupCards = groupSizes.Sum();
        if (totalGroupCards > deckSize) return 0;
        if (minimumCopies.Sum() > drawCount) return 0;

        double denominator = BinomialCoefficient(deckSize, drawCount);
        if (denominator == 0) return 0;

        int nonGroupCards = deckSize - totalGroupCards;
        double numeratorSum = SumGroupCombinations(groupSizes, minimumCopies, nonGroupCards, 0, drawCount);

        return numeratorSum / denominator;
    }

    // Recursively enumerates all valid draw tuples across groups and accumulates the
    // product of binomial coefficients for each combination.
    // At the final group, multiplies by C(nonGroupCards, remainingDraws) to account
    // for the draws that come from cards outside any defined group.
    private static double SumGroupCombinations(
        IReadOnlyList<int> groupSizes,
        IReadOnlyList<int> minimumCopies,
        int nonGroupCards,
        int groupIndex,
        int remainingDraws)
    {
        if (groupIndex == groupSizes.Count)
            return BinomialCoefficient(nonGroupCards, remainingDraws);

        int min = minimumCopies[groupIndex];
        int max = Math.Min(groupSizes[groupIndex], remainingDraws);

        if (min > max) return 0;

        double sum = 0;
        for (int copies = min; copies <= max; copies++)
            sum += BinomialCoefficient(groupSizes[groupIndex], copies)
                * SumGroupCombinations(groupSizes, minimumCopies, nonGroupCards, groupIndex + 1, remainingDraws - copies);

        return sum;
    }

    /// <summary>
    /// Computes the binomial coefficient C(n, k) = n! / (k! * (n - k)!), i.e. the number of
    /// ways to choose <paramref name="subsetSize"/> items from a set of <paramref name="setSize"/>
    /// items without regard to order.
    /// Uses an iterative multiplicative formula to avoid factorial overflow.
    /// </summary>
    /// <param name="setSize">Total number of items to choose from (n).</param>
    /// <param name="subsetSize">Number of items to choose (k).</param>
    /// <returns>C(n, k) as a double, or 0 if the parameters are outside the valid range.</returns>
    private static double BinomialCoefficient(int setSize, int subsetSize)
    {
        if (subsetSize < 0 || subsetSize > setSize) return 0;
        if (subsetSize == 0 || subsetSize == setSize) return 1;
        subsetSize = Math.Min(subsetSize, setSize - subsetSize);
        double result = 1;
        for (int i = 0; i < subsetSize; i++)
        {
            result *= (setSize - i);
            result /= (i + 1);
        }
        return result;
    }
}
