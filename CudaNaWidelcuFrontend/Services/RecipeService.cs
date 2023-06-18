using CudaNaWidelcuFrontend.Models;
using Microsoft.DotNet.MSIdentity.Shared;
using Newtonsoft.Json;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Xml.Linq;

namespace CudaNaWidelcuFrontend.Services
{
    public class RecipeService
    {
        private readonly HttpClient _httpClient;

        public RecipeService()
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            
            string username = "root";
            string password = "root";
            string authHeaderValue = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}"));
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authHeaderValue);
        }

        public async Task<List<Recipe>?> GetRecipes()
        {
            string url = "https://localhost:8181/recipe";

            HttpResponseMessage response = await _httpClient.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                string jsonResponse = await response.Content.ReadAsStringAsync();
                List<Recipe>? recipes = JsonConvert.DeserializeObject<List<Recipe>>(jsonResponse);
                return recipes;
            }
            else
            {
                throw new Exception("Nie udało się pobrać listy przepisów.");
            }
        }

        public async Task<Recipe> getRecipeAsync(int id)
        {
            string url = $"https://localhost:8181/recipe/{id}";

            HttpResponseMessage response = await _httpClient.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                string jsonResponse = await response.Content.ReadAsStringAsync();
                Recipe? recipes = JsonConvert.DeserializeObject<Recipe>(jsonResponse);
                return recipes ?? new Recipe();
            }
            else
            {
                string errorMessage = await response.Content.ReadAsStringAsync();
                ErrorMessage? err = JsonConvert.DeserializeObject<ErrorMessage>(errorMessage);
                
                if (err is not null)
                    throw new Exception(err.message);
                else
                    throw new Exception("Nie udało się pobrać przepisu.");
            }
        }

        public async Task<Recipe> getRecipeByNameAsync(string? name)
        {
            string url = $"https://localhost:8181/recipe/byname/{name}";

            HttpResponseMessage response = await _httpClient.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                string jsonResponse = await response.Content.ReadAsStringAsync();
                Recipe? recipes = JsonConvert.DeserializeObject<Recipe>(jsonResponse);
                return recipes ?? new Recipe();
            }
            else
            {
                throw new Exception("Nie udało się pobrać listy przepisów.");
            }
        }

        public async Task<List<Recipe>?> getRecipesByCategoryAsync(Category category)
        {
            string url = $"https://localhost:8181/recipe/bycategory/{(int)category}";/*
            string jsonData = JsonConvert.SerializeObject(category);
            var content = new StringContent(jsonData, Encoding.UTF8, "application/json");*/


            HttpResponseMessage response = await _httpClient.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                string jsonResponse = await response.Content.ReadAsStringAsync();
                List<Recipe>? recipes = JsonConvert.DeserializeObject<List<Recipe>>(jsonResponse);
                return recipes;
            }
            else
            {
                throw new Exception("Nie udało się pobrać listy przepisów.");
            }
        }

        public async Task rateRecipeAsync(RatingData data)
        {
            RateRecipeDto rateRecipe = new RateRecipeDto(data.RecipeId, data.Rating);

            string url = $"https://localhost:8181/recipe/rate";
            string jsonData = JsonConvert.SerializeObject(rateRecipe);
            var content = new StringContent(jsonData, Encoding.UTF8, "application/json");
            await _httpClient.PostAsync(url, content);
        }
    }
}
