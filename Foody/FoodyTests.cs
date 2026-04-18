using NUnit.Framework;
using System;
using RestSharp;
using RestSharp.Authenticators;
using System.Net;
using System.Text.Json;
using Foody.DTOs;




namespace Foody
{
    [TestFixture]
    public class FoodyTests
    {
        private const string BaseUrl = "http://144.91.123.158:81/";

        private const string LoginUsername = "ExamTest2";
        private const string LoginPassword = "Test123";


        private RestClient client;
        private static string foodId;

        [OneTimeSetUp]
        public void Setup()
        {
            string jwtToken = GetJwtToken(LoginUsername, LoginPassword);
            RestClientOptions options = new RestClientOptions(BaseUrl)
            {
                Authenticator = new JwtAuthenticator(jwtToken)
            };
            this.client = new RestClient(options);
        }

        private string GetJwtToken(string username, string password)
        {
            RestClient client = new RestClient(BaseUrl);
            RestRequest request = new RestRequest("/api/User/Authentication", Method.Post);
            request.AddJsonBody(new { username, password });
            RestResponse response = client.Execute(request);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                var content = JsonSerializer.Deserialize<JsonElement>(response.Content);
                var token = content.GetProperty("accessToken").GetString();

                if (string.IsNullOrWhiteSpace(token))
                {
                    throw new InvalidOperationException("Token is not found in the response.");
                }
                return token;
            }
            else
            {
                throw new InvalidOperationException($"Failed to retrieve token. Status code: {response.StatusCode}, Response: {response.Content}");
            }
        }

        [Order(1)]
        [Test]
        public void CreateFood_WithRequiredFields_ShouldSuccess()
        {
            //Arrange
            FoodDTO food = new FoodDTO()
            {
                Name = "Soup",
                Description = "Chicken Soup with patatoes",
                Url = ""
            };
            RestRequest request = new RestRequest("/api/Food/Create", Method.Post);
            request.AddJsonBody(food);

            //Act
            RestResponse response = client.Execute(request);

            //Assert
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
            ApiResponseDTO readyResponse = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);


           foodId = readyResponse.FoodId;
           // JsonElement content = JsonSerializer.Deserialize<JsonElement>(response.Content);
           // foodId = content.GetProperty("foodId").GetString();

            

        }

        [Order(2)]
        [Test]
        public void EditFoodTitle_ShouldChangeTitle()
        {
            //Arrange
          RestRequest request = new RestRequest($"/api/Food/Edit/{foodId}", Method.Patch);
           

            request.AddBody(new[]
            {
                 new
                 {
                     path = "/name",
                     op = "replace",
                     value = "Chicken Soup with vegetables"
                 }
            });
            //Act
            RestResponse response = client.Execute(request);

            //Assert
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            ApiResponseDTO readyResponse = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);
            Assert.That(readyResponse.Msg, Is.EqualTo("Successfully edited"));
        }

        [Order(3)]
        [Test]
        public void GetAllFoods_ShouldReturnNotEmptyArray()
        {
            RestRequest request = new RestRequest("/api/Food/All", Method.Get);
            RestResponse response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            List<ApiResponseDTO> readyResponse = JsonSerializer.Deserialize<List<ApiResponseDTO>>(response.Content);

            Assert.That(readyResponse, Is.Not.Empty);
            Assert.That(readyResponse, Is.Not.Null);
            Assert.That(readyResponse.Count, Is.GreaterThan(0));
        }

        [Order(4)]
        [Test]
        public void DeleteExistingFood_ShouldSuccess()
        {
          

            RestRequest request = new RestRequest($"/api/Food/Delete/{foodId}", Method.Delete);
            RestResponse response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            ApiResponseDTO readyResponse = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);
            Assert.That(readyResponse.Msg, Is.EqualTo("Deleted successfully!"));
        }

        [Order(5)]
        [Test]
        public void CreateFood_WithMissingRequiredFields_ShouldFail()
        {
            //Arrange
            FoodDTO food = new FoodDTO()
            {
                Name = "",
                Description = "Chicken Soup with patatoes",
                Url = ""
            };
            RestRequest request = new RestRequest("/api/Food/Create", Method.Post);
            request.AddJsonBody(food);
            //Act
            RestResponse response = client.Execute(request);
            //Assert
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        }

        [Order(6)]
        [Test]
        public void EditFood_WithInvalidId_ShouldFail()
        {
            //Arrange
            RestRequest request = new RestRequest($"/api/Food/Edit/3233321", Method.Patch);
            request.AddBody(new[]
            {
                 new
                 {
                     path = "/name",
                     op = "replace",
                     value = "Chicken Soup with vegetables"
                 }
            });
            //Act
            RestResponse response = client.Execute(request);
            //Assert
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
            ApiResponseDTO readyResponse = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);
            Assert.That(readyResponse.Msg, Is.EqualTo("No food revues..."));
        }

        [Order(7)]
        [Test]
        public void DeleteFood_ThatDoesNotExist_ShouldFail()
        {
            string invalidFoodId = "11111111-1111-1111-1111-111111111111";

            RestRequest request = new RestRequest($"/api/Food/Delete/{invalidFoodId}", Method.Delete);
            RestResponse response = client.Execute(request);

      


            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            ApiResponseDTO readyResponse = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);
            Assert.That(readyResponse.Msg, Is.EqualTo("Unable to delete this food revue!"));
        }


        [OneTimeTearDown]
        public void TearDown()
        {
           this.client.Dispose();
        }
    }
}