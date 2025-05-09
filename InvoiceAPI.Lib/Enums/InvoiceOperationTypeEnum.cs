using System.ComponentModel;

namespace InvoiceAPI.Lib.Enums
{
    public enum InvoiceOperationTypeEnum
    {
        [Description("開立發票")]
        F0401 = 1,
        [Description("作廢發票")]
        F0501 = 2,
        [Description("註銷發票")]
        F0701 = 3,
    }
}
