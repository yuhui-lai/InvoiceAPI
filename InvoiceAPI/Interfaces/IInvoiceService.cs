using InvoiceAPI.Models.Common;
using InvoiceAPI.Models.Invoice;

namespace InvoiceAPI.Interfaces
{
    public interface IInvoiceService
    {
        /// <summary>
        /// 發票開立
        /// </summary>
        /// <param name="req"></param>
        /// <returns>發票號碼</returns>
        Task<CommonAPIModel<IssueRes>> IssueAsync(IssueReq req);
    }
}
