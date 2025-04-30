using InvoiceAPI.Exceptions;
using InvoiceAPI.Interfaces;
using InvoiceAPI.Models.Invoice;
using Microsoft.AspNetCore.Mvc;

namespace InvoiceAPI.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class InvoiceController : ControllerBase
    {
        private readonly IInvoiceService _invoiceService;

        public InvoiceController(IInvoiceService invoiceService)
        {
            _invoiceService = invoiceService;
        }

        /// <summary>
        /// 發票開立
        /// </summary>
        /// <param name="req"></param>
        /// <returns>發票號碼</returns>
        [HttpPost]
        public async Task<IActionResult> Issue(IssueReq req)
        {
            var invoiceNumber = await _invoiceService.IssueAsync(req);
            return Ok(invoiceNumber);
        }
    }
}
