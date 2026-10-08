using System.Text;
using PdfSharpCore.Pdf;
using PdfSharpCore.Pdf.Content;
using PdfSharpCore.Pdf.Content.Objects;
using PdfSharpCore.Pdf.IO;

namespace Api.Conhecimento.Ingest;

public sealed class PdfSharpTextExtractor : IPdfTextExtractor
{
    public (string Texto, int Paginas) Extrair(Stream pdf)
    {
        using var documento = PdfReader.Open(pdf, PdfDocumentOpenMode.Import);
        var builder = new StringBuilder();

        foreach (PdfPage pagina in documento.Pages)
        {
            var conteudo = ContentReader.ReadContent(pagina);
            var textoPagina = new StringBuilder();
            ExtrairObjeto(conteudo, textoPagina);

            if (textoPagina.Length == 0)
            {
                continue;
            }

            if (builder.Length > 0)
            {
                builder.AppendLine();
                builder.AppendLine();
            }

            builder.Append(textoPagina.ToString().Trim());
        }

        return (builder.ToString(), documento.PageCount);
    }

    private static void ExtrairObjeto(CObject objeto, StringBuilder builder)
    {
        switch (objeto)
        {
            case COperator operador:
                if (operador.OpCode.OpCodeName is OpCodeName.Tj or OpCodeName.TJ)
                {
                    foreach (var operando in operador.Operands)
                    {
                        ExtrairObjeto(operando, builder);
                    }

                    builder.Append(' ');
                }

                break;
            case CSequence sequencia:
                foreach (var item in sequencia)
                {
                    ExtrairObjeto(item, builder);
                }

                break;
            case CString texto:
                builder.Append(texto.Value);
                break;
        }
    }
}
