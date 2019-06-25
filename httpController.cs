using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json.Linq;

namespace Sykehusinnkjop.Function
{
    public static class graphController
    {
        public static readonly HttpClient Client;
        static graphController()
        {
            Client = new HttpClient()
            {
                BaseAddress = new Uri(Environment.GetEnvironmentVariable("resource_URL")),
                Timeout = new TimeSpan(0, 0, 15),
            };
            Client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }
    }
}