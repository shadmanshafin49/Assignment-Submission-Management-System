using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Ams.Api.Filters;

/// <summary>
/// Runs any registered FluentValidation validator against each action argument before the
/// action executes, returning a 400 ValidationProblemDetails on failure.
/// <para>
/// Written by hand rather than pulling in <c>FluentValidation.AspNetCore</c>, whose
/// auto-validation integration is deprecated as of FluentValidation 11 and unsupported in 12.
/// </para>
/// </summary>
public class ValidationFilter(IServiceProvider serviceProvider) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(
        ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var errors = new Dictionary<string, string[]>();

        foreach (var (_, argument) in context.ActionArguments)
        {
            if (argument is null)
                continue;

            var validatorType = typeof(IValidator<>).MakeGenericType(argument.GetType());
            if (serviceProvider.GetService(validatorType) is not IValidator validator)
                continue;

            var validationContext = new ValidationContext<object>(argument);
            var result = await validator.ValidateAsync(validationContext, context.HttpContext.RequestAborted);

            if (result.IsValid)
                continue;

            foreach (var group in result.Errors.GroupBy(e => e.PropertyName))
            {
                errors[group.Key] = group.Select(e => e.ErrorMessage).ToArray();
            }
        }

        if (errors.Count > 0)
        {
            var problem = new ValidationProblemDetails(errors)
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Validation failed",
                Instance = context.HttpContext.Request.Path
            };
            problem.Extensions["errorCode"] = "validation_failed";
            problem.Extensions["traceId"] = context.HttpContext.TraceIdentifier;

            context.Result = new BadRequestObjectResult(problem);
            return;
        }

        await next();
    }
}
