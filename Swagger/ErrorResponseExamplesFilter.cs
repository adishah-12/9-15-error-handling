using System.Text.Json.Nodes;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace FinanceApi.Swagger;

/// <summary>
/// Adds an example ProblemDetails body to any 400/404/500 response already declared
/// on an action via [ProducesResponseType].
/// </summary>
public class ErrorResponseExamplesFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        AddExample(operation, "400", 400, "Validation Error", "One or more fields are invalid.",
            extra: new JsonObject
            {
                ["errors"] = new JsonObject
                {
                    ["amount"] = new JsonArray("Amount must be greater than zero.")
                }
            });

        AddExample(operation, "404", 404, "Not Found", "Account with id 42 was not found.");

        AddExample(operation, "500", 500, "Internal Server Error",
            "An unexpected error occurred. Please contact support if the problem persists.");
    }

    private static void AddExample(OpenApiOperation operation, string statusCode, int status, string title,
        string detail, JsonObject? extra = null)
    {
        if (!operation.Responses.TryGetValue(statusCode, out var response) || response is null
            || response.Content is not { Count: > 0 })
        {
            return;
        }

        foreach (var mediaType in response.Content.Values)
        {
            // JsonNode instances can only belong to one parent, so build a fresh
            // example object for each content type on this response.
            var example = new JsonObject
            {
                ["status"] = status,
                ["title"] = title,
                ["detail"] = detail,
                ["instance"] = "/api/accounts/42",
                ["traceId"] = "0HN8F1D5R2G3O:00000001"
            };

            if (extra is not null)
            {
                foreach (var (key, value) in extra)
                {
                    example[key] = value?.DeepClone();
                }
            }

            mediaType.Example = example;
        }
    }
}