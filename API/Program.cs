using API.Errors;
using API.Extensions;
using Core.Interfaces;
using Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplicationServices(builder.Configuration);
builder.Services.AddIdentityServices(builder.Configuration);
builder.Services.AddCorsPolicy(builder.Configuration);
builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var errors = context.ModelState
                .Where(entry => entry.Value is { Errors.Count: > 0 })
                .ToDictionary(
                    entry => entry.Key,
                    entry => entry.Value!.Errors.Select(error => error.ErrorMessage).ToArray());

            var response = new ApiValidationErrorResponse(StatusCodes.Status400BadRequest, errors);
            return new BadRequestObjectResult(response);
        };
    });

// Stripe
builder.Services.AddScoped<IPaymentService, PaymentService>();


var app = builder.Build();

app.UseApiPipeline();

await app.ApplyMigrationsAndSeedAsync();

app.Run();
