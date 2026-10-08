namespace Api.Conhecimento.Ingest;

public interface IPdfTextExtractor
{
    (string Texto, int Paginas) Extrair(Stream pdf);
}
