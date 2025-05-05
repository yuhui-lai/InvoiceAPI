using System.ComponentModel;

namespace InvoiceAPI.Lib.Enums
{
    public enum InvoiceOperationTypeEnum
    {
        [Description("開立發票")]
        C0401 = 1,
        [Description("作廢發票")]
        C0501 = 2,
        [Description("註銷發票")]
        C0701 = 3,
    }
}
