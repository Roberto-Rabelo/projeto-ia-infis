using Api.Conhecimento.Rag;

namespace Api.Conhecimento.Tests;

public class SimilaridadeCossenoTests
{
    [Fact]
    public void Calcular_VetoresIdenticos_DeveRetornarUm()
    {
        float[] vetor = [1f, 0f, 0f];

        var similaridade = SimilaridadeCosseno.Calcular(vetor, vetor);

        Assert.Equal(1d, similaridade, 5);
    }

    [Fact]
    public void Calcular_VetoresOrtogonais_DeveRetornarZero()
    {
        float[] vetorA = [1f, 0f];
        float[] vetorB = [0f, 1f];

        var similaridade = SimilaridadeCosseno.Calcular(vetorA, vetorB);

        Assert.Equal(0d, similaridade, 5);
    }

    [Fact]
    public void Calcular_VetoresOpostos_DeveRetornarMenosUm()
    {
        float[] vetorA = [1f, 0f];
        float[] vetorB = [-1f, 0f];

        var similaridade = SimilaridadeCosseno.Calcular(vetorA, vetorB);

        Assert.Equal(-1d, similaridade, 5);
    }

    [Fact]
    public void Calcular_VetorNulo_DeveRetornarZero()
    {
        float[] vetorA = [0f, 0f];
        float[] vetorB = [3f, 4f];

        var similaridade = SimilaridadeCosseno.Calcular(vetorA, vetorB);

        Assert.Equal(0d, similaridade, 5);
    }

    [Fact]
    public void Calcular_DimensoesDiferentes_DeveLancarExcecao()
    {
        float[] vetorA = [1f, 0f];
        float[] vetorB = [1f];

        Assert.Throws<ArgumentException>(() => SimilaridadeCosseno.Calcular(vetorA, vetorB));
    }
}
