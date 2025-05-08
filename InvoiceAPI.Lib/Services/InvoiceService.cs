using InvoiceAPI.Lib.Entity;
using InvoiceAPI.Lib.Enums;
using InvoiceAPI.Lib.Exceptions;
using InvoiceAPI.Lib.Interfaces;
using InvoiceAPI.Lib.Models.Common;
using InvoiceAPI.Lib.Models.Invoice;
using InvoiceAPI.Lib.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Encodings.Web;

namespace InvoiceAPI.Lib.Services
{
    public class InvoiceService : IInvoiceService
    {
        private readonly WelldoneContext dbContext;
        private readonly HtmlEncoder htmlEncoder;
        private readonly IConfiguration config;
        private readonly ILogger<InvoiceService> logger;

        public InvoiceService(WelldoneContext dbContext, HtmlEncoder htmlEncoder, IConfiguration config, ILogger<InvoiceService> logger)
        {
            this.dbContext = dbContext;
            this.htmlEncoder = htmlEncoder;
            this.config=config;
            this.logger = logger;
        }

        /// <summary>
        /// 發票開立:
        /// 1. 會員綁定(若已綁定略過)
        /// 2. 發票若已開立直接回傳發票號碼
        /// 3. 開立發票並回傳發票號碼
        /// </summary>
        /// <param name="req"></param>
        /// <returns></returns>
        public async Task<CommonAPIModel<IssueRes>> IssueAsync(IssueReq req)
        {
            try
            {
                return await IssueProcessAsync(req);
            }
            catch (BusinessException ex)
            {
                return new CommonAPIModel<IssueRes>
                {
                    success = false,
                    msg = ex.Message,
                    data = null
                };
            }
        }

        private async Task<CommonAPIModel<IssueRes>> IssueProcessAsync(IssueReq req)
        {
            // 驗證輸入參數
            ValidateIssueRequest(req);
            // 驗證系統代碼
            var systemCodeRecord = await GetSystemCodeRecordAsync(req.system_code);
            // 驗證使用者ID已綁定
            var userMapping = await GetOrBindingUserMapAsync(systemCodeRecord, req.user_id);
            // 驗證訂單編號與發票是否已存在
            string existInvoiceNumber = await GetExistInvoiceNumberAsync(systemCodeRecord.id, req.order_no);
            if (!string.IsNullOrEmpty(existInvoiceNumber))
            {
                return new CommonAPIModel<IssueRes>
                {
                    success = true,
                    msg = "發票已存在",
                    data = new IssueRes
                    {
                        invoice_number = existInvoiceNumber
                    }
                };
            }

            // 取得發票會員編號
            var userCarrierId = GetInvoiceCarrierId(systemCodeRecord.system_code, userMapping.serial_no);
            // 準備發票資料
            var invoice = GetNewInvoiceRecord(req, userMapping.id, userCarrierId, systemCodeRecord.id);
            var invoiceProducts = GetNewInvoiceProductRecord(req);
            // 取得或開立發票號碼
            var invoiceNumber = await IssueInvoiceAndGetNumberAsync(systemCodeRecord.id, invoice, invoiceProducts);
            return new CommonAPIModel<IssueRes>
            {
                success = true,
                msg = "發票開立成功",
                data = new IssueRes
                {
                    invoice_number = invoiceNumber
                }
            };
        }

        /// <summary>
        /// 取得已存在的發票號碼
        /// </summary>
        /// <param name="systemCodeRecord"></param>
        /// <param name="orderId"></param>
        /// <returns></returns>
        private async Task<string> GetExistInvoiceNumberAsync(int systemCodeId, string orderId)
        {
            // 同系統代碼下的訂單編號
            var existInvoice = await dbContext.invoice_record
                .Where(x => x.order_no.Equals(orderId) && x.system_code_id == systemCodeId)
                .FirstOrDefaultAsync();
            if (existInvoice == null)
                return string.Empty;
            return existInvoice.invoice_number;
        }

