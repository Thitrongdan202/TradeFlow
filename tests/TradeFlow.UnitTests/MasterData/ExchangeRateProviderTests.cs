using System.Net;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using TradeFlow.Infrastructure.Services;
using Xunit;

namespace TradeFlow.UnitTests.MasterData;

public class ExchangeRateProviderTests
{
    private class MockHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;

        public MockHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder)
        {
            _responder = responder;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(_responder(request));
        }
    }

    [Fact]
    public async Task FetchRatesAsync_SuccessfulUsdResponse_ParsesAndNormalizesRates()
    {
        // Arrange
        var jsonResponse = @"{
            ""result"": ""success"",
            ""base_code"": ""USD"",
            ""rates"": {
                ""USD"": 1,
                ""VND"": 25400.0,
                ""EUR"": 0.92,
                ""JPY"": 155.0,
                ""SGD"": 1.34
            }
        }";

        var handler = new MockHttpMessageHandler(req => new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(jsonResponse, Encoding.UTF8, "application/json")
        });

        var client = new HttpClient(handler);
        var mockFactory = new Mock<IHttpClientFactory>();
        mockFactory.Setup(f => f.CreateClient("ExchangeRateProvider")).Returns(client);

        var provider = new ExchangeRateProvider(mockFactory.Object, Mock.Of<ILogger<ExchangeRateProvider>>());

        // Act
        var result = await provider.FetchRatesAsync();

        // Assert
        result.Success.Should().BeTrue();
        result.RatesToVnd.Should().ContainKey("VND");
        result.RatesToVnd["VND"].Should().Be(1.0m);

        result.RatesToVnd.Should().ContainKey("USD");
        result.RatesToVnd["USD"].Should().Be(25400.0m);

        // EUR = 25400 / 0.92 = 27608.695652
        result.RatesToVnd.Should().ContainKey("EUR");
        result.RatesToVnd["EUR"].Should().BeApproximately(27608.695652m, 0.01m);
    }

    [Fact]
    public async Task FetchRatesAsync_MalformedJson_HandlesGracefullyWithoutThrowing()
    {
        // Arrange
        var handler = new MockHttpMessageHandler(req => new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent("<!DOCTYPE html><html><body>Error</body></html>", Encoding.UTF8, "text/html")
        });

        var client = new HttpClient(handler);
        var mockFactory = new Mock<IHttpClientFactory>();
        mockFactory.Setup(f => f.CreateClient("ExchangeRateProvider")).Returns(client);

        var provider = new ExchangeRateProvider(mockFactory.Object, Mock.Of<ILogger<ExchangeRateProvider>>());

        // Act
        var result = await provider.FetchRatesAsync();

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task FetchRatesAsync_WhenBothEndpointsFail_ReturnsFailureGracefully()
    {
        // Arrange
        var handler = new MockHttpMessageHandler(req => new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.InternalServerError,
            Content = new StringContent("Server Error", Encoding.UTF8, "text/plain")
        });

        var client = new HttpClient(handler);
        var mockFactory = new Mock<IHttpClientFactory>();
        mockFactory.Setup(f => f.CreateClient("ExchangeRateProvider")).Returns(client);

        var provider = new ExchangeRateProvider(mockFactory.Object, Mock.Of<ILogger<ExchangeRateProvider>>());

        // Act
        var result = await provider.FetchRatesAsync();

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }
}
