using Tarefas.db;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

// Conexão
builder.Services.AddDbContext<tarefasContext>(opt =>
{
    string connectionString = builder.Configuration.GetConnectionString("tarefasConnection");
    var serverVersion = ServerVersion.AutoDetect(connectionString);
    opt.UseMySql(connectionString, serverVersion);
});

// OpenAPI (Swagger)
builder.Services.AddSwaggerGen();
builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();

    // OpenAPI (Swagger)
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Arquivos estáticos
app.UseDefaultFiles();
app.UseStaticFiles();

// Endpoints da API
app.MapGet("/api/tarefas", ([FromServices] tarefasContext _db) =>
{
    return Results.Ok(_db.Tarefa.ToList<Tarefa>());
});

app.MapGet("/api/tarefas/{id}", (
    [FromServices] tarefasContext _db,
    [FromRoute] int id
) =>
{
    // BUCAS PELO ID QUE ESTÁ NA ROTA
    var tarefa = _db.Tarefa.Find(id);

    // ENCONTROU
    if (tarefa == null)
    {
        // NÃO ENCONTROU
        // RETORNAR 404
        return Results.NotFound();
    }

    // ENCONTROU
    // RETORNAR 200 E OS DADOS DA TAREFA
    return Results.Ok(tarefa);
});

app.MapPost("/api/tarefas", (
    [FromServices] tarefasContext _db,
    [FromBody] Tarefa novaTarefa
) =>
{
    if (String.IsNullOrEmpty(novaTarefa.Descricao))
    {
        return Results.BadRequest(new { mensagem = "Informe uma descrição." });
    }

    if (novaTarefa.Concluida)
    {
        return Results.BadRequest(new { mensagem = "Cadastre uma tarefa como pendente." });
    }

    var tarefa = new Tarefa
    {
        Descricao = novaTarefa.Descricao,
        Concluida = novaTarefa.Concluida,
    };

    _db.Tarefa.Add(tarefa);
    _db.SaveChanges();

    string urlTarefaCriada = $"/api/tarefas/{tarefa.Id}";

    return Results.Created(urlTarefaCriada, tarefa);
});

app.Run();
