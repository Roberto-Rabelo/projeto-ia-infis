using System.Text.RegularExpressions;

namespace Api.Conhecimento.Rag;

public static partial class ConsultaRagTermos
{
    public const double BoostSiglaLiteral = 0.12;
    public const double BoostTermoLiteral = 0.04;

    [GeneratedRegex(@"\b[A-Z]{2,6}\b", RegexOptions.CultureInvariant)]
    private static partial Regex SiglasMaiusculas();

    public static IReadOnlyList<string> ExtrairSiglas(string consulta)
    {
        if (string.IsNullOrWhiteSpace(consulta))
        {
            return [];
        }

        return SiglasMaiusculas()
            .Matches(consulta)
            .Select(match => match.Value)
            .Distinct(StringComparer.Ordinal)
            .ToList();
    }

    public static double CalcularBoostLiteral(DocumentChunk chunk, IReadOnlyList<string> siglas, string consulta)
    {
        var boost = 0d;

        foreach (var sigla in siglas)
        {
            if (chunk.Texto.Contains(sigla, StringComparison.OrdinalIgnoreCase))
            {
                boost += BoostSiglaLiteral;
            }
        }

        foreach (var termo in ExtrairTermosSignificativos(consulta))
        {
            if (chunk.Texto.Contains(termo, StringComparison.OrdinalIgnoreCase))
            {
                boost += BoostTermoLiteral;
            }
        }

        return boost;
    }

    private static IEnumerable<string> ExtrairTermosSignificativos(string consulta)
    {
        var normalizada = consulta
            .Replace('\n', ' ')
            .Replace('\r', ' ');

        foreach (Match match in Regex.Matches(normalizada, @"\b[\p{L}]{5,}\b"))
        {
            var termo = match.Value;
            if (EhStopword(termo))
            {
                continue;
            }

            yield return termo;
        }
    }

    private static bool EhStopword(string termo) =>
        termo.Equals("sobre", StringComparison.OrdinalIgnoreCase)
        || termo.Equals("qual", StringComparison.OrdinalIgnoreCase)
        || termo.Equals("quais", StringComparison.OrdinalIgnoreCase)
        || termo.Equals("como", StringComparison.OrdinalIgnoreCase)
        || termo.Equals("quando", StringComparison.OrdinalIgnoreCase)
        || termo.Equals("onde", StringComparison.OrdinalIgnoreCase)
        || termo.Equals("para", StringComparison.OrdinalIgnoreCase)
        || termo.Equals("pela", StringComparison.OrdinalIgnoreCase)
        || termo.Equals("pelo", StringComparison.OrdinalIgnoreCase)
        || termo.Equals("reforma", StringComparison.OrdinalIgnoreCase)
        || termo.Equals("imposto", StringComparison.OrdinalIgnoreCase);
}
