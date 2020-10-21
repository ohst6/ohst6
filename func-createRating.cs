using System;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Net.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace BFYOC.ohts6
{
    public static class func_createRating
    {
        private static readonly string _endpointUrl = "https://oh-cosmos-sql.documents.azure.com:443/";
        private static readonly string _primaryKey = "ZMoQ00DFFs1O0wsj1kgU10HukOganoCSITH8UGDOr29ngr9UQVLKlbTHwLo8ZeP4AiA57178O23iuV06xySqQA==";
        private static readonly string _databaseId = "RatingsDB";
        private static readonly string _containerId = "Ratings";
        private static CosmosClient cosmosClient = new CosmosClient(_endpointUrl, _primaryKey);

        [FunctionName("func_createRating")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            Rating objRequest = JsonSerializer.Deserialize<Rating>(requestBody);

            /* Validate User ID */
            using var clientUID = new HttpClient();
            string userQuery = "?UserID=" + objRequest.userId;            

            var resultUserID = await clientUID.GetAsync("https://serverlessohuser.trafficmanager.net/api/GetUser/" + userQuery);
            string apiResponseUID = await resultUserID.Content.ReadAsStringAsync();

            /* Validate Product ID */
            using var clientPID = new HttpClient();
            string productQuery = "?productID=" + objRequest.productId;  

            var resultProductID = await clientPID.GetAsync("https://serverlessohproduct.trafficmanager.net/api/GetProduct/" + productQuery);
            string apiResponsePID = await resultProductID.Content.ReadAsStringAsync();

            if (apiResponseUID == "Please pass a valid userId on the query string" || apiResponseUID == "")
                return new NotFoundResult();

            if (apiResponsePID == "Please pass a valid productId on the query string" || apiResponsePID == "")
                return new NotFoundResult();

            string regEx = @"^([0-5]{1})$";
            if (!Regex.IsMatch(objRequest.rating.ToString(), regEx))
                return new BadRequestObjectResult(@"Please choose a rating between 0 and 5");

            Rating currRating = new Rating();
            currRating.locationName = objRequest.locationName;
            currRating.productId = objRequest.productId;
            currRating.rating = objRequest.rating;
            currRating.userId = objRequest.userId;
            currRating.userNotes = objRequest.userNotes;
            currRating.timeStamp = System.DateTime.Today.ToString();
            currRating.id = Guid.NewGuid().ToString();;

            var container = cosmosClient.GetContainer(_databaseId, _containerId);
            try{
                await container.UpsertItemAsync<Rating>(currRating, new PartitionKey(currRating.userId));
            }
            catch (Exception e) {
                return new BadRequestObjectResult(@"Error creating the rating into the DB");
            }

            string responseMessage = string.IsNullOrEmpty(objRequest.userId)
                ? "This HTTP triggered function executed successfully. Pass a name in the query string or in the request body for a personalized response."
                : $"Hello, {objRequest.userId}. This HTTP triggered function executed successfully.";

            return new OkObjectResult(responseMessage);
        }
        
    }
}
