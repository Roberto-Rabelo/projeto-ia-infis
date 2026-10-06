using System.ComponentModel.DataAnnotations;

namespace Api.Atendimento.Models;

public sealed class BuscarConhecimentoRequest
{
    [Required]
    public string Consulta { get; set; } = string.Empty;
}

public sealed record BuscarConhecimentoResponse(string Resultado);

public sealed record FerramentaMcpResponse(string Nome, string? Descricao);
