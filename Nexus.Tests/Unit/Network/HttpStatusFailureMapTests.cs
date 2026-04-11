using Nexus.Network;
using Nexus.Network.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Nexus.Tests.Unit.Network
{
    public class HttpStatusFailureMapTests
    {
        [Theory]
        [InlineData(HttpStatusCode.BadRequest, ApiFailureReason.ValidationError)]
        [InlineData(HttpStatusCode.Unauthorized, ApiFailureReason.Unauthorized)]
        [InlineData(HttpStatusCode.Forbidden, ApiFailureReason.Forbidden)]
        [InlineData(HttpStatusCode.NotFound, ApiFailureReason.NotFound)]
        [InlineData(HttpStatusCode.TooManyRequests, ApiFailureReason.RateLimited)]
        [InlineData(HttpStatusCode.RequestTimeout, ApiFailureReason.Timeout)]
        [InlineData(HttpStatusCode.InternalServerError, ApiFailureReason.ServerError)]
        [InlineData((HttpStatusCode)499, ApiFailureReason.Unknown)]
        public void Resolve_WithKnownStatusCode_ShouldReturnExpectedFailureReason(HttpStatusCode statusCode, ApiFailureReason expected)
        {
            // Act
            var result = HttpStatusFailureMap.Resolve(statusCode);

            // Assert
            Assert.Equal(expected, result);            
        }
    }
}
