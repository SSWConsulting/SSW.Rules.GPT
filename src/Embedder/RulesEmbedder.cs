using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using RulesEmbeddingFunction.Services;

namespace RulesEmbeddingFunction
{
    public class RulesEmbedder
    {
        private readonly ILogger _logger;
        private readonly EmbeddingService _embeddingService;
        private readonly DatabaseService _database;

        public RulesEmbedder(
            ILoggerFactory loggerFactory,
            EmbeddingService embeddingService,
            DatabaseService database)
        {
            _logger = loggerFactory.CreateLogger<RulesEmbedder>();
            _embeddingService = embeddingService;
            _database = database;
        }

        [Function("EmbedRule")]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req)
        {
            var ruleName = req.Query["rule"];
            if (string.IsNullOrWhiteSpace(ruleName))
            {
                var failedResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await failedResponse.WriteStringAsync("Failed to read rule name from the query string.");
                
                _logger.LogError("Failed to read rule name from the query string.");

                return failedResponse;
            }

            try
            {
                var embeddingList = await _embeddingService.GetEmbedding(ruleName);
                if (embeddingList == null)
                {
                    var failedResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                    failedResponse.Headers.Add("Content-Type", "text/plain; charset=utf-8");
                    await failedResponse.WriteStringAsync($"Failed to embed {ruleName}.");

                    return failedResponse;
                }

                await _database.SaveEmbeddings(embeddingList);
                _logger.LogInformation("C# HTTP trigger function processed a request.");

                var response = req.CreateResponse(HttpStatusCode.OK);
                response.Headers.Add("Content-Type", "text/plain; charset=utf-8");
                await response.WriteStringAsync($"Successfully embedded {ruleName}.");

                return response;
            }

            catch
            {
                _logger.LogError("Failed to embed {ruleName}.", ruleName);
                throw;
            }
        }
    }
}