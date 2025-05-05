using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InvoiceAPI.Lib.Models.Invoice
{
    public class AvailableInvoiceDTO
    {
        public int year { get; set; }
        public int term { get; set; }
        public string invoice_number { get; set; }
    }
}
