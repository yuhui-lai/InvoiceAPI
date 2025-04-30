namespace InvoiceAPI.Exceptions
{
    public class BusinessException : Exception
    {
        public BusinessException(string message, Exception ex) : base(message, ex)
        {
        }

        public BusinessException(string message) : base(message)
        {
        }
    }
}
