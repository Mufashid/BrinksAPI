using BrinksAPI.Auth;
using BrinksAPI.Helpers;
using BrinksAPI.Models;
using Cargowise;
using eAdaptor;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BrinksAPI.Controllers
{
    //[Authorize]
    //[ApiController]
    public class ShipmentController : Controller
    {
        private readonly IConfigManager Configuration;

        private readonly ApplicationDbContext _context;
        public ShipmentController(IConfigManager _configuration)
        {
            Configuration = _configuration;

        }

        #region Create Multiple Shipments
        [HttpPost]
        [Route("api/shipments/multiple")]
        public IActionResult CreateMultipleShipments(BrinksMultipleShipment brinksShipment)
        {
            string responseData = "";
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest();
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
                company.Code = Configuration.CompanyCode;
                dataContext.Company = company;

                dataContext.DataProvider = Configuration.ServiceDataProvider;
                dataContext.EnterpriseID = Configuration.EnterpriseId;

                dataContext.ServerID = Configuration.ServerId;
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
                //responseData = Services.SendToCargowise(xml, Configuration.URI, Configuration.Username, Configuration.Password);

            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }

            return Created("", responseData);
        } 
        #endregion
    }
}