        /// <summary>
        /// 驗證發票開立請求
        /// </summary>
        /// <param name="req"></param>
        /// <exception cref="BusinessException"></exception>
        private void ValidateIssueRequest(IssueReq req)
        {
            var sanitizedSystemCode = htmlEncoder.Encode(req.system_code ?? "");
            var sanitizedUserId = htmlEncoder.Encode(req.user_id ?? "");
            if (string.IsNullOrEmpty(sanitizedSystemCode) || string.IsNullOrEmpty(sanitizedUserId))
            {
                throw new BusinessException("系統代碼和使用者ID不可為空");
            }
        }


        /// <summary>
        /// 取得或開立發票號碼
        /// </summary>
        /// <param name="systemCodeId"></param>
        /// <param name="invoice"></param>
        /// <param name="invoiceProducts"></param>
        /// <returns></returns>
        /// <exception cref="BusinessException"></exception>
        private async Task<string> IssueInvoiceAndGetNumberAsync(int systemCodeId, invoice_record invoice,
            List<invoice_product_record> invoiceProducts)
        {
            // 重試機制處理併發
            const int maxRetries = 3;
            const int delayMs = 100;
            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                using var transaction = await dbContext.Database.BeginTransactionAsync();
                try
                {
                    // 產生可用發票號碼
                    AvailableInvoiceDTO availableInvoice = await CreateAvailableInvoiceNumber(systemCodeId);
                    invoice.invoice_number = availableInvoice.invoice_number;
                    invoice.year = availableInvoice.year;
                    invoice.term = availableInvoice.term;
                    // 儲存發票記錄
                    await dbContext.invoice_record.AddAsync(invoice);
                    await dbContext.SaveChangesAsync();
                    // 儲存發票商品明細
                    SetInvoiceProductRecordId(invoiceProducts, invoice.id);
                    await dbContext.AddRangeAsync(invoiceProducts);
                    await dbContext.SaveChangesAsync();
                    // 提交事務
                    await transaction.CommitAsync();
                    return invoice.invoice_number;
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    await transaction.RollbackAsync();
                    if (attempt == maxRetries)
                    {
                        throw new BusinessException($"{invoice.order_no} 發票開立失敗，重試 {maxRetries} 次後仍未成功", ex);
                    }
                    await Task.Delay(delayMs + new Random().Next(0, 300));
                }
                catch (BusinessException)
                {
                    await transaction.RollbackAsync();
                    // 業務異常不應重試
                    throw;
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    throw new BusinessException($"{invoice.order_no} 發票開立失敗: {ex.Message}", ex);
                }
            }
            throw new BusinessException("發票開立失敗，請稍後重試");
        }

