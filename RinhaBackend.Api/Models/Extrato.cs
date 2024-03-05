namespace RinhaBackend.Api.Models;

public record ExtratoResponse(SaldoResponse Saldo, List<TransacoesResponse> Ultimas_transacoes);
public record SaldoResponse(int Total, DateTime Data_Extrato, int Limite);
public record TransacoesResponse(int Valor, string Tipo, string Descricao, DateTime Realizada_em);