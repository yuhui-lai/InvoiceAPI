using InvoiceAPI.Lib.Interfaces;
using InvoiceAPI.Lib.Models.Common;
using InvoiceAPI.Lib.Models.Invoice;
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
        /// 發票開立:
        /// 1. 會員綁定(若已綁定略過)
        /// 2. 發票若已開立直接回傳發票號碼
        /// 3. 開立發票並回傳發票號碼
        /// </summary>
        /// <param name="req">開立發票資料</param>
        /// <returns>發票號碼</returns>
        [HttpPost]
        [ProducesResponseType(typeof(CommonAPIModel<IssueRes>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Issue(IssueReq req)
        {
            var invoiceNumber = await _invoiceService.IssueAsync(req);
            return Ok(invoiceNumber);
        }
    }
}
