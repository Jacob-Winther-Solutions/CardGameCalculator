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
