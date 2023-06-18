using CudaNaWidelcuFrontend.Models;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Text;

namespace CudaNaWidelcuFrontend.Services
{
    public class FileService
    {
        private readonly HttpClient _httpClient;

        public FileService()
        {
            _httpClient = new HttpClient();

            string username = "root";
            string password = "root";
            string authHeaderValue = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}"));
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authHeaderValue);
        }

        public async Task<byte[]> downloadImageAsync(string imageEndpoint)
        {
            var result = await _httpClient.GetAsync(imageEndpoint);
            var image = await result.Content.ReadAsByteArrayAsync();

            return image;
        }

        public async Task<byte[]> downloadRecipeProductsPdfAsync(string? name, string products)
        {
            string url = $"https://localhost:8181/recipe/pdf?name={name}&products={products}";
            var result = await _httpClient.GetAsync(url);
            var image = await result.Content.ReadAsByteArrayAsync();

            return image;
        }
    }
}
