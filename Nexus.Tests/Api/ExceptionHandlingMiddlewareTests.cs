using Microsoft.AspNetCore.Http;
using Nexus.Application.Constants;
using Nexus.Application.Dtos;
using Nexus.Api.Middleware;
using Nexus.Application.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;
using Microsoft.Extensions.Logging;
using Moq;
using Nexus.Network.Constants;

namespace Nexus.Tests.Api
{
    public class ExceptionHandlingMiddlewareTests
    {
        [Theory]
        [InlineData(typeof(NotFoundException), CustomStatusCodes.NotFound, "NOT_FOUND")]
        [InlineData(typeof(PayPalException), NetworkStatusCodes.PayPalIssue, "PAYPAL_ISSUE")]
        [InlineData(typeof(PaymentException), CustomStatusCodes.PaymentIssue, "PAYMENT_ISSUE")]
        [InlineData(typeof(RefundException), CustomStatusCodes.RefundIssue, "REFUND_ISSUE")]
        public async Task Middleware_Returns_AppException_Correctly(Type exceptionType, int expectedStatus, string expectedName)
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();

            var exceptionMessage = "Test message";

            var exception = (Exception)Activator.CreateInstance(exceptionType, exceptionMessage)!;

            var loggerMock = new Mock<ILogger<ExceptionHandlingMiddleware>>();

            RequestDelegate next = (ctx) => throw exception;

            var middleware = new ExceptionHandlingMiddleware(next, loggerMock.Object);

            // Act
            await middleware.Invoke(context);

            // Assert
            Assert.Equal(expectedStatus, context.Response.StatusCode);
            Assert.StartsWith("application/json", context.Response.ContentType);

            context.Response.Body.Seek(0, SeekOrigin.Begin);

            using var reader = new StreamReader(context.Response.Body);
            var json = await reader.ReadToEndAsync();

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var response = JsonSerializer.Deserialize<ErrorResponse>(json, options);

            Assert.NotNull(response);
            Assert.Equal(expectedStatus, response.Code);
            Assert.Equal(expectedName, response.Name);
            Assert.Equal(exceptionMessage, response.Message);
        }

        [Fact]
        public async Task Middleware_Handles_Unexpected_Exception_As_500()
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();

            var exceptionMessage = "Unexpected error";
            var exception = new Exception(exceptionMessage);

            var loggerMock = new Mock<ILogger<ExceptionHandlingMiddleware>>();

            RequestDelegate next = (ctx) => throw exception;

            var middleware = new ExceptionHandlingMiddleware(next, loggerMock.Object);

            // Act
            await middleware.Invoke(context);

            // Assert HTTP response
            Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
            Assert.StartsWith("application/json", context.Response.ContentType);

            context.Response.Body.Seek(0, SeekOrigin.Begin);

            using var reader = new StreamReader(context.Response.Body);
            var json = await reader.ReadToEndAsync();

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var response = JsonSerializer.Deserialize<ErrorResponse>(json, options);

            Assert.NotNull(response);
            Assert.Equal(StatusCodes.Status500InternalServerError, response!.Code);
            Assert.Equal("UNEXPECTED_ERROR", response.Name);
            Assert.Equal("An unexpected error occurred.", response.Message);
        }
    }
}
