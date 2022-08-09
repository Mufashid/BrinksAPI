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
        /***
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
       ***/
        #endregion


        #region UPSERT SHIPMENT
        [HttpPost]
        [ProducesResponseType(200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(500)]
        [Route("api/shipment")]
        public ActionResult<ShipemtResponse> UpsertShipment([FromBody] Models.Shipment shipment)
        {
            ShipemtResponse dataResponse = new ShipemtResponse();
            try
            {
                dataResponse.HawbNum = shipment?.hawbNum;

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
                    dataResponse.Status = "ERROR";
                    dataResponse.Message = errorString;

                    return Ok(dataResponse);
                }
                #endregion

                int serverId = Convert.ToInt32(shipment.originServerId);
                var site = _context.sites.Where(s => s.ServerID == serverId).FirstOrDefault();
                if (site == null)
                {
                    dataResponse.Status = "ERROR";
                    dataResponse.Message = String.Format("Server ID '{0}' was not found in the database.",serverId.ToString());
                    return Ok(dataResponse);
                }
                UniversalShipmentData universalShipmentData = new UniversalShipmentData();
                Shipment cwShipment = new Shipment();

                #region Data Context
                DataContext dataContext = new DataContext();
                DataTarget dataTarget = new DataTarget();
                dataTarget.Type = "ForwardingShipment";

                List<DataTarget> dataTargets = new List<DataTarget>();
                dataTargets.Add(dataTarget);
                dataContext.DataTargetCollection = dataTargets.ToArray();

                Company company = new Company();
                company.Code = site.CompanyCode;
                dataContext.Company = company;

                dataContext.DataProvider = _configuration.ServiceDataProvider;
                dataContext.EnterpriseID = _configuration.EnterpriseId;

                dataContext.ServerID = _configuration.ServerId;

                cwShipment.DataContext = dataContext;
                #endregion
                
                string shipmentId = GetShipmentNumberByHawb(dataContext, shipment.hawbNum);
                dataContext.DataTargetCollection[0].Key = shipmentId;

                //cwShipment.BookingConfirmationReference = shipment.shippersReference;

                ServiceLevel serviceLevel = new ServiceLevel();
                serviceLevel.Code = shipment.serviceType;
                cwShipment.ServiceLevel = serviceLevel;

                //Mapping
                CodeDescriptionPair transportMode = new CodeDescriptionPair();
                transportMode.Code = shipment.modeOfTransport;
                cwShipment.TransportMode = transportMode;

                //Mapping
                CodeDescriptionPair paymentMethod = new CodeDescriptionPair();
                paymentMethod.Code = shipment.chargesType; 
                cwShipment.PaymentMethod = paymentMethod;
                
                #region PORT
                var loadingPort = _context.sites.Where(s => s.Airport == shipment.pickupAirportCode).FirstOrDefault();
                UNLOCO portOfLoading = new UNLOCO();
                portOfLoading.Code = loadingPort?.Country + loadingPort?.Airport;
                cwShipment.PortOfLoading = portOfLoading;

                var dischargePort = _context.sites.Where(s => s.Airport == shipment.deliveryAirportCode).FirstOrDefault();
                UNLOCO portOfDischarge = new UNLOCO();
                portOfDischarge.Code = dischargePort?.Country + dischargePort?.Airport;
                cwShipment.PortOfDischarge = portOfDischarge;
                #endregion

                #region NOTES
                ShipmentNoteCollection shipmentNoteCollection = new ShipmentNoteCollection();
                List<Note> notes = new List<Note>();

                Note note = new Note();
                note.Description = "Special Instructions";
                note.NoteText = shipment.notes;
                notes.Add(note);

                Note pickUpNote = new Note();
                note.Description = "Pickup Note";
                note.NoteText = shipment.puNotes;
                notes.Add(pickUpNote);

                Note deliveryNote = new Note();
                note.Description = "Devlivery Note";
                note.NoteText = shipment.dlvNotes;
                notes.Add(deliveryNote);

                shipmentNoteCollection.Note = notes.ToArray();
                cwShipment.NoteCollection = shipmentNoteCollection;
                #endregion

                #region ORGANIZATION ADDRESS
                List<OrganizationAddress> organizationAddresses = new List<OrganizationAddress>();

                OrganizationAddress shipperAddress = new OrganizationAddress();
                shipperAddress.AddressType = "ConsignorDocumentaryAddress";
                shipperAddress.CompanyName = shipment.shipperName;
                shipperAddress.Address1 = shipment.shipperAddress1 + "," + shipment.shipperAddress2;
                shipperAddress.Address2 = shipment.shipperAddress3 + "," + shipment.shipperAddress4;
                shipperAddress.City = shipment.shipperCity;
                OrganizationAddressState shipperState = new OrganizationAddressState();
                shipperState.Value = shipment.shipperProvinceCode;
                shipperAddress.State = shipperState;
                Country shipperCountry = new Country();
                shipperCountry.Code = shipment.shipperCountryCode;
                shipperAddress.Country = shipperCountry;
                shipperAddress.Postcode = shipment.shipperPostalCode;
                shipperAddress.Contact = shipment.shipperContactName;
                shipperAddress.Phone = shipment.shipperPhoneNumber;
                shipperAddress.Mobile = shipment.shipperMobileNumber;
                List<RegistrationNumber> shipperRegistrationNumbers = new List<RegistrationNumber>();
                RegistrationNumber shipperRegistrationNumber = new RegistrationNumber();
                RegistrationNumberType shipperRegistrationNumberType = new RegistrationNumberType();
                shipperRegistrationNumberType.Code = "LSC";
                shipperRegistrationNumber.Type = shipperRegistrationNumberType;
                shipperRegistrationNumber.Value = shipment.shipperGlobalCustomerCode;
                shipperRegistrationNumbers.Add(shipperRegistrationNumber);
                shipperAddress.RegistrationNumberCollection = shipperRegistrationNumbers.ToArray();
                organizationAddresses.Add(shipperAddress);

                OrganizationAddress consigneeAddress = new OrganizationAddress();
                consigneeAddress.AddressType = "ConsigneeDocumentaryAddress";
                consigneeAddress.CompanyName = shipment.consigneeName;
                consigneeAddress.Address1 = shipment.consigneeAddress1 + "," + shipment.consigneeAddress2;
                consigneeAddress.Address2 = shipment.consigneeAddress3 + "," + shipment.consigneeAddress4;
                consigneeAddress.City = shipment.consigneeCity;
                OrganizationAddressState consigneeState = new OrganizationAddressState();
                consigneeState.Value = shipment.consigneeProvinceCode;
                consigneeAddress.State = consigneeState;
                Country consigneeCountry = new Country();
                consigneeCountry.Code = shipment.consigneeCountryCode;
                consigneeAddress.Country = consigneeCountry;
                consigneeAddress.Postcode = shipment.consigneePostalCode;
                consigneeAddress.Contact = shipment.consigneeContactName;
                consigneeAddress.Phone = shipment.consigneePhoneNumber;
                consigneeAddress.Mobile = shipment.consigneeMobileNumber;
                List<RegistrationNumber> consigneeRegistrationNumbers = new List<RegistrationNumber>();
                RegistrationNumber consigneeRegistrationNumber = new RegistrationNumber();
                RegistrationNumberType consigneeRegistrationNumberType = new RegistrationNumberType();
                consigneeRegistrationNumberType.Code = "LSC";
                consigneeRegistrationNumber.Type = consigneeRegistrationNumberType;
                consigneeRegistrationNumber.Value = shipment.consigneeGlobalCustomerCode;
                consigneeRegistrationNumbers.Add(consigneeRegistrationNumber);
                consigneeAddress.RegistrationNumberCollection = consigneeRegistrationNumbers.ToArray();
                organizationAddresses.Add(consigneeAddress);

                cwShipment.OrganizationAddressCollection = organizationAddresses.ToArray();
                #endregion
                
                ShipmentPackingLineCollection shipmentPackingLineCollection = new ShipmentPackingLineCollection();
                List<PackingLine> packings = new List<PackingLine>();
                foreach(var shipmentItem in shipment.shipmentItems)
                {
                    PackingLine packingLine = new PackingLine();

                    Commodity commodity = new Commodity();
                    commodity.Code = shipmentItem.globalCommodityCode;
                    packingLine.Commodity = commodity;

                    packingLine.GoodsDescription = shipmentItem.commodityDescription;
                    
                    PackageType packageType = new PackageType();
                    packageType.Code = shipmentItem.packageTypeCd;
                    packingLine.PackType = packageType;

                    packingLine.LengthSpecified = true;
                    packingLine.WeightSpecified = true;
                    packingLine.WidthSpecified = true;
                    packingLine.HeightSpecified = true;
                    packingLine.PackQtySpecified = true;
                    packingLine.PackQtySpecified = true;

                    packingLine.Length = Convert.ToDecimal(shipmentItem.dimLength);
                    packingLine.Weight = Convert.ToDecimal(shipmentItem.dimWeight);
                    packingLine.Width = Convert.ToDecimal(shipmentItem.dimWidth);
                    packingLine.Height = Convert.ToDecimal(shipmentItem.dimLength);

                    packingLine.Barcode = shipmentItem.barcode;
                    packingLine.PackQty = Convert.ToInt64(shipmentItem.numberOfItems);

                    Country countryOforigin = new Country();
                    countryOforigin.Code = shipmentItem.originCountry;
                    packingLine.CountryOfOrigin = countryOforigin;

                    UnitOfLength unitOfLength = new UnitOfLength();
                    unitOfLength.Code = shipmentItem.dimUOM;
                    packingLine.LengthUnit = unitOfLength;

                    UnitOfWeight unitOfWeight = new UnitOfWeight();
                    unitOfWeight.Code = shipmentItem.dimUOM;
                    packingLine.WeightUnit = unitOfWeight;

                    
                    #region CUSTOMIZED FIELDS COLLECTION
                    List<CustomizedField> customizedFields = new List<CustomizedField>();

                    CustomizedField pickupDateCF = new CustomizedField();
                    pickupDateCF.DataType = CustomizedFieldDataType.DateTime;
                    pickupDateCF.Key = "Picked up date";
                    pickupDateCF.Value = shipmentItem.puDate;
                    customizedFields.Add(pickupDateCF);

                    CustomizedField deliveryDateCF = new CustomizedField();
                    deliveryDateCF.DataType = CustomizedFieldDataType.DateTime;
                    deliveryDateCF.Key = "Delivery Date";
                    deliveryDateCF.Value = shipmentItem.dlvDate;
                    customizedFields.Add(deliveryDateCF);

                    packingLine.CustomizedFieldCollection = customizedFields.ToArray(); 
                    #endregion

                    packings.Add(packingLine);
                }

                shipmentPackingLineCollection.PackingLine = packings.ToArray();
                cwShipment.PackingLineCollection = shipmentPackingLineCollection;

                string successMessage = shipmentId == null ? "Shipment Created" : "Shipment Updated";
                string xml = Utilities.Serialize(universalShipmentData);
                var documentResponse = eAdaptor.Services.SendToCargowise(xml, _configuration.URI, _configuration.Username, _configuration.Password);
                if (documentResponse.Status == "ERROR")
                {
                    dataResponse.Status = documentResponse.Status;
                    dataResponse.Message = documentResponse.Data.Data.FirstChild.InnerText.Replace("Error - ", "").Replace("Warning - ", "");
                    return Ok(dataResponse);
                }

                dataResponse.Status = "SUCCESS";
                dataResponse.Message = successMessage;
                return Ok(dataResponse);

            }
            catch (Exception ex)
            {
                dataResponse.Status = "ERROR";
                dataResponse.Message = ex.Message;
                return StatusCode(StatusCodes.Status500InternalServerError, dataResponse);
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
        public ActionResult<List<ShipemtHistoryResponse>> UpdateShipmentHistory([FromBody] ShipmentHistory[] histories)
        {
            List<ShipemtHistoryResponse> dataResponses = new List<ShipemtHistoryResponse>();

            try
            {
                
                foreach (var history in histories)
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
                            int serverId = Int32.Parse(history.ServerId);
                            Site? site = _context.sites.Where(s => s.ServerID == serverId).FirstOrDefault();
                            if (site != null)
                            {
                                Events.UniversalEventData universalEvent = new Events.UniversalEventData();

                                string? actionType = "";
                                string? eventType = "";
                                string? companyCode = "";
                                if (history.ActionType != null)
                                {
                                    var actionTypeObj = _context.actionTypes.Where(a => a.BrinksCode == history.ActionType).FirstOrDefault();
                                    actionType = actionTypeObj?.CWCode;
                                    eventType = actionTypeObj == null ? "Z00" : actionTypeObj.EventType;
                                    companyCode = site.CompanyCode;
                                }
                                else
                                {
                                    actionType = "Picked up date";
                                    eventType = "Z00";
                                }
                                if(history.SiteCode != null)
                                    companyCode = history.SiteCode;

                                #region DataContext
                                Events.Event @event = new Events.Event();
                                Events.DataContext dataContext = new Events.DataContext();

                                List<Events.DataTarget> dataTargets = new List<Events.DataTarget>();
                                Events.DataTarget dataTarget = new Events.DataTarget();
                                dataTarget.Type = "ForwardingShipment";
                                dataTargets.Add(dataTarget);
                                dataContext.DataTargetCollection = dataTargets.ToArray();

                                Events.Company company = new Events.Company();
                                company.Code = companyCode;
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

        public string GetShipmentNumberByHawb(DataContext dataContext, string hawb)
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

                eventDataContext.EnterpriseID = dataContext.EnterpriseID;
                eventDataContext.ServerID = dataContext.ServerID;

                Events.Company company = new Events.Company();
                company.Code = dataContext.Company.Code;
                eventDataContext.Company = company;
                @event.DataContext = eventDataContext; 
                #endregion

                @event.EventTime = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss");
                @event.EventType = "Z00";

                #region CONTEXT COLLECTION
                List<Events.Context> contexts = new List<Events.Context>();
                Events.Context context = new Events.Context();
                Events.ContextType type = new Events.ContextType();
                type.Value = "HAWBNumber";
                context.Type = type;
                context.Value =hawb;
                contexts.Add(context);
                @event.ContextCollection = contexts.ToArray();
                #endregion

                universalEvent.Event = @event;

                string xml = Utilities.Serialize(universalEvent);
                var shipmentRequestResponse = eAdaptor.Services.SendToCargowise(xml, _configuration.URI, _configuration.Username, _configuration.Password);
                if (shipmentRequestResponse.Status == "SUCCESS")
                {
                    using (var reader = new StringReader(shipmentRequestResponse.Data.Data.OuterXml))
                    {
                        var serializer = new XmlSerializer(typeof(Events.UniversalEventData));
                        Events.UniversalEventData eventResponse = (Events.UniversalEventData)serializer.Deserialize(reader);
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
    }
}
