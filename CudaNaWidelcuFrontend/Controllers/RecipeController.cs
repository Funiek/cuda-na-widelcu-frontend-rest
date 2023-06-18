using CudaNaWidelcuFrontend.Models;
using CudaNaWidelcuFrontend.Services;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Text;

namespace CudaNaWidelcuFrontend.Controllers
{
    public class RecipeController : Controller
    {
        private readonly ILogger<RecipeController> _logger;
        private readonly RecipeService _recipeService;
        private readonly FileService _fileService;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly Dictionary<Category, string> _categoryNames;
        private readonly MessageHeader _authHeader;

        public class RecipeModelView
        {
            public int Id { get; set; }
            public string? Name { get; set; }
            public string? Category { get; set; }
            public double Rating { get; set; }
            public string? Description { get; set; }
            public double CountVotes { get; set; }
            public List<Product>? Products { get; set; }
        }

        public RecipeController(ILogger<RecipeController> logger, IWebHostEnvironment webHostEnvironment)
        {
            _logger = logger;
            _recipeService = new RecipeService();
            _fileService = new FileService();
            _webHostEnvironment = webHostEnvironment;
            _categoryNames = new Dictionary<Category, string>
            {
                { Category.BREAKFAST, "Śniadanie" },
                { Category.LUNCH, "Obiad" },
                { Category.DINNER, "Kolacja" }
            };

            _authHeader = MessageHeader.CreateHeader(
                                    "authAddress",
                                    "http://localhost:8080/websoap/HelloWorldImpl",
                                    "f0f8270db484173c2e0e52cb7fb0c8a53c2483c9cbb46bc0b88bead6c082cbf8", false, "http://schemas.xmlsoap.org/soap/actor/next"
                                    );
        }

        public async Task<IActionResult> Index()
        {
            var recipes = await _recipeService.GetRecipes();
            
            var modelViews = new List<RecipeModelView>();

            if(recipes is not null)
            {
                foreach (var recipe in recipes)
                {
                    modelViews.Add(new RecipeModelView
                    {
                        Id = recipe.Id,
                        Name = recipe.Name,
                        Category = _categoryNames[recipe.Category],
                        Rating = recipe.Rating,

                    });
                }
            }
            
            return View(modelViews);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Index(int id)
        {
            var category = id switch
            {
                1 => Category.BREAKFAST,
                2 => Category.LUNCH,
                3 => Category.DINNER,
                _ => Category.BREAKFAST
            };

            var recipes = await _recipeService.getRecipesByCategoryAsync(category);
            var modelViews = new List<RecipeModelView>();

            foreach (var recipe in recipes)
            {
                modelViews.Add(new RecipeModelView
                {
                    Id = recipe.Id,
                    Name = recipe.Name,
                    Category = _categoryNames[recipe.Category],
                    Rating = recipe.Rating,
                    CountVotes = recipe.CountVotes,
                    Description = recipe.Description,
                    Products = recipe.Products
                });
            }

            return View(modelViews);
        }

        public async Task<IActionResult> Detail(int id)
        {
            try
            {
                Recipe recipe = await _recipeService.getRecipeAsync(id);
                string imageEndpoint = "";

                if (recipe.Links is null) return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });

                foreach (var item in recipe.Links)
                {
                    if (item.Title == "image")
                        imageEndpoint = item.Uri;
                }

                var path = Path.Combine(_webHostEnvironment.WebRootPath, "img", recipe.Name + ".jpeg");

                if (recipe.Image is null && recipe.Name is not null && !System.IO.File.Exists(path))
                {
                    var imageInByte = await _fileService.downloadImageAsync(imageEndpoint);
                    recipe.Image = imageInByte;

                    using (var ms = new MemoryStream(recipe.Image))
                    {
                        using (var fs = new FileStream(path, FileMode.Create))
                        {
                            ms.WriteTo(fs);
                        }
                    }
                }

                var modelView = new RecipeModelView
                {
                    Id = recipe.Id,
                    Name = recipe.Name,
                    Category = _categoryNames[recipe.Category],
                    Rating = recipe.Rating,
                    CountVotes = recipe.CountVotes,
                    Description = recipe.Description,
                    Products = recipe.Products
                };

                return View(modelView);
            }
            catch (Exception ex)
            {
                return View("Error", new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier, Message = ex.Message });
            }
        }

        [HttpPost]
        public async void Rating(RatingData data)
        {
            await _recipeService.rateRecipeAsync(data);
        }

        [HttpPost]
        public async Task Pdf(RecipeNameData data)
        {
            var path = Path.Combine(_webHostEnvironment.WebRootPath, "pdf", data.Name + ".pdf");

            if (!System.IO.File.Exists(path))
            {
                byte[] pdfInBytes;
                Recipe recipe = await _recipeService.getRecipeByNameAsync(data.Name);
                StringBuilder stringBuilder = new StringBuilder();

                if (recipe.Products is not null)
                {
                    foreach (var product in recipe.Products)
                    {
                        stringBuilder.Append($"{product.Name}: {product.Qty} {product.Measure};");
                    }
                }
                

                stringBuilder.Remove(stringBuilder.Length - 1, 1);

                pdfInBytes = await _fileService.downloadRecipeProductsPdfAsync(recipe.Name, stringBuilder.ToString());

                using (var ms = new MemoryStream(pdfInBytes))
                {
                    using (var fs = new FileStream(path, FileMode.Create))
                    {
                        ms.WriteTo(fs);
                    }
                }
            }
        }


        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error(Exception ex)
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier, Message = ex.Message});
        }
    }
}
