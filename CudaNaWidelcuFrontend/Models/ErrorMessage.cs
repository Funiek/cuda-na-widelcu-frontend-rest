namespace CudaNaWidelcuFrontend.Models
{
    public class ErrorMessage
    {
        public string message { get; set; }
        public int errorCode { get; set; }

        public ErrorMessage() { }
        public ErrorMessage(string message, int errorCode) 
        {
            this.message = message;
            this.errorCode = errorCode;
        }
    }
}
