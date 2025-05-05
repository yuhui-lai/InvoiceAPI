using InvoiceAPI.Lib.Models.Common;
using InvoiceAPI.Lib.Models.Invoice;

namespace InvoiceAPI.Lib.Interfaces
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
