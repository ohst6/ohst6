using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.Net.Http;
using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace BFYOC.createRating
{
    public static class func_createRating
    {
        public class objRating
    {
        public string userId { get; set; }
        public string productId { get; set; }
        public string locationName { get; set; }
        public int rating { get; set; }
        public string userNotes { get; set; }
    }
    public class objStorRating
    {
        public string userId { get; set; }
        public string productId { get; set; }
        public string locationName { get; set; }
        public int rating { get; set; }
        public string userNotes { get; set; }
        public string guid {get; set;}
        public string timestamp {get;set;}
    }
        [FunctionName("func_createRating")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            objRating objRequest = JsonSerializer.Deserialize<objRating>(requestBody);

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
            {
                return new NotFoundResult();
            } 
            if (apiResponsePID == "Please pass a valid productId on the query string" || apiResponsePID == "")
            {
                return new NotFoundResult();
            }

            objStorRating currRating = new objStorRating();
            string g = Guid.NewGuid().ToString();
            string timeStamp = System.DateTime.Today.ToString();
            currRating.locationName = objRequest.locationName;
            currRating.productId = objRequest.productId;
            currRating.rating = objRequest.rating;
            currRating.userId = objRequest.userId;
            currRating.userNotes = objRequest.userNotes;
            currRating.timestamp = timeStamp;
            currRating.guid = g;

            string responseMessage = string.IsNullOrEmpty(objRequest.userId)
                ? "This HTTP triggered function executed successfully. Pass a name in the query string or in the request body for a personalized response."
                : $"Hello, {objRequest.userId}. This HTTP triggered function executed successfully.";

            return new OkObjectResult(responseMessage);
        }
        
    }
}
