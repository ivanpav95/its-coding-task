using Microsoft.AspNetCore.Mvc;

namespace Claims.Api;

public static class ValidationProblemExtensions
{
    /// <summary>
    /// 400 with <see cref="ValidationProblemDetails"/>, built through <c>ModelState</c> so it gets the same
    /// <c>type</c> and <c>traceId</c> as ASP.NET's own model-binding errors.
    /// </summary>
    public static ActionResult ValidationProblem(this ControllerBase controller, IDictionary<string, string[]> errors)
    {
        foreach (var (field, messages) in errors)
        {
            foreach (var message in messages)
            {
                controller.ModelState.AddModelError(field, message);
            }
        }

        return controller.ValidationProblem(controller.ModelState);
    }
}
