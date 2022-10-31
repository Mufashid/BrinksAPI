using BrinksAPI.Auth;
using BrinksAPI.Helpers;
using BrinksAPI.Interfaces;
using BrinksAPI.Models;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Xml.Serialization;

namespace BrinksAPI.Controllers
{
    public class BillingController : Controller
    {
        private readonly IConfigManager _configuration;

        private readonly ApplicationDbContext _context;
        public BillingController(IConfigManager configuration, ApplicationDbContext applicationDbContext)
        {
            _configuration = configuration;
            _context = applicationDbContext;
        }

        #region HERMES COST FILE
        /// <summary>
        /// Creates Revenue
        /// </summary>
        /// <param name="invoice"></param>
        /// <returns>A newly created Revenue</returns>
        /// <remarks>
        /// Sample request:
        ///
        ///     POST /api/shipment/revenue
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
        [Route("api/shipment/revenue")]
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
                            string shipmentId = GetShipmentNumberByHawb(revenue?.hawb_number);
                      
                            if (shipmentId != null && shipmentId != "")
                            {
                                UniversalShipmentData shipmentData = GetShipmentById(shipmentId);
                                string xml2 = Utilities.Serialize(shipmentData);
                                if (shipmentData.Shipment != null)
                                {
                                    Shipment shipment = new Shipment();
                                    
                                    #region DATA CONTEXT
                                    DataContext dataContext = new DataContext();
                                    List<DataTarget> dataTargets = new List<DataTarget>();
                                    DataTarget dataTarget = new DataTarget();
                                    dataTarget.Type = "ForwardingShipment";
                                    dataTarget.Key = shipmentId;
                                    dataTargets.Add(dataTarget);

                                    dataContext.EnterpriseID = shipmentData.Shipment.DataContext.EnterpriseID;
                                    dataContext.ServerID = shipmentData.Shipment.DataContext.ServerID;
                                    dataContext.Company = shipmentData.Shipment.DataContext.Company;

                                    dataContext.DataTargetCollection = dataTargets.ToArray();
                                    shipment.DataContext = dataContext;
                                    #endregion

                                    List<ChargeLine> chargeLines = new List<ChargeLine>();
                                    ChargeLine chargeLine = new ChargeLine();

                                    string? chargeCodeCW = _context.Categories.Where(c => c.BrinksCode.ToLower() == revenue.category_code.ToLower())?.FirstOrDefault()?.CWCode;

                                    ImportMetaData importMetaData = new ImportMetaData();
                                    importMetaData.Instruction = ImportMetaDataInstruction.UpdateAndInsertIfNotFound;
                                    List<MatchingCriteria> matchingCriterias = new List<MatchingCriteria>();
                                    MatchingCriteria matchingCriteria = new MatchingCriteria();
                                    matchingCriteria.FieldName = "ChargeCode";
                                    matchingCriteria.Value = chargeCodeCW;
                                    matchingCriterias.Add(matchingCriteria);
                                    importMetaData.MatchingCriteriaCollection = matchingCriterias.ToArray();
                                    chargeLine.ImportMetaData = importMetaData;

                                    ChargeCode chargeCode = new ChargeCode();
                                    chargeCode.Code = chargeCodeCW;
                                    chargeLine.ChargeCode = chargeCode;

                                    OrganizationReference debtor = new OrganizationReference();
                                    debtor.Type = "Organization";
                                    debtor.Key = revenue.customer_gcc;
                                    chargeLine.Debtor = debtor;

                                    string? taxCodeCW = _context.TaxTypes.Where(t => t.BrinksCode.ToLower() == revenue.tax_code.ToLower()).FirstOrDefault()?.CWCode;
                                    TaxID taxID = new TaxID();
                                    taxID.TaxCode = taxCodeCW;
                                    chargeLine.SellGSTVATID = taxID;

                                    chargeLine.Description = revenue.description;
                                    chargeLine.SellOSAmountSpecified = true;
                                    chargeLine.SellOSGSTVATAmountSpecified = true;
                                    chargeLine.SellOSAmount = revenue.invoice_amount;
                                    chargeLine.SellOSGSTVATAmount = revenue.invoice_tax_amount;
                                    chargeLines.Add(chargeLine);

                                    JobCosting jobCosting = new JobCosting();
                                    jobCosting.ChargeLineCollection = chargeLines.ToArray();
                                    shipment.JobCosting = jobCosting;
                                    shipmentData.Shipment = shipment;
                                    string xml = Utilities.Serialize(shipmentData);
                                    //string xml2 = Utilities.Serialize(shipmentData);
                                }
                                else
                                {

                                }
                        
                            }
                            else
                            {
                                dataResponse.Status = "ERROR";
                                dataResponse.Message = "Shipment does not exist with hawb" + revenue.hawb_number;
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
                return StatusCode(StatusCodes.Status500InternalServerError, dataResponses);
            }
        }
        #endregion

        public string GetShipmentNumberByHawb(string hawb)
        {
            string? shipmentNumber = null;
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
                        Events.UniversalEventData? eventResponse = (Events.UniversalEventData)serializer.Deserialize(reader);
                        shipmentNumber = eventResponse?.Event?.DataContext?.DataSourceCollection?.Where(d => d.Type == "ForwardingShipment")?.FirstOrDefault()?.Key;
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return shipmentNumber;
        }

        public UniversalShipmentData GetShipmentById(string shipmentId)
        {
            UniversalShipmentData? response = new UniversalShipmentData();
            try
            {
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
            }

            catch (Exception ex)
            {
                throw ex;
            }
            return response;
        }
    }
}
