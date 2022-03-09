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
builder.Services.AddSwaggerGen(c =>
{
    c.ResolveConflictingActions(apiDescriptions => apiDescriptions.First());
});
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

app.MapGet("/api/tarefas/{id}", ([FromServices] tarefasContext _db,
    [FromRoute] int id
) =>
{
    var tarefa = _db.Tarefa.Find(id);

    if (tarefa == null)
    {
        return Results.NotFound();
    }

    return Results.Ok(tarefa);
});

app.MapGet("/api/tarefas/descricao", ([FromServices] tarefasContext _db,
    [FromQuery] string? descricao
) =>
{
    var query = _db.Tarefa.AsQueryable<Tarefa>();

    if (!String.IsNullOrEmpty(descricao))
    {
        query = query.Where(t => t.Descricao.Contains(descricao));
    }

    var tarefas = query.ToList<Tarefa>();

    return Results.Ok(tarefas);
});

app.MapGet("/api/tarefas/pendentes", ([FromServices] tarefasContext _db,
    [FromQuery(Name = "somente_pendentes")] bool? somentePendentes,
    [FromQuery] string? descricao
) =>
{
    bool filtrarPendentes = somentePendentes ?? false;

    var query = _db.Tarefa.AsQueryable<Tarefa>();

    if (!String.IsNullOrEmpty(descricao))
    {
        query = query.Where(t => t.Descricao.Contains(descricao));
    }

    if (filtrarPendentes)
    {
        query = query.Where(t => !t.Concluida)
            .OrderByDescending(t => t.Id);
    }

    var tarefas = query.ToList<Tarefa>();

    return Results.Ok(tarefas);
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

app.MapPut("/api/tarefas/{id}", ([FromServices] tarefasContext _db,
    [FromRoute] int id,
    [FromBody] Tarefa tarefaAlterada
) =>
{
    if (tarefaAlterada.Id != id)
    {
        return Results.BadRequest(new { mensagem = "Id inconsistente." });
    }

    if (String.IsNullOrEmpty(tarefaAlterada.Descricao))
    {
        return Results.BadRequest(new { mensagem = "Não é permitido deixar uma tarefa sem título." });
    }

    var tarefa = _db.Tarefa.Find(id);

    if (tarefa == null)
    {
        return Results.NotFound();
    }

    tarefa.Descricao = tarefaAlterada.Descricao;
    tarefa.Concluida = tarefaAlterada.Concluida;

    _db.SaveChanges();

    return Results.Ok(tarefa);
});

app.MapMethods("/api/tarefas/{id}/concluir", new[] { "PATCH" }, ([FromServices] tarefasContext _db,
    [FromRoute] int id
) =>
{
    var tarefa = _db.Tarefa.Find(id);

    if (tarefa == null)
    {
        return Results.NotFound();
    }

    if (tarefa.Concluida)
    {
        return Results.BadRequest(new { mensagem = "Tarefa concluída anteriormente." });
    }

    tarefa.Concluida = true;
    _db.SaveChanges();

    return Results.Ok(tarefa);
});

app.MapDelete("/api/tarefas/{id}", ([FromServices] tarefasContext _db,
    [FromRoute] int id
) =>
{
    var tarefa = _db.Tarefa.Find(id);

    if (tarefa == null)
    {
        return Results.NotFound();
    }

    _db.Tarefa.Remove(tarefa);
    _db.SaveChanges();

    return Results.Ok();
});

app.Run();