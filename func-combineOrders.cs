using System;
using System.IO;
using System.Text.Json;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text.Json.Serialization;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace BFYOC.ohst6
{
    public static class combineOrders
    {
        public class OrderItem
        {
            public string orderHeaderDetailsCSVUrl { get; set; }
            public string orderLineItemsCSVUrl { get; set; }
            public string productInformationCSVUrl { get; set; }
        }
        [FunctionName("combineOrders")]
        public static void Run([BlobTrigger("orders/{name}", Connection = "ohst6orders_STORAGE")] Stream myBlob, string name, ILogger log)
        {
            log.LogInformation($"C# Blob trigger function Processed blob\n Name:{name} \n Size: {myBlob.Length} Bytes");
            string connectionString = Environment.GetEnvironmentVariable("ohst6orders_STORAGE");

            string[] orderNumbers = name.Split("-");
            string oNumber = orderNumbers[0];
            int fileCounter = 0;
            BlobServiceClient blobServiceClient = new BlobServiceClient(connectionString);
            var container = blobServiceClient.GetBlobContainerClient("orders");
            string baseURI = container.Uri.ToString();
            foreach (BlobItem blobItem in container.GetBlobs(prefix: oNumber))
            {
                fileCounter++;
                log.LogInformation(baseURI + "/" + blobItem.Name);
            }
            OrderItem orderItem = new OrderItem();
            if (fileCounter == 3)
            {

                foreach (BlobItem blobItem in container.GetBlobs(prefix: oNumber))
                {
                    if (blobItem.Name.Contains("OrderHeaderDetails"))
                    {
                        orderItem.orderHeaderDetailsCSVUrl = baseURI + "/" + blobItem.Name;
                    }
                    if (blobItem.Name.Contains("OrderLineItems"))
                    {
                        orderItem.orderLineItemsCSVUrl = baseURI + "/" + blobItem.Name;
                    }
                    if (blobItem.Name.Contains("ProductInformation"))
                    {
                        orderItem.productInformationCSVUrl = baseURI + "/" + blobItem.Name;
                    }
                }

            }
            string jsonOrder2Combine = JsonSerializer.Serialize(orderItem);
            var data = new StringContent(jsonOrder2Combine, Encoding.UTF8, "application/json");


            using var client = new HttpClient();
            var response = client.PostAsync("https://serverlessohmanagementapi.trafficmanager.net/api/order/combineOrderContent", data);
            
            var resultsCombined = response.Result.Content.ReadAsStringAsync().Result; 

        }
    }
}