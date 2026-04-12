using Nexus.Application.Dtos.Responses;
using Nexus.Application.Exceptions;

namespace Nexus.Api.Extensions
{
    public static class ExceptionExtensions
    {
        public static (int StatusCode, ErrorResponse Response) ToHttpResponse(this Exception exception, HttpContext context)
        {
            var result = exception switch
            {
                AppException appEx => (
                    appEx.HttpStatusCode,
                     new ErrorResponse
                     {
                         Name = appEx.Name,
                         Code = appEx.StatusCode,
                         Message = appEx.Message,
                     }),
                NetworkException netEx => (
                    netEx.HttpStatusCode,
                    new ErrorResponse
                    {
                        Name = netEx.Name,
                        Code = netEx.StatusCode,
                        Message = netEx.Message,
                    }),
                _ => (
                   StatusCodes.Status500InternalServerError,
                   new ErrorResponse
                   {
                       Name = "UNEXPECTED_ERROR",
                       Code = StatusCodes.Status500InternalServerError,
                       Message = "An unexpected error occurred.",
                   }
                )
            };

            return result;
        }
    }
}
