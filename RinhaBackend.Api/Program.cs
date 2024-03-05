using Microsoft.AspNetCore.Mvc;
using RinhaBackend.Api.Models;
using RinhaBackend.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddNpgsqlDataSource("Host=db;Username=rinha;Password=rinha;Database=rinha;Minimum Pool Size=10;Maximum Pool Size=10;Multiplexing=true;");
builder.Services.AddTransient<TransacoesService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

//app.UseHttpsRedirection();

app.MapPost("clientes/{id}/transacoes", async (
    TransacoesService service,
    int id,
    [FromBody] TransacaoRequest info) =>
{
    bool clienteExists = await service.ClienteExists(id);

    if (!clienteExists)
        return Results.NotFound();

    if (!info.Tipo.Equals("c") && !info.Tipo.Equals("d"))
        return Results.UnprocessableEntity();

    if (string.IsNullOrEmpty(info.Descricao))
        return Results.UnprocessableEntity();

    if (info.Descricao.Length < 1 || info.Descricao.Length > 10)
        return Results.UnprocessableEntity();

    return await service.RealizarTransacao(id, info);
})
.WithOpenApi();

app.MapGet("clientes/{id}/extrato", async (
    TransacoesService service,
    int id) =>
{
    bool clienteExists = await service.ClienteExists(id);

    if (!clienteExists)
        return Results.NotFound();

    return await service.GetExtrato(id);
})
.WithOpenApi();

app.Run();
