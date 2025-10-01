using LankaConnect.Application.Common.Exceptions;
using LankaConnect.Domain.Common.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Net;

namespace LankaConnect.API.Filters;

public class ApiExceptionFilterAttribute : ExceptionFilterAttribute
{
    private readonly ILogger<ApiExceptionFilterAttribute> _logger;

    public ApiExceptionFilterAttribute(ILogger<ApiExceptionFilterAttribute> logger)
    {
        _logger = logger;
    }

    public override void OnException(ExceptionContext context)
    {
        HandleException(context);
        base.OnException(context);
    }

    private void HandleException(ExceptionContext context)
    {
        var exception = context.Exception;

        _logger.LogError(exception, "An exception occurred: {ExceptionMessage}", exception.Message);

        var response = exception switch
        {
            NotFoundException notFoundException => new ApiErrorResponse
            {
                Title = "Resource Not Found",
                Status = (int)HttpStatusCode.NotFound,
                Detail = notFoundException.Message,
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.4"
            },
            DomainException domainException => new ApiErrorResponse
            {
                Title = "Domain Rule Violation",
                Status = (int)HttpStatusCode.BadRequest,
                Detail = domainException.Message,
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1"
            },
            FluentValidation.ValidationException validationException => new ApiErrorResponse
            {
                Title = "Validation Failed",
                Status = (int)HttpStatusCode.BadRequest,
                Detail = "One or more validation errors occurred.",
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                Errors = validationException.Errors
                    .GroupBy(e => e.PropertyName, e => e.ErrorMessage)
                    .ToDictionary(g => g.Key, g => g.ToArray())
            },
            _ => new ApiErrorResponse
            {
                Title = "An error occurred while processing your request",
                Status = (int)HttpStatusCode.InternalServerError,
                Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1"
            }
        };

        context.Result = new ObjectResult(response)
        {
            StatusCode = response.Status
        };

        context.ExceptionHandled = true;
    }

    private class ApiErrorResponse
    {
        public string Title { get; set; } = string.Empty;
        public int Status { get; set; }
        public string Detail { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public Dictionary<string, string[]>? Errors { get; set; }
    }
}