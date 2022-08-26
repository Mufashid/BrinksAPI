using BrinksAPI.Auth;
using BrinksAPI.Helpers;
using BrinksAPI.Interfaces;
using BrinksAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BrinksAPI.Controllers
{
    [Authorize]
    public class BookingController : Controller
    {
        private readonly IConfigManager Configuration;
        private readonly ApplicationDbContext _context;
        public BookingController(IConfigManager _configuration, ApplicationDbContext applicationDbContext)
        {
            Configuration = _configuration;
            _context = applicationDbContext;
        }

        #region Create Forward Booking Using Single-Location Json
        [HttpPost]
        [Route("api/booking/create")]
        public IActionResult CreateBooking([FromBody]BrinksSingleShipment.Root brinksShipment)
        {
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
                dataTarget.Type = "ForwardingBooking";
                dataTarget.Key = "";

                List<DataTarget> dataTargets = new List<DataTarget>();
                dataTargets.Add(dataTarget);
                dataContext.DataTargetCollection = dataTargets.ToArray();

                Company company = new Company();
                company.Code = Configuration.CompanyCode;
                dataContext.Company = company;

                dataContext.DataProvider = Configuration.ServiceDataProvider;
                dataContext.EnterpriseID = Configuration.EnterpriseId;

                dataContext.ServerID = Configuration.ServerId;
                shipment.DataContext = dataContext;
                #endregion

                #region Customized Fields
                List<CustomizedField> customizedFields = new List<CustomizedField>();
                CustomizedField customizedField = new CustomizedField();
                customizedField.Key = "requestId";
                customizedField.Value = brinksShipment.requestId;
                customizedFields.Add(customizedField);
                shipment.CustomizedFieldCollection = customizedFields.ToArray();
                #endregion

                //Need to clarify
                IncoTerm shipmentIncoTerm = new IncoTerm();
                shipmentIncoTerm.Code = brinksShipment.shipInfo.billingType == "P" ? "PPT" : "CLT";
                shipment.ShipmentIncoTerm = shipmentIncoTerm;

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
                pickupAddress.CompanyName = brinksShipment.shipInfo.pickupLocation.name;
                pickupAddress.Address1 = brinksShipment.shipInfo.pickupLocation.address1;
                pickupAddress.Address2 = brinksShipment.shipInfo.pickupLocation.address2;
                pickupAddress.City = brinksShipment.shipInfo.pickupLocation.city;
                pickupAddress.AddressShortCode = brinksShipment.shipInfo.pickupLocation.provinceCode;
                pickupAddress.Postcode = brinksShipment.shipInfo.pickupLocation.postalCode;
                pickupCountry.Code = brinksShipment.shipInfo.pickupLocation.countryCode;
                pickupAddress.Country = pickupCountry;
                pickupAddress.Phone = brinksShipment.shipInfo.pickupLocation.phoneNumber;
                organizationAddresses.Add(pickupAddress);

                deliveryAddress.AddressType = "ConsigneePickupDeliveryAddress";
                deliveryAddress.CompanyName = brinksShipment.shipInfo.deliveryLocation.name;
                deliveryAddress.Address1 = brinksShipment.shipInfo.pickupLocation.address1;
                deliveryAddress.Address2 = brinksShipment.shipInfo.pickupLocation.address2;
                deliveryAddress.City = brinksShipment.shipInfo.pickupLocation.city;
                deliveryAddress.AddressShortCode = brinksShipment.shipInfo.pickupLocation.provinceCode;
                deliveryAddress.Postcode = brinksShipment.shipInfo.pickupLocation.postalCode;
                deliveryCountry.Code = brinksShipment.shipInfo.pickupLocation.countryCode;
                deliveryAddress.Country = deliveryCountry;
                deliveryAddress.Phone = brinksShipment.shipInfo.pickupLocation.phoneNumber;
                organizationAddresses.Add(deliveryAddress);

                shipment.OrganizationAddressCollection = organizationAddresses.ToArray();
                #endregion


                var serviceLevelInDB = _context.serviceLevels.Where(s => s.BrinksCode == brinksShipment.shipInfo.serviceType).FirstOrDefault();
                if (serviceLevelInDB != null)
                {
                    ServiceLevel serviceLevel = new ServiceLevel();
                    serviceLevel.Code = serviceLevelInDB.CWCode;
                    shipment.ServiceLevel = serviceLevel;
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
                shipment.TransportMode = transportMode;


                #region Dates
                //List<Date> dates = new List<Date>();

                //Date pickupDateTime = new Date();
                //pickupDateTime.IsEstimate = false;
                //pickupDateTime.Type = DateType.Departure;
                //pickupDateTime.Value = brinksShipment.shipInfo.pickupDate + "T" + brinksShipment.shipInfo.pickupTime;
                //dates.Add(pickupDateTime);

                //Date deliveryDateTime = new Date();
                //deliveryDateTime.IsEstimate = false;
                //deliveryDateTime.Type = DateType.Arrival;
                //deliveryDateTime.Value = brinksShipment.shipInfo.deliveryDate + "T" + brinksShipment.shipInfo.deliveryTime;
                //dates.Add(deliveryDateTime);

                //shipment.DateCollection = dates.ToArray(); 
                #endregion

                ShipmentLocalProcessing shipmentLocalProcessing = new ShipmentLocalProcessing();
                shipmentLocalProcessing.PickupRequiredBy = brinksShipment.shipInfo.pickupDate + "T" + brinksShipment.shipInfo.pickupTime;
                shipmentLocalProcessing.DeliveryRequiredBy = brinksShipment.shipInfo.deliveryDate + "T" + brinksShipment.shipInfo.deliveryTime;
                shipment.LocalProcessing = shipmentLocalProcessing;

                shipment.GoodsDescription = brinksShipment.shipInfo.reference;

                List<Note> notes = new List<Note>();
                Note shipmentNote = new Note();
                shipmentNote.Description = "Shipment Notes";
                shipmentNote.IsCustomDescription = false;
                shipmentNote.NoteText = brinksShipment.shipInfo.shipmentNotes;
                notes.Add(shipmentNote);

                Note pickupNote = new Note();
                pickupNote.Description = "Pickup Notes";
                pickupNote.IsCustomDescription = false;
                pickupNote.NoteText = brinksShipment.shipInfo.pickupNotes;
                notes.Add(pickupNote);

                Note deliveryNote = new Note();
                deliveryNote.Description = "Delivery Notes";
                deliveryNote.IsCustomDescription = false;
                deliveryNote.NoteText = brinksShipment.shipInfo.deliveryNotes;
                notes.Add(deliveryNote);

                ShipmentNoteCollection shipmentNoteCollection = new ShipmentNoteCollection();
                shipmentNoteCollection.Note = notes.ToArray();
                shipment.NoteCollection = shipmentNoteCollection;

                int goodsOuters = 0;
                decimal goodsWeight = 0, goodsVolume = 0, insuranceValue = 0;
                List<PackingLine> packingLines = new List<PackingLine>();

                foreach (var shipmentItem in brinksShipment.shipInfo.shipmentItems)
                {
                    PackingLine packingLine = new PackingLine();

                    // Need Mapping
                    Commodity commodity = new Commodity();
                    commodity.Code = shipmentItem.commodityId.ToString();
                    packingLine.Commodity = commodity;

                    packingLine.WeightSpecified = true;
                    packingLine.Weight = Convert.ToDecimal(shipmentItem.packageWeight);
                    goodsWeight += Convert.ToDecimal(shipmentItem.packageWeight);

                    UnitOfWeight packageWeightUnit = new UnitOfWeight();
                    packageWeightUnit.Code = shipmentItem.packageWeightUOM == "KGS" ? "KG" : "LBS";
                    packingLine.WeightUnit = packageWeightUnit;

                    PackageType packageType = new PackageType();
                    packageType.Code = shipmentItem.packageTypeCode;
                    packingLine.PackType = packageType;

                    packingLine.PackQtySpecified = true;
                    packingLine.PackQty = Convert.ToInt32(shipmentItem.contentQuantity);
                    goodsOuters += Convert.ToInt32(shipmentItem.contentQuantity);

                    packingLines.Add(packingLine);

                    insuranceValue += Convert.ToDecimal(shipmentItem.insuranceLiabilityValueInUSD);
                }
                ShipmentPackingLineCollection shipmentPackingLineCollection = new ShipmentPackingLineCollection();
                shipmentPackingLineCollection.PackingLine = packingLines.ToArray();
                shipment.PackingLineCollection = shipmentPackingLineCollection;

                shipment.OuterPacksSpecified = true;
                PackageType outerPackageType = new PackageType();
                outerPackageType.Code = brinksShipment.shipInfo.shipmentItems[0].packageTypeCode;
                shipment.OuterPacksPackageType = outerPackageType;
                shipment.OuterPacks = goodsOuters;

                shipment.TotalWeightSpecified = true;
                UnitOfWeight outerPackageWeightUnit = new UnitOfWeight();
                outerPackageWeightUnit.Code = brinksShipment.shipInfo.shipmentItems[0].packageWeightUOM == "KGS" ? "KG" : "LBS";
                shipment.TotalWeightUnit = outerPackageWeightUnit;
                shipment.TotalWeight = goodsWeight;

                shipment.TotalVolumeSpecified = false;
                UnitOfVolume outerPackageVolumeUnit = new UnitOfVolume();
                outerPackageVolumeUnit.Code = "";
                shipment.TotalVolumeUnit = outerPackageVolumeUnit;
                shipment.TotalVolume = goodsVolume;

                shipment.InsuranceValueSpecified = true;
                Currency insuranceCurrency = new Currency();
                insuranceCurrency.Code = "USD";
                shipment.InsuranceValueCurrency = insuranceCurrency;
                shipment.InsuranceValue = insuranceValue;

                universalShipmentData.Shipment = shipment;
                string xml = Utilities.Serialize(universalShipmentData);
                var documentResponse = eAdaptor.Services.SendToCargowise(xml, Configuration.URI, Configuration.Username, Configuration.Password);
                dataResponse.Status = "SUCCESS";
                dataResponse.Message = "Successfully created the bookig.";
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

        #region Create Transport Booking Using Multi-Location Json
        [HttpPost]
        [ProducesResponseType(200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(500)]
        [Route("api/booking/transport")]
        public ActionResult<ShipemtResponse> TransportBooking([FromBody] Models.Shipment shipment)
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
                    dataResponse.Message = String.Format("Server ID '{0}' was not found in the database.", serverId.ToString());
                    return Ok(dataResponse);
                }

            }
            catch (Exception ex)
            {
                throw ex;
            }
            return dataResponse;
        }
        #endregion
        }
}
