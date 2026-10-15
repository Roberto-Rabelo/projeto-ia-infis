using Api.Conhecimento.Rag;

namespace Api.Conhecimento.Tests;

public class ConsultaRagTermosTests
{
    [Fact]
    public void ExtrairSiglas_DeveRetornarSiglasMaiusculasDistintas()
    {
        var siglas = ConsultaRagTermos.ExtrairSiglas("O que é IBS e CBS na reforma? IBS");

        Assert.Equal(2, siglas.Count);
        Assert.Contains("IBS", siglas);
        Assert.Contains("CBS", siglas);
    }

    [Fact]
    public void CalcularBoostLiteral_DevePriorizarChunkComSigla()
    {
        var chunkComSigla = new DocumentChunk("doc.pdf", 0, "O IBS substituirá parte dos tributos.", [1f]);
        var chunkSemSigla = new DocumentChunk("doc.pdf", 1, "Conteúdo genérico sobre tributos.", [1f]);
        var siglas = ConsultaRagTermos.ExtrairSiglas("Explique IBS");

        var boostCom = ConsultaRagTermos.CalcularBoostLiteral(chunkComSigla, siglas, "Explique IBS");
        var boostSem = ConsultaRagTermos.CalcularBoostLiteral(chunkSemSigla, siglas, "Explique IBS");

        Assert.True(boostCom > boostSem);
    }
}
