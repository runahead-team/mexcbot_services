using System.Linq;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using sp.Core.Constants;
using sp.Core.Exceptions;

namespace multexbot.Api.Infrastructure.Filters
{
    public class ValidationFilter : IActionFilter
    {
        public void OnActionExecuting(ActionExecutingContext context)
        {
            if (!context.ModelState.IsValid)
            {
                var errorMessages = GetValidationMessage(context.ModelState);

                throw new AppException(AppError.INVALID_PARAMETERS, errorMessages[0]);
            }
        }

        public void OnActionExecuted(ActionExecutedContext context)
        {
            //Ignored
        }

        private static string[] GetValidationMessage(ModelStateDictionary modelStates)
        {
            var messageList =
                (from modelState in modelStates from error in modelState.Value.Errors select error.ErrorMessage)
                .ToList();
            return messageList.Distinct().ToArray();
        }
    }
}