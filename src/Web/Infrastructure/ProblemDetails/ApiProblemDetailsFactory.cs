using Microsoft.AspNetCore.Mvc;

namespace modular_mlm.Web.Infrastructure;

public sealed class ApiProblemDetailsFactory
{
    public Microsoft.AspNetCore.Mvc.ProblemDetails Create(
        HttpContext context,
        int status,
        string errorCode,
        string title,
        string? detail = null
    )
    {
        var problem = new Microsoft.AspNetCore.Mvc.ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = detail,
            Type = TypeFor(status),
            Instance = context.Request.Path,
        };
        Enrich(problem, context, errorCode);
        return problem;
    }

    public ValidationProblemDetails CreateValidation(
        HttpContext context,
        IDictionary<string, string[]> errors
    )
    {
        var problem = new ValidationProblemDetails(errors)
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "One or more validation errors occurred.",
            Type = TypeFor(StatusCodes.Status400BadRequest),
            Instance = context.Request.Path,
        };
        Enrich(problem, context, ApiErrorCodes.ValidationFailed);
        return problem;
    }

    public void Enrich(
        Microsoft.AspNetCore.Mvc.ProblemDetails problem,
        HttpContext context,
        string errorCode
    )
    {
        problem.Extensions["errorCode"] = errorCode;
        problem.Extensions["traceId"] = context.TraceIdentifier;
    }

    private static string TypeFor(int status) =>
        status switch
        {
            400 => "https://tools.ietf.org/html/rfc9110#section-15.5.1",
            401 => "https://tools.ietf.org/html/rfc9110#section-15.5.2",
            403 => "https://tools.ietf.org/html/rfc9110#section-15.5.4",
            404 => "https://tools.ietf.org/html/rfc9110#section-15.5.5",
            409 => "https://tools.ietf.org/html/rfc9110#section-15.5.10",
            415 => "https://tools.ietf.org/html/rfc9110#section-15.5.16",
            422 => "https://tools.ietf.org/html/rfc9110#section-15.5.21",
            429 => "https://tools.ietf.org/html/rfc6585#section-4",
            503 => "https://tools.ietf.org/html/rfc9110#section-15.6.4",
            504 => "https://tools.ietf.org/html/rfc9110#section-15.6.5",
            _ => "https://tools.ietf.org/html/rfc9110#section-15.6.1",
        };
}
