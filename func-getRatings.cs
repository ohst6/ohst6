using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BFYOC.ohts6
{
    public class Rating
    {
        public string id { get; set; }
        public string userId { get; set; }
        public string productId { get; set; }
        public string timeStamp { get; internal set; }
        public string locationName { get; set; }
        public int rating { get; set; }
        public string userNotes { get; set; }
    }
    
    public static class func_getRatings
    {
        private static readonly string _endpointUrl = "https://oh-cosmos-sql.documents.azure.com:443/";
        private static readonly string _primaryKey = "ZMoQ00DFFs1O0wsj1kgU10HukOganoCSITH8UGDOr29ngr9UQVLKlbTHwLo8ZeP4AiA57178O23iuV06xySqQA==";
        private static readonly string _databaseId = "RatingsDB";
        private static readonly string _containerId = "Ratings";
        private static CosmosClient cosmosClient = new CosmosClient(_endpointUrl, _primaryKey);
        
        [FunctionName("func_getRatings")]
        public static async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req, ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string userId = null;

            if (req.GetQueryParameterDictionary()?.TryGetValue(@"userId", out userId) == true && !string.IsNullOrWhiteSpace(userId))
            {
                var sqlQueryText = $@"SELECT * FROM c WHERE c.userId='{userId}'";
                QueryDefinition queryDefinition = new QueryDefinition(sqlQueryText);
                // Run query against Cosmos DB
                var container = cosmosClient.GetContainer(_databaseId, _containerId);

                FeedIterator<dynamic> queryResultSetIterator = container.GetItemQueryIterator<dynamic>(queryDefinition, requestOptions: new QueryRequestOptions());
                List<dynamic> userRatings = new List<dynamic>();

                while (queryResultSetIterator.HasMoreResults)
                {
                    FeedResponse<dynamic> currentResultSet = await queryResultSetIterator.ReadNextAsync();
                    foreach (var item in currentResultSet)
                        userRatings.Add(item);
                }

                return !userRatings.Any() ? new NotFoundObjectResult($@"No ratings found for user '{userId}'") : (IActionResult)new OkObjectResult(userRatings);
            }
            else
                return new BadRequestObjectResult(@"userId is required as a query parameter");
        }
    }
}
