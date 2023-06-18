namespace CudaNaWidelcuFrontend.Models
{
    public class Recipe
    {
        public int Id { get; set; }
        public int NextProductId { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public byte[]? Image { get; set; }
        public List<Product>? Products { get; set; }
        public double Rating { get; set; }
        public List<int>? Votes { get; set; }
        public List<Link>? Links { get; set; }
        public int CountVotes { get; set; }
        public Category Category { get; set; }
    }

}
