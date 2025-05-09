using System.ComponentModel.DataAnnotations;

namespace InvoiceAPI.Lib.Models.InvoiceXml
{
    public class F0401Model
    {
        public required string InvoiceNumber { get; set; }
        public required string InvoiceDate { get; set; }
        public required string InvoiceTime { get; set; }
        public required F0401Seller Seller { get; set; }
        public required F0401Buyer Buyer { get; set; }
        public string BuyerRemark { get; set; } = "";
        public string MainRemark { get; set; } = "";
        public string CustomsClearanceMark { get; set; } = "";
        public string Category { get; set; } = "";
        public string RelateNumber { get; set; } = "";
        public required string InvoiceType { get; set; }
        public string GroupMark { get; set; } = "";
        public required string DonateMark { get; set; }
        public required string CarrierType { get; set; }
        public required string CarrierId1 { get; set; }
        public required string CarrierId2 { get; set; }
        public required string PrintMark { get; set; }
        public string NPOBAN { get; set; }
        public required string RandomNumber { get; set; }
        public string BondedAreaConfirm { get; set; } = "";
        public string ZeroTaxRateReason { get; set; } = "";
        public string Reserved1 { get; set; } = "";
        public string Reserved2 { get; set; } = "";
        public List<F0401ProductItem> Details { get; set; } = [];
        public required F0401Amount Amount { get; set; }
    }

    public class F0401ProductItem
    {
        public required string Description { get; set; }
        public int Quantity { get; set; }
        public string Unit { get; set; } = "";
        public int UnitPrice { get; set; }
        public required string TaxType { get; set; }
        public int Amount { get; set; }
        public required string SequenceNumber { get; set; }
        public string Remark { get; set; } = "";
        public string RelateNumber { get; set; } = "";
    }

    public class F0401Seller()
    {
        public required string Identifier { get; set; }
        public required string Name { get; set; }
        public required string Address { get; set; }
        public string PersonInCharge { get; set; } = "";
        public required string TelephoneNumber { get; set; }
        public string FacsimileNumber { get; set; } = "";
        public string EmailAddress { get; set; } = "";
        public string CustomerNumber { get; set; } = "";
        public string RoleRemark { get; set; } = "";
    }

    public class F0401Buyer
    {
        public required string Identifier { get; set; }
        public required string Name { get; set; }
        public string Address { get; set; } = "";
        public string PersonInCharge { get; set; } = "";
        public string TelephoneNumber { get; set; } = "";
        public string FacsimileNumber { get; set; } = "";
        public string EmailAddress { get; set; } = "";
        public string CustomerNumber { get; set; } = "";
        public string RoleRemark { get; set; } = "";
    }

    public class F0401Amount
    {
        public int SalesAmount { get; set; }
        public int FreeTaxSalesAmount { get; set; }
        public int ZeroTaxSalesAmount { get; set; }
        public required string TaxType { get; set; }
        public decimal TaxRate { get; set; }
        public int TaxAmount { get; set; }
        public int TotalAmount { get; set; }
        public string DiscountAmount { get; set; } = "";
        public string OriginalCurrencyAmount { get; set; } = "";
        public string ExchangeRate { get; set; } = "";
        public string Currency { get; set; } = "";
    }
}