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
using System.Net.Http.Headers;
using System.Text;

namespace Sykehusinnkjop.BrukerPortalen
{
    public static class props
    {
        public static string userProperties = Environment.GetEnvironmentVariable("display_user_properties");
        public static string managerSecurityGroupID = Environment.GetEnvironmentVariable("manager_Security_Group_ID");
        public static bool ForceChangePasswordNextSignIn = bool.Parse(Environment.GetEnvironmentVariable("force_change_password_new_users"));
        public static bool ForceChangePasswordNextSignInWithMfa = bool.Parse(Environment.GetEnvironmentVariable("force_mfa_on_new_users"));
    }

    public static class Manager
    {


        //Get Managers gets a list of all managers in the organization. this requires that there exists a Security group
        //in the AAD and that the security group ID is registered in the app settings.
        [FunctionName("getManagers")]
        public static async Task<IActionResult> getManagers(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "Managers")] HttpRequest req, ILogger log)
        {
            // cant find a way to do this check before every function, any suggestions would be greatly appreaciated.
            string managerUserID = req.Headers["X-MS-CLIENT-PRINCIPAL-ID"];
            if (security.isManager(managerUserID) != true)
            {
                return new UnauthorizedResult();
            }

            var request = new HttpRequestMessage(HttpMethod.Get, "/v1.0/groups/" + props.managerSecurityGroupID + "/members" + "?$top=999&" + props.userProperties);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Authenticate.getToken());

            HttpResponseMessage response = graphController.Client.SendAsync(request).Result;

            if (!response.IsSuccessStatusCode)
            {
                return new BadRequestObjectResult(response.Content.ReadAsStringAsync().Result);
            }

            OdataUsers Managers = JsonConvert.DeserializeObject<OdataUsers>(response.Content.ReadAsStringAsync().Result);
            responseUser[] responseManagers = Managers.Users;
            return new OkObjectResult(responseManagers);
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // assignDirectReport takes managerUserID as a URL parameter and the DirectReportUserID as a json parameter.
        [FunctionName("assignDirectReportToManager")]
        public static async Task<IActionResult> assignDirectReportToManager(
            [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "Managers/{newManagerUserID}")] HttpRequest req, string newManagerUserID, ILogger log)
        {
            // checking that the requesting manager and that the new manager is registered in the correct security group.
            string managerUserID = req.Headers["X-MS-CLIENT-PRINCIPAL-ID"];
            responseUser DirectReport = JsonConvert.DeserializeObject<responseUser>(new StreamReader(req.Body).ReadToEnd());


            if (!security.isManager(managerUserID) || !security.isManager(newManagerUserID) || !security.isDirectReport(managerUserID, DirectReport.Id))
            {
                return new UnauthorizedResult();
            }

            var requestContent = new JObject(new JProperty("@odata.id", "https://graph.microsoft.com/v1.0/users/" + newManagerUserID));
            var request = new HttpRequestMessage(HttpMethod.Put, "/v1.0/users/" + DirectReport.Id + "/manager/$ref");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Authenticate.getToken());
            request.Content = new StringContent(requestContent.ToString(), Encoding.UTF8, "application/json");

            HttpResponseMessage response = graphController.Client.SendAsync(request).Result;
            if (!response.IsSuccessStatusCode)
            {
                return new StatusCodeResult(Int32.Parse(response.StatusCode.ToString()));
            }

            return new OkObjectResult(new JObject(new JProperty("id", newManagerUserID)));
        }



    }


    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    public static class DirectReport
    {

        [FunctionName("getDirectReports")]
        public static async Task<IActionResult> getDirectReports(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "DirectReports")] HttpRequest req, ILogger log)
        {

            // cant find a way to do this check before every function, any suggestions would be greatly appreaciated.
            string managerUserID = req.Headers["X-MS-CLIENT-PRINCIPAL-ID"];
            if (security.isManager(managerUserID) != true)
            {
                return new UnauthorizedResult();
            }


            var request = new HttpRequestMessage(HttpMethod.Get, "/v1.0/users/" + managerUserID + "/directReports" + "?$top=999&" + props.userProperties);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Authenticate.getToken());

            HttpResponseMessage response = graphController.Client.SendAsync(request).Result;
            if (!response.IsSuccessStatusCode)
            {
                return new BadRequestObjectResult(response.Content.ReadAsStringAsync().Result);
            }

            OdataUsers directReporsts = JsonConvert.DeserializeObject<OdataUsers>(response.Content.ReadAsStringAsync().Result);
            responseUser[] responseDirectReports = directReporsts.Users;

            return new OkObjectResult(responseDirectReports);
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        [FunctionName("getDirectReport")]
        public static async Task<IActionResult> getDirectReport(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "DirectReports/{DirectReportUserID}")] HttpRequest req, string DirectReportUserID, ILogger log)
        {

            // cant find a way to do this check before every function, any suggestions would be greatly appreaciated.
            string managerUserID = req.Headers["X-MS-CLIENT-PRINCIPAL-ID"];
            if (security.isManager(managerUserID) != true)
            {
                return new UnauthorizedResult();
            }

            var request = new HttpRequestMessage(HttpMethod.Get, "/v1.0/users/" + managerUserID + "/DirectReports/" + DirectReportUserID);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Authenticate.getToken());

            HttpResponseMessage response = graphController.Client.SendAsync(request).Result;
            if (!response.IsSuccessStatusCode)
            {
                return new BadRequestObjectResult(response.Content.ReadAsStringAsync().Result);
            }

            responseUser directReport = JsonConvert.DeserializeObject<responseUser>(response.Content.ReadAsStringAsync().Result);
            return new OkObjectResult(directReport);
        }


        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // Updates the fields requested, if a field is nulled or doesnt exist, it wont update the user store
        [FunctionName("updateDirectReport")]
        public static async Task<IActionResult> updateDirectReport(
            [HttpTrigger(AuthorizationLevel.Anonymous, "patch", Route = "DirectReports/{directReportUserID}")] HttpRequest req, string directReportUserID, ILogger log)
        {

            // cant find a way to do this check before every function, any suggestions would be greatly appreaciated.
            string managerUserID = req.Headers["X-MS-CLIENT-PRINCIPAL-ID"];
            if (!security.isManager(managerUserID) || !security.isDirectReport(managerUserID, directReportUserID))
            {
                return new UnauthorizedResult();
            }

            // Deserialize and Serialize the object straight after each other to make sure only valid fields are passed by the caller
            responseUser DirectReport = JsonConvert.DeserializeObject<responseUser>(new StreamReader(req.Body).ReadToEnd());
            string JsonDirectReport = JsonConvert.SerializeObject(DirectReport, Formatting.Indented, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            });

            var request = new HttpRequestMessage(HttpMethod.Patch, "/v1.0/users/" + directReportUserID);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Authenticate.getToken());
            request.Content = new StringContent(JsonDirectReport, Encoding.UTF8, "application/json");

            HttpResponseMessage response = graphController.Client.SendAsync(request).Result;
            if (!response.IsSuccessStatusCode)
            {
                return new BadRequestObjectResult(response.Content.ReadAsStringAsync().Result);
            }

            return new OkObjectResult(DirectReport);
        }


        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //required fields when creating a user is
        // accountEnabled, displayName, mailNickname, passwordProfile, userPrincipalName
        [FunctionName("createDirectReport")]
        public static async Task<IActionResult> createDirectReport(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "DirectReports")] HttpRequest req, ILogger log)
        {
            // cant find a way to do this check before every function, any suggestions would be greatly appreaciated.
            string managerUserID = req.Headers["X-MS-CLIENT-PRINCIPAL-ID"];
            if (security.isManager(managerUserID) != true)
            {
                return new UnauthorizedResult();
            }


            responseUser NewDirectReport = JsonConvert.DeserializeObject<responseUser>(new StreamReader(req.Body).ReadToEnd());
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
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Authenticate.getToken());
            request.Content = new StringContent(JsonNewDirectReport, Encoding.UTF8, "application/json");

            HttpResponseMessage response = graphController.Client.SendAsync(request).Result;
            if (!response.IsSuccessStatusCode)
            {
                return new BadRequestObjectResult(response.Content.ReadAsStringAsync().Result);
            }


            ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            // When a user is created, we also need to bind it to the manager that creates the user.
            responseUser DirectReport = JsonConvert.DeserializeObject<responseUser>(response.Content.ReadAsStringAsync().Result);
            var requestContent = new JObject(new JProperty("@odata.id", "https://graph.microsoft.com/v1.0/users/" + managerUserID));

            request = new HttpRequestMessage(HttpMethod.Put, "/v1.0/users/" + DirectReport.Id + "/manager/$ref");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Authenticate.getToken());
            request.Content = new StringContent(requestContent.ToString(), Encoding.UTF8, "application/json");

            response = graphController.Client.SendAsync(request).Result;
            if (!response.IsSuccessStatusCode)
            {
                return new BadRequestObjectResult(response.Content.ReadAsStringAsync().Result);
            }
            /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

            return new OkObjectResult(DirectReport);
        }
    }





}
