using BrinksAPI.Auth;
using BrinksAPI.Helpers;
using BrinksAPI.Interfaces;
using BrinksAPI.Models;
using Microsoft.AspNetCore.Mvc;
using NativeOrganization;
using NativeRequest;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace BrinksAPI.Controllers
{
    public class BillingController : Controller
    {
        private readonly ILogger<BillingController> _logger;
        private readonly IConfigManager _configuration;
        private readonly ApplicationDbContext _context;
        public BillingController(IConfigManager configuration, ApplicationDbContext applicationDbContext, ILogger<BillingController> logger)
        {
            _configuration = configuration;
            _context = applicationDbContext;
            _logger = logger;
        }

        #region HERMES COST FILE - REVENUE
        /// <summary>
        /// Creates Revenue
        /// </summary>
        /// <param name="revenues"></param>
        /// <returns>A newly created Revenues</returns>
        /// <remarks>
        /// Sample request:
        ///
        ///     POST /api/invoice/revenue
        ///     [{
        ///         "customer_gcc": "HERMESLTD",
        ///         "category_code": "GROUND",
        ///         "tax_code": "NONEURO0",
        ///         "description": "CH - Ground support",
        ///         "invoice_amount": 60.00,
        ///         "invoice_tax_amount": 0,
        ///         "hawb_number": "37100449646"
        ///     },
        ///     {
        ///         "customer_gcc": "HERMESLTD",
        ///         "category_code": "GROUND",
        ///         "tax_code": "NONEURO0",
        ///         "description": "CH - Ground support",
        ///         "invoice_amount": 60.00,
        ///         "invoice_tax_amount": 0,
        ///         "hawb_number": "37100449647"
        ///     }]
        /// </remarks>
        /// <response code="200">Success</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="500">Internal server error</response>
        [HttpPost]
        [ProducesResponseType(200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(500)]
        [Route("api/invoice/revenue")]
        public ActionResult<List<RevenueResponse>> Revenue([FromBody] Billing.Revenue[] revenues)
        {
            List<RevenueResponse> dataResponses = new List<RevenueResponse>();
            try
            {
                foreach (var revenue in revenues)
                {
                    RevenueResponse dataResponse = new RevenueResponse();
                    dataResponse.HawbNum = revenue?.hawb_number;
                    try
                    {
                        var validationResults = new List<ValidationResult>();
                        var validationContext = new ValidationContext(revenue);
                        var isValid = Validator.TryValidateObject(revenue, validationContext, validationResults);
                        if (isValid)
                        {
                            var shipmentDetails = GetShipmentDetailByHawb(revenue?.hawb_number);
                            string? shipmentId = shipmentDetails?.ShipmentNo;

                            if (!String.IsNullOrEmpty(shipmentId))
                            {
                                UniversalShipmentData shipmentData = GetShipmentById(shipmentId);
                                if (shipmentData.Shipment != null)
                                {
                                    Shipment shipment = new Shipment();

                                    // Consol or Shipment
                                    string? destinationCountry = "";
                                    string? destinationPort = "";
                                    if (shipmentData.Shipment.PortOfDestination != null)
                                    {
                                        destinationCountry = shipmentData.Shipment.PortOfDestination?.Code.Substring(0, 2);
                                        destinationPort = shipmentData.Shipment.PortOfDestination?.Code.Substring(2, 3);

                                    }
                                    else
                                    {
                                        if (shipmentData.Shipment.SubShipmentCollection != null)
                                        {
                                            destinationCountry = shipmentData.Shipment.SubShipmentCollection[0].PortOfDestination?.Code.Substring(0, 2);
                                            destinationPort = shipmentData.Shipment.SubShipmentCollection[0].PortOfDestination?.Code.Substring(2, 3);
                                        }
                                    }
                                    string? companyCode = _context.sites.Where(s => s.Country == destinationCountry && s.Airport == destinationPort).FirstOrDefault()?.CompanyCode;

                                    #region DATA CONTEXT
                                    DataContext dataContext = new DataContext();
                                    List<DataTarget> dataTargets = new List<DataTarget>();
                                    DataTarget dataTarget = new DataTarget();
                                    dataTarget.Type = "ForwardingShipment";
                                    dataTarget.Key = shipmentId;
                                    dataTargets.Add(dataTarget);

                                    dataContext.EnterpriseID = shipmentData.Shipment.DataContext.EnterpriseID;
                                    dataContext.ServerID = shipmentData.Shipment.DataContext.ServerID;
                                    dataContext.DataProvider = "RevenueAPI";
                                    //dataContext.Company = shipmentData.Shipment.DataContext.Company; // Sending default to CHN
                                    if (revenue.site_id != null)
                                    {
                                        companyCode = _context.sites.Where(s => s.SiteCode == Convert.ToInt32(revenue.site_id)).FirstOrDefault()?.CompanyCode;
                                    }



                                    Company company = new Company();
                                    company.Code = companyCode;
                                    dataContext.Company = company;

                                    dataContext.DataTargetCollection = dataTargets.ToArray();
                                    shipment.DataContext = dataContext;
                                    #endregion

                                    List<ChargeLine> chargeLines = new List<ChargeLine>();
                                    ChargeLine chargeLine = new ChargeLine();

                                    //waiting for confirmation to add department code in revenue -- renz 11/01/2023
                                    //var sDetails = GetShipmentByIdAndCompanyCode(shipmentId, dataContext.EnterpriseID, dataContext.ServerID, dataContext.Company.Code);
                                    //if (sDetails != null)
                                    //{
                                    //    if (sDetails.Shipment.DataContext.DataSourceCollection.Any(y => y.Type == "ForwardingConsol"))
                                    //        departmentCode = sDetails.Shipment.SubShipmentCollection.FirstOrDefault().JobCosting?.Department?.Code;
                                    //    else
                                    //        departmentCode = sDetails.Shipment?.JobCosting?.Department?.Code;
                                    //}
                                    ////chargeLine.Department = new Department { Code = departmentCode };

                                    string? chargeCodeCW = _context.Categories.Where(c => c.BrinksCode.ToLower() == revenue.category_code.ToLower())?.FirstOrDefault()?.CWCode;

                                    ImportMetaData importMetaData = new ImportMetaData();
                                    importMetaData.Instruction = ImportMetaDataInstruction.Insert;
                                    List<MatchingCriteria> matchingCriterias = new List<MatchingCriteria>();
                                    importMetaData.MatchingCriteriaCollection = matchingCriterias.ToArray();
                                    chargeLine.ImportMetaData = importMetaData;

                                    ChargeCode chargeCode = new ChargeCode();
                                    chargeCode.Code = chargeCodeCW;
                                    chargeLine.ChargeCode = chargeCode;
                                    string hermesCostId = revenue.hermesCostID.ToString() + "-" + revenue.description;
                                    chargeLine.Description = hermesCostId;

                                    OrganizationReference debtor = new OrganizationReference();
                                    debtor.Type = "Organization";
                                    debtor.Key = revenue.customer_gcc;
                                    chargeLine.Debtor = debtor;

                                    string? taxCodeCW = _context.TaxTypes.Where(t => t.BrinksCode.ToLower() == revenue.tax_code.ToLower()).FirstOrDefault()?.CWCode;
                                    TaxID taxID = new TaxID();
                                    taxID.TaxCode = taxCodeCW;
                                    chargeLine.SellGSTVATID = taxID;

                                    //renz 06/01/2023
                                    if (debtor.Key.ToUpper() == "HERMESLTD")
                                    {
                                        chargeLine.SellInvoiceType = "CUD";
                                    }
                                    //chargeLine.Description = revenue.description;
                                    chargeLine.SellOSAmountSpecified = true;
                                    chargeLine.SellOSGSTVATAmountSpecified = true;
                                    chargeLine.SellOSAmount = revenue.invoice_amount;
                                    chargeLine.SellOSGSTVATAmount = revenue.invoice_tax_amount;
                                    chargeLine.SellReference = "API";
                                    chargeLines.Add(chargeLine);

                                    JobCosting jobCosting = new JobCosting();
                                    jobCosting.ChargeLineCollection = chargeLines.ToArray();
                                    shipment.JobCosting = jobCosting;
                                    shipmentData.Shipment = shipment;
                                    string xml = Utilities.Serialize(shipmentData);
                                    var billingResponse = eAdaptor.Services.SendToCargowise(xml, _configuration.URI, _configuration.Username, _configuration.Password);

                                    if (billingResponse.Status == "SUCCESS")
                                    {
                                        using (var reader = new StringReader(billingResponse.Data.Data.OuterXml))
                                        {
                                            var serializer = new XmlSerializer(typeof(Events.UniversalEventData));
                                            Events.UniversalEventData? responseEvent = (Events.UniversalEventData?)serializer.Deserialize(reader);

                                            bool isError = responseEvent.Event.ContextCollection.Any(c => c.Type.Value.Contains("FailureReason"));
                                            if (isError)
                                            {
                                                string errorMessage = responseEvent.Event.ContextCollection.Where(c => c.Type.Value == "FailureReason").FirstOrDefault().Value;
                                                if (errorMessage.Contains("hasn't saved it yet"))
                                                {
                                                    dataResponse.Status = "RETRY";
                                                }
                                                else
                                                {
                                                    dataResponse.Status = "ERROR";
                                                }
                                                MatchCollection matchedError = Regex.Matches(errorMessage, "(Error)(.*)");
                                                string[] groupedErrors = matchedError.GroupBy(x => x.Value).Select(y => y.Key).ToArray();
                                                dataResponse.Message = string.Join(",", groupedErrors);
                                                _logger.LogError("Error: {@Error} Request: {@Request}", dataResponse.Message, revenue);
                                            }
                                            else
                                            {
                                                dataResponse.Status = "SUCCESS";
                                                dataResponse.Message = "Revenue Created Sucessfully";
                                                _logger.LogInformation("Success: {@Success} Request: {@Request}", dataResponse.Message, revenue);
                                            }
                                        }
                                    }
                                    else
                                    {
                                        dataResponse.Status = "ERROR";
                                        string errorMessage = billingResponse.Data.Data.FirstChild.InnerText;
                                        MatchCollection matchedError = Regex.Matches(errorMessage, "(Error)(.*)");
                                        string[] groupedErrors = matchedError.GroupBy(x => x.Value).Select(y => y.Key).ToArray();
                                        dataResponse.Message = string.Join(",", groupedErrors);
                                        _logger.LogError("Error: {@Error} Request: {@Request}", dataResponse.Message, revenue);
                                    }

                                }
                                else
                                {
                                    dataResponse.Status = "ERROR";
                                    dataResponse.Message = "Unable to fetch the shipment from the CW.";
                                    _logger.LogError("Error: {@Error} Request: {@Request}", dataResponse.Message, revenue);
                                }

                            }
                            else
                            {
                                dataResponse.Status = "NOTFOUND";
                                dataResponse.Message = String.Format("{0} Not Found.", revenue?.hawb_number);
                                _logger.LogError("Error: {@Error} Request: {@Request}", dataResponse.Message, revenue);
                            }
                        }
                        else
                        {
                            string validationMessage = "";
                            dataResponse.Status = "ERROR";
                            foreach (var validationResult in validationResults)
                                validationMessage += validationResult.ErrorMessage;
                            dataResponse.Message = validationMessage;
                        }
                        dataResponses.Add(dataResponse);
                    }
                    catch (Exception ex)
                    {
                        dataResponse.Status = "ERROR";
                        dataResponse.Message = ex.Message;
                        _logger.LogError("Error: {@Error}", dataResponse.Message);
                        dataResponses.Add(dataResponse);
                        continue;
                    }
                }
                return Ok(dataResponses);
            }
            catch (Exception ex)
            {
                RevenueResponse dataResponse = new RevenueResponse();
                dataResponse.Status = "ERROR";
                dataResponse.Message = ex.Message;
                dataResponses.Add(dataResponse);
                _logger.LogError("Error: {@Error}", dataResponse.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, dataResponses);
            }
        }
        #endregion

        #region HERMES PAYABLE INVOICE
        /// <summary>
        /// Creates Payable Invoice
        /// </summary>
        /// <param name="payableInvoice"></param>
        /// <returns>A newly created payable invoice</returns>
        /// <remarks>
        /// Sample request:
        ///
        ///     POST /api/invoice/payable
        ///     {
        ///       "invoice_number": "456795",
        ///       "invoice_gcc": "HERMESLTD",
        ///       "invoice_date": "03-11-2022 10:00:00",
        ///       "exchange_date": "03-11-2022 10:00:00",
        ///       "currency_code": "AED",
        ///       "exchange_rate": "1",
        ///       "origin_site_code": "3210",
        ///       "revenues": [
        ///         {
        ///           "category_code": "CUSTOMS",
        ///           "description": "Revenue Line Discription",
        ///           "invoice_currency": "AED",
        ///           "invoice_amount": "37",
        ///           "invoice_tax_amount": "5",
        ///           "tax_code": "NONEURO0",
        ///           "billed_from_site_code": "3210",
        ///           "revenue_hawb_number": "32100235812"
        ///         }
        ///       ]
        ///     }
        /// </remarks>
        /// <response code="200">Success</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="500">Internal server error</response>
        [HttpPost]
        [ProducesResponseType(200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(500)]
        [Route("api/invoice/payable")]
        public ActionResult<List<PayableInvoiceResponse>> PayableInvoice([FromBody] Billing.PayableInvoice payableInvoice)
        {
            PayableInvoiceResponse dataResponse = new PayableInvoiceResponse();
            try
            {
                dataResponse.InvoiceNum = payableInvoice?.invoice_number;

                #region MODEL VALIDATION
                if (!ModelState.IsValid)
                {
                    string errorString = "";
                    var errors = ModelState.Select(x => x.Value.Errors)
                         .Where(y => y.Count > 0)
                         .ToList();
                    foreach (var error in errors)
                    {
                        foreach (var subError in error)
                        {
                            errorString += String.Format("{0}", subError.ErrorMessage);
                        }
                    }
                    _logger.LogError("Error: {@Error} Request: {@Request}", errorString, payableInvoice);

                    dataResponse.Status = "ERROR";
                    dataResponse.Message = errorString;

                    return Ok(dataResponse);
                }
                #endregion

                UniversalTransaction.UniversalTransactionData universalTransactionData = new UniversalTransaction.UniversalTransactionData();
                UniversalTransaction.TransactionInfo transactionInfo = new UniversalTransaction.TransactionInfo();

                int originSiteCode = Convert.ToInt32(payableInvoice.origin_site_code);
                string? branchCodeCW = _context.sites.Where(s => s.SiteCode == originSiteCode).FirstOrDefault()?.CompanyCode;

                #region DATA CONTEXT
                UniversalTransaction.DataContext dataContext = new UniversalTransaction.DataContext();
                List<UniversalTransaction.DataTarget> dataTargets = new List<UniversalTransaction.DataTarget>();
                UniversalTransaction.DataTarget dataTarget = new UniversalTransaction.DataTarget();
                dataTarget.Type = "AccountingInvoice";
                dataTargets.Add(dataTarget);

                dataContext.EnterpriseID = _configuration.EnterpriseId;
                dataContext.ServerID = _configuration.ServerId;
                UniversalTransaction.Company company = new UniversalTransaction.Company();
                company.Code = branchCodeCW;
                dataContext.Company = company;

                dataContext.DataTargetCollection = dataTargets.ToArray();
                transactionInfo.DataContext = dataContext;
                #endregion


                transactionInfo.Ledger = "AP";
                transactionInfo.TransactionType = UniversalTransaction.TransactionInfoTransactionType.INV;
                UniversalTransaction.CodeDescriptionPair apAccountGroup = new UniversalTransaction.CodeDescriptionPair();
                apAccountGroup.Code = "TPY";
                transactionInfo.APAccountGroup = apAccountGroup;

                transactionInfo.Number = payableInvoice.invoice_number;
                transactionInfo.TransactionDate = payableInvoice.invoice_date;
                //transactionInfo.DueDate = payableInvoice.exchange_date;

                OrganizationData invoiceOrg = SearchOrgWithRegNo(payableInvoice.invoice_gcc);
                string invoiceOrgCode = invoiceOrg.OrgHeader == null ? "HERMESLTD" : invoiceOrg.OrgHeader.Code;
                UniversalTransaction.OrganizationAddress invoiceAddress = new UniversalTransaction.OrganizationAddress();
                invoiceAddress.AddressType = "OFC";
                invoiceAddress.OrganizationCode = invoiceOrgCode;
                transactionInfo.OrganizationAddress = invoiceAddress;

                UniversalTransaction.Currency currency = new UniversalTransaction.Currency();
                currency.Code = payableInvoice.currency_code;
                transactionInfo.OSCurrency = currency;

                //currency for hermes is always usd - renz 03/01/2023
                if (invoiceOrgCode == "HERMESLTD")
                {
                    currency.Code = "USD";
                    transactionInfo.OSCurrency = currency;

                }
                //transactionInfo.ExchangeRateSpecified = true;
                //transactionInfo.ExchangeRate = Convert.ToDecimal(payableInvoice.exchange_rate);

                UniversalTransaction.Branch branch = new UniversalTransaction.Branch();
                branch.Code = branchCodeCW;
                transactionInfo.Branch = branch;

                //UniversalTransaction.Department department = new UniversalTransaction.Department();
                //department.Code = "FES";
                //transactionInfo.Department = department;


                List<UniversalTransaction.PostingJournal> journals = new List<UniversalTransaction.PostingJournal>();
                List<string> shipmentIds = new List<string>();

                foreach (var revenue in payableInvoice.revenues)
                {
                    var shipmentDetails = GetShipmentDetailByHawb(revenue?.revenue_hawb_number);
                    string shipmentId = "";
                    string departmentCode = "";
                    if (shipmentDetails != null)
                    {
                        shipmentId = shipmentDetails.ShipmentNo;

                        //removed due to class error on delilveryDuedate field -- renz 16/01/2023
                        //var sDetails = GetShipmentByIdAndCompanyCode(shipmentId,dataContext.EnterpriseID,dataContext.ServerID, dataContext.Company.Code);
                        //if (sDetails.Shipment.DataContext.DataSourceCollection.Any(y => y.Type == "ForwardingConsol"))
                        //    departmentCode = sDetails.Shipment.SubShipmentCollection.FirstOrDefault().JobCosting?.Department?.Code;
                        //else
                        //    departmentCode = sDetails.Shipment.SubShipmentCollection.FirstOrDefault().JobCosting?.Department?.Code;

                        var dept1 = GetDepartmentCodeByIdAndCompanyCode(shipmentId, dataContext.EnterpriseID, dataContext.ServerID, dataContext.Company.Code);
                        departmentCode = dept1;
                    }

                    UniversalTransaction.Department department = new UniversalTransaction.Department();
                    department.Code = departmentCode;
                    //int billFromSiteCode = Convert.ToInt32(revenue.billed_from_site_code);
                    //string? companyCodeCW = _context.sites.Where(s => s.SiteCode == billFromSiteCode).FirstOrDefault()?.CompanyCode;

                    UniversalTransaction.PostingJournal journal = new UniversalTransaction.PostingJournal();

                    string? chargeCodeCW = _context.Categories.Where(c => c.BrinksCode == revenue.category_code).FirstOrDefault()?.CWCode;
                    UniversalTransaction.ChargeCode chargeCode = new UniversalTransaction.ChargeCode();
                    chargeCode.Code = DefaultValue(chargeCodeCW, "OTHER");
                    journal.ChargeCode = chargeCode;

                    journal.Description = revenue.description;

                    UniversalTransaction.Currency osCurrency = new UniversalTransaction.Currency();
                    osCurrency.Code = revenue.invoice_currency;
                    journal.OSCurrency = osCurrency;
                    //currency for hermes is always usd - renz 03/01/2023
                    if (invoiceOrgCode == "HERMESLTD")
                    {
                        osCurrency.Code = "USD";
                        journal.OSCurrency = osCurrency;

                    }
                    journal.OSAmountSpecified = true;
                    journal.OSAmount = -Math.Abs(Convert.ToDecimal(revenue.invoice_amount));

                    journal.OSGSTVATAmountSpecified = true;
                    journal.OSGSTVATAmount = -Math.Abs(Convert.ToDecimal(revenue.invoice_tax_amount));

                    //journal.OSTotalAmountSpecified = true;
                    //journal.OSTotalAmount = -Math.Abs(Convert.ToDecimal(revenue.invoice_amount) + Convert.ToDecimal(revenue.invoice_tax_amount));

                    string? taxCodeCW = _context.TaxTypes.Where(t => t.BrinksCode.ToLower() == revenue.tax_code.ToLower()).FirstOrDefault()?.CWCode;
                    UniversalTransaction.TaxID taxID = new UniversalTransaction.TaxID();
                    taxID.TaxCode = DefaultValue(taxCodeCW, "FREEVAT");
                    journal.VATTaxID = taxID;

                    journal.Branch = branch;
                    journal.Department = department;

                    UniversalTransaction.EntityReference job = new UniversalTransaction.EntityReference();
                    job.Type = "Job";
                    job.Key = shipmentId;
                    journal.Job = job;
                    shipmentIds.Add(shipmentId);

                    UniversalTransaction.OrganizationReference organizationReference = new UniversalTransaction.OrganizationReference();
                    organizationReference.Type = "Organization";
                    organizationReference.Key = invoiceOrgCode;
                    journal.Organization = organizationReference;

                    journals.Add(journal);
                }
                //removed dont convert for OS - renz 03/01/2022
                //decimal totalAmountExcludeTax = payableInvoice.revenues.Sum(s => Convert.ToDecimal(s.invoice_amount)) * Convert.ToDecimal(payableInvoice.exchange_rate);
                //decimal totalAmountTax = payableInvoice.revenues.Sum(s => Convert.ToDecimal(s.invoice_tax_amount)) * Convert.ToDecimal(payableInvoice.exchange_rate);


                //removed the exchange rate since this is going to os amount
                decimal totalAmountExcludeTax = journals.Sum(x => x.OSAmount);
                decimal totalAmountTax = journals.Sum(x => x.OSGSTVATAmount);


                transactionInfo.OSExGSTVATAmountSpecified = !IsNullOrEmpty(totalAmountExcludeTax.ToString()); ;
                transactionInfo.OSExGSTVATAmount = -Math.Abs(totalAmountExcludeTax);

                transactionInfo.OSGSTVATAmountSpecified = !IsNullOrEmpty(totalAmountTax.ToString());
                transactionInfo.OSGSTVATAmount = -Math.Abs(totalAmountTax);

                transactionInfo.OSTotalSpecified = true;
                transactionInfo.OSTotal = -Math.Abs(totalAmountExcludeTax + totalAmountTax);

                transactionInfo.PostingJournalCollection = journals.ToArray();

                List<UniversalTransaction.Shipment> shipments = new List<UniversalTransaction.Shipment>();


                shipmentIds = shipmentIds.Distinct().ToList();


                foreach (var shipmentId in shipmentIds)
                {
                    UniversalTransaction.Shipment shipment = new UniversalTransaction.Shipment();
                    UniversalTransaction.DataContext shipmentDataContext = new UniversalTransaction.DataContext();
                    List<UniversalTransaction.DataSource> shipmentDataSources = new List<UniversalTransaction.DataSource>();
                    List<UniversalTransaction.DataTarget> shipmentDataTargets = new List<UniversalTransaction.DataTarget>();

                    UniversalTransaction.DataSource shipmentDataSource = new UniversalTransaction.DataSource();
                    shipmentDataSource.Type = "ForwardingShipment";
                    shipmentDataSource.Key = shipmentId;
                    shipmentDataSources.Add(shipmentDataSource);

                    UniversalTransaction.DataTarget shipmentDataTarget = new UniversalTransaction.DataTarget();
                    shipmentDataTarget.Type = "ForwardingShipment";
                    shipmentDataTarget.Key = shipmentId;
                    shipmentDataTargets.Add(shipmentDataTarget);

                    shipmentDataContext.DataSourceCollection = shipmentDataSources.ToArray();
                    shipmentDataContext.DataTargetCollection = shipmentDataTargets.ToArray();
                    shipment.DataContext = shipmentDataContext;
                    shipments.Add(shipment);
                }
                if (shipments.Count() > 0)
                    transactionInfo.ShipmentCollection = shipments.ToArray();

                universalTransactionData.TransactionInfo = transactionInfo;

                string xml = Utilities.Serialize(universalTransactionData);
                var invoiceResponse = eAdaptor.Services.SendToCargowise(xml, _configuration.URI, _configuration.Username, _configuration.Password);

                if (invoiceResponse.Status == "SUCCESS")
                {
                    using (var reader = new StringReader(invoiceResponse.Data.Data.OuterXml))
                    {
                        var serializer = new XmlSerializer(typeof(Events.UniversalEventData));
                        Events.UniversalEventData? responseEvent = (Events.UniversalEventData?)serializer.Deserialize(reader);

                        bool isError = responseEvent.Event.ContextCollection.Any(c => c.Type.Value.Contains("FailureReason"));
                        if (isError)
                        {
                            string errorMessage = responseEvent.Event.ContextCollection.Where(c => c.Type.Value == "FailureReason").FirstOrDefault().Value.Replace("Error - ", "").Replace("Warning - ", "");
                            dataResponse.Status = "ERROR";
                            MatchCollection matchedError = Regex.Matches(errorMessage, "(Error)(.*)");
                            string[] groupedErrors = matchedError.GroupBy(x => x.Value).Select(y => y.Key).ToArray();
                            dataResponse.Message = string.Join(",", groupedErrors);
                            _logger.LogError("Error: {@Error} Request: {@Request}", dataResponse.Message, payableInvoice);
                        }
                        else
                        {
                            dataResponse.Status = "SUCCESS";
                            dataResponse.Message = "Invoice Created Sucessfully";
                            _logger.LogError("Success: {@Success} Request: {@Request}", dataResponse.Message, payableInvoice);
                        }
                    }
                }
                else
                {
                    string errorMessage = invoiceResponse.Data.Data.FirstChild.InnerText;
                    dataResponse.Status = "ERROR";
                    MatchCollection matchedError = Regex.Matches(errorMessage, "(Error)(.*)");
                    string[] groupedErrors = matchedError.GroupBy(x => x.Value).Select(y => y.Key).ToArray();
                    dataResponse.Message = string.Join(",", groupedErrors);
                    _logger.LogError("Error: {@Error} Request: {@Request}", dataResponse.Message, payableInvoice);
                }

            }
            catch (Exception ex)
            {
                dataResponse.Status = "ERROR";
                dataResponse.Message = ex.Message;
                _logger.LogError("Error: {@Error}", dataResponse.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, dataResponse);
            }
            return Ok(dataResponse);
        }
        #endregion
        public ShipmentDetails GetShipmentDetailByHawb(string hawb)
        {
            string? shipmentNumber = null;
            ShipmentDetails s = new ShipmentDetails();
            try
            {
                Events.UniversalEventData universalEvent = new Events.UniversalEventData();
                Events.Event @event = new Events.Event();

                #region DATA CONTEXT
                Events.DataContext eventDataContext = new Events.DataContext();
                List<Events.DataTarget> dataTargets = new List<Events.DataTarget>();
                Events.DataTarget dataTarget = new Events.DataTarget();
                dataTarget.Type = "ForwardingShipment";
                dataTargets.Add(dataTarget);
                eventDataContext.DataTargetCollection = dataTargets.ToArray();
                @event.DataContext = eventDataContext;
                #endregion

                #region EVENT DEATAIL
                @event.EventTime = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss");
                @event.EventType = "Z00";
                #endregion

                #region CONTEXT COLLECTION
                List<Events.Context> contexts = new List<Events.Context>();
                Events.Context context = new Events.Context();
                Events.ContextType type = new Events.ContextType();
                type.Value = "HAWBNumber";
                context.Type = type;
                context.Value = hawb;
                contexts.Add(context);
                @event.ContextCollection = contexts.ToArray();
                #endregion

                universalEvent.Event = @event;

                string xml = Utilities.Serialize(universalEvent);
                eAdaptor.Entities.XMLDataResponse? shipmentRequestResponse = eAdaptor.Services.SendToCargowise(xml, _configuration.URI, _configuration.Username, _configuration.Password);
                if (shipmentRequestResponse.Status == "SUCCESS")
                {
                    using (var reader = new StringReader(shipmentRequestResponse.Data.Data.OuterXml))
                    {
                        var serializer = new XmlSerializer(typeof(Events.UniversalEventData));
                        Events.UniversalEventData eventResponse = (Events.UniversalEventData)serializer.Deserialize(reader);
                        shipmentNumber = eventResponse?.Event?.DataContext?.DataSourceCollection?.Where(d => d.Type == "ForwardingShipment")?.FirstOrDefault()?.Key;

                        s.ShipmentNo = shipmentNumber;


                    }
                }
            }
            catch (Exception ex)
            {

            }
            return s;
        }

        public UniversalShipmentData GetShipmentById(string shipmentId)
        {
            UniversalShipmentData? response = new UniversalShipmentData();

            ShipmentRequest.UniversalShipmentRequestData dataRequest = new ShipmentRequest.UniversalShipmentRequestData();
            ShipmentRequest.ShipmentRequest shipmentRequest = new ShipmentRequest.ShipmentRequest();
            ShipmentRequest.DataContext requestDataContext = new ShipmentRequest.DataContext();
            List<ShipmentRequest.DataTarget> dataTargets = new List<ShipmentRequest.DataTarget>();
            ShipmentRequest.DataTarget dataTarget = new ShipmentRequest.DataTarget();

            dataTarget.Type = "ForwardingShipment";
            dataTarget.Key = shipmentId;
            dataTargets.Add(dataTarget);
            requestDataContext.DataTargetCollection = dataTargets.ToArray();
            shipmentRequest.DataContext = requestDataContext;
            dataRequest.ShipmentRequest = shipmentRequest;

            string xml = Utilities.Serialize(dataRequest);
            var shipmentRequestResponse = eAdaptor.Services.SendToCargowise(xml, _configuration.URI, _configuration.Username, _configuration.Password);
            if (shipmentRequestResponse.Status == "SUCCESS")
            {
                using (var reader = new StringReader(shipmentRequestResponse.Data.Data.OuterXml))
                {
                    var serializer = new XmlSerializer(typeof(UniversalShipmentData));
                    response = (UniversalShipmentData?)serializer.Deserialize(reader);
                }
            }

            return response;
        }

        public UniversalShipmentData GetShipmentByIdAndCompanyCode(string shipmentId, string enterpriseId, string serverId, string companyCode)
        {
            UniversalShipmentData? response = new UniversalShipmentData();

            ShipmentRequest.UniversalShipmentRequestData dataRequest = new ShipmentRequest.UniversalShipmentRequestData();
            ShipmentRequest.ShipmentRequest shipmentRequest = new ShipmentRequest.ShipmentRequest();
            ShipmentRequest.DataContext requestDataContext = new ShipmentRequest.DataContext();
            List<ShipmentRequest.DataTarget> dataTargets = new List<ShipmentRequest.DataTarget>();
            ShipmentRequest.DataTarget dataTarget = new ShipmentRequest.DataTarget();

            dataTarget.Type = "ForwardingShipment";
            dataTarget.Key = shipmentId;

            dataTargets.Add(dataTarget);
            requestDataContext.DataTargetCollection = dataTargets.ToArray();
            shipmentRequest.DataContext = requestDataContext;
            requestDataContext.EnterpriseID = enterpriseId;
            requestDataContext.ServerID = serverId;
            requestDataContext.Company = new ShipmentRequest.Company { Code = companyCode };
            dataRequest.ShipmentRequest = shipmentRequest;

            string xml = Utilities.Serialize(dataRequest);
            var shipmentRequestResponse = eAdaptor.Services.SendToCargowise(xml, _configuration.URI, _configuration.Username, _configuration.Password);
            if (shipmentRequestResponse.Status == "SUCCESS")
            {

                using (var reader = new StringReader(shipmentRequestResponse.Data.Data.OuterXml))
                {
                    var serializer = new XmlSerializer(typeof(UniversalShipmentData));
                    response = (UniversalShipmentData?)serializer.Deserialize(reader);
                }
            }

            return response;
        }

        public string GetDepartmentCodeByIdAndCompanyCode(string shipmentId, string enterpriseId, string serverId, string companyCode)
        {
            string? response = "";

            ShipmentRequest.UniversalShipmentRequestData dataRequest = new ShipmentRequest.UniversalShipmentRequestData();
            ShipmentRequest.ShipmentRequest shipmentRequest = new ShipmentRequest.ShipmentRequest();
            ShipmentRequest.DataContext requestDataContext = new ShipmentRequest.DataContext();
            List<ShipmentRequest.DataTarget> dataTargets = new List<ShipmentRequest.DataTarget>();
            ShipmentRequest.DataTarget dataTarget = new ShipmentRequest.DataTarget();

            dataTarget.Type = "ForwardingShipment";
            dataTarget.Key = shipmentId;

            dataTargets.Add(dataTarget);
            requestDataContext.DataTargetCollection = dataTargets.ToArray();
            shipmentRequest.DataContext = requestDataContext;
            requestDataContext.EnterpriseID = enterpriseId;
            requestDataContext.ServerID = serverId;
            requestDataContext.Company = new ShipmentRequest.Company { Code = companyCode };
            dataRequest.ShipmentRequest = shipmentRequest;

            string xml = Utilities.Serialize(dataRequest);
            var shipmentRequestResponse = eAdaptor.Services.SendToCargowise(xml, _configuration.URI, _configuration.Username, _configuration.Password);
            if (shipmentRequestResponse.Status == "SUCCESS")
            {
                XDocument doc;
                XNamespace ns = XNamespace.Get("http://www.cargowise.com/Schemas/Universal/2011/11");
                doc = XDocument.Parse(shipmentRequestResponse.Data.Data.OuterXml);
                var xmldocu = doc.Descendants();
                var jobcosting = xmldocu.Elements(ns + "JobCosting").FirstOrDefault();
                if (jobcosting != null)
                {
                    var dept = jobcosting.Element(ns + "Department");
                    if (dept != null)
                        response = dept.Element(ns + "Code").Value;

                }
                //foreach (var item in xmldocu.Elements(ns + "Shipment"))
                //{

                //}


                //using (var reader = new StringReader(shipmentRequestResponse.Data.Data.OuterXml))
                //{

                //    var serializer = new XmlSerializer(typeof(UniversalShipmentData));
                //    response = (UniversalShipmentData?)serializer.Deserialize(reader);
                //}
            }

            return response;
        }

        public OrganizationData SearchOrgWithRegNo(string regNo)
        {
            OrganizationData organizationData = new OrganizationData();

            NativeRequest.Native native = new NativeRequest.Native();
            NativeRequest.NativeBody body = new NativeRequest.NativeBody();
            CriteriaData criteria = new CriteriaData();

            CriteriaGroupType criteriaGroupType = new CriteriaGroupType();
            criteriaGroupType.Type = TypeEnum.Partial;

            List<CriteriaType> criteriaTypes = new List<CriteriaType>();

            CriteriaType criteriaType1 = new CriteriaType();
            criteriaType1.Entity = "OrgHeader.OrgCusCode";
            criteriaType1.FieldName = "CodeType";
            criteriaType1.Value = "LSC";
            criteriaTypes.Add(criteriaType1);

            CriteriaType criteriaType2 = new CriteriaType();
            criteriaType2.Entity = "OrgHeader.OrgCusCode";
            criteriaType2.FieldName = "CustomsRegNo";
            criteriaType2.Value = regNo;
            criteriaTypes.Add(criteriaType2);

            criteriaGroupType.Criteria = criteriaTypes.ToArray();

            criteria.CriteriaGroup = criteriaGroupType;
            body.ItemElementName = ItemChoiceType.Organization;
            body.Item = criteria;
            native.Body = body;

            string xml = Utilities.Serialize(native);
            var documentResponse = eAdaptor.Services.SendToCargowise(xml, _configuration.URI, _configuration.Username, _configuration.Password);
            if (documentResponse.Status == "SUCCESS" && documentResponse.Data.Status == "PRS" && documentResponse.Data.ProcessingLog != null)
            {
                using (TextReader reader = new StringReader(documentResponse.Data.Data.OuterXml))
                {
                    var serializer = new XmlSerializer(typeof(NativeOrganization.Native));
                    NativeOrganization.Native result = (NativeOrganization.Native)serializer.Deserialize(reader);
                    string organization = result.Body.Any[0].OuterXml;
                    using (TextReader reader2 = new StringReader(organization))
                    {
                        var serializer2 = new XmlSerializer(typeof(OrganizationData));
                        organizationData = (OrganizationData)serializer2.Deserialize(reader2);
                    }
                }
            }


            return organizationData;
        }
        #region DEFAULT VALUE
        private static string DefaultValue(string value, string defaultValue)
        {
            return value == null || value == "" ? defaultValue : value;
        }
        #endregion

        public bool IsNullOrEmpty(string value)
        {
            return value == null || value.Length == 0 || value == "";
        }
    }

    public class ShipmentDetails
    {
        public string? ShipmentNo { get; set; }
        public string? DepartmentCode { get; set; }
    }
}
