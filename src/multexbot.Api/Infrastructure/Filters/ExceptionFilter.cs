using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Serilog;
using System.Net;
using sp.Core.Constants;
using sp.Core.Exceptions;
using sp.Core.Models;

namespace multexBot.Api.Infrastructure.Filters
{
    public class ExceptionFilter : IExceptionFilter
    {
        public void OnException(ExceptionContext context)
        {
            if (context.Exception is AppException appException)
            {
                context.Result = new OkObjectResult(new ErrorResponse
                {
                    Error = new Error
                    {
                        Code = appException.Error,
                        Msg = appException.Message
                    }
                });

                context.HttpContext.Response.StatusCode = (int) HttpStatusCode.OK;

                if (appException.Error == AppError.UNKNOWN)
                    Log.Error(context.Exception, context.Exception.Message);
            }
            else
            {
                context.Result = new OkObjectResult(new ErrorResponse
                {
                    Error = new Error
                    {
                        Code = AppError.UNKNOWN,
                        Msg = context.Exception.Message
                    }
                });

                context.HttpContext.Response.StatusCode = (int) HttpStatusCode.OK;

                Log.Error(context.Exception, context.Exception.Message);
            }

            context.ExceptionHandled = true;
        }
    }
}