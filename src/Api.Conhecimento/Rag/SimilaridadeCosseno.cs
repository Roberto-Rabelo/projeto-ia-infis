namespace Api.Conhecimento.Rag;

public static class SimilaridadeCosseno
{
    public static double Calcular(IReadOnlyList<float> vetorA, IReadOnlyList<float> vetorB)
    {
        ArgumentNullException.ThrowIfNull(vetorA);
        ArgumentNullException.ThrowIfNull(vetorB);

        if (vetorA.Count != vetorB.Count)
        {
            throw new ArgumentException("Os vetores devem ter a mesma dimensão.");
        }

        if (vetorA.Count == 0)
        {
            return 0;
        }

        double produtoEscalar = 0;
        double magnitudeA = 0;
        double magnitudeB = 0;

        for (var i = 0; i < vetorA.Count; i++)
        {
            produtoEscalar += vetorA[i] * vetorB[i];
            magnitudeA += vetorA[i] * vetorA[i];
            magnitudeB += vetorB[i] * vetorB[i];
        }

        if (magnitudeA == 0 || magnitudeB == 0)
        {
            return 0;
        }

        return produtoEscalar / (Math.Sqrt(magnitudeA) * Math.Sqrt(magnitudeB));
    }
}
