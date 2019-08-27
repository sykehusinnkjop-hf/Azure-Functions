using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Sykehusinnkjop.Function;
using System.Net.Http;
using Newtonsoft.Json.Linq;
using System.Text;

namespace Sykehusinnkjop.BrukerPortalen
{
    public static class Manager
    {
        //==============================================================================================================================//
        //Get Managers gets a list of all managers in the organization. this requires that there exists a Security group
        //in the AAD and that the security group ID is registered in the app settings.
        [FunctionName("getManagers")]
        public static async Task<IActionResult> getManagers(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "Managers")] HttpRequest req, ILogger log)
        {
            var token = await Authenticate.getTokenOnBehalf(req.Headers["Authorization"], log);
            if (!token.isAuthenticated)
            {
                return new UnauthorizedResult();
            }


            if (!security.isManager(token.onBehalfToken))
            {
                return new UnauthorizedResult();
            }

            var request = new HttpRequestMessage(HttpMethod.Get, "/v1.0/groups/" + props.managerSecurityGroupID + "/members" + "?$top=999&" + props.userProperties);
            request.Headers.Add("Authorization", "Bearer " + token.onBehalfToken);
            HttpResponseMessage response = await graphController.Client.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                log.LogError("getManagers failed and fetched the error:" + await response.Content.ReadAsStringAsync());
                return new BadRequestObjectResult(await response.Content.ReadAsStringAsync());
            }

            OdataUsers Managers = JsonConvert.DeserializeObject<OdataUsers>(await response.Content.ReadAsStringAsync());
            responseUser[] responseManagers = Managers.Users;
            return new OkObjectResult(responseManagers);
        }

        //==============================================================================================================================//


        // assignDirectReport takes managerUserID as a URL parameter and the DirectReportUserID as a json parameter.
        [FunctionName("assignDirectReportToManager")]
        public static async Task<IActionResult> assignDirectReportToManager(
            [HttpTrigger(AuthorizationLevel.Function, "put", Route = "Managers/{newManagerUserID}")] HttpRequest req, string newManagerUserID, ILogger log)
        {
            var token = await Authenticate.getTokenOnBehalf(req.Headers["Authorization"], log);
            if (!token.isAuthenticated)
            {
                return new UnauthorizedResult();
            }


            responseUser DirectReport = JsonConvert.DeserializeObject<responseUser>(new StreamReader(req.Body).ReadToEnd());


            if (!security.isManager(token.onBehalfToken))
            {
                return new UnauthorizedResult();
            }
            if (!security.isManager(token.onBehalfToken, newManagerUserID))
            {
                return new UnauthorizedResult();
            }
            if (!security.isDirectReport(token.onBehalfToken, DirectReport.Id))
            {
                log.LogError("could not find directReport with id " + DirectReport.Id);
                return new NotFoundObjectResult(new JObject(new JProperty("error", "could not find directReport with id " + DirectReport.Id)));
            }

            var requestContent = new JObject(new JProperty("@odata.id", "https://graph.microsoft.com/v1.0/users/" + newManagerUserID));
            var request = new HttpRequestMessage(HttpMethod.Put, "/v1.0/users/" + DirectReport.Id + "/manager/$ref");
            request.Headers.Add("Authorization", "Bearer " + token.onBehalfToken);
            request.Content = new StringContent(requestContent.ToString(), Encoding.UTF8, "application/json");

            HttpResponseMessage response = await graphController.Client.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                return new StatusCodeResult(Int32.Parse(response.StatusCode.ToString()));
            }

            return new OkObjectResult(new JObject(new JProperty("id", newManagerUserID)));
        }
    }

    public static class DirectReport
    {
        //==============================================================================================================================//
        // Get a list of user that directly reports to the manager doing the request.
        [FunctionName("getDirectReports")]
        public static async Task<IActionResult> getDirectReports(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "DirectReports")] HttpRequest req, ILogger log)
        {

            var token = await Authenticate.getTokenOnBehalf(req.Headers["Authorization"], log);
            if (!token.isAuthenticated)
            {
                return new UnauthorizedResult();
            }

            string managerUserID = token.GetUserID();

            if (!security.isManager(token.onBehalfToken))
            {
                return new UnauthorizedResult();
            }


            var request = new HttpRequestMessage(HttpMethod.Get, "/v1.0/users/" + managerUserID + "/directReports" + "?$top=999&" + props.userProperties);
            request.Headers.Add("Authorization", "Bearer " + token.onBehalfToken);

            HttpResponseMessage response = await graphController.Client.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                return new BadRequestObjectResult(await response.Content.ReadAsStringAsync());
            }

            OdataUsers directReporsts = JsonConvert.DeserializeObject<OdataUsers>(await response.Content.ReadAsStringAsync());
            responseUser[] responseDirectReports = directReporsts.Users;

            return new OkObjectResult(responseDirectReports);
        }

        //===================================================================================================================================================================//




        // Get a single user based on ID passed in the URI that directly reports to the manager doing the request.
        [FunctionName("getDirectReport")]
        public static async Task<IActionResult> getDirectReport(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "DirectReports/{DirectReportUserID}")] HttpRequest req, string DirectReportUserID, ILogger log)
        {

            var token = await Authenticate.getTokenOnBehalf(req.Headers["Authorization"], log);
            if (!token.isAuthenticated)
            {
                return new UnauthorizedResult();
            }

            string managerUserID = token.GetUserID();
            if (!security.isManager(token.onBehalfToken))
            {
                return new UnauthorizedResult();
            }

            var request = new HttpRequestMessage(HttpMethod.Get, "/v1.0/users/" + managerUserID + "/DirectReports/" + DirectReportUserID);
            request.Headers.Add("Authorization", "Bearer " + token.onBehalfToken);

            HttpResponseMessage response = await graphController.Client.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                var content = new ContentResult();
                content.StatusCode = (int)response.StatusCode;
                content.Content = await response.Content.ReadAsStringAsync();
                return content;
            }

            responseUser directReport = JsonConvert.DeserializeObject<responseUser>(await response.Content.ReadAsStringAsync());
            return new OkObjectResult(directReport);
        }


        //==============================================================================================================================//





        // allows a manager to update the information about users that directly reporsts to the manager doing the request.
        [FunctionName("updateDirectReport")]
        public static async Task<IActionResult> updateDirectReport(
            [HttpTrigger(AuthorizationLevel.Function, "patch", Route = "DirectReports/{directReportUserID}")] HttpRequest req, string directReportUserID, ILogger log)
        {

            var token = await Authenticate.getTokenOnBehalf(req.Headers["Authorization"], log);
            if (!token.isAuthenticated)
            {
                return new UnauthorizedResult();
            }

            if (!security.isManager(token.onBehalfToken) || !security.isDirectReport(token.onBehalfToken, directReportUserID))
            {
                return new UnauthorizedResult();
            }

            // Deserialize and Serialize the object straight after each other to make sure only valid fields are passed by the caller
            responseUser DirectReport;
            try
            {
                DirectReport = JsonConvert.DeserializeObject<responseUser>(new StreamReader(req.Body).ReadToEnd(), new JsonSerializerSettings
                {
                    MissingMemberHandling = MissingMemberHandling.Error
                });
            }
            catch (Exception ex)
            {
                return new BadRequestObjectResult(new JObject(new JProperty("error", ex.Message)));
            }
            string JsonDirectReport = JsonConvert.SerializeObject(DirectReport, Formatting.Indented, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            });

            var request = new HttpRequestMessage(HttpMethod.Patch, "/v1.0/users/" + directReportUserID);
            request.Headers.Add("Authorization", "Bearer " + token.onBehalfToken);
            request.Content = new StringContent(JsonDirectReport, Encoding.UTF8, "application/json");

            HttpResponseMessage response = await graphController.Client.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                return new BadRequestObjectResult(await response.Content.ReadAsStringAsync());
            }

            return new OkObjectResult(DirectReport);
        }


        //==============================================================================================================================//




        //required fields when creating a user is
        // accountEnabled, displayName, mailNickname, passwordProfile, userPrincipalName
        [FunctionName("createDirectReport")]
        public static async Task<IActionResult> createDirectReport(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "DirectReports")] HttpRequest req, ILogger log)
        {
            var token = await Authenticate.getTokenOnBehalf(req.Headers["Authorization"], log);
            if (!token.isAuthenticated)
            {
                return new UnauthorizedResult();
            }

            string managerUserID = token.GetUserID();
            if (!security.isManager(token.onBehalfToken))
            {
                return new UnauthorizedResult();
            }


            responseUser NewDirectReport;
            try
            {
                NewDirectReport = JsonConvert.DeserializeObject<responseUser>(new StreamReader(req.Body).ReadToEnd(), new JsonSerializerSettings
                {
                    MissingMemberHandling = MissingMemberHandling.Error
                });
            }
            catch (Exception ex)
            {
                return new BadRequestObjectResult(new JObject(new JProperty("error", ex.Message)));
            }
            NewDirectReport.passwordProfile = new PasswordProfile
            {
                ForceChangePasswordNextSignIn = props.ForceChangePasswordNextSignIn,
                ForceChangePasswordNextSignInWithMfa = props.ForceChangePasswordNextSignInWithMfa,
                Password = security.generatePassword()
            };

            string JsonNewDirectReport = JsonConvert.SerializeObject(NewDirectReport, Formatting.Indented, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            });

            var request = new HttpRequestMessage(HttpMethod.Post, "/v1.0/users/");
            request.Headers.Add("Authorization", "Bearer " + token.onBehalfToken);
            request.Content = new StringContent(JsonNewDirectReport, Encoding.UTF8, "application/json");

            HttpResponseMessage response = await graphController.Client.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                return new BadRequestObjectResult(await response.Content.ReadAsStringAsync());
            }


            //----------------------------------------------------------------------------------------------------------------------//
            // When a user is created, we also need to bind it to the manager that creates the user.
            responseUser DirectReport = JsonConvert.DeserializeObject<responseUser>(response.Content.ReadAsStringAsync().Result);
            var requestContent = new JObject(new JProperty("@odata.id", "https://graph.microsoft.com/v1.0/me/"));

            request = new HttpRequestMessage(HttpMethod.Put, "/v1.0/users/" + DirectReport.Id + "/manager/$ref");
            request.Headers.Add("Authorization", "Bearer " + token.onBehalfToken);
            request.Content = new StringContent(requestContent.ToString(), Encoding.UTF8, "application/json");

            response = graphController.Client.SendAsync(request).Result;
            if (!response.IsSuccessStatusCode)
            {
                return new BadRequestObjectResult(response.Content.ReadAsStringAsync().Result);
            }
            //----------------------------------------------------------------------------------------------------------------------//

            return new OkObjectResult(DirectReport);
        }
    }





    //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++//
    //========================================================================================================================================================================================================================================================================================//
    // Enviroment variables
    public static class props
    {
        public static string managerSecurityGroupID = Environment.GetEnvironmentVariable("security_group_ID");
        public static string userProperties = "$select=accountEnabled,birthday,city,companyName,country,department,displayName,employeeId,givenName,hireDate,id,jobTitle,mail,mailNickname,mobilePhone,officeLocation,pastProjects,postalCode,state,streetAddress,surname,userPrincipalName";
        public static bool ForceChangePasswordNextSignIn = bool.TryParse(Environment.GetEnvironmentVariable("force_change_password_new_users"), out bool result) && result;
        public static bool ForceChangePasswordNextSignInWithMfa = bool.TryParse(Environment.GetEnvironmentVariable("force_mfa_on_new_users"), out bool result) && result;
    }
}
