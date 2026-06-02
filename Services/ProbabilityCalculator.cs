namespace CardGameCalculator.Services;

public static class ProbabilityCalculator
{
    /// <summary>
    /// Hypergeometric PMF: probability of drawing exactly k successes.
    /// N = population size (deck), K = successes in population (copies in deck),
    /// n = sample size (cards drawn), k = observed successes (copies drawn).
    /// </summary>
    public static double Hypergeometric(int N, int K, int n, int k)
    {
        if (k < Math.Max(0, n + K - N) || k > Math.Min(K, n))
            return 0.0;
        return BinomialCoefficient(K, k) * BinomialCoefficient(N - K, n - k) / BinomialCoefficient(N, n);
    }

    public static double[] HypergeometricPmf(int N, int K, int n)
    {
        int maxK = Math.Min(K, n);
        var pmf = new double[maxK + 1];
        for (int k = 0; k <= maxK; k++)
            pmf[k] = Hypergeometric(N, K, n, k);
        return pmf;
    }

    public static double HypergeometricAtLeast(int N, int K, int n, int k)
    {
        double sum = 0;
        for (int i = k; i <= Math.Min(K, n); i++)
            sum += Hypergeometric(N, K, n, i);
        return sum;
    }

    public static double HypergeometricAtMost(int N, int K, int n, int k)
    {
        double sum = 0;
        for (int i = 0; i <= k; i++)
            sum += Hypergeometric(N, K, n, i);
        return sum;
    }

    private static double BinomialCoefficient(int n, int k)
    {
        if (k < 0 || k > n) return 0;
        if (k == 0 || k == n) return 1;
        k = Math.Min(k, n - k);
        double result = 1;
        for (int i = 0; i < k; i++)
        {
            result *= (n - i);
            result /= (i + 1);
        }
        return result;
    }
}
