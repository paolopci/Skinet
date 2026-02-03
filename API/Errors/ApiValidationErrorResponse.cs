using Microsoft.AspNetCore.Http;

namespace API.Errors;

public class ApiValidationErrorResponse(int statusCode, Dictionary<string, string[]> errors)
{
    public int StatusCode { get; set; } = statusCode;
    public string Message { get; set; } = "Uno o piu' errori di validazione";
    public Dictionary<string, string[]> Errors { get; set; } = errors;

    public static ApiValidationErrorResponse FromIdentityErrors(IEnumerable<string> errors)
    {
        var dictionary = new Dictionary<string, string[]>
        {
            ["identity"] = errors.ToArray()
        };

        return new ApiValidationErrorResponse(StatusCodes.Status400BadRequest, dictionary);
    }
}
