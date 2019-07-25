using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json.Linq;
using Microsoft.Extensions.Logging;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;

namespace Sykehusinnkjop.Function
{


    public static class token {

        public static string getUserID(string token) {
            

            var Token = new JwtSecurityToken(token.Split("Bearer ")[1]);

            string currentUserID = Token.Claims.Where(c => c.Type == "oid").Select(prop => prop.Value).Single();

            return currentUserID;
        }
    }
    
    public static class security
    {


        public static bool isDirectReport(string managerID, string userID)
        {
            if (userID == "" || userID == null)
            {
                return false;
            }
            var request = new HttpRequestMessage(HttpMethod.Get,
             "https://graph.microsoft.com/v1.0/users/" +
             managerID +
             "/directReports/" +
             userID +
             "?$select=id");

            var response = graphController.Client.SendAsync(request).Result;

            return response.IsSuccessStatusCode;
        }

        public static bool isManager(string userID)
        {
            if (userID == "" || userID == null)
            {
                return false;
            }
            var request = new HttpRequestMessage(HttpMethod.Get,
            "https://graph.microsoft.com/v1.0/groups/" +
            Environment.GetEnvironmentVariable("security_group_ID") +
            "/members/" + userID);

            var response = graphController.Client.SendAsync(request).Result;

            return response.IsSuccessStatusCode;
        }


        // quick and dirty password generator, its OK because a user will have to reset their password before they can access their account.
        public static string generatePassword()
        {
            string numbers = "0,1,2,3,4,5,6,7,8,9";
            string smallCharacters = "abcdefghijklmnopqrstuvwxyz";
            string largeCharacters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

            var random = new Random();

            char[] password = new char[8];
            for (int i = 0; i < 8; i++)
            {
                password[i] = smallCharacters[random.Next(0, 25)];
            }

            int numberIndex = random.Next(0, 7);
            password[numberIndex] = numbers[random.Next(0, 9)];

            int largeCharacterIndex = 0;
            do
            {
                largeCharacterIndex = random.Next(0, 7);
            } while (largeCharacterIndex == numberIndex);

            password[largeCharacterIndex] = largeCharacters[random.Next(0, 25)];

            return new string(password);
        }
    }

    public static class Authenticate
    {
        private static string token;
        private static DateTime tokenCreatedAt;

        //Authenticate.getToken() will return a token to the resource configured in local.settings.json
        public static string getToken()
        {
            if (token == null || tokenCreatedAt == null || DateTime.Now.AddMinutes(-59) > tokenCreatedAt)
            {

                var authBody = new Dictionary<string, string>{
                    {"resource", "https://graph.microsoft.com"},
                    {"client_id",Environment.GetEnvironmentVariable("application_ID")},
                    {"client_secret",Environment.GetEnvironmentVariable("application_secret")},
                    {"grant_type","client_credentials"}
                };
                string tennantID = Environment.GetEnvironmentVariable("tenant_ID");

                using (HttpClient client = new HttpClient())
                {
                    client.BaseAddress = new Uri("https://login.microsoftonline.com/" + tennantID + "/oauth2/token");
                    client.Timeout = new TimeSpan(0, 0, 15);
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    var request = new HttpRequestMessage(HttpMethod.Post, "");
                    request.Content = new FormUrlEncodedContent(authBody);

                    var response = client.SendAsync(request).Result;

                    var body = JObject.Parse(response.Content.ReadAsStringAsync().Result);
                    if (response.IsSuccessStatusCode == true)
                    {

                        token = (string)body["access_token"];
                        tokenCreatedAt = DateTime.Now;
                        return token;
                    }
                    else
                    {
                        throw new ArgumentException(response.Content.ReadAsStringAsync().Result);
                    }
                }
            }
            else
            {
                return token;
            }
        }
    }
}