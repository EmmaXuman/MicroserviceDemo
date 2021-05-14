using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Web.MVC
{
    public class ServiceHelper : IServiceHelper
    {
        public async Task<string> GetOrder()
        {
            string[] serviceUrls = { "http://localhost:9060/api", "http://localhost:9061/api", "http://localhost:9062/api" };
            //string serviceUrl = "http://localhost:9060/api";

            var client = new RestClient(serviceUrls[new Random().Next(0,3)]);
            var request = new RestRequest("/orders",Method.GET);

            var response = await client.ExecuteAsync(request);
            return response.Content;
        }

        public async Task<string> GetProduct()
        {
            string[] serviceUrls = { "http://localhost:9050/api", "http://localhost:9051/api", "http://localhost:9052/api" };
            //string serviceUrl = "http://localhost:9050/api";

            var client = new RestClient(serviceUrls[new Random().Next(0,3)]);
            var request = new RestRequest("/products", Method.GET);

            var response = await client.ExecuteAsync(request);
            return response.Content;

        }
    }
}
