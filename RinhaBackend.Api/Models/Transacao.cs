namespace RinhaBackend.Api.Models;

public record TransacaoRequest(int Valor, string Tipo, string Descricao);
public record TransacaoResponse(int Limite, int Saldo);