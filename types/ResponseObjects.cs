using System;
using Newtonsoft.Json;

namespace Sykehusinnkjop.Function
{


    // its important that all properties of responseUser is nullable, that way we can filter out values that are given.
    public class responseUser
    {
        [JsonProperty("accountEnabled")]
        public bool? AccountEnabled { get; set; }

        [JsonProperty("birthday")]
        public DateTimeOffset? Birthday { get; set; }

        [JsonProperty("city")]
        public string City { get; set; }

        [JsonProperty("companyName")]
        public string CompanyName { get; set; }

        [JsonProperty("country")]
        public string Country { get; set; }

        [JsonProperty("department")]
        public string Department { get; set; }

        [JsonProperty("displayName")]
        public string DisplayName { get; set; }

        [JsonProperty("employeeId")]
        public string EmployeeId { get; set; }

        [JsonProperty("givenName")]
        public string GivenName { get; set; }

        [JsonProperty("hireDate")]
        public DateTimeOffset? HireDate { get; set; } 

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("jobTitle")]
        public string JobTitle { get; set; }

        [JsonProperty("mail")]
        public string Mail { get; set; }

        [JsonProperty("mailNickname")]
        public string MailNickname { get; set; }

        [JsonProperty("mobilePhone")]
        public string MobilePhone { get; set; }

        [JsonProperty("officeLocation")]
        public string OfficeLocation { get; set; }

        [JsonProperty("pastProjects")]
        public string[] PastProjects { get; set; }

        [JsonProperty("passwordProfile")]
        public PasswordProfile? passwordProfile { get; set; }

        [JsonProperty("postalCode")]
        public string PostalCode { get; set; }

        [JsonProperty("state")]
        public string State { get; set; }

        [JsonProperty("streetAddress")]
        public string StreetAddress { get; set; }

        [JsonProperty("surname")]
        public string Surname { get; set; }

        [JsonProperty("userPrincipalName")]
        public string UserPrincipalName { get; set; }

    }
        public class OdataUsers
    {
        [JsonProperty("@odata.context")]
        public string Odata { get; set; }

        [JsonProperty("@odata.nextLink")]
        public string NextLink { get; set; }

        [JsonProperty("value")]
        public responseUser[] Users { get; set; }
    }


    public struct PasswordProfile
    {
        [JsonProperty("forceChangePasswordNextSignIn")]
        public bool ForceChangePasswordNextSignIn;

        [JsonProperty("forceChangePasswordNextSignInWithMfa")]
        public bool ForceChangePasswordNextSignInWithMfa;

        [JsonProperty("password")]
        public string Password; // 8 characters long, at least 1 capitalized letter and 1 number;
    }
}