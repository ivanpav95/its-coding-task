using Claims.Application.Auditing;
using Claims.Application.Contracts;
using Claims.Application.Repositories;
using Claims.Application.Services;
using Claims.Application.Validation;
using FluentValidation;
using Claims.Infrastructure.Auditing;
using Claims.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;
using System.Threading.Channels;
using Testcontainers.MongoDb;
using Testcontainers.MsSql;

var builder = WebApplication.CreateBuilder(args);

// Start Testcontainers for SQL Server and MongoDB
var sqlContainer = new MsSqlBuilder(RuntimeInformation.IsOSPlatform(OSPlatform.Linux)
        ? "mcr.microsoft.com/mssql/server:2022-latest"
        : "mcr.microsoft.com/mssql/server:2019-CU18-ubuntu-20.04")
    .Build();

var mongoContainer = new MongoDbBuilder("mongo:latest")
    .Build();

await sqlContainer.StartAsync();
await mongoContainer.StartAsync();

// Add services to the container.
builder.Services
    .AddControllers()
    .AddJsonOptions(x =>
    {
        x.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

var auditConnectionString = new SqlConnectionStringBuilder(sqlContainer.GetConnectionString())
{
    InitialCatalog = builder.Configuration["SqlServer:DatabaseName"]
}.ConnectionString;

builder.Services.AddDbContext<AuditContext>(options =>
    options.UseSqlServer(auditConnectionString));

var mongoDatabaseName = builder.Configuration.GetRequiredSection("MongoDb:DatabaseName").Value!;

builder.Services.AddSingleton<IMongoClient>(_ => new MongoClient(mongoContainer.GetConnectionString()));

builder.Services.AddDbContext<ClaimsContext>((services, options) =>
    options.UseMongoDB(services.GetRequiredService<IMongoClient>(), mongoDatabaseName));

builder.Services.AddScoped<ICoverRepository, CoverRepository>();
builder.Services.AddScoped<IClaimRepository, ClaimRepository>();
builder.Services.AddScoped<ICoverService, CoverService>();
builder.Services.AddScoped<IClaimService, ClaimService>();

var auditQueueCapacity = builder.Configuration.GetRequiredSection("Auditing:QueueCapacity").Get<int>();
builder.Services.AddSingleton(Channel.CreateBounded<AuditMessage>(new BoundedChannelOptions(auditQueueCapacity)
{
    FullMode = BoundedChannelFullMode.Wait,
    SingleReader = true
}));
builder.Services.AddSingleton(services => services.GetRequiredService<Channel<AuditMessage>>().Writer);
builder.Services.AddSingleton<IAuditer, ChannelAuditer>();
builder.Services.AddHostedService<AuditWriter>();

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<IValidator<CreateCoverRequest>, CreateCoverRequestValidator>();
builder.Services.AddSingleton<IValidator<CreateClaimRequest>, CreateClaimRequestValidator>();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
    options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, "Claims.xml")));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AuditContext>();
    context.Database.Migrate();
}

app.Run();

public partial class Program { }
