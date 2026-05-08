using System.Net;
using System.Text.Json;
using FluentValidation;
using IsoDocs.Domain.Common;
using Microsoft.AspNetCore.Mvc;

namespace IsoDocs.Api.Middleware;

/// <summary>
/// 全域例外處理中介軟體。將不同類型的例外統一轉換為 RFC 7807 ProblemDetails 回應。
///
/// 例外對應：
/// - <see cref="ValidationException"/> → 400 Bad Request（含欄位驗證錯誤清單）
/// - <see cref="DomainException"/>     → 422 Unprocessable Entity（業務規則違反）
/// - <see cref="UnauthorizedAccessException"/> → 401 Unauthorized
/// - 其他未處理例外 → 500 Internal Server Error（隱藏細節，僅回 traceId）
/// </summary>
public sealed class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public GlobalExceptionMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionMiddleware> logger,
        IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var traceId = context.TraceIdentifier;
        ProblemDetails problem;

        switch (exception)
        {
            case ValidationException validationEx:
                _logger.LogInformation(
                    "Validation failed for {Path}: {Errors}",
                    context.Request.Path,
                    string.Join("; ", validationEx.Errors.Select(e => $"{e.PropertyName}: {e.ErrorMessage}")));

                problem = new ValidationProblemDetails(
                    validationEx.Errors
                        .GroupBy(e => e.PropertyName)
                        .ToDictionary(
                            g => g.Key,
                            g => g.Select(e => e.ErrorMessage).ToArray()))
                {
                    Status = (int)HttpStatusCode.BadRequest,
                    Title = "One or more validation errors occurred.",
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                };
                break;

            case DomainException domainEx:
                _logger.LogWarning(domainEx,
                    "Domain rule violation: {Code} on {Path}",
                    domainEx.Code, context.Request.Path);

                problem = new ProblemDetails
                {
                    Status = (int)HttpStatusCode.UnprocessableEntity,
                    Title = "Business rule violation",
                    Type = "https://tools.ietf.org/html/rfc4918#section-11.2",
                    Detail = domainEx.Message,
                    Extensions = { ["code"] = domainEx.Code },
                };
                break;

            case UnauthorizedAccessException:
                problem = new ProblemDetails
                {
                    Status = (int)HttpStatusCode.Unauthorized,
                    Title = "Unauthorized",
                    Type = "https://tools.ietf.org/html/rfc7235#section-3.1",
                };
                break;

            default:
                _logger.LogError(exception,
                    "Unhandled exception on {Path} (traceId={TraceId})",
                    context.Request.Path, traceId);

                problem = new ProblemDetails
                {
                    Status = (int)HttpStatusCode.InternalServerError,
                    Title = "An unexpected error occurred.",
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
                    Detail = _environment.IsDevelopment() ? exception.ToString() : "Please contact the administrator.",
                };
                break;
        }

        problem.Instance = context.Request.Path;
        problem.Extensions["traceId"] = traceId;

        context.Response.Clear();
        context.Response.StatusCode = problem.Status ?? 500;
        context.Response.ContentType = "application/problem+json";

        await context.Response.WriteAsync(JsonSerializer.Serialize(problem, JsonOptions));
    }
}
