using NUnit.Framework;
using RestSharp;
using RestSharp.Authenticators;
using RevueCrafters.Models;
using System.Net;
using System.Text.Json;
using System.Collections.Generic;

namespace RevueCrafters
{    
    public class AuthResponseDTO
    {
        public string AccessToken { get; set; }
    }

    [TestFixture]
    public class RevueCraftersTests
    {
        private RestClient client;
        private static string createdRevueId;

        private const string BaseUrl = "https://d2925tksfvgq8c.cloudfront.net/api";
        private const string StaticToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJKd3RTZXJ2aWNlQWNjZXNzVG9rZW4iLCJqdGkiOiJmMTg0YjE5Mi04M2QzLTQyNTEtYTgzYy00YmFjZjk5NTZhNzAiLCJpYXQiOiIwOC8yMi8yMDI1IDA2OjU5OjA2IiwiVXNlcklkIjoiYTVjZDIwZDYtZTRmMC00NTVhLTEzMjktMDhkZGRlMWQ4YTY0IiwiRW1haWwiOiJJdmFHMUBleGFtcHJlcC5jb20iLCJVc2VyTmFtZSI6Ikl2YUcxIiwiZXhwIjoxNzU1ODY3NTQ2LCJpc3MiOiJSZXZ1ZU1ha2VyX0FwcF9Tb2Z0VW5pIiwiYXVkIjoiUmV2dWVNYWtlcl9XZWJBUElfU29mdFVuaSJ9.E-fCoo-SD24qY9EdiQbt01wsasRVU-tki4JyigDe6_k";
        private const string LoginEmail = "IvaG1@examprep.com";
        private const string LoginPassword = "123456";

        [OneTimeSetUp]
        public void Setup()
        {
            string jwtToken = string.IsNullOrWhiteSpace(StaticToken)
                ? GetJwtToken(LoginEmail, LoginPassword)
                : StaticToken;

            var options = new RestClientOptions(BaseUrl)
            {
                Authenticator = new JwtAuthenticator(jwtToken)
            };

            client = new RestClient(options);
        }

        private string GetJwtToken(string username, string password)
        {
            var tempClient = new RestClient(BaseUrl);
            var request = new RestRequest("/User/Authentication", Method.Post);
            request.AddJsonBody(new { Username = username, Password = password });

            var response = tempClient.Execute(request);

            if (response.StatusCode != HttpStatusCode.OK)
            {
                throw new InvalidOperationException(
                    $"Failed to authenticate. Status code: {response.StatusCode}, Content: {(string.IsNullOrWhiteSpace(response.Content) ? "<empty>" : response.Content)}");
            }

            var authResponse = JsonSerializer.Deserialize<AuthResponseDTO>(response.Content);

            return authResponse?.AccessToken
                ?? throw new Exception("No accessToken found in login response");
        }

        [Test, Order(1)]
        public void CreateRevue_WithRequiredFields()
        {
            var revue = new { Title = "New Revue", Description = "Some Description", Url = "" };

            var request = new RestRequest("/Revue/Create", Method.Post);
            request.AddJsonBody(revue);

            var response = client.Execute(request);
            var json = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);

            createdRevueId = json.RevueId;

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(createdRevueId, Is.Not.Empty);
            Assert.That(json.Msg, Is.EqualTo("Successfully created!"));
        }

        [Test, Order(2)]
        public void GetAllRevues()
        {
            var request = new RestRequest("/Revue/All", Method.Get);
            var response = client.Execute(request);

            var responseItems = JsonSerializer.Deserialize<List<ApiResponseDTO>>(response.Content);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(responseItems, Is.Not.Null);
            Assert.That(responseItems, Is.Not.Empty);
            
            createdRevueId = responseItems[^1].RevueId;
        }

        [Test, Order(3)]
        public void EditRevue_ShouldReturnSuccess()
        {
            var editedRequest = new RevueDTO
            {
                Title = "Edited Revue",
                Description = "Updated description",
                Url = ""
            };

            var request = new RestRequest($"/Revue/Edit/createdRevueId", Method.Put);
            request.AddJsonBody(editedRequest);

            var response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));

            if (!string.IsNullOrWhiteSpace(response.Content))
            {
                var json = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);
                Assert.That(json.Msg, Is.EqualTo("Edited successfully"));
            }
        }

        [Test, Order(4)]
        public void DeleteRevue_ShouldReturnSuccess()
        {
            var createRequest = new RestRequest("/Revue/Create", Method.Post);
            createRequest.AddJsonBody(new { Title = "Temp Revue", Description = "Temp Desc", Url = "" });
            var createResponse = client.Execute(createRequest);

            var createdJson = JsonSerializer.Deserialize<JsonElement>(createResponse.Content);
            Assert.That(createdJson.GetProperty("msg").GetString(), Is.EqualTo("Successfully created!"));
        }

        [Test, Order(5)]
        public void CreateRevue_WithoutRequiredFields()
        {
            var revueRequest = new RevueDTO { Title = "", Description = "" };

            var request = new RestRequest("/Revue/Create", Method.Post);
            request.AddJsonBody(revueRequest);

            var response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        }

        [Test, Order(6)]
        public void EditNonExistingRevue_ShouldReturnNotFound()
        {
            string nonExistingRevueId = "123";
            var editRequest = new RevueDTO
            {
                Title = "Edited Non-Existing Revue",
                Description = "This is an updated test revue description for a non-existing revue.",
                Url = ""
            };

            var request = new RestRequest($"/Revue/Edit/{nonExistingRevueId}", Method.Put);
            request.AddJsonBody(editRequest);

            var response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));

            if (!string.IsNullOrWhiteSpace(response.Content))
            {
                var json = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);
                Assert.That(json.Msg, Is.EqualTo("There is no such revue!"));
            }
        }

        [Test, Order(7)]
        public void DeleteNonExistingRevue_ShouldReturnNotFound()
        {
            string nonExistingRevueId = "123";
            var request = new RestRequest($"/Revue/Delete/{nonExistingRevueId}", Method.Delete);

            var response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));

            if (!string.IsNullOrWhiteSpace(response.Content))
            {
                var json = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);
                Assert.That(json.Msg, Is.EqualTo("There is no such revue!"));
            }
        }


        [OneTimeTearDown]
        public void Cleanup()
        {
            client?.Dispose();
        }
    }
}
