using Npgsql;
using RinhaBackend.Api.Models;

namespace RinhaBackend.Api.Services;

public class TransacoesService(NpgsqlConnection connection)
{
    public async Task<bool> ClienteExists(int id)
    {
        await connection.OpenAsync();

        await using var commando = new NpgsqlCommand("SELECT id FROM clientes WHERE id = @Id", connection);
        commando.Parameters.AddWithValue("Id", id);
        var retorno = await commando.ExecuteScalarAsync();

        await connection.CloseAsync();
        return Convert.ToInt32(retorno) > 0;
    }

    internal async Task<IResult> GetExtrato(int id)
    {
        List<TransacoesResponse> transacoes = new();
        var saldo = 0;
        var limite = 0;

        await connection.OpenAsync();

        using (var comando1 = new NpgsqlCommand("SELECT saldo, limite FROM clientes WHERE id = @id", connection))
        {
            comando1.Parameters.AddWithValue("id", id);
            
            await using var reader1 = await comando1.ExecuteReaderAsync();
            while (await reader1.ReadAsync())
            {
                saldo = reader1.GetInt32(0);
                limite = reader1.GetInt32(1);
            }
        };

        using (var comando2 = new NpgsqlCommand("SELECT valor, tipo, descricao, data_ins FROM transacoes WHERE cliente_id = @id ORDER BY data_ins DESC LIMIT 10", connection))
        {
            comando2.Parameters.AddWithValue("id", id);

            await using var reader2 = await comando2.ExecuteReaderAsync();
            while (await reader2.ReadAsync())
            {
                transacoes.Add(new(
                    reader2.GetInt32(0),
                    reader2.GetString(1),
                    reader2.GetString(2),
                    reader2.GetDateTime(3)
                    ));
            }
        };

        await connection.CloseAsync();

        SaldoResponse saldoResponse = new(saldo, DateTime.UtcNow, limite);

        ExtratoResponse response = new(saldoResponse, transacoes);

        return Results.Ok(response);

    }

    internal async Task<IResult> RealizarTransacao(int id, TransacaoRequest info)
    {

        await connection.OpenAsync();

        await using var transaction = await connection.BeginTransactionAsync();

        var saldo = 0;
        var limite = 0;

        using (var comando1 = new NpgsqlCommand("SELECT saldo, limite FROM clientes WHERE id = @id FOR UPDATE", connection, transaction))
        {
            comando1.Parameters.AddWithValue("id", id);

            await using var reader = await comando1.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                saldo = reader.GetInt32(0);
                limite = reader.GetInt32(1);
            }
        };

        if (info.Tipo.Equals("d"))
        {
            if (saldo - info.Valor < (limite * -1))
            {
                transaction.Rollback();
                return Results.UnprocessableEntity();
            }
            else
            {
                saldo -= info.Valor;
            }
        }

        if (info.Tipo.Equals("c"))
        {
            saldo += info.Valor;
        }

        using (var batch = new NpgsqlBatch(connection, transaction)
        {
            BatchCommands =
            {
                new("UPDATE clientes set saldo = @saldo WHERE id = @id")
                {
                    Parameters =
                    {
                        new NpgsqlParameter("saldo", saldo),
                        new NpgsqlParameter("id", id)
                    }
                },
                new("INSERT INTO transacoes (cliente_id, valor, tipo, descricao, data_ins) values (@cliente_id, @valor, @tipo, @descricao, @data_ins)")
                {
                    Parameters =
                    {
                        new NpgsqlParameter("cliente_id", id),
                        new NpgsqlParameter("valor", info.Valor),
                        new NpgsqlParameter("tipo", info.Tipo),
                        new NpgsqlParameter("descricao", info.Descricao),
                        new NpgsqlParameter("data_ins", DateTime.UtcNow)
                    }
                }
            }
        })
        {
            await batch.ExecuteNonQueryAsync();
        };

        await transaction.CommitAsync();
        await connection.CloseAsync();

        return Results.Ok(new
        {
            limite,
            saldo
        });
    }

}