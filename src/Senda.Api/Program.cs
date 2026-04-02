using Microsoft.EntityFrameworkCore;
using Senda.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);


// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
// Configurar PostgreSQL con soporte para vectores
builder.Services
    .AddDbContext<SendaDbContext>(options =>
        options.UseNpgsql(
            builder.Configuration.GetConnectionString("DefaultConnection"), 
            o => o.UseVector()
        )
    ) // Habilita el mapeo de tipos de vectores
    .AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.Run();
