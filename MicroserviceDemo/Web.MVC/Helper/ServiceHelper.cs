using Consul;
using Microsoft.Extensions.Configuration;
using RestSharp;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;

namespace Web.MVC.Helper
{
    public class ServiceHelper : IServiceHelper
    {
        private readonly IConfiguration _configuration;
        private readonly ConsulClient _consulClient;
        private ConcurrentBag<string> _orderServiceUrls;
        private ConcurrentBag<string> _productServiceUrls;

        public ServiceHelper( IConfiguration configuration )
        {
            _configuration = configuration;
            _consulClient = new ConsulClient(c =>
            {
                //consul地址
                c.Address = new Uri(_configuration["ConsulSetting:ConsulAddress"]);
            });
        }

        public async Task<string> GetOrder()
        {
            var consulClient = new ConsulClient(c =>
            {
                //consul地址
                c.Address = new Uri(_configuration["ConsulSetting:ConsulAddress"]);
            });
            var services =(await consulClient.Health.Service("OrderService", null, true, null)).Response;//健康的服务

            string[] serviceUrls = services.Select(p => $"http://{p.Service.Address + ":" + p.Service.Port}").ToArray();//订单服务地址列表

            if (!serviceUrls.Any())
            {
                return await Task.FromResult("【订单服务】服务列表为空");
            }

            //string[] serviceUrls = { "http://localhost:9060/api", "http://localhost:9061/api", "http://localhost:9062/api" };
            //string serviceUrl = "http://localhost:9060/api";

            var client = new RestClient(serviceUrls[new Random().Next(0, serviceUrls.Length)]+"/api");
            var request = new RestRequest("/orders", Method.GET);

            var response = await client.ExecuteAsync(request);
            return response.Content;
        }

        public async Task<string> GetProduct()
        {
            //string[] serviceUrls = { "http://localhost:9050/api", "http://localhost:9051/api", "http://localhost:9052/api" };
            ////string serviceUrl = "http://localhost:9050/api";
            var consulClient = new ConsulClient(c =>
            {
                //consul地址
                c.Address = new Uri(_configuration["ConsulSetting:ConsulAddress"]);
            });
            var services = (await consulClient.Health.Service("ProductService", null, true, null)).Response;//健康的服务

            string[] serviceUrls = services.Select(p => $"http://{p.Service.Address + ":" + p.Service.Port}").ToArray();//订单服务地址列表

            if (!serviceUrls.Any())
            {
                return await Task.FromResult("【订单服务】服务列表为空");
            }

            var client = new RestClient(serviceUrls[new Random().Next(0, serviceUrls.Length)]+"/api");
            var request = new RestRequest("/products", Method.GET);

            var response = await client.ExecuteAsync(request);
            return response.Content;

        }

        public void GetService()
        {
            var serviceNames = new string[] { "OrderService", "ProductService" };
            Array.ForEach(serviceNames, p =>
            {
                Task.Run(() =>
                {
                    //WaitTime默认为5分钟
                    var queryOptions = new QueryOptions { WaitTime = TimeSpan.FromMinutes(10) };
                    while (true)
                    {
                        GetServices(queryOptions, p);
                    }
                });
            });
        }

        private void GetServices( QueryOptions queryOptions, string serviceName )
        {
            var res = _consulClient.Health.Service(serviceName, null, true, queryOptions).Result;

            //控制台打印一下获取服务列表的响应时间等信息
            Console.WriteLine($"{DateTime.Now}获取{serviceName}：queryOptions.WaitIndex：{queryOptions.WaitIndex}  LastIndex：{res.LastIndex}");

            //版本号不一致 说明服务列表发生了变化
            if (queryOptions.WaitIndex != res.LastIndex)
            {
                queryOptions.WaitIndex = res.LastIndex;

                //服务地址列表
                var serviceUrls = res.Response.Select(p => $"http://{p.Service.Address + ":" + p.Service.Port}").ToArray();

                if (serviceName == "OrderService")
                    _orderServiceUrls = new ConcurrentBag<string>(serviceUrls);
                else if (serviceName == "ProductService")
                    _productServiceUrls = new ConcurrentBag<string>(serviceUrls);
            }
        }
    }
}
