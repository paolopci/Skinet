using System.Text.Json;
using API.Errors;

namespace API.Middleware
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate next;
        private readonly ILogger<ExceptionMiddleware> logger;
        private readonly IHostEnvironment env;

        public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger, IHostEnvironment env)
        {
            this.next = next;
            this.logger = logger;
            this.env = env;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                // Prosegue la pipeline; le eccezioni vengono intercettate qui.
                await next(context);
            }
            catch (Exception ex)
            {
                // Logga l'errore non gestito.
                logger.LogError(ex, "Errore non gestito durante la richiesta.");

                // Imposta la risposta di errore standard.
                context.Response.ContentType = "application/json";
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;

                // In sviluppo includiamo i dettagli dell'eccezione.
                var details = env.IsDevelopment() ? ex.ToString() : null;
                var response = new ApiErrorResponse(
                    context.Response.StatusCode,
                    "Errore del server",
                    details
                );

                // Serializza la risposta con naming camelCase.
                var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
                var json = JsonSerializer.Serialize(response, options);
                await context.Response.WriteAsync(json);
            }
        }
    }
}
