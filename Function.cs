using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace GasBetFunction
{
    public class Function
    {
        //Variables
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<Function> _logger;

        //Constructor
        public Function(IHttpClientFactory httpClientFactory, IConfiguration configuration, ILogger<Function> logger)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _logger = logger;
        }

        //Method: Poll Prices
        [Function("PollPrices")]
        public async Task PollPricesAsync([TimerTrigger("0 * * * * *", RunOnStartup = true)] TimerInfo timer)
        {
            await CallInternalEndpointAsync("PollPrices", "/api/internal/poll-prices");
        }

        //Method: Create Events
        [Function("CreateEvents")]
        public async Task CreateEventsAsync([TimerTrigger("0 0 * * * *", RunOnStartup = true)] TimerInfo timer)
        {
            await CallInternalEndpointAsync("CreateEvents", "/api/internal/create-events");
        }

        //Method: Lock Events
        [Function("LockEvents")]
        public async Task LockEventsAsync([TimerTrigger("5 0 * * * *", RunOnStartup = true)] TimerInfo timer)
        {
            await CallInternalEndpointAsync("LockEvents", "/api/internal/lock-events");
        }

        //Method: Resolve Events
        [Function("ResolveEvents")]
        public async Task ResolveEventsAsync([TimerTrigger("10 0 * * * *", RunOnStartup = true)] TimerInfo timer)
        {
            await CallInternalEndpointAsync("ResolveEvents", "/api/internal/resolve-events");
        }

        //Method: Delete old snapshots
        [Function("DeleteOldSnapshots")]
        public async Task DeleteOldSnapshotsAsync([TimerTrigger("0 0 3 * * *", RunOnStartup = true)] TimerInfo timer)
        {
            await CallInternalEndpointAsync("DeleteOldSnapshots", "/api/internal/delete-old-snapshots");
        }

        //Method: Call internal endpoint
        private async Task CallInternalEndpointAsync(string functionName, string relativePath)
        {
            //Try getting configuration
            if (!TryGetConfiguration(out var backendBaseUrl, out var internalApiKey))
                throw new Exception("Required configuration is missing.");

            //Compose HTTP request
            var client = _httpClientFactory.CreateClient();

            var request = new HttpRequestMessage(
                HttpMethod.Post,
                $"{backendBaseUrl}{relativePath}");

            request.Headers.Add("X-Internal-Key", internalApiKey);

            //Send request and handle response
            var response = await client.SendAsync(request);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("{FunctionName} failed. Status: {StatusCode}, Body: {Body}",
                    functionName, response.StatusCode, responseContent);

                throw new Exception($"{functionName} failed: {response.StatusCode} - {responseContent}");
            }

            //Log success
            _logger.LogInformation("{FunctionName} succeeded: {Body}", functionName, responseContent);
        }

        //Method: Check configuration
        private bool TryGetConfiguration(out string backendBaseUrl, out string internalApiKey)
        {
            backendBaseUrl = _configuration["BackendApi:BaseUrl"] ?? string.Empty;
            internalApiKey = _configuration["InternalApi:Key"] ?? string.Empty;

            return (!string.IsNullOrWhiteSpace(backendBaseUrl) &&
                    !string.IsNullOrWhiteSpace(internalApiKey));
        }
    }
}