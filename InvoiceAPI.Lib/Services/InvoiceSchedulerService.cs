using InvoiceAPI.Lib.Entity;
using InvoiceAPI.Lib.Enums;
using InvoiceAPI.Lib.Models.InvoiceXml;
using InvoiceAPI.Lib.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Xml.Linq;

namespace InvoiceAPI.Lib.Services
{
    public class InvoiceSchedulerService
    {
        private readonly WelldoneContext dbContext;
        private readonly IConfiguration config;
        private string errorContent = "";

        private readonly string f0401GeinvNamespaceUri = "urn:GEINV:eInvoiceMessage:F0401:4.1";
        private readonly string f0401XmlSchemaInstanceNamespaceUri = "http://www.w3.org/2001/XMLSchema-instance";
        private readonly string f0401SchemaLocation = "urn:GEINV:eInvoiceMessage:F0401:4.1 F0401.xsd";


        public InvoiceSchedulerService(WelldoneContext dbContext, IConfiguration config)
        {
            this.dbContext = dbContext;
            this.config = config;
            errorContent = "";
        }

        /// <summary>
        /// 建立 F0401 XML 檔案
        /// </summary>
        /// <returns></returns>
        public async Task CreateF0401()
        {
            string targetDirectory = config["F0401_Folder"];
            // 取得未處理的 F0401 發票記錄
            var pendingInvoices = await GetF0401InvoiceRecord();
            // 用於儲存成功產生 XML 的發票記錄實體
            // 產生 F0401 XML 檔案的處理過程
            List<invoice_record> successCreateRecords = await CreateF0401XmlProcess(pendingInvoices, targetDirectory);
            // 更新成功處理的發票記錄的發送狀態
            await UpdateSendStatus(successCreateRecords);
            // 記錄錯誤內容
            await WriteErrorLogToFile();
        }

        /// <summary>
        /// 將錯誤內容寫入檔案
        /// </summary>
        /// <param name="baseDirectory">基礎目錄，用於決定錯誤日誌儲存位置</param>
        /// <returns></returns>
        private async Task WriteErrorLogToFile()
        {
            if (!string.IsNullOrWhiteSpace(errorContent))
            {
                if(!Directory.Exists(config["LogFolder"]))
                {
                    Directory.CreateDirectory(config["LogFolder"]);
                }

                // 設定錯誤日誌檔案名稱，可以包含日期和時間以作區分
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string errorLogFileName = $"ErrorLog_{timestamp}.txt";
                string errorLogFilePath = Path.Combine(config["LogFolder"], errorLogFileName);

                // 非同步寫入錯誤內容到檔案
                await File.WriteAllTextAsync(errorLogFilePath, errorContent);
                Console.WriteLine($"錯誤日誌已寫入: {errorLogFilePath}");
            }
            else
            {
                Console.WriteLine("本次執行沒有錯誤內容需要記錄。");
            }
        }

        /// <summary>
        /// 產生 F0401 XML 檔案的處理過程
        /// </summary>
        /// <param name="pendingInvoices"></param>
        /// <param name="targetDirectory"></param>
        /// <returns></returns>
        private async Task<List<invoice_record>> CreateF0401XmlProcess(List<invoice_record> pendingInvoices, string targetDirectory)
        {
            List<invoice_record> successCreateRecords = new List<invoice_record>();
            foreach (var record in pendingInvoices)
            {
                try
                {
                    // 建立F0401模型並進行資料映射
                    var model = GetF0401Model(record);
                    // 將model轉換為XML並儲存
                    await CreateF0401Xml(model, targetDirectory);
                    // XML 成功產生，將原始發票記錄加入成功列表
                    successCreateRecords.Add(record);
                }
                catch (Exception ex)
                {
                    string errro = $"產生發票失敗: {record.invoice_number} order_no: {record.order_no} {ex.Message} {ex.StackTrace}\n\n";
                    // 記錄錯誤
                    Console.WriteLine(errro);
                    errorContent += $"{errro}";
                }
            }
            return successCreateRecords;
        }

        /// <summary>
        /// 更新發票記錄的發送狀態
        /// </summary>
        /// <param name="successCreateRecords"></param>
        /// <returns></returns>
        private async Task UpdateSendStatus(List<invoice_record> successCreateRecords)
        {
            if (successCreateRecords.Count!=0)
            {
                foreach (var recordToUpdate in successCreateRecords)
                {
                    recordToUpdate.send_status = true;
                    recordToUpdate.update_date = TimeUtil.UnifiedNow();
                }

                try
                {
                    int count = await dbContext.SaveChangesAsync();
                    Console.WriteLine($"已成功更新 {count} 筆發票記錄的發送狀態。");
                }
                catch (DbUpdateException dbEx)
                {
                    // 記錄資料庫更新錯誤
                    string error = $"更新發票狀態時資料庫發生錯誤: {dbEx.Message} {dbEx.StackTrace}\n\n";
                    Console.WriteLine(error);
                    errorContent += error;
                }
                catch (Exception ex)
                {
                    // 記錄其他可能的儲存錯誤
                    string error = $"儲存發票狀態變更時發生未預期錯誤: {ex.Message} {ex.StackTrace}\n\n";
                    Console.WriteLine(error);
                    errorContent += error;
                }
            }
            else
            {
                string error = "沒有成功產生 XML 的發票記錄可供更新狀態。\n\n";
                Console.WriteLine(error);
                errorContent += error;
            }
        }

        /// <summary>
        /// 取得未處理F0401發票記錄
        /// </summary>
        /// <returns></returns>
        private async Task<List<invoice_record>> GetF0401InvoiceRecord()
        {
            return await dbContext.invoice_record
                .Include(x => x.system_code)
                .Include(x => x.user_serial_map)
                .Include(x => x.invoice_product_record)
                .Where(x => x.send_status==false &&
                    x.operation_type_id == (int)InvoiceOperationTypeEnum.F0401)
                .ToListAsync();
        }

        /// <summary>
        /// 將F0401模型轉換為XML並儲存
        /// </summary>
        /// <param name="model"></param>
        /// <param name="targetDirectory"></param>
        private async Task CreateF0401Xml(F0401Model model, string targetDirectory)
        {
            XNamespace ns = f0401GeinvNamespaceUri;
            XNamespace xsi = f0401XmlSchemaInstanceNamespaceUri;

            XDocument xmlDoc = new XDocument(
                new XDeclaration("1.0", "UTF-8", null),
                new XElement(ns + "Invoice",
                    new XAttribute(XNamespace.Xmlns + "xsi", xsi),
                    new XAttribute(xsi + "schemaLocation", f0401SchemaLocation),
                    new XElement(ns + "Main",
                        new XElement(ns + "InvoiceNumber", model.InvoiceNumber),
                        new XElement(ns + "InvoiceDate", model.InvoiceDate),
                        new XElement(ns + "InvoiceTime", model.InvoiceTime),
                        new XElement(ns + "Seller",
                            new XElement(ns + "Identifier", model.Seller.Identifier),
                            new XElement(ns + "Name", model.Seller.Name),
                            new XElement(ns + "Address", model.Seller.Address),
                            new XElement(ns + "PersonInCharge", model.Seller.PersonInCharge),
                            new XElement(ns + "TelephoneNumber", model.Seller.TelephoneNumber),
                            new XElement(ns + "FacsimileNumber", model.Seller.FacsimileNumber),
                            new XElement(ns + "EmailAddress", model.Seller.EmailAddress),
                            new XElement(ns + "CustomerNumber", model.Seller.CustomerNumber),
                            new XElement(ns + "RoleRemark", model.Seller.RoleRemark)
                        ),
                        new XElement(ns + "Buyer",
                            new XElement(ns + "Identifier", model.Buyer.Identifier),
                            new XElement(ns + "Name", model.Buyer.Name),
                            new XElement(ns + "Address", model.Buyer.Address),
                            new XElement(ns + "PersonInCharge", model.Buyer.PersonInCharge),
                            new XElement(ns + "TelephoneNumber", model.Buyer.TelephoneNumber),
                            new XElement(ns + "FacsimileNumber", model.Buyer.FacsimileNumber),
                            new XElement(ns + "EmailAddress", model.Buyer.EmailAddress),
                            new XElement(ns + "CustomerNumber", model.Buyer.CustomerNumber),
                            new XElement(ns + "RoleRemark", model.Buyer.RoleRemark)
                        ),
                        new XElement(ns + "BuyerRemark", model.BuyerRemark),
                        new XElement(ns + "MainRemark", model.MainRemark),
                        new XElement(ns + "CustomsClearanceMark", model.CustomsClearanceMark),
                        new XElement(ns + "Category", model.Category),
                        new XElement(ns + "RelateNumber", model.RelateNumber),
                        new XElement(ns + "InvoiceType", model.InvoiceType),
                        new XElement(ns + "GroupMark", model.GroupMark),
                        new XElement(ns + "DonateMark", model.DonateMark),
                        new XElement(ns + "CarrierType", model.CarrierType),
                        new XElement(ns + "CarrierId1", model.CarrierId1),
                        new XElement(ns + "CarrierId2", model.CarrierId2),
                        new XElement(ns + "PrintMark", model.PrintMark),
                        new XElement(ns + "NPOBAN", model.NPOBAN),
                        new XElement(ns + "RandomNumber", model.RandomNumber),
                        new XElement(ns + "BondedAreaConfirm", model.BondedAreaConfirm),
                        new XElement(ns + "ZeroTaxRateReason", model.ZeroTaxRateReason),
                        new XElement(ns + "Reserved1", model.Reserved1),
                        new XElement(ns + "Reserved2", model.Reserved2)
                    ),
                    new XElement(ns + "Details",
                        model.Details.Select(item =>
                            new XElement(ns + "ProductItem",
                                new XElement(ns + "Description", item.Description),
                                new XElement(ns + "Quantity", item.Quantity),
                                new XElement(ns + "Unit", item.Unit),
                                new XElement(ns + "UnitPrice", item.UnitPrice),
                                new XElement(ns + "TaxType", item.TaxType),
                                new XElement(ns + "Amount", item.Amount),
                                new XElement(ns + "SequenceNumber", item.SequenceNumber),
                                new XElement(ns + "Remark", item.Remark),
                                new XElement(ns + "RelateNumber", item.RelateNumber)
                            )
                        )
                    ),
                    new XElement(ns + "Amount",
                        new XElement(ns + "SalesAmount", model.Amount.SalesAmount),
                        new XElement(ns + "FreeTaxSalesAmount", model.Amount.FreeTaxSalesAmount),
                        new XElement(ns + "ZeroTaxSalesAmount", model.Amount.ZeroTaxSalesAmount),
                        new XElement(ns + "TaxType", model.Amount.TaxType),
                        new XElement(ns + "TaxRate", model.Amount.TaxRate),
                        new XElement(ns + "TaxAmount", model.Amount.TaxAmount),
                        new XElement(ns + "TotalAmount", model.Amount.TotalAmount),
                        new XElement(ns + "DiscountAmount", model.Amount.DiscountAmount),
                        new XElement(ns + "OriginalCurrencyAmount", model.Amount.OriginalCurrencyAmount),
                        new XElement(ns + "ExchangeRate", model.Amount.ExchangeRate),
                        new XElement(ns + "Currency", model.Amount.Currency)
                    )
                )
            );

            // 檔案儲存路徑 (儲存在應用程式根目錄下的 "GeneratedInvoices" 資料夾)
            string fileName = $"{model.InvoiceNumber}_{model.Buyer.Name}_{config["Env"]}.xml"; // 使用發票號碼作為檔名
                                                                                 // 確保目標資料夾存在
            if (!Directory.Exists(targetDirectory))
            {
                Directory.CreateDirectory(targetDirectory);
            }
            string filePath = Path.Combine(targetDirectory, fileName);

            // 儲存 XML 檔案
            // 使用 FileStream 進行非同步儲存
            using (FileStream stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 4096, useAsync: true))
            {
                await xmlDoc.SaveAsync(stream, SaveOptions.None, CancellationToken.None);
            }
            Console.WriteLine($"已儲存發票 XML: {filePath}"); // 可選：記錄儲存的檔案路徑
        }

