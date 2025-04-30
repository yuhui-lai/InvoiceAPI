namespace InvoiceAPI.Models.Common
{
    public class CommonAPIModel<T>
    {
        /// <summary>
        /// api狀態
        /// </summary>
        public bool success { get; set; } = true;
        /// <summary>
        /// 說明內容
        /// </summary>
        public string msg { get; set; } = "";
        public T data { get; set; }
    }
}
