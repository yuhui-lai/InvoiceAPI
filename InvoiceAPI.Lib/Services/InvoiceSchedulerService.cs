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

        private readonly string c0401GeinvNamespaceUri = "urn:GEINV:eInvoiceMessage:C0401:3.2";
        private readonly string c0401XmlSchemaInstanceNamespaceUri = "http://www.w3.org/2001/XMLSchema-instance";
        private readonly string c0401SchemaLocation = "urn:GEINV:eInvoiceMessage:C0401:3.2 C0401.xsd";


        public InvoiceSchedulerService(WelldoneContext dbContext, IConfiguration config)
        {
            this.dbContext = dbContext;
            this.config = config;
        }

        /// <summary>
        /// 建立 C0401 XML 檔案
        /// </summary>
        /// <returns></returns>
        public async Task CreateC0401()
        {
            string targetDirectory = config["C0401_Folder"];
            // 取得未處理的 C0401 發票記錄
            var pendingInvoices = await GetC0401InvoiceRecord();
            // 用於儲存成功產生 XML 的發票記錄實體
            // 產生 C0401 XML 檔案的處理過程
            List<invoice_record> successCreateRecords = await CreateC0401XmlProcess(pendingInvoices, targetDirectory);
            // 更新成功處理的發票記錄的發送狀態
            await UpdateSendStatus(successCreateRecords);
        }

        /// <summary>
        /// 產生 C0401 XML 檔案的處理過程
        /// </summary>
        /// <param name="pendingInvoices"></param>
        /// <param name="targetDirectory"></param>
        /// <returns></returns>
        private async Task<List<invoice_record>> CreateC0401XmlProcess(List<invoice_record> pendingInvoices, string targetDirectory)
        {
            List<invoice_record> successCreateRecords = new List<invoice_record>();
            foreach (var record in pendingInvoices)
            {
                try
                {
                    // 建立C0401模型並進行資料映射
                    var model = GetC0401Model(record);
                    // 將model轉換為XML並儲存
                    await CreateC0401Xml(model, targetDirectory);
                    // XML 成功產生，將原始發票記錄加入成功列表
                    successCreateRecords.Add(record);
                }
                catch (Exception ex)
                {
                    // 記錄錯誤
                    Console.WriteLine($"產生發票失敗: {record.invoice_number}: {ex.Message}");
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
                    Console.WriteLine($"更新發票狀態時資料庫發生錯誤: {dbEx.Message}");
                }
                catch (Exception ex)
                {
                    // 記錄其他可能的儲存錯誤
                    Console.WriteLine($"儲存發票狀態變更時發生未預期錯誤: {ex.Message}");
                }
            }
            else
            {
                Console.WriteLine("沒有成功產生 XML 的發票記錄可供更新狀態。");
            }
        }

        /// <summary>
        /// 取得未處理C0401發票記錄
        /// </summary>
        /// <returns></returns>
        private async Task<List<invoice_record>> GetC0401InvoiceRecord()
        {
            return await dbContext.invoice_record
                .Include(x => x.system_code)
                .Include(x => x.user_serial_map)
                .Include(x => x.invoice_product_record)
                .Where(x => x.send_status==false &&
                    x.operation_type_id == (int)InvoiceOperationTypeEnum.C0401)
                .ToListAsync();
        }

        /// <summary>
        /// 將C0401模型轉換為XML並儲存
        /// </summary>
        /// <param name="model"></param>
        /// <param name="targetDirectory"></param>
        private async Task CreateC0401Xml(C0401Model model, string targetDirectory)
        {
            XNamespace ns = c0401GeinvNamespaceUri;
            XNamespace xsi = c0401XmlSchemaInstanceNamespaceUri;

            XDocument xmlDoc = new XDocument(
                new XDeclaration("1.0", "UTF-8", null),
                new XElement(ns + "Invoice",
                    new XAttribute(XNamespace.Xmlns + "xsi", xsi),
                    new XAttribute(xsi + "schemaLocation", c0401SchemaLocation),
                    new XElement(ns + "Main",
                        new XElement(ns + "InvoiceNumber", model.InvoiceNumber),
                        new XElement(ns + "InvoiceDate", model.InvoiceDate),
                        new XElement(ns + "InvoiceTime", model.InvoiceTime),
                        new XElement(ns + "Seller",
                            new XElement(ns + "Identifier", model.SellerIdentifier),
                            new XElement(ns + "Name", model.SellerName),
                            new XElement(ns + "Address", model.SellerAddress),
                            new XElement(ns + "TelephoneNumber", model.SellerTelephoneNumber)
                        ),
                        new XElement(ns + "Buyer",
                            new XElement(ns + "Identifier", model.BuyerIdentifier),
                            new XElement(ns + "Name", model.BuyerName)
                        ),
                        new XElement(ns + "InvoiceType", model.InvoiceType),
                        new XElement(ns + "DonateMark", model.DonateMark),
                        new XElement(ns + "CarrierType", model.CarrierType),
                        new XElement(ns + "CarrierId1", model.CarrierId1),
                        new XElement(ns + "CarrierId2", model.CarrierId2),
                        new XElement(ns + "PrintMark", model.PrintMark),
                        new XElement(ns + "RandomNumber", model.RandomNumber)
                    ),
                    new XElement(ns + "Details",
                        model.Details.Select(item =>
                            new XElement(ns + "ProductItem",
                                new XElement(ns + "Description", item.Description),
                                new XElement(ns + "Quantity", item.Quantity),
                                new XElement(ns + "Unit", item.Unit),
                                new XElement(ns + "UnitPrice", item.UnitPrice),
                                new XElement(ns + "Amount", item.Amount),
                                new XElement(ns + "SequenceNumber", item.SequenceNumber)
                            )
                        )
                    ),
                    new XElement(ns + "Amount",
                        new XElement(ns + "SalesAmount", model.SalesAmount),
                        new XElement(ns + "FreeTaxSalesAmount", model.FreeTaxSalesAmount),
                        new XElement(ns + "ZeroTaxSalesAmount", model.ZeroTaxSalesAmount),
                        new XElement(ns + "TaxType", model.TaxType),
                        new XElement(ns + "TaxRate", model.TaxRate.ToString("0.00")),
                        new XElement(ns + "TaxAmount", model.TaxAmount),
                        new XElement(ns + "TotalAmount", model.TotalAmount)
                    )
                )
            );

            // 檔案儲存路徑 (儲存在應用程式根目錄下的 "GeneratedInvoices" 資料夾)
            string fileName = $"{model.InvoiceNumber}_{model.BuyerName}_{config["Env"]}.xml"; // 使用發票號碼作為檔名
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
        /// 將發票記錄轉換為 C0401 模型
        /// </summary>
        /// <param name="record"></param>
        /// <returns></returns>
        private C0401Model GetC0401Model(invoice_record record)
        {
            // 建立 C0401 模型並進行資料映射
            var model = new C0401Model
            {
                InvoiceNumber = record.invoice_number,
                InvoiceDate = record.invoice_date,
                InvoiceTime = record.invoice_time,
                SellerIdentifier = record.system_code.seller_identifier,
                SellerName = record.system_code.seller_name,
                SellerAddress = record.system_code.seller_address,
                SellerTelephoneNumber = record.system_code.seller_telephone_number,
                BuyerIdentifier = record.buyer_identifier,
                BuyerName = record.carrier_id_1,
                InvoiceType = record.invoice_type,
                DonateMark = record.donate_mark,
                CarrierType = record.system_code.carrier_type,
                CarrierId1 = record.carrier_id_1,
                CarrierId2 = record.carrier_id_1,
                PrintMark = record.print_mark,
                RandomNumber = record.random_number,
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
                TotalAmount = (int)Math.Round(record.total_amount, MidpointRounding.AwayFromZero),
                // 發票品項明細
                Details = record.invoice_product_record.Select(p => new C0401ProductItem
                {
                    Description = p.description,
                    Quantity = p.quantity,
                    Unit = p.unit,
                    UnitPrice = (int)Math.Round(p.unit_price, MidpointRounding.AwayFromZero),
                    Amount = (int)Math.Round(p.amount, MidpointRounding.AwayFromZero),
                    // 001, 002, 003
                    SequenceNumber = $"{p.sequence_number:D3}"
                }).ToList()
            };
            return model;
        }
    }
}
