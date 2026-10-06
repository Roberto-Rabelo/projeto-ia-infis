using Api.Conhecimento.Options;
using Microsoft.Extensions.Options;

namespace Api.Conhecimento.Rag;

public sealed class MarkdownChunker : IMarkdownChunker
{
    private readonly RagOptions _options;

    public MarkdownChunker(IOptions<RagOptions> options)
    {
        _options = options.Value;
    }

    public IReadOnlyList<string> Dividir(string conteudo)
    {
        if (string.IsNullOrWhiteSpace(conteudo))
        {
            return [];
        }

        var blocos = conteudo
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Split("\n\n", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var chunks = new List<string>();
        var atual = new List<string>();
        var tamanhoAtual = 0;

        foreach (var bloco in blocos)
        {
            if (bloco.Length > _options.TamanhoMaximoChunk)
            {
                if (atual.Count > 0)
                {
                    chunks.Add(string.Join("\n\n", atual));
                    atual.Clear();
                    tamanhoAtual = 0;
                }

                chunks.AddRange(DividirBlocoLongo(bloco));
                continue;
            }

            var tamanhoComBloco = tamanhoAtual == 0 ? bloco.Length : tamanhoAtual + 2 + bloco.Length;
            if (tamanhoComBloco > _options.TamanhoMaximoChunk && atual.Count > 0)
            {
                chunks.Add(string.Join("\n\n", atual));
                AplicarSobreposicao(atual, ref tamanhoAtual);
            }

            atual.Add(bloco);
            tamanhoAtual = atual.Sum(parte => parte.Length) + Math.Max(0, (atual.Count - 1) * 2);
        }

        if (atual.Count > 0)
        {
            chunks.Add(string.Join("\n\n", atual));
        }

        return chunks;
    }

    private IEnumerable<string> DividirBlocoLongo(string bloco)
    {
        var tamanho = _options.TamanhoMaximoChunk;
        var passo = Math.Max(1, tamanho - _options.SobreposicaoChunk);

        for (var i = 0; i < bloco.Length; i += passo)
        {
            var comprimento = Math.Min(tamanho, bloco.Length - i);
            yield return bloco.Substring(i, comprimento).Trim();

            if (i + comprimento >= bloco.Length)
            {
                yield break;
            }
        }
    }

    private void AplicarSobreposicao(List<string> atual, ref int tamanhoAtual)
    {
        if (_options.SobreposicaoChunk <= 0 || atual.Count == 0)
        {
            atual.Clear();
            tamanhoAtual = 0;
            return;
        }

        var texto = string.Join("\n\n", atual);
        var sobreposicao = texto.Length <= _options.SobreposicaoChunk
            ? texto
            : texto[^_options.SobreposicaoChunk..];

        atual.Clear();
        atual.Add(sobreposicao.Trim());
        tamanhoAtual = atual[0].Length;
    }
}
