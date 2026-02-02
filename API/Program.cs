using API.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplicationServices(builder.Configuration);
builder.Services.AddIdentityServices(builder.Configuration);
builder.Services.AddCorsPolicy(builder.Configuration);
builder.Services.AddControllers();

var app = builder.Build();

app.UseApiPipeline();

await app.ApplyMigrationsAndSeedAsync();

app.Run();
