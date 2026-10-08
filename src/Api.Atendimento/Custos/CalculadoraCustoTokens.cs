namespace Api.Atendimento.Custos;

public static class CalculadoraCustoTokens
{
    public static (decimal Entrada, decimal Saida, decimal Total) Calcular(
        int tokensEntrada,
        int tokensSaida,
        decimal precoEntradaPorMilhao,
        decimal precoSaidaPorMilhao)
    {
        var entrada = Math.Round(tokensEntrada * precoEntradaPorMilhao / 1_000_000m, 6);
        var saida = Math.Round(tokensSaida * precoSaidaPorMilhao / 1_000_000m, 6);
        return (entrada, saida, entrada + saida);
    }
}
