using Claims.Api;
using Claims.Application.Auditing;
using Claims.Application.Contracts;
using Claims.Application.Repositories;
using Claims.Application.Services;
using Claims.Application.Validation;
using FluentValidation;
using Claims.Infrastructure.Auditing;
using Claims.Infrastructure.Persistence;
using Claims.Infrastructure.Time;
using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;
using System.Text.Json.Serialization;
using System.Threading.Channels;

var builder = WebApplication.CreateBuilder(args);

string RequiredConnectionString(string name) =>
    builder.Configuration.GetConnectionString(name)
    ?? throw new InvalidOperationException($"Connection string '{name}' is missing.");

// Add services to the container.
builder.Services
    .AddControllers()
    .AddJsonOptions(x =>
    {
        x.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        x.JsonSerializerOptions.Converters.Add(new RequireOffsetDateTimeOffsetConverter());
    });

builder.Services.AddDbContext<AuditContext>(options =>
    options.UseSqlServer(RequiredConnectionString("Audit")));

var mongoDatabaseName = builder.Configuration.GetRequiredSection("MongoDb:DatabaseName").Value!;

builder.Services.AddSingleton<IMongoClient>(_ => new MongoClient(RequiredConnectionString("Claims")));

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

var businessTimeZone = TimeZoneInfo.FindSystemTimeZoneById(
    builder.Configuration.GetRequiredSection("BusinessTimeZone").Value!);
builder.Services.AddSingleton<TimeProvider>(new BusinessTimeProvider(businessTimeZone));
builder.Services.AddSingleton<IValidator<CreateCoverRequest>, CreateCoverRequestValidator>();
builder.Services.AddSingleton<IValidator<CreateClaimRequest>, CreateClaimRequestValidator>();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
    options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, $"{typeof(Program).Assembly.GetName().Name}.xml")));

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
