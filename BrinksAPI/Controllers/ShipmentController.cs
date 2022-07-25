using BrinksAPI.Auth;
using BrinksAPI.Entities;
using BrinksAPI.Helpers;
using BrinksAPI.Interfaces;
using BrinksAPI.Models;
using eAdaptor;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace BrinksAPI.Controllers
{
    [Authorize]
    //[ApiController]
    public class ShipmentController : Controller
    {
        private readonly IConfigManager _configuration;

        private readonly ApplicationDbContext _context;
        public ShipmentController(IConfigManager configuration, ApplicationDbContext applicationDbContext)
        {
            _configuration = configuration;
            _context = applicationDbContext;

        }

        #region CREATE MULTIPLE SHIMENTS
        /// <summary>
        /// Creates a Shipment.
        /// </summary>
        /// <param name="shipment"></param>
        /// <returns>A newly created Shipmnet</returns>
        /// <remarks>
        /// Sample request:
        ///
        ///     POST /api/shipments/multiple
        ///     {
        ///         "requestId": "1234567890",
        ///         "shipInfo": {
        ///             "billingType": "P",
        ///             "shipper": {
        ///                 "name": "BRINK'S GLOBAL SERVICES KOREA LTD",
        ///                 "address1": "#1122, 86 MAPO-DAERO",
        ///                 "address2": "MAPO-GU",
        ///                 "city": "SEOUL",
        ///                 "provinceCode": "",
        ///                 "postalCode": "04168",
        ///                 "countryCode": "KR",
        ///                 "phoneNumber": "469.549.6618"
        ///             },
        ///             "consignee": {
        ///                 "name": "VALE EUROPE LIMITED",
        ///                 "address1": "BASHLEY ROAD",
        ///                 "address2": "",
        ///                 "city": "LONDON",
        ///                 "provinceCode": "",
        ///                 "postalCode": "NW10 6SN",
        ///                 "countryCode": "GB",
        ///                 "contactName": "DEW",
        ///                 "phoneNumber": "469.549.6618"
        ///             },
        ///             "serviceType": "DD",
        ///             "modeOfTransportCode": "A",
        ///             "requestLabelImage": "true",
        ///             "shipmentLabelRequest": {
        ///                 "labelType": "PDF",
        ///                 "printOption": "Label"
        ///             },
        ///             "lvpOptions": null,
        ///             "shipmentItems": [
        ///                 {
        ///                     "licenseCode": "C33",
        ///                     "exportLicenseNumber": "NLR",
        ///                     "exportOrigin": "D",
        ///                     "insuranceLiabilityValue": 500.00,
        ///                     "customsCurrencyCode": "USD",
        ///                     "customsValue": 500.00,
        ///                     "packageWeightUOM": "KGS",
        ///                     "packageWeight": 2.0,
        ///                     "netWeight": 1.80,
        ///                     "netWeightUOM": "KGS",
        ///                     "commodityId": 5,
        ///                     "packageTypeCode": "BOX",
        ///                     "termCode": "CIF",
        ///                     "pickupLocation": {
        ///                         "name": "BRINK'S GLOBAL SERVICES KOREA LTD",
        ///                         "address1": "#1122, 86 MAPO-DAERO",
        ///                         "address2": "MAPO-GU",
        ///                         "city": "SEOUL",
        ///                         "provinceCode": "",
        ///                         "postalCode": "04168",
        ///                         "countryCode": "KR",
        ///                         "contactName": "DEW",
        ///                         "phoneNumber": "469.549.6618"
        ///                     },
        ///                     "deliveryLocation": {
        ///                         "name": "VALE EUROPE LIMITED",
        ///                         "address1": "BASHLEY ROAD",
        ///                         "address2": "",
        ///                         "city": "LONDON",
        ///                         "provinceCode": "",
        ///                         "postalCode": "NW10 6SN",
        ///                         "countryCode": "GB",
        ///                         "contactName": "DEW",
        ///                         "phoneNumber": "469.549.6618"
        ///                     }
        ///                 },
        ///                 {
        ///                     "licenseCode": "C33",
        ///                     "exportLicenseNumber": "NLR",
        ///                     "exportOrigin": "D",
        ///                     "insuranceLiabilityValue": 599.90,
        ///                     "customsCurrencyCode": "USD",
        ///                     "customsValue": 599.90,
        ///                     "packageWeightUOM": "KGS",
        ///                     "packageWeight": 1.0,
        ///                     "netWeight": 1.0,
        ///                     "netWeightUOM": "KGS",
        ///                     "commodityId": 241,
        ///                     "packageTypeCode": "BOX",
        ///                     "termCode": "CIF",
        ///                     "pickupLocation": {
        ///                         "name": "BRINK'S GLOBAL SERVICES KOREA LTD",
        ///                         "address1": "#1122, 86 MAPO-DAERO",
        ///                         "address2": "MAPO-GU",
        ///                         "city": "SEOUL",
        ///                         "provinceCode": "",
        ///                         "postalCode": "04168",
        ///                         "countryCode": "KR",
        ///                         "contactName": "DEW",
        ///                         "phoneNumber": "469.549.6618"
        ///                     },
        ///                     "deliveryLocation": {
        ///                         "name": "BRINKS LTD",
        ///                         "address1": "UNIT 1, RADIUS PARK",
        ///                         "address2": "FELTHAM",
        ///                         "city": "LONDON",
        ///                         "provinceCode": "",
        ///                         "postalCode": "TW14 0NG",
        ///                         "countryCode": "GB",
        ///                         "contactName": "BGS",
        ///                         "phoneNumber": "469.549.6618"
        ///                     }
        ///                 }
        ///             ]
        ///         }
        ///      }
        ///
        /// </remarks>
        /// <response code="200">Success</response>
        /// <response code="400">Data not valid</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="500">Internal server error</response>
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(500)]
        [HttpPost]
        [Route("api/shipments/multiple")]
        public IActionResult CreateMultipleShipments([FromBody]BrinksMultipleShipment brinksShipment)
        {
            string responseData = "";
            Response dataResponse = new Response();
            try
            {
                if (!ModelState.IsValid)
                {
                    //dataResponse.Status = "Validation Error";
                    //dataResponse.Message = String.Format("{0} Error found", ModelState.ErrorCount);
                    //dataResponse.Data = ModelState.ToString();
                    return BadRequest(ModelState);
                }
                UniversalShipmentData universalShipmentData = new UniversalShipmentData();
                Shipment shipment = new Shipment();

                #region Data Context
                DataContext dataContext = new DataContext();
                DataTarget dataTarget = new DataTarget();
                dataTarget.Type = "ForwardingConsol";

                List<DataTarget> dataTargets = new List<DataTarget>();
                dataTargets.Add(dataTarget);
                dataContext.DataTargetCollection = dataTargets.ToArray();

                Company company = new Company();
                company.Code = _configuration.CompanyCode;
                dataContext.Company = company;

                dataContext.DataProvider = _configuration.ServiceDataProvider;
                dataContext.EnterpriseID = _configuration.EnterpriseId;

                dataContext.ServerID = _configuration.ServerId;
                shipment.DataContext = dataContext;
                #endregion

                #region Sub Shipment Collection
                List<Shipment> subShipments = new List<Shipment>();
                foreach (var shipmentItem in brinksShipment.shipInfo.shipmentItems)
                {
                    Shipment subShipment = new Shipment();

                    DataContext subShipmentDataContext = new DataContext();
                    DataTarget subShipmentDataTarget = new DataTarget();
                    subShipmentDataTarget.Type = "ForwardingShipment";

                    List<DataTarget> subShipmentDataTargets = new List<DataTarget>();
                    subShipmentDataTargets.Add(subShipmentDataTarget);
                    subShipmentDataContext.DataTargetCollection = subShipmentDataTargets.ToArray();

                    var serviceLevelInDB = _context.serviceLevels.Where(s => s.BrinksCode == brinksShipment.shipInfo.serviceType).FirstOrDefault();
                    if (serviceLevelInDB != null)
                    {
                        ServiceLevel serviceLevel = new ServiceLevel();
                        serviceLevel.Code = serviceLevelInDB.CWCode;
                        subShipment.ServiceLevel = serviceLevel;
                    }


                    string transportModeCode = "";
                    CodeDescriptionPair transportMode = new CodeDescriptionPair();

                    switch (brinksShipment.shipInfo.modeOfTransportCode)
                    {
                        case "A":
                            transportModeCode = "AIR";
                            break;
                        case "R":
                            transportModeCode = "ROAD";
                            break;
                        case "S":
                            transportModeCode = "SEA";
                            break;
                        case "L":
                            transportModeCode = "RAIL";
                            break;
                        default:
                            transportModeCode = "AIR";
                            break;
                    }
                    transportMode.Code = transportModeCode;
                    subShipment.TransportMode = transportMode;

                    #region Customized Fields
                    List<CustomizedField> customizedFields = new List<CustomizedField>();
                    CustomizedField customizedField = new CustomizedField();
                    customizedField.Key = "requestId";
                    customizedField.Value = brinksShipment.requestId;
                    customizedFields.Add(customizedField);
                    subShipment.CustomizedFieldCollection = customizedFields.ToArray();
                    #endregion

                    //Need to clarify
                    IncoTerm shipmentIncoTerm = new IncoTerm();
                    shipmentIncoTerm.Code = brinksShipment.shipInfo.billingType == "P" ? "PPT" : "CLT";
                    subShipment.ShipmentIncoTerm = shipmentIncoTerm;

                    subShipment.InsuranceValueSpecified = true;
                    Currency insuranceCurrency = new Currency();
                    insuranceCurrency.Code = "USD";
                    subShipment.InsuranceValueCurrency = insuranceCurrency;
                    subShipment.InsuranceValue = Convert.ToDecimal(shipmentItem.insuranceLiabilityValue);

                    List<PackingLine> packingLines = new List<PackingLine>();
                    PackingLine packingLine = new PackingLine();

                    // Need Mapping
                    Commodity commodity = new Commodity();
                    commodity.Code = shipmentItem.commodityId.ToString();
                    packingLine.Commodity = commodity;

                    PackageType packageType = new PackageType();
                    packageType.Code = shipmentItem.packageTypeCode;
                    packingLine.PackType = packageType;

                    packingLines.Add(packingLine);

                    subShipment.TotalWeightSpecified = true;
                    subShipment.TotalWeight = Convert.ToDecimal(shipmentItem.packageWeight);
                    UnitOfWeight packageWeightUnit = new UnitOfWeight();
                    packageWeightUnit.Code = shipmentItem.packageWeightUOM == "KGS" ? "KG" : "LBS";
                    subShipment.TotalWeightUnit = packageWeightUnit;

                    ShipmentPackingLineCollection shipmentPackingLineCollection = new ShipmentPackingLineCollection();
                    shipmentPackingLineCollection.PackingLine = packingLines.ToArray();
                    subShipment.PackingLineCollection = shipmentPackingLineCollection;

                    #region Organization Collection
                    List<OrganizationAddress> organizationAddresses = new List<OrganizationAddress>();

                    OrganizationAddress consignorAddress = new OrganizationAddress();
                    Country consignorCountry = new Country();

                    OrganizationAddress consigneeAddress = new OrganizationAddress();
                    Country consigneeCountry = new Country();

                    OrganizationAddress pickupAddress = new OrganizationAddress();
                    Country pickupCountry = new Country();

                    OrganizationAddress deliveryAddress = new OrganizationAddress();
                    Country deliveryCountry = new Country();

                    OrganizationAddress deliveryTransportAddress = new OrganizationAddress();
                    Country deliveryTransportCountry = new Country();

                    consignorAddress.AddressType = "ConsignorDocumentaryAddress";
                    consignorAddress.CompanyName = brinksShipment.shipInfo.shipper.name;
                    consignorAddress.Address1 = brinksShipment.shipInfo.shipper.address1;
                    consignorAddress.Address2 = brinksShipment.shipInfo.shipper.address2;
                    consignorAddress.City = brinksShipment.shipInfo.shipper.city;
                    consignorAddress.AddressShortCode = brinksShipment.shipInfo.shipper.provinceCode;
                    consignorAddress.Postcode = brinksShipment.shipInfo.shipper.postalCode;
                    consignorCountry.Code = brinksShipment.shipInfo.shipper.countryCode;
                    consignorAddress.Country = consignorCountry;
                    consignorAddress.Phone = brinksShipment.shipInfo.shipper.phoneNumber;
                    organizationAddresses.Add(consignorAddress);

                    consigneeAddress.AddressType = "ConsigneeDocumentaryAddress";
                    consigneeAddress.CompanyName = brinksShipment.shipInfo.consignee.name;
                    consigneeAddress.Address1 = brinksShipment.shipInfo.consignee.address1;
                    consigneeAddress.Address2 = brinksShipment.shipInfo.consignee.address2;
                    consigneeAddress.City = brinksShipment.shipInfo.consignee.city;
                    consigneeAddress.AddressShortCode = brinksShipment.shipInfo.consignee.provinceCode;
                    consigneeAddress.Postcode = brinksShipment.shipInfo.consignee.postalCode;
                    consigneeCountry.Code = brinksShipment.shipInfo.consignee.countryCode;
                    consigneeAddress.Country = consigneeCountry;
                    consigneeAddress.Phone = brinksShipment.shipInfo.consignee.phoneNumber;
                    organizationAddresses.Add(consigneeAddress);

                    pickupAddress.AddressType = "ConsignorPickupDeliveryAddress";
                    pickupAddress.CompanyName = shipmentItem.pickupLocation.name;
                    pickupAddress.Address1 = shipmentItem.pickupLocation.address1;
                    pickupAddress.Address2 = shipmentItem.pickupLocation.address2;
                    pickupAddress.City = shipmentItem.pickupLocation.city;
                    pickupAddress.AddressShortCode = shipmentItem.pickupLocation.provinceCode;
                    pickupAddress.Postcode = shipmentItem.pickupLocation.postalCode;
                    pickupCountry.Code = shipmentItem.pickupLocation.countryCode;
                    pickupAddress.Country = pickupCountry;
                    pickupAddress.Phone = shipmentItem.pickupLocation.phoneNumber;
                    organizationAddresses.Add(pickupAddress);

                    deliveryAddress.AddressType = "ConsigneePickupDeliveryAddress";
                    deliveryAddress.CompanyName = shipmentItem.deliveryLocation.name;
                    deliveryAddress.Address1 = shipmentItem.deliveryLocation.address1;
                    deliveryAddress.Address2 = shipmentItem.deliveryLocation.address2;
                    deliveryAddress.City = shipmentItem.deliveryLocation.city;
                    deliveryAddress.AddressShortCode = shipmentItem.deliveryLocation.provinceCode;
                    deliveryAddress.Postcode = shipmentItem.deliveryLocation.postalCode;
                    deliveryCountry.Code = shipmentItem.deliveryLocation.countryCode;
                    deliveryAddress.Country = deliveryCountry;
                    deliveryAddress.Phone = shipmentItem.deliveryLocation.phoneNumber;
                    organizationAddresses.Add(deliveryAddress);

                    deliveryTransportAddress.AddressType = "DeliveryLocalCartage";
                    deliveryTransportAddress.CompanyName = brinksShipment.shipInfo.consignee.name;
                    deliveryTransportAddress.Address1 = brinksShipment.shipInfo.consignee.address1;
                    deliveryTransportAddress.Address2 = brinksShipment.shipInfo.consignee.address2;
                    deliveryTransportAddress.City = brinksShipment.shipInfo.consignee.city;
                    deliveryTransportAddress.AddressShortCode = brinksShipment.shipInfo.consignee.provinceCode;
                    deliveryTransportAddress.Postcode = brinksShipment.shipInfo.consignee.postalCode;
                    deliveryTransportCountry.Code = brinksShipment.shipInfo.consignee.countryCode;
                    deliveryTransportAddress.Country = deliveryTransportCountry;
                    deliveryTransportAddress.Phone = brinksShipment.shipInfo.consignee.phoneNumber;
                    organizationAddresses.Add(deliveryTransportAddress);


                    subShipment.OrganizationAddressCollection = organizationAddresses.ToArray();
                    #endregion

                    subShipments.Add(subShipment);


                }
                shipment.SubShipmentCollection = subShipments.ToArray();
                #endregion

                universalShipmentData.Shipment = shipment;

                string xml = Utilities.Serialize(universalShipmentData);
                var documentResponse = eAdaptor.Services.SendToCargowise(xml, _configuration.URI, _configuration.Username, _configuration.Password);
                dataResponse.Status = "SUCCESS";
                dataResponse.Message = "Successfully created the shipment.";
                //dataResponse.Data = documentResponse.Data.Data.OuterXml;

            }
            catch (Exception ex)
            {
                dataResponse.Status = "Internal Error";
                dataResponse.Message = ex.Message;
                return BadRequest(ex.Message);
            }

            return Ok(dataResponse);
        }
        #endregion

        #region SHIPMENT HISTORY
        /// <summary>
        /// Creates Shipment History.
        /// </summary>
        /// <param name="shipmentHistory"></param>
        /// <returns>A newly created Shipment History</returns>
        /// <remarks>
        /// Sample request:
        ///
        ///     POST /api/shipment/history
        ///     [{
        ///         "requestId": "12345678",
        ///         "trackingNumber": "1234",
        ///         "userId": "HJ",
        ///         "actionType": "PICK",
        ///         "areaType": "HJ",
        ///         "historyDetails": "This is a test event details new for pickup",
        ///         "historyDate": "2022-05-21 10:00:00",
        ///         "siteCode": "3210",
        ///         "serverId": "42",
        ///         "hawbNumber": "HAWB1234"
        ///     },
        ///     {
        ///         "requestId": "12345678",
        ///         "trackingNumber": "1234",
        ///         "userId": "HJ",
        ///         "actionType": "DLVD",
        ///         "areaType": "HJ",
        ///         "historyDetails": "This is a test event details new for delivery",
        ///         "historyDate": "2022-05-21 10:00:00",
        ///         "siteCode": "3210",
        ///         "serverId": "TRN",
        ///         "hawbNumber": "HAWB1234"
        ///     }]
        /// </remarks>
        /// <response code="200">Success</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="500">Internal server error</response>
        [HttpPost]
        [ProducesResponseType(200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(500)]
        [Route("api/shipment/history")]
        public ActionResult<List<ShipemtHistoryResponse>> UpdateShipmentHistory([FromBody] Models.Shipment.History[] histories)
        {
            List<ShipemtHistoryResponse> dataResponses = new List<ShipemtHistoryResponse>();

            try
            {
                
                foreach (Models.Shipment.History history in histories)
                {
                    ShipemtHistoryResponse dataResponse = new ShipemtHistoryResponse();
                    dataResponse.RequestId = history?.RequestId;
                    try
                    {
                        var validationResults = new List<ValidationResult>();
                        var validationContext = new ValidationContext(history);
                        var isValid = Validator.TryValidateObject(history, validationContext, validationResults);

                        if (isValid)
                        {
                            var site = _context.sites.Where(s => s.ServerID == Int32.Parse(history.ServerId)).FirstOrDefault();
                            if (site != null)
                            {
                                Events.UniversalEventData universalEvent = new Events.UniversalEventData();

                                string actionType = "";
                                string eventType = "";
                                if (history.ActionType != null)
                                {
                                    var actionTypeObj = _context.actionTypes.Where(a => a.BrinksCode == history.ActionType).FirstOrDefault();
                                    actionType = actionTypeObj?.CWCode;
                                    eventType = actionTypeObj == null ? "Z00" : actionTypeObj.EventType;
                                }
                                else
                                {
                                    actionType = "Picked up date";
                                    eventType = "Z00";
                                }

                                #region DataContext
                                Events.Event @event = new Events.Event();
                                Events.DataContext dataContext = new Events.DataContext();

                                List<Events.DataTarget> dataTargets = new List<Events.DataTarget>();
                                Events.DataTarget dataTarget = new Events.DataTarget();
                                dataTarget.Type = "ForwardingShipment";
                                dataTargets.Add(dataTarget);
                                dataContext.DataTargetCollection = dataTargets.ToArray();

                                Events.Company company = new Events.Company();
                                company.Code = _configuration.CompanyCode;
                                Events.Country country = new Events.Country();
                                country.Code = site.Country;
                                company.Country = country;
                                dataContext.Company = company;

                                dataContext.EnterpriseID = _configuration.EnterpriseId;
                                dataContext.ServerID = _configuration.ServerId;
                                Events.Staff staff = new Events.Staff();
                                staff.Code = history.UserId;
                                dataContext.EventUser = staff;
                                @event.DataContext = dataContext;
                                #endregion

                                #region Event
                                @event.EventTime = history.HistoryDate;
                                @event.EventType = eventType;
                                @event.EventReference = history.HistoryDetails;
                                #endregion

                                #region Contexts
                                List<Events.Context> contexts = new List<Events.Context>();
                                Events.Context context = new Events.Context();
                                Events.ContextType type = new Events.ContextType();
                                type.Value = "HAWBNumber";
                                context.Type = type;
                                context.Value = history.HawbNumber;
                                contexts.Add(context);
                                @event.ContextCollection = contexts.ToArray();
                                #endregion

                                universalEvent.Event = @event;

                                string xml = Utilities.Serialize(universalEvent);
                                var documentResponse = eAdaptor.Services.SendToCargowise(xml, _configuration.URI, _configuration.Username, _configuration.Password);

                                if (documentResponse.Status == "SUCCESS")
                                {
                                    using (var reader = new StringReader(documentResponse.Data.Data.OuterXml))
                                    {
                                        var serializer = new XmlSerializer(typeof(Events.UniversalEventData));
                                        Events.UniversalEventData responseEvent = (Events.UniversalEventData)serializer.Deserialize(reader);

                                        bool isError = responseEvent.Event.ContextCollection.Any(c => c.Type.Value.Contains("FailureReason"));
                                        if (isError)
                                        {
                                            string errorMessage = responseEvent.Event.ContextCollection
                                                .Where(c => c.Type.Value == "FailureReason")
                                                .FirstOrDefault().Value
                                                .Replace("Error - ", "")
                                                .Replace("Warning - ", "");
                                            dataResponse.Status = "ERROR";
                                            if (errorMessage == "No Module found a Business Entity to link this Universal Event to.")
                                                dataResponse.Message = String.Format("{0} - Hawb does not exist", history.HawbNumber);
                                            else
                                                dataResponse.Message = errorMessage;
                                        }
                                        else
                                        {
                                            string message = "Shipment history created.";
                                            string shipmentId = responseEvent.Event.DataContext.DataSourceCollection.Where(d => d.Type == "ForwardingShipment").FirstOrDefault().Key;
                                            if (history.TrackingNumber != null && shipmentId != null)
                                            {
                                                UniversalShipmentData universalShipmentData = GetShipmentById(dataContext, shipmentId);
                                                if (universalShipmentData is not null)
                                                {
                                                    if (universalShipmentData.Shipment.SubShipmentCollection is not null)
                                                    {
                                                        var packingLineObject = universalShipmentData.Shipment
                                                                    .SubShipmentCollection
                                                                    .Where(sub => sub.PackingLineCollection.PackingLine
                                                                    .All(pkg => pkg.ReferenceNumber == history.TrackingNumber))
                                                                    .FirstOrDefault();
                                                        if (packingLineObject is null)
                                                            message += "Tracking Number " + history.TrackingNumber + " can't find. Unable to set the " + actionType + " value.";
                                                        else
                                                        {
                                                            if (universalShipmentData.Shipment.SubShipmentCollection[0].SubShipmentCollection is not null)
                                                            {
                                                                for (int i = 0; i < universalShipmentData.Shipment.SubShipmentCollection[0].SubShipmentCollection.Length; i++)
                                                                {
                                                                    for (int j = 0; j < universalShipmentData.Shipment.SubShipmentCollection[0].SubShipmentCollection[i].PackingLineCollection.PackingLine.Length; j++)
                                                                    {
                                                                        if (universalShipmentData.Shipment.SubShipmentCollection[0].SubShipmentCollection[i].PackingLineCollection.PackingLine[j].ReferenceNumber == history.TrackingNumber)
                                                                        {
                                                                            for (int k = 0; k < universalShipmentData.Shipment.SubShipmentCollection[0].SubShipmentCollection[i].PackingLineCollection.PackingLine[j].CustomizedFieldCollection.Length; k++)
                                                                            {
                                                                                if (universalShipmentData.Shipment.SubShipmentCollection[0].SubShipmentCollection[i].PackingLineCollection.PackingLine[j].CustomizedFieldCollection[k].Key == actionType)
                                                                                {
                                                                                    universalShipmentData.Shipment.SubShipmentCollection[0].SubShipmentCollection[i].PackingLineCollection.PackingLine[j].CustomizedFieldCollection[k].Value = history.HistoryDate;
                                                                                }
                                                                            }
                                                                        }
                                                                    }
                                                                }
                                                            }
                                                        }

                                                    }
                                                    else
                                                    {
                                                        var packingLineObject = universalShipmentData.Shipment
                                                        .PackingLineCollection.PackingLine
                                                        .Where(p => p.ReferenceNumber == history.TrackingNumber)
                                                        .FirstOrDefault();
                                                        if (packingLineObject is null)
                                                            message += "Tracking Number " + history.TrackingNumber + " can't find. Unable to set the " + actionType + " value.";
                                                        else
                                                        {
                                                            packingLineObject
                                                                .CustomizedFieldCollection
                                                                .Where(c => c.Key == actionType)
                                                                .FirstOrDefault()
                                                                .Value = history.HistoryDate;
                                                        }

                                                    }

                                                    string universalShipmentDataXml = Utilities.Serialize(universalShipmentData);
                                                    universalShipmentDataXml = universalShipmentDataXml.Replace("DataSource", "DataTarget");
                                                    var universalShipmentDataResponse = eAdaptor.Services.SendToCargowise(universalShipmentDataXml, _configuration.URI, _configuration.Username, _configuration.Password);
                                                    if (universalShipmentDataResponse.Status != "SUCCESS")
                                                        message += "Unable to update the packing line with the traking number " + history.TrackingNumber;
                                                }
                                                //else
                                                //    message = "No matching tracking number("+ history.trackingNumber + ") found.";

                                            }
                                            dataResponse.Status = "SUCCESS";
                                            dataResponse.Message = message;
                                        }
                                    }
                                }
                                else
                                {
                                    dataResponse.Status = documentResponse.Status;
                                    dataResponse.Message = documentResponse.Data.Data.FirstChild.InnerText.Replace("Error - ", "").Replace("Warning - ", "");

                                }
                            }
                            else
                            {
                                dataResponse.Status = "ERROR";
                                dataResponse.Message = "Server ID " + history.ServerId + " is not found in mapping DB.";
                            }
                        }
                        else
                        {
                            string validationMessage = "";
                            dataResponse.Status = "ERROR";
                            foreach (var validationResult in validationResults)
                            {
                                validationMessage += validationResult.ErrorMessage;
                            }
                            dataResponse.Message = validationMessage;
                        }
                        dataResponses.Add(dataResponse);
                    }
                    catch(Exception ex)
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
                ShipemtHistoryResponse dataResponse = new ShipemtHistoryResponse();
                dataResponse.Status = "ERROR";
                dataResponse.Message = ex.Message;
                dataResponses.Add(dataResponse);
                return StatusCode(StatusCodes.Status500InternalServerError, dataResponses);
            }
        } 
        #endregion
        public UniversalShipmentData GetShipmentById(Events.DataContext dataContext,string shipmentId)
        {
            UniversalShipmentData dataResponse = new UniversalShipmentData();
            try
            {
                ShipmentRequest.UniversalShipmentRequestData dataRequest = new ShipmentRequest.UniversalShipmentRequestData();
                ShipmentRequest.ShipmentRequest shipmentRequest = new ShipmentRequest.ShipmentRequest();
                ShipmentRequest.DataContext requestDataContext = new ShipmentRequest.DataContext();
                List<ShipmentRequest.DataTarget> dataTargets = new List<ShipmentRequest.DataTarget>();
                ShipmentRequest.DataTarget dataTarget = new ShipmentRequest.DataTarget();

                dataTarget.Type = dataContext.DataTargetCollection[0].Type;
                dataTarget.Key = shipmentId;
                dataTargets.Add(dataTarget);
                requestDataContext.DataTargetCollection = dataTargets.ToArray();
                ShipmentRequest.Company company = new ShipmentRequest.Company();
                company.Code = dataContext.Company.Code;
                requestDataContext.Company = company;
                requestDataContext.EnterpriseID = dataContext.EnterpriseID;
                requestDataContext.ServerID = dataContext.ServerID;
                shipmentRequest.DataContext = requestDataContext;
                dataRequest.ShipmentRequest = shipmentRequest;

                string xml = Utilities.Serialize(dataRequest);
                var shipmentRequestResponse = eAdaptor.Services.SendToCargowise(xml, _configuration.URI, _configuration.Username, _configuration.Password);
                if (shipmentRequestResponse.Status == "SUCCESS")
                {
                    using (var reader = new StringReader(shipmentRequestResponse.Data.Data.OuterXml))
                    {
                        var serializer = new XmlSerializer(typeof(UniversalShipmentData));
                        dataResponse = (UniversalShipmentData)serializer.Deserialize(reader);
                    }
                }

                return dataResponse;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