        /// <summary>
        /// 綁定使用者並取得會員綁定記錄，樂觀鎖
        /// </summary>
        /// <param name="systemCodeRecord"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        /// <exception cref="BusinessException"></exception>
        private async Task<invoice_system_user_serial_map> UserBindingAsync(invoice_system_code systemCodeRecord, string userId)
        {
            const int maxRetries = 3;
            const int delayMs = 100;
            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                using var transaction = await dbContext.Database.BeginTransactionAsync();
                try
                {
                    // 取得並更新流水號
                    var serialNo = await GenerateSerialNoAsync(systemCodeRecord);
                    // 建立並儲存綁定記錄
                    var mapping = await CreateUserBindingAsync(systemCodeRecord.id, userId, serialNo);
                    // 統一提交變更
                    await dbContext.SaveChangesAsync();
                    // 提交交易
                    await transaction.CommitAsync();
                    return mapping;
                }
                catch (BusinessException ex)
                {
                    await transaction.RollbackAsync();
                    // 業務異常不應重試
                    throw new BusinessException($"system {systemCodeRecord.system_code} user_id {userId} 使用者綁定失敗", ex);
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    await transaction.RollbackAsync();
                    if (attempt == maxRetries)
                    {
                        throw new BusinessException($"system {systemCodeRecord.system_code} user_id {userId} 使用者綁定失敗，重試 {maxRetries} 次後仍未成功", ex);
                    }
                    // 隨機延遲以避免重試衝突
                    await Task.Delay(delayMs + new Random().Next(0, 300));
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            throw new BusinessException($"system {systemCodeRecord.system_code} user_id {userId} 使用者綁定失敗，請稍後重試");
        }

        /// <summary>
        /// 產生可用的發票號碼，悲觀鎖
        /// </summary>
        /// <param name="systemCodeId"></param>
        /// <returns></returns>
        /// <exception cref="BusinessException"></exception>
        private async Task<AvailableInvoiceDTO> CreateAvailableInvoiceNumber(int systemCodeId)
        {
            int currentYear = TimeUtil.UnifiedNow().Year;
            int currentTerm = (TimeUtil.UnifiedNow().Month - 1) / 2 + 1;

            var invoiceManagement = await dbContext.invoice_number_management
                    .FromSqlRaw($@"
                        SELECT * FROM invoice_number_management WITH (UPDLOCK)
                        WHERE system_code_id = {systemCodeId}
                        AND year = {currentYear}
                        AND term = {currentTerm}
                        AND status_id = {(int)InvoiceNumberStatusEnum.Used}")
                    .FirstOrDefaultAsync();

            if (invoiceManagement == null || invoiceManagement.now_number >= invoiceManagement.end_number)
            {
                throw new BusinessException("無可用發票號碼範圍");
            }
            invoiceManagement.now_number++;
            invoiceManagement.update_date = DateTime.UtcNow;
            string invoiceNumber = $"{invoiceManagement.letter}{invoiceManagement.now_number:D8}";
            if (await dbContext.invoice_record.AnyAsync(x => x.invoice_number == invoiceNumber))
            {
                throw new BusinessException("發票號碼已存在");
            }
            return new AvailableInvoiceDTO
            {
                year = invoiceManagement.year,
                term = invoiceManagement.term,
                invoice_number = invoiceNumber
            };
        }

        /// <summary>
        /// 設定發票商品明細的發票記錄ID
        /// </summary>
        /// <param name="products"></param>
        /// <param name="invoiceId"></param>
        private void SetInvoiceProductRecordId(List<invoice_product_record> products, int invoiceId)
        {
            foreach (var product in products)
            {
                product.invoice_record_id = invoiceId;
            }
        }

        /// <summary>
        /// 取得新的發票商品明細
        /// </summary>
        /// <param name="req"></param>
        /// <returns></returns>
        /// <exception cref="BusinessException"></exception>
        private List<invoice_product_record> GetNewInvoiceProductRecord(IssueReq req)
        {
            var invoiceProductRecords = new List<invoice_product_record>();
            foreach (var product in req.invoice_products)
            {
                if (string.IsNullOrEmpty(product.description) || product.quantity <= 0 ||
                    string.IsNullOrEmpty(product.unit) || product.unit_price <= 0 || product.amount <= 0)
                {
                    throw new BusinessException("商品明細不正確");
                }
                invoiceProductRecords.Add(new invoice_product_record
                {
                    description = product.description,
                    quantity = product.quantity,
                    unit = product.unit,
                    unit_price = product.unit_price,
                    amount = product.amount,
                    sequence_number = product.sequence_number
                });
            }
            return invoiceProductRecords;
        }

        /// <summary>
        /// 取得新的發票記錄
        /// </summary>
        /// <param name="req"></param>
        /// <param name="userMapId"></param>
        /// <param name="userCarrierId"></param>
        /// <returns></returns>
        private invoice_record GetNewInvoiceRecord(IssueReq req, int userMapId, string userCarrierId, int systeCodeId)
        {
            // 取得新的發票記錄
            return new invoice_record
            {
                invoice_number = string.Empty,
                invoice_time = req.invoice_time,
                invoice_date = req.invoice_date,
                invoice_type = config["Invoice:NormalInvoiceType"],
                donate_mark = config["Invoice:NonDonateMark"],
                print_mark = config["Invoice:NonPrintMark"],
                random_number = RandomUtil.GetCommonRandomNumber(4),
                buyer_identifier = config["Invoice:BuyerIdentifier"],
                sales_amount = req.total_amount,
                free_tax_sales_amount = 0m,
                zero_tax_sales_amount = 0m,
                tax_type = config["Invoice:TaxType"],
                tax_rate = 0.05m,
                tax_amount = 0m,
                total_amount = req.total_amount,
                order_no = req.order_no,
                reason = string.Empty,
                send_status = false,
                operation_type_id = (int)InvoiceOperationTypeEnum.C0401,
                create_date = TimeUtil.UnifiedNow(),
                update_date = TimeUtil.UnifiedNow(),
                user_serial_map_id = userMapId, // 需設定有效的 user_serial_map ID
                carrier_id_1 = userCarrierId,
                system_code_id = systeCodeId
            };
        }

        /// <summary>
        /// systemCode + 9碼流水號 ex: QB000000123
        /// </summary>
        /// <param name="systemCode"></param>
        /// <param name="serialNo"></param>
        /// <returns>會員編號</returns>
        private string GetInvoiceCarrierId(string systemCode, int serialNo)
        {
            return $"{systemCode}{serialNo:D9}";
        }

        /// <summary>
        /// 取得或綁定使用者
        /// </summary>
        /// <param name="systemCodeRecord"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        private async Task<invoice_system_user_serial_map> GetOrBindingUserMapAsync(invoice_system_code systemCodeRecord, string userId)
        {
            var existingBinding = await dbContext
                .invoice_system_user_serial_map
                .Where(x => x.user_id == userId && x.system_code_id == systemCodeRecord.id)
                .FirstOrDefaultAsync();
            if (existingBinding == null)
            {
                // 未綁定進行綁定
                return await UserBindingAsync(systemCodeRecord, userId);
            }
            return existingBinding;
        }

        /// <summary>
        /// 取得系統代碼記錄
        /// </summary>
        /// <param name="systemCode"></param>
        /// <returns></returns>
        /// <exception cref="BusinessException"></exception>
        private async Task<invoice_system_code> GetSystemCodeRecordAsync(string systemCode)
        {
            var record = await dbContext.invoice_system_code
                .FirstOrDefaultAsync(x => x.system_code == systemCode);

            if (record == null)
            {
                throw new BusinessException($"系統代碼 {systemCode} 不存在");
            }
            return record;
        }

        /// <summary>
        /// 產生新的流水號，樂觀鎖
        /// </summary>
        /// <param name="systemCodeRecord"></param>
        /// <returns></returns>
        /// <exception cref="BusinessException"></exception>
        private async Task<int> GenerateSerialNoAsync(invoice_system_code systemCodeRecord)
        {
            // 檢查是否需要初始化流水號
            var maxSerial = await dbContext.invoice_system_user_max_serial
                .Where(x => x.system_code_id==systemCodeRecord.id)
                .FirstOrDefaultAsync();

            if (maxSerial == null)
            {
                throw new BusinessException($"系統 {systemCodeRecord.system_name} 的流水號未初始化");
            }

            maxSerial.serial_no++;
            return maxSerial.serial_no;
        }

        /// <summary>
        /// 建立使用者綁定記錄
        /// </summary>
        /// <param name="systemCodeId"></param>
        /// <param name="userId"></param>
        /// <param name="serialNo"></param>
        /// <returns></returns>
        private async Task<invoice_system_user_serial_map> CreateUserBindingAsync(int systemCodeId, string userId, int serialNo)
        {
            var mapping = new invoice_system_user_serial_map
            {
                system_code_id = systemCodeId,
                user_id = userId,
                serial_no = serialNo
            };
            await dbContext.invoice_system_user_serial_map.AddAsync(mapping);
            return mapping;
        }
    }
}
