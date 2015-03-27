using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Web.Http;
using System.Xml.Linq;
using Merchello.Core.Models;
using Microsoft.Web.Infrastructure;
using Umbraco.Core.IO;
using Umbraco.Web.Mvc;

namespace Merchello.Plugin.Reports.ExportOrders
{
    using System.Collections.Generic;

    using Merchello.Core;
    using Merchello.Web.Models.Querying;
    using Merchello.Web.Trees;
    using Merchello.Web.Reporting;
    using Merchello.Web.WebApi;
    using Merchello.Core.Services;
    using Merchello.Web;
    

    /// <summary>
    /// The sales over time report controller.
    /// </summary>
    [BackOfficeTree("exportOrders", "reports", "Export Orders", "icon-download", "Merchello.ExportOrders\\ExportOrders", 100)]
    public class ExportOrdersReportApiController : ReportController
    {
        /// <summary>
        /// The <see cref="IInvoiceService"/>.
        /// </summary>
        private readonly IInvoiceService _invoiceService;

        /// <summary>
        /// The <see cref="MerchelloHelper"/>
        /// </summary>
        private readonly MerchelloHelper _merchello;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExportOrdersReportApiController"/> class.
        /// </summary>
        public ExportOrdersReportApiController()
            : this(Core.MerchelloContext.Current)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExportOrdersReportApiController"/> class.
        /// </summary>
        /// <param name="merchelloContext">
        /// The merchello context.
        /// </param>
        public ExportOrdersReportApiController(IMerchelloContext merchelloContext)
            : base(merchelloContext)
        {
            _invoiceService = merchelloContext.Services.InvoiceService;
            
            _merchello = new MerchelloHelper(merchelloContext.Services);
        }

        /// <summary>
        /// Gets the base url.
        /// GET /umbraco/Merchello/InvoiceApi/GetInvoice/{guid}

        /// </summary>
        public override KeyValuePair<string, object> BaseUrl
        {
            get
            {
                return GetBaseUrl<ExportOrdersReportApiController>("merchelloReportExportOrders");
            }
        }
         
         /// <summary> 
         /// The get default report data. 
         /// </summary> 
         /// <returns> 
         /// The <see cref="QueryResultDisplay"/>. 
         /// </returns> 
         public override QueryResultDisplay GetDefaultReportData() 
         { 
             throw new System.NotImplementedException(); 
         }

        private string FormatAddress(IAddress extendedData )
        {
            var sb = new StringBuilder();

            sb.Append(extendedData.Address1).AppendLine();
            if (extendedData.Address2.Length > 0)
            {
                sb.Append(extendedData.Address2).AppendLine();
            }
            sb.AppendFormat("{0},{1} {2}", extendedData.Locality, extendedData.Region, extendedData.PostalCode).AppendLine();
            sb.Append(extendedData.CountryCode);

            return sb.ToString();
        }

        /// <summary>
        /// The get default report data.
        /// 
        /// GET /umbraco/Merchello/ExportOrdersReportApi/GetOrderReportData/
        /// 
        /// </summary>
        /// <returns>
        /// The <see cref="QueryResultDisplay"/>.
        /// </returns>
        [HttpPost]
        public HttpResponseMessage GetOrderReportData(QueryDisplay query)
        {
            HttpResponseMessage result = null;

            var invoiceDateStart = query.Parameters.FirstOrDefault(x => x.FieldName == "invoiceDateStart");
            var invoiceDateEnd = query.Parameters.FirstOrDefault(x => x.FieldName == "invoiceDateEnd");

            DateTime dtStart;
            DateTime dtEnd;
            if (invoiceDateStart == null)
            {
                result = Request.CreateErrorResponse(HttpStatusCode.BadRequest, "invoiceDateStart is a required parameter");
                return result;
            }
            if (!DateTime.TryParse(invoiceDateStart.Value, out dtStart))
            {
                result = Request.CreateErrorResponse(HttpStatusCode.BadRequest, "Failed to convert invoiceDateStart to a valid DateTime");
                return result;
            }

            dtEnd = invoiceDateEnd == null
                ? DateTime.MaxValue
                : DateTime.TryParse(invoiceDateEnd.Value, out dtEnd)
                    ? dtEnd
                    : DateTime.MaxValue;

            var invoices = _invoiceService.GetInvoicesByDateRange(dtStart,dtEnd).ToArray();

            try
            {
                var csvExport = new CvsExport();
                foreach (var invoice in invoices)
                {
                    csvExport.AddRow();

                    csvExport["Invoice Number"] = invoice.InvoiceNumber;
                    csvExport["PO Number"] = invoice.PoNumber;
                    csvExport["Order Date"] = invoice.InvoiceDate;
                    csvExport["Bill To Name"] = invoice.BillToName;
                    csvExport["Bill To Company"] = invoice.BillToCompany;
                    csvExport["Bill To Address"] = invoice.BillToAddress1;
                    csvExport["Bill To Address2"] = invoice.BillToAddress2;
                    csvExport["Email"] = invoice.BillToEmail;
                    csvExport["Phone"] = invoice.BillToPhone;
                    csvExport["City"] = invoice.BillToLocality;
                    csvExport["State"] = invoice.BillToRegion;
                    csvExport["Postal Code"] = invoice.BillToPostalCode;
                    csvExport["Total"] = invoice.Total;
                    csvExport["Status"] = invoice.InvoiceStatus.Name;

                    foreach (var invoiceItems in invoice.Items)
                    {
                        foreach (var invoiceItem in invoice.Items)
                        {
                            if (invoiceItem.LineItemType == LineItemType.Product)
                            {
                                csvExport["Name"] = invoiceItem.Name;
                                csvExport["Sku"] = invoiceItem.Sku;
                                csvExport["Quantity"] = invoiceItem.Quantity;
                                csvExport["Price"] = invoiceItem.Price;
                            }
                            else if (invoiceItem.LineItemType == LineItemType.Shipping)
                            {
                                csvExport["Ship Method"] = invoiceItem.Name;
                                csvExport["Ship Quantity"] = invoiceItem.Quantity;
                                csvExport["Ship Price"] = invoiceItem.Price;

                                var origin =invoiceItem.ExtendedData.GetAddress(Constants.ExtendedDataKeys.ShippingOriginAddress);
                                var destination =invoiceItem.ExtendedData.GetAddress(Constants.ExtendedDataKeys.ShippingDestinationAddress);

                                csvExport["Ship Origin"] = FormatAddress(origin);
                                csvExport["Ship Destination"] = FormatAddress(destination);
                            }
                            else if (invoiceItem.LineItemType == LineItemType.Tax)
                            {
                                csvExport["Tax"] = invoiceItem.Name;
                                csvExport["Tax Quantity"] = invoiceItem.Quantity;
                                csvExport["Tax Price"] = invoiceItem.Price;
                            }
                            else if (invoiceItem.LineItemType == LineItemType.Discount)
                            {
                                csvExport["Coupon"] = invoiceItem.Name;
                                csvExport["Coupon Quantity"] = invoiceItem.Quantity;
                                csvExport["Coupon Price"] = invoiceItem.Price;
                            }
                        }
                    }
                }

                result = Request.CreateResponse(HttpStatusCode.OK);
                result.Content = new StreamContent(csvExport.ExportToStream());
                result.Content.Headers.ContentType = new MediaTypeHeaderValue("text/csv");
                result.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment")
                {
                    FileName = "Orders.csv"
                };
            }
            catch (SystemException ex)
            {
                result = Request.CreateErrorResponse(HttpStatusCode.InternalServerError, ex);
                return result;
            }

            return result;
        }
    }
}