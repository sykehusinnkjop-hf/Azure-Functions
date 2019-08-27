using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using Newtonsoft.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Sykehusinnkjop.Function
{



    public static class security
    {
        public static bool isDirectReport(string onBehalfToken, string userID)
        {
            if (userID == "" || userID == null)
            {
                return false;
            }
            var request = new HttpRequestMessage(HttpMethod.Get,
            "https://graph.microsoft.com/v1.0/me/"
            + "/directReports/"
            + userID
            + "?$select=id");
            request.Headers.Add("Authorization", "Bearer " + onBehalfToken);

            var response = graphController.Client.SendAsync(request).Result;

            return response.IsSuccessStatusCode;
        }


        //isManager will check if the currently logged in user is registered in the correct security group
        public static bool isManager(string onBehalfToken)
        {
            if (onBehalfToken == "" || onBehalfToken == null)
            {
                return false;
            }

            string managerID = Token.GetUserIDFromToken(onBehalfToken);
            var request = new HttpRequestMessage(HttpMethod.Get,
            "https://graph.microsoft.com/v1.0/users/"
            + managerID + "/memberof/"
            + Environment.GetEnvironmentVariable("security_group_ID"));
            request.Headers.Add("Authorization", "Bearer " + onBehalfToken);


            var response = graphController.Client.SendAsync(request).Result;
            if (!response.IsSuccessStatusCode)
            {
                throw new ArgumentException(response.Content.ReadAsStringAsync().Result);
            }

            return response.IsSuccessStatusCode;
        }

        // overloads the isManager method to check if another user is registered in the correct security group
        public static bool isManager(string onBehalfToken, string userID)
        {
            if (onBehalfToken == "" || onBehalfToken == null)
            {
                return false;
            }

            string managerID = userID;
            var request = new HttpRequestMessage(HttpMethod.Get,
            "https://graph.microsoft.com/v1.0/users/"
            + managerID + "/memberof/"
            + Environment.GetEnvironmentVariable("security_group_ID"));
            request.Headers.Add("Authorization", "Bearer " + onBehalfToken);


            var response = graphController.Client.SendAsync(request).Result;
            if (!response.IsSuccessStatusCode)
            {
                throw new ArgumentException(response.Content.ReadAsStringAsync().Result);
            }

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

        private const string resource = "https://graph.microsoft.com";
        private static readonly HttpClient Client;
        private static string tennantID = Environment.GetEnvironmentVariable("tenant_ID");
        private static string cliendID = Environment.GetEnvironmentVariable("application_ID");
        private static string applicationSecret = Environment.GetEnvironmentVariable("application_secret");


        // Key should be UserPrincipalName, value is Tokens
        private static Dictionary<string, Token> userTokens = new Dictionary<string, Token> { };

        static Authenticate()
        {
            Client = new HttpClient()
            {
                BaseAddress = new Uri("https://login.microsoftonline.com/"),
                Timeout = new TimeSpan(0, 0, 15),
            };
            Client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            Client.Timeout = new TimeSpan(0, 0, 15);
        }



        //getTokenOnBehalf takes a token that the that a user aquired, and tries to authenticate on behalf ot that user.
        //if authentication is successful it will return a new token that can be used to querry the graph api.
        public static async Task<Token> getTokenOnBehalf(string assertionToken, ILogger log)
        {
            assertionToken = assertionToken.Split("Bearer ")[1];
            string userPrincipalName = Token.GetUserIDFromToken(assertionToken);
            try // Get Token if it already exist
            {
                var userToken = userTokens[userPrincipalName];
                if (!userToken.isTimedOut() && userToken.assertionToken == assertionToken)
                {
                    return userToken;
                }
                else
                {
                    userTokens.Remove(userPrincipalName);
                }
            }
            catch (KeyNotFoundException) // if token does not exist fetch new token
            {
                return (await SendOnBehalfRequest(assertionToken, log));
            }
            catch (Exception error)
            {
                log.LogError(error.Message);
            }

            return (await SendOnBehalfRequest(assertionToken, log));
        }



        private static async Task<Token> SendOnBehalfRequest(string assertionToken, ILogger log)
        {
            var authBody = new Dictionary<string, string>{
                {"resource", resource},
                {"assertion", assertionToken},
                {"client_id", cliendID},
                {"client_secret",applicationSecret},
                {"grant_type","urn:ietf:params:oauth:grant-type:jwt-bearer"},
                {"requested_token_use", "on_behalf_of"},
            };


            var request = new HttpRequestMessage(HttpMethod.Post, tennantID + "/oauth2/token");
            request.Content = new FormUrlEncodedContent(authBody);


            var response = await Client.SendAsync(request);
            string responseBody = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                log.LogError(responseBody);
                return new Token{isAuthenticated = false};
            }


            Token tokenResponse = JsonConvert.DeserializeObject<Token>(responseBody);
            tokenResponse.createdAt = DateTime.Now;
            tokenResponse.assertionToken = assertionToken;

            string userPrincipalName = Token.GetUserIDFromToken(assertionToken);
            try
            {
                userTokens.Add(userPrincipalName, tokenResponse);
            }
            catch (ArgumentException)
            {
                userTokens.Remove(userPrincipalName);
                userTokens.Add(userPrincipalName, tokenResponse);
            }

            return tokenResponse;
        }
    }



    public class Token
    {
        [JsonProperty("token_type")]
        internal string tokenType;


        [JsonProperty("scope")]
        internal string scope;


        [JsonProperty("expires_in")]
        internal int expiresIn;


        [JsonProperty("expires_on")]
        internal int expiresOn;


        [JsonProperty("not_before")]
        internal int notBefore;


        [JsonProperty("resource")]
        internal string resource;


        [JsonProperty("access_token")]
        internal string onBehalfToken;

        internal string assertionToken;


        internal DateTime createdAt;

        internal bool isAuthenticated = true;


        internal bool isTimedOut()
        {
            if (DateTime.Now > createdAt.AddSeconds(expiresIn - 5))
            {
                return true;
            }
            return false;
        }

        internal string GetUserID()
        {
            JwtSecurityToken Token = new JwtSecurityToken(assertionToken);
            return Token.Claims.Where(c => c.Type == "oid").Select(prop => prop.Value).Single();
        }
        internal static string GetUserIDFromToken(string token)
        {
            JwtSecurityToken Token = new JwtSecurityToken(token);
            return Token.Claims.Where(c => c.Type == "oid").Select(prop => prop.Value).Single();
        }
    }
}