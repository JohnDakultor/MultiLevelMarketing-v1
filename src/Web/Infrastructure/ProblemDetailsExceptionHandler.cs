using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using modular_mlm.Application.Common.Exceptions;
using modular_mlm.Domain.Exceptions;
using modular_mlm.Infrastructure.Observability;

namespace modular_mlm.Web.Infrastructure;

/// <summary>
/// Converts well-known application exceptions into RFC 9110-compliant <see cref="ProblemDetails"/> responses,
/// mapping <see cref="ValidationException"/> → 400, <see cref="NotFoundException"/> → 404,
/// <see cref="UnauthorizedAccessException"/> → 401, and <see cref="ForbiddenAccessException"/> → 403.
/// Unrecognised exceptions are not handled and fall through to the default middleware.
/// </summary>
public class ProblemDetailsExceptionHandler(
    ApiProblemDetailsFactory problemFactory,
    ILogger<ProblemDetailsExceptionHandler> logger
) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken
    )
    {
        var (statusCode, problemDetails) = exception switch
        {
            ValidationException ve => (
                StatusCodes.Status400BadRequest,
                (ProblemDetails)
                    new ValidationProblemDetails(ve.Errors)
                    {
                        Status = StatusCodes.Status400BadRequest,
                        Type = "https://tools.ietf.org/html/rfc9110#section-15.5.1",
                    }
            ),
            DomainInvariantException domainException => (
                StatusCodes.Status422UnprocessableEntity,
                new ProblemDetails
                {
                    Status = StatusCodes.Status422UnprocessableEntity,
                    Title = "Business rule validation failed",
                    Detail = domainException.Message,
                    Type = "https://tools.ietf.org/html/rfc9110#section-15.5.21",
                }
            ),
            ProviderTimeoutException => (
                StatusCodes.Status504GatewayTimeout,
                problemFactory.Create(
                    httpContext,
                    StatusCodes.Status504GatewayTimeout,
                    ApiErrorCodes.ProviderTimeout,
                    "Provider timeout",
                    "An external service did not respond in time."
                )
            ),
            ProviderUnavailableException => (
                StatusCodes.Status503ServiceUnavailable,
                problemFactory.Create(
                    httpContext,
                    StatusCodes.Status503ServiceUnavailable,
                    ApiErrorCodes.ProviderUnavailable,
                    "Provider unavailable",
                    "An external service is temporarily unavailable."
                )
            ),
            InvalidDataException invalidMedia => (
                StatusCodes.Status415UnsupportedMediaType,
                new ProblemDetails
                {
                    Status = StatusCodes.Status415UnsupportedMediaType,
                    Title = "Unsupported media",
                    Detail = invalidMedia.Message,
                    Type = "https://tools.ietf.org/html/rfc9110#section-15.5.16",
                }
            ),
            ObjectStorageException => (
                StatusCodes.Status503ServiceUnavailable,
                new ProblemDetails
                {
                    Status = StatusCodes.Status503ServiceUnavailable,
                    Title = "Media storage unavailable",
                    Detail = "The media service is temporarily unavailable.",
                    Type = "https://tools.ietf.org/html/rfc9110#section-15.6.4",
                }
            ),
            KeyNotFoundException ne => (
                StatusCodes.Status404NotFound,
                new ProblemDetails
                {
                    Status = StatusCodes.Status404NotFound,
                    Type = "https://tools.ietf.org/html/rfc9110#section-15.5.5",
                    Title = "The specified resource was not found.",
                    Detail = ne.Message,
                }
            ),
            BadHttpRequestException badRequest => (
                StatusCodes.Status400BadRequest,
                new ProblemDetails
                {
                    Status = StatusCodes.Status400BadRequest,
                    Type = "https://tools.ietf.org/html/rfc9110#section-15.5.1",
                    Title = "The request body is invalid.",
                    Detail = badRequest.InnerException
                        is System.Text.Json.JsonException jsonException
                        ? $"The JSON value at '{jsonException.Path}' has an invalid format."
                        : "The request body could not be read.",
                }
            ),
            UnauthorizedAccessException => (
                StatusCodes.Status401Unauthorized,
                new ProblemDetails
                {
                    Status = StatusCodes.Status401Unauthorized,
                    Title = "Unauthorized",
                    Type = "https://tools.ietf.org/html/rfc9110#section-15.5.2",
                }
            ),
            ForbiddenAccessException => (
                StatusCodes.Status403Forbidden,
                new ProblemDetails
                {
                    Status = StatusCodes.Status403Forbidden,
                    Title = "Forbidden",
                    Type = "https://tools.ietf.org/html/rfc9110#section-15.5.4",
                }
            ),
            ConflictException conflict => (
                StatusCodes.Status409Conflict,
                new ProblemDetails
                {
                    Status = StatusCodes.Status409Conflict,
                    Title = "Conflict",
                    Detail = conflict.Message,
                    Type = "https://tools.ietf.org/html/rfc9110#section-15.5.10",
                }
            ),
            IdempotencyConflictException conflict => (
                StatusCodes.Status409Conflict,
                new ProblemDetails
                {
                    Status = StatusCodes.Status409Conflict,
                    Title = "Idempotency conflict",
                    Detail = conflict.Message,
                    Type = "https://tools.ietf.org/html/rfc9110#section-15.5.10",
                }
            ),
            InventoryConcurrencyConflictException conflict => (
                StatusCodes.Status409Conflict,
                new ProblemDetails
                {
                    Status = StatusCodes.Status409Conflict,
                    Title = "Inventory changed",
                    Detail = conflict.Message,
                    Type = "https://tools.ietf.org/html/rfc9110#section-15.5.10",
                    Extensions = { ["productVariantId"] = conflict.ProductVariantId },
                }
            ),
            PlacementConflictException conflict => (
                StatusCodes.Status409Conflict,
                new ProblemDetails
                {
                    Status = StatusCodes.Status409Conflict,
                    Title = "Placement conflict",
                    Detail = conflict.Message,
                    Type = "https://tools.ietf.org/html/rfc9110#section-15.5.10",
                }
            ),
            DbUpdateConcurrencyException => (
                StatusCodes.Status409Conflict,
                problemFactory.Create(
                    httpContext,
                    StatusCodes.Status409Conflict,
                    ApiErrorCodes.ConcurrencyConflict,
                    "Concurrent update conflict",
                    "The resource changed while the request was being processed."
                )
            ),
            _ => (
                StatusCodes.Status500InternalServerError,
                problemFactory.Create(
                    httpContext,
                    StatusCodes.Status500InternalServerError,
                    ApiErrorCodes.UnexpectedError,
                    "An unexpected error occurred",
                    "The request could not be completed."
                )
            ),
        };

        if (exception is PlacementConflictException)
            MarketplaceTelemetry.PlacementConflicts.Add(1);

        if (statusCode >= StatusCodes.Status500InternalServerError)
        {
            logger.LogError(
                exception,
                "Request {Method} {Path} failed with status {StatusCode} and trace {TraceId}",
                httpContext.Request.Method,
                httpContext.Request.Path,
                statusCode,
                httpContext.TraceIdentifier
            );
        }

        problemFactory.Enrich(problemDetails, httpContext, ErrorCode(exception));
        if (
            exception is ProviderUnavailableException { RetryAfter: { } retryAfter }
            && retryAfter > TimeSpan.Zero
        )
            httpContext.Response.Headers.RetryAfter = Math.Ceiling(retryAfter.TotalSeconds)
                .ToString(System.Globalization.CultureInfo.InvariantCulture);
        await Results
            .Json(problemDetails, statusCode: statusCode, contentType: "application/problem+json")
            .ExecuteAsync(httpContext);
        return true;
    }

    private static string ErrorCode(Exception exception) =>
        exception switch
        {
            ValidationException => ApiErrorCodes.ValidationFailed,
            DomainInvariantException => ApiErrorCodes.DomainRuleViolation,
            InvalidDataException => ApiErrorCodes.UnsupportedMedia,
            ObjectStorageException or ProviderUnavailableException =>
                ApiErrorCodes.ProviderUnavailable,
            ProviderTimeoutException => ApiErrorCodes.ProviderTimeout,
            KeyNotFoundException => ApiErrorCodes.NotFound,
            BadHttpRequestException => ApiErrorCodes.ValidationFailed,
            UnauthorizedAccessException => ApiErrorCodes.Unauthorized,
            ForbiddenAccessException => ApiErrorCodes.Forbidden,
            IdempotencyConflictException => ApiErrorCodes.IdempotencyConflict,
            InventoryConcurrencyConflictException or DbUpdateConcurrencyException =>
                ApiErrorCodes.ConcurrencyConflict,
            PlacementConflictException => ApiErrorCodes.PlacementConflict,
            ConflictException => ApiErrorCodes.Conflict,
            _ => ApiErrorCodes.UnexpectedError,
        };
}
