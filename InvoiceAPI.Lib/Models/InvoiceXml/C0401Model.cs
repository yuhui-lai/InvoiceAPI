namespace InvoiceAPI.Lib.Models.InvoiceXml
{
    public class C0401Model
    {
        public string InvoiceNumber { get; set; }
        public string InvoiceDate { get; set; }
        public string InvoiceTime { get; set; }
        public string SellerIdentifier { get; set; }
        public string SellerName { get; set; }
        public string SellerAddress { get; set; }
        public string SellerTelephoneNumber { get; set; }
        public string BuyerIdentifier { get; set; }
        public string BuyerName { get; set; }
        public string InvoiceType { get; set; }
        public string DonateMark { get; set; }
        public string CarrierType { get; set; }
        public string CarrierId1 { get; set; }
        public string CarrierId2 { get; set; }
        public string PrintMark { get; set; }
        public string RandomNumber { get; set; }
        public List<C0401ProductItem> Details { get; set; } = [];
        public int SalesAmount { get; set; }
        public int FreeTaxSalesAmount { get; set; }
        public int ZeroTaxSalesAmount { get; set; }
        public string TaxType { get; set; }
        public decimal TaxRate { get; set; }
        public int TaxAmount { get; set; }
        public int TotalAmount { get; set; }
    }

    public class C0401ProductItem
    {
        public string Description { get; set; }
        public int Quantity { get; set; }
        public string Unit { get; set; }
        public int UnitPrice { get; set; }
        public int Amount { get; set; }
        public string SequenceNumber { get; set; }
    }
}
