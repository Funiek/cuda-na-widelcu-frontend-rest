namespace CudaNaWidelcuFrontend.Models
{
    public class RateRecipeDto
    {
        public int id { get; set; }
        public int vote { get; set; }

        public RateRecipeDto(int id, int vote)
        {
            this.id = id;
            this.vote = vote;
        }

        public RateRecipeDto() { }
    }
}
