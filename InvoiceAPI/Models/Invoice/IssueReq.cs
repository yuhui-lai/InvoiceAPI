namespace InvoiceAPI.Models.Invoice
{
    public class IssueReq
    {
        /// <summary>
        /// 系統代碼
        /// </summary>
        public string system_code { get; set; }
        /// <summary>
        /// 訂單編號
        /// </summary>
        public string order_no { get; set; }
        /// <summary>
        /// 使用者ID
        /// </summary>
        public string user_id { get; set; }
        /// <summary>
        /// 發票日期 yyyyMMdd
        /// </summary>
        public string invoice_date { get; set; }
        /// <summary>
        /// 發票時間 HHmmss
        /// </summary>
        public string invoice_time { get; set; }
        /// <summary>
        /// 發票商品明細
        /// </summary>
        public List<InvoiceProductReq> invoice_products { get; set; }
        /// <summary>
        /// 總金額
        /// </summary>
        public decimal total_amount { get; set; }
    }

    public class InvoiceProductReq
    {
        /// <summary>
        /// 商品名稱
        /// </summary>
        public string description { get; set; }
        /// <summary>
        /// 商品數量
        /// </summary>
        public int quantity { get; set; }
        /// <summary>
        /// 商品單位 ex:筆
        /// </summary>
        public string unit { get; set; }
        /// <summary>
        /// 商品單價
        /// </summary>
        public decimal unit_price { get; set; }
        /// <summary>
        /// 商品項目總金額
        /// </summary>
        public decimal amount { get; set; }
        /// <summary>
        /// 明細排列序號
        /// </summary>
        public int sequence_number { get; set; }
    }
}