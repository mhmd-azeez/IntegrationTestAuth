using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

using Xunit;

namespace IntegrationTests
{
    public class SimpleTest : IClassFixture<WebAppFactory>
    {
        private HttpClient _httpClient;

        public SimpleTest(WebAppFactory factory)
        {
            factory.DefaultUserId = "5";

            _httpClient = factory.CreateClient();
            _httpClient.BaseAddress = new Uri("https://localhost/");
            // Use our mock Auth scheme 
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Test");
        }

        [Fact]
        public async Task SayHiToNumber5()
        {
            _httpClient.DefaultRequestHeaders.Remove(TestAuthHandler.UserId);

            var response = await _httpClient.GetStringAsync("hi");
            Assert.Equal("Hello #5", response);
        }

        [Fact]
        public async Task SayHiToNumber1()
        {
            _httpClient.DefaultRequestHeaders.Add(TestAuthHandler.UserId, "1");

            var response = await _httpClient.GetStringAsync("hi");
            Assert.Equal("Hello #1", response);
        }

        [Fact]
        public async Task SayHiToNumber3()
        {
            _httpClient.DefaultRequestHeaders.Add(TestAuthHandler.UserId, "3");

            var response = await _httpClient.GetStringAsync("hi");
            Assert.Equal("Hello #3", response);
        }
    }

    public class WebAppFactory : WebApplicationFactory<Program>
    {
        public string DefaultUserId { get; set; } = "1";

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureTestServices(services =>
            {
                services.Configure<TestAuthHandlerOptions>(options => options.DefaultUserId = DefaultUserId);

                services.AddAuthentication(TestAuthHandler.AuthenticationScheme)
                    .AddScheme<TestAuthHandlerOptions, TestAuthHandler>(TestAuthHandler.AuthenticationScheme, options => { });
            });
        }
    }

    public class TestAuthHandlerOptions : AuthenticationSchemeOptions
    {
        public string DefaultUserId { get; set; } = null!;
    }

    public class TestAuthHandler : AuthenticationHandler<TestAuthHandlerOptions>
    {
        public const string UserId = "UserId";

        public const string AuthenticationScheme = "Test";
        private readonly string _defaultUserId;

        public TestAuthHandler(
            IOptionsMonitor<TestAuthHandlerOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            ISystemClock clock) : base(options, logger, encoder, clock)
        {
            _defaultUserId = options.CurrentValue.DefaultUserId;
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var claims = new List<Claim> { new Claim(ClaimTypes.Name, "Test user") };

            // Extract User ID from the request headers if it exists,
            // otherwise use the default User ID from the options.
            if (Context.Request.Headers.TryGetValue(UserId, out var userId))
            {
                claims.Add(new Claim(ClaimTypes.NameIdentifier, userId[0]));
            }
            else
            {
                claims.Add(new Claim(ClaimTypes.NameIdentifier, _defaultUserId));
            }

            // TODO: Add as many claims as you need here

            var identity = new ClaimsIdentity(claims, AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, AuthenticationScheme);

            var result = AuthenticateResult.Success(ticket);

            return Task.FromResult(result);
        }
    }
}
