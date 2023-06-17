using CudaNaWidelcuFrontend.Models;
using FileReference;
using Microsoft.AspNetCore.Mvc;
using RecipeReference;
using System.Diagnostics;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Text;

namespace CudaNaWidelcuFrontend.Controllers
{
    public class RecipeController : Controller
    {
        private readonly ILogger<RecipeController> _logger;
        private readonly RecipeServiceClient _recipeService;
        private readonly FileServiceClient _fileService;
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
            public Product[]? Products { get; set; }
        }

        public RecipeController(ILogger<RecipeController> logger, IWebHostEnvironment webHostEnvironment)
        {
            _logger = logger;
            _recipeService = new RecipeServiceClient();
            _fileService = new FileServiceClient();
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
            Recipe[] recipes;
            using (var scope = new OperationContextScope(_recipeService.InnerChannel))
            {
                OperationContext.Current.OutgoingMessageHeaders.Add(_authHeader);
                var recipesResponse = await _recipeService.getRecipesAsync();
                recipes = recipesResponse.@return;
            }
            
            var modelViews = new List<RecipeModelView>();

            foreach (var recipe in recipes)
            {
                modelViews.Add(new RecipeModelView
                {
                    Id = recipe.id,
                    Name = recipe.name,
                    Category = _categoryNames[recipe.category],
                    Rating = recipe.rating,

                });
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

            Recipe[] recipes;
            using (var scope = new OperationContextScope(_recipeService.InnerChannel))
            {
                OperationContext.Current.OutgoingMessageHeaders.Add(_authHeader);
                var recipesResponse = await _recipeService.getRecipesByCategoryAsync(category);
                recipes = recipesResponse.@return;
            }
                
            
            var modelViews = new List<RecipeModelView>();

            foreach (var recipe in recipes)
            {
                modelViews.Add(new RecipeModelView
                {
                    Id = recipe.id,
                    Name = recipe.name,
                    Category = _categoryNames[recipe.category],
                    Rating = recipe.rating,
                    CountVotes = recipe.countVotes,
                    Description = recipe.description,
                    Products = recipe.products
                });
            }

            return View(modelViews);
        }

        public async Task<IActionResult> Detail(int id)
        {
            Recipe recipe;
            using (var scope = new OperationContextScope(_recipeService.InnerChannel))
            {
                OperationContext.Current.OutgoingMessageHeaders.Add(_authHeader);
                var recipeResponse = await _recipeService.getRecipeAsync(id);
                recipe = recipeResponse.@return;
            }

            var path = Path.Combine(_webHostEnvironment.WebRootPath, "img", recipe.name + ".jpeg");

            if (recipe.image is null && recipe.name is not null && !System.IO.File.Exists(path))
            {
                using (var scope = new OperationContextScope(_fileService.InnerChannel))
                {
                    OperationContext.Current.OutgoingMessageHeaders.Add(_authHeader);
                    var imageInBytesResponse = await _fileService.downloadImageAsync(recipe.name);
                    recipe.image = imageInBytesResponse.@return;
                }

                using (var ms = new MemoryStream(recipe.image))
                {
                    using (var fs = new FileStream(path, FileMode.Create))
                    {
                        ms.WriteTo(fs);
                    }
                }
            }

            var modelView = new RecipeModelView
            {
                Id = recipe.id,
                Name = recipe.name,
                Category = _categoryNames[recipe.category],
                Rating = recipe.rating,
                CountVotes = recipe.countVotes,
                Description = recipe.description,
                Products = recipe.products
            };

            return View(modelView);
        }

        [HttpPost]
        public void Rating(RatingData data)
        {
            using (var scope = new OperationContextScope(_recipeService.InnerChannel))
            {
                OperationContext.Current.OutgoingMessageHeaders.Add(_authHeader);
                _recipeService.rateRecipeAsync(data.RecipeId, data.Rating);
            }
        }

        [HttpPost]
        public async Task Pdf(RecipeNameData data)
        {
            var path = Path.Combine(_webHostEnvironment.WebRootPath, "pdf", data.Name + ".pdf");

            if (!System.IO.File.Exists(path))
            {
                byte[] pdfInBytes;
                Recipe recipe;

                using (var scope = new OperationContextScope(_recipeService.InnerChannel))
                {
                    OperationContext.Current.OutgoingMessageHeaders.Add(_authHeader);
                    var recipeResponse = await _recipeService.getRecipeByNameAsync(data.Name);
                    recipe = recipeResponse.@return;
                }

                    StringBuilder stringBuilder = new StringBuilder();

                    foreach (var product in recipe.products)
                    {
                        stringBuilder.Append($"{product.name}: {product.qty} {product.measure};");
                    }

                    stringBuilder.Remove(stringBuilder.Length - 1, 1);

                using (var scope = new OperationContextScope(_fileService.InnerChannel))
                {
                    var pdfResponse = await _fileService.downloadRecipeProductsPdfAsync(recipe.name, stringBuilder.ToString());
                    pdfInBytes = pdfResponse.@return;

                }

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
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