        /// <summary>
        /// 將發票記錄轉換為 F0401 模型
        /// </summary>
        /// <param name="record"></param>
        /// <returns></returns>
        private F0401Model GetF0401Model(invoice_record record)
        {
            // 建立 C0401 模型並進行資料映射
            var model = new F0401Model
            {
                InvoiceNumber = record.invoice_number,
                InvoiceDate = record.invoice_date,
                InvoiceTime = record.invoice_time,
                Seller = new F0401Seller
                {
                    Identifier = record.system_code.seller_identifier,
                    Name = record.system_code.seller_name,
                    Address = record.system_code.seller_address,
                    TelephoneNumber = record.system_code.seller_telephone_number
                },
                Buyer = new F0401Buyer
                {
                    Identifier = record.buyer_identifier,
                    Name = record.carrier_id_1,
                },
                InvoiceType = record.invoice_type,
                DonateMark = record.donate_mark,
                CarrierType = record.system_code.carrier_type,
                CarrierId1 = record.carrier_id_1,
                CarrierId2 = record.carrier_id_1,
                PrintMark = record.print_mark,
                RandomNumber = record.random_number,

                Amount = new F0401Amount
                {
                    // (decimal 轉 int，四捨五入)
                    SalesAmount = (int)Math.Round(record.sales_amount, MidpointRounding.AwayFromZero),
                    // 0
                    FreeTaxSalesAmount = (int)record.free_tax_sales_amount,
                    // 0
                    ZeroTaxSalesAmount = (int)record.zero_tax_sales_amount,
                    TaxType = record.tax_type,
                    TaxRate = record.tax_rate,
                    // 0
                    TaxAmount = (int)record.tax_amount,
                    // (decimal 轉 int，四捨五入)
                    TotalAmount = (int)Math.Round(record.total_amount, MidpointRounding.AwayFromZero)
                },
                // 發票品項明細
                Details = record.invoice_product_record.Select(p => new F0401ProductItem
                {
                    Description = p.description,
                    Quantity = p.quantity,
                    Unit = p.unit,
                    UnitPrice = (int)Math.Round(p.unit_price, MidpointRounding.AwayFromZero),
                    Amount = (int)Math.Round(p.amount, MidpointRounding.AwayFromZero),
                    // 001, 002, 003
                    SequenceNumber = $"{p.sequence_number:D3}",
                    TaxType = record.tax_type,
                }).ToList()
            };
            return model;
        }
    }
}
