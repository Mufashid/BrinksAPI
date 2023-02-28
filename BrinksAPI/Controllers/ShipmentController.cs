using BrinksAPI.Auth;
using BrinksAPI.Entities;
using BrinksAPI.Helpers;
using BrinksAPI.Interfaces;
using BrinksAPI.Models;
using eAdaptor;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NativeOrganization;
using NativeRequest;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;

using WinSCP;

namespace BrinksAPI.Controllers
{
    [Authorize]
    public class ShipmentController : Controller
    {
        private readonly ILogger<ShipmentController> _logger;
        private readonly IConfigManager _configuration;
        private readonly ApplicationDbContext _context;
        public ShipmentController(IConfigManager configuration, ApplicationDbContext applicationDbContext, ILogger<ShipmentController> logger)
        {
            _configuration = configuration;
            _context = applicationDbContext;
            _logger = logger;
        }


        #region UPSERT SHIPMENT  API
        /// <summary>
        /// Creates Shipment and Transport Booking.
        /// </summary>
        /// <param name="shipment"></param>
        /// <returns>A newly created Shipment </returns>

        /// <response code="200">Success</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="500">Internal server error</response>
        [HttpPost]
        [ProducesResponseType(200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(500)]
        [Route("api/shipment")]
        public ActionResult<ShipemtResponse> UpsertShipment([FromBody] Models.Shipment shipment)
        {
            ShipemtResponse dataResponse = new ShipemtResponse();
            string successMessage = "";
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
                    _logger.LogError("Error: {@Error} Request: {@Request}", errorString, shipment);

                    dataResponse.Status = "ERROR";
                    dataResponse.Message = errorString;

                    return Ok(dataResponse);
                }
                #endregion

                UniversalShipmentData universalShipmentData = new UniversalShipmentData();
                Shipment cwShipment = new Shipment();

                ShipmentItem? firstShipmentItem = shipment.shipmentItems?.FirstOrDefault();
                string? shipmentId = GetShipmentNumberByHawb(shipment.hawbNum);

                var loadingPort = shipment.pickupAirportCode != null ?_context.sites.Where(s => s.Airport == shipment.pickupAirportCode).FirstOrDefault():null;
                loadingPort = loadingPort == null ? _context.sites.Where(s => s.SiteCode.ToString() == shipment.pickupSiteCode).FirstOrDefault() : loadingPort;
                loadingPort = loadingPort == null ? _context.sites.Where(s => s.Country.ToLower() == shipment.shipperCountryCode.ToLower()).FirstOrDefault() : loadingPort;
                string? companyCode = loadingPort?.CompanyCode;

                #region Data Context
                DataContext dataContext = new DataContext();
                DataTarget dataTarget = new DataTarget();
                dataTarget.Type = "ForwardingShipment";
                dataTarget.Key = shipmentId;

                List<DataTarget> dataTargets = new List<DataTarget>();
                dataTargets.Add(dataTarget);
                dataContext.DataTargetCollection = dataTargets.ToArray();

                Staff staff = new Staff();
                staff.Code = shipment.userId;
                dataContext.EventUser = staff;

                dataContext.EnterpriseID = _configuration.EnterpriseId;
                dataContext.ServerID = _configuration.ServerId;
                dataContext.DataProvider = "ShipmentAPI";
                Company company = new Company();
                company.Code = companyCode;
                dataContext.Company = company;

                cwShipment.DataContext = dataContext;
                #endregion

                #region PORT
                UNLOCO portOfLoading = new UNLOCO();
                portOfLoading.Code = loadingPort?.Country + loadingPort?.Airport;
                cwShipment.PortOfOrigin = portOfLoading;
                cwShipment.PortOfLoading = portOfLoading;

                var dischargePort = shipment.deliveryAirportCode != null ? _context.sites.Where(s => s.Airport == shipment.deliveryAirportCode).FirstOrDefault() : null;
                dischargePort = dischargePort == null ? _context.sites.Where(s => s.SiteCode.ToString() == shipment.deliverySiteCode).FirstOrDefault() : dischargePort;
                dischargePort = dischargePort == null ? _context.sites.Where(s => s.Country.ToLower() == shipment.consigneeCountryCode.ToLower()).FirstOrDefault() : dischargePort;
                UNLOCO portOfDischarge = new UNLOCO();
                portOfDischarge.Code = dischargePort?.Country + dischargePort?.Airport;
                cwShipment.PortOfDestination = portOfDischarge;
                cwShipment.PortOfDischarge = portOfDischarge;
                #endregion

                #region NOTES
                ShipmentNoteCollection shipmentNoteCollection = new ShipmentNoteCollection();
                List<Note> notes = new List<Note>();

                Note note = new Note();
                note.Description = "Special Instructions";
                note.NoteText = shipment.notes;
                notes.Add(note);

                Note markAndNumberNote = new Note();
                markAndNumberNote.Description = "Marks & Numbers";
                markAndNumberNote.NoteText = firstShipmentItem?.showSealNumber;
                markAndNumberNote.NoteContext = new NoteContext() { Code = "AAA" };
                markAndNumberNote.Visibility = new CodeDescriptionPair() { Code = "PUB" };
                notes.Add(markAndNumberNote);

                Note shipmentInfo = new Note();
                shipmentInfo.Description = "Shipment Imported";
                shipmentInfo.NoteText = "Y";
                shipmentInfo.NoteContext = new NoteContext() { Code = "AAA" };
                shipmentInfo.Visibility = new CodeDescriptionPair() { Code = "PUB" };
                notes.Add(shipmentInfo);

                Note pickUpNote = new Note();
                pickUpNote.Description = "Pickup Note";
                pickUpNote.NoteText = shipment.puNotes;
                notes.Add(pickUpNote);

                Note deliveryNote = new Note();
                deliveryNote.Description = "Devlivery Note";
                deliveryNote.NoteText = shipment.dlvNotes;
                notes.Add(deliveryNote);

                notes.RemoveAll(n => n.NoteText == null);
                shipmentNoteCollection.Note = notes.ToArray();
                cwShipment.NoteCollection = shipmentNoteCollection;
                #endregion

                #region ORGANIZATION ADDRESS
                List<OrganizationAddress> organizationAddresses = new List<OrganizationAddress>();

                #region SHIPPER ADDRESS
                OrganizationData? shipperOrganizationData = SearchOrgWithRegNo(shipment.shipperGlobalCustomerCode);
                OrganizationAddress shipperAddress = new OrganizationAddress();
                shipperAddress.AddressType = "ConsignorDocumentaryAddress";
                if (shipperOrganizationData?.OrgHeader == null)
                {
                    // Create organization if not exist
                    Organization organization = new Organization()
                    {
                        isConsignor = true,
                        isConsignee = true,
                        CompanyName = shipment.shipperName,
                        Address1 = shipment.shipperAddress1,
                        Address2 = shipment.shipperAddress2,
                        Address3 = shipment.shipperAddress3,
                        Address4 = shipment.shipperAddress4,
                        City = shipment.shipperCity,
                        ProviceCode = shipment.shipperProvinceCode,
                        Postcode = shipment.shipperPostalCode,
                        Phone = shipment.shipperPhoneNumber,
                        Mobile = shipment.shipperMobileNumber,
                        Contact = shipment.shipperContactName,
                        Country = shipment.shipperCountryCode,
                        Unloco = loadingPort?.Country + loadingPort?.Airport,
                        RegistrationNumber = shipment.shipperGlobalCustomerCode,
                    };
                    shipperAddress.OrganizationCode = CreateOrganization(organization);

                }
                else
                {
                    shipperAddress.OrganizationCode = shipperOrganizationData.OrgHeader.Code;
                }
                organizationAddresses.Add(shipperAddress);
                #endregion

                #region CONSIGNEE ADDRESS
                OrganizationData consigneeOrganizationData = SearchOrgWithRegNo(shipment.consigneeGlobalCustomerCode);
                OrganizationAddress consigneeAddress = new OrganizationAddress();
                consigneeAddress.AddressType = "ConsigneeDocumentaryAddress";
                if (consigneeOrganizationData.OrgHeader == null)
                {
                    // Create organization if not exist
                    Organization organization = new Organization()
                    {
                        isConsignor = true,
                        isConsignee = true,
                        CompanyName = shipment.consigneeName,
                        Address1 = shipment.consigneeAddress1,
                        Address2 = shipment.consigneeAddress2,
                        Address3 = shipment.consigneeAddress3,
                        Address4 = shipment.consigneeAddress4,
                        City = shipment.consigneeCity,
                        ProviceCode = shipment.consigneeProvinceCode,
                        Postcode = shipment.consigneePostalCode,
                        Phone = shipment.consigneePhoneNumber,
                        Mobile = shipment.consigneeMobileNumber,
                        Contact = shipment.consigneeContactName,
                        Country = shipment.consigneeCountryCode,
                        Unloco = dischargePort?.Country + dischargePort?.Airport,
                        RegistrationNumber = shipment.consigneeGlobalCustomerCode,
                        Eori = shipment.consigneeEori
                    };
                    consigneeAddress.OrganizationCode = CreateOrganization(organization);

                }
                else
                {
                    consigneeAddress.OrganizationCode = consigneeOrganizationData.OrgHeader.Code;
                }
                organizationAddresses.Add(consigneeAddress);
                #endregion

                #region PICKUP ADDRESS
                OrganizationData? pickupOrganizationData = SearchOrgWithRegNo(firstShipmentItem.puGlobalCustomerCode);
                OrganizationAddress puAddress = new OrganizationAddress();
                puAddress.AddressType = "ConsignorPickupDeliveryAddress";
                if (pickupOrganizationData.OrgHeader == null)
                {
                    // Create organization if not exist
                    var site = _context.sites.Where(s=>s.Country.ToLower() == firstShipmentItem.puCountryCode.ToLower()).FirstOrDefault();
                    Organization organization = new Organization()
                    {
                        isConsignor = true,
                        isConsignee = true,
                        CompanyName = firstShipmentItem.puName,
                        Address1 = firstShipmentItem.puAddress1,
                        Address2 = firstShipmentItem.puAddress2,
                        Address3 = firstShipmentItem.puAddress3,
                        Address4 = firstShipmentItem.puAddress4,
                        City = firstShipmentItem.puCity,
                        ProviceCode = firstShipmentItem.puProvinceCode,
                        Postcode = firstShipmentItem.puPostalCode,
                        Phone = firstShipmentItem.puPhoneNumber,
                        Mobile = firstShipmentItem.puMobileNumber,
                        Contact = firstShipmentItem.puContactName,
                        Country = firstShipmentItem.puCountryCode,
                        Unloco = site?.Country + site?.Airport,
                        RegistrationNumber = firstShipmentItem.puGlobalCustomerCode,
                    };
                    puAddress.OrganizationCode = CreateOrganization(organization);

                }
                else
                {
                    puAddress.OrganizationCode = pickupOrganizationData.OrgHeader.Code;
                }
                organizationAddresses.Add(puAddress);
                #endregion

                #region DELIVERY ADDRESS
                OrganizationData? dlvOrganizationData = SearchOrgWithRegNo(firstShipmentItem.dlvGlobalCustomerCode);
                OrganizationAddress dlvAddress = new OrganizationAddress();
                dlvAddress.AddressType = "ConsigneePickupDeliveryAddress";
                if (dlvOrganizationData.OrgHeader == null)
                {
                    // Create Org
                    var site = _context.sites.Where(s => s.Country.ToLower() == firstShipmentItem.dlvCountryCode.ToLower()).FirstOrDefault();
                    Organization organization = new Organization()
                    {
                        isConsignor = true,
                        isConsignee = true,
                        CompanyName = firstShipmentItem.dlvName,
                        Address1 = firstShipmentItem.dlvAddress1,
                        Address2 = firstShipmentItem.dlvAddress2,
                        Address3 = firstShipmentItem.dlvAddress3,
                        Address4 = firstShipmentItem.dlvAddress4,
                        City = firstShipmentItem.dlvCity,
                        ProviceCode = firstShipmentItem.dlvProvinceCode,
                        Postcode = firstShipmentItem.dlvPostalCode,
                        Phone = firstShipmentItem.dlvPhoneNumber,
                        Mobile = firstShipmentItem.dlvMobileNumber,
                        Contact = firstShipmentItem.dlvContactName,
                        Country = firstShipmentItem.dlvCountryCode,
                        Unloco = site?.Country + site?.Airport,
                        RegistrationNumber = firstShipmentItem.dlvGlobalCustomerCode,
                    };
                    dlvAddress.OrganizationCode = CreateOrganization(organization);

                }
                else
                {
                    dlvAddress.OrganizationCode = dlvOrganizationData.OrgHeader.Code;
                }
                organizationAddresses.Add(dlvAddress);
                #endregion

                cwShipment.OrganizationAddressCollection = organizationAddresses.ToArray();
                #endregion

                #region PACKING LINE
                int shipmentPacklineCount = 0;
                ShipmentPackingLineCollection shipmentPackingLineCollection = new ShipmentPackingLineCollection();
                List<PackingLine> packings = new List<PackingLine>();
                foreach (var shipmentItem in shipment.shipmentItems)
                {
                    PackingLine packingLine = new PackingLine();

                    packingLine.LinkSpecified = true;
                    packingLine.Link = shipmentPacklineCount;
                    shipmentPacklineCount++;

                    Commodity commodity = new Commodity();
                    commodity.Code = shipmentItem.globalCommodityCode;
                    packingLine.Commodity = commodity;

                    packingLine.HarmonisedCode = shipmentItem.uscsScheduleBCode;
                    packingLine.GoodsDescription = shipmentItem.commodityDescription;

                    string? packageTypeCodeCW = _context.packageTypes.Where(p => p.BrinksCode == shipmentItem.packageTypeCd)?.FirstOrDefault()?.CWCode;
                    PackageType packageType = new PackageType();
                    packageType.Code = packageTypeCodeCW;
                    packingLine.PackType = packageType;

                    packingLine.PackQtySpecified = true;
                    packingLine.PackQty = Convert.ToInt64(shipmentItem.numberOfItems);

                    UnitOfWeight unitOfWeight = new UnitOfWeight();
                    unitOfWeight.Code = "KG";
                    packingLine.WeightUnit = unitOfWeight;

                    string? unitOfLengthCWCode = shipmentItem.dimUOM == "in" ? "IN" : "CM";
                    UnitOfLength unitOfLength = new UnitOfLength();
                    unitOfLength.Code = unitOfLengthCWCode;
                    packingLine.LengthUnit = unitOfLength;

                    string? unitOfVolumeCWCode = shipmentItem.dimUOM == "in" ? "CI" : "CC";
                    UnitOfVolume unitOfVolume = new UnitOfVolume();
                    unitOfVolume.Code = unitOfVolumeCWCode;
                    packingLine.VolumeUnit = unitOfVolume;

                    packingLine.WeightSpecified = true;
                    packingLine.LengthSpecified = true;
                    packingLine.WidthSpecified = true;
                    packingLine.HeightSpecified = true;
                    packingLine.VolumeSpecified = true;

                    packingLine.Weight = Convert.ToDecimal(shipmentItem.grossWeight);
                    packingLine.Length = Convert.ToDecimal(shipmentItem.dimLength);
                    packingLine.Width = Convert.ToDecimal(shipmentItem.dimWidth);
                    packingLine.Height = Convert.ToDecimal(shipmentItem.dimHeight);
                    packingLine.Volume = Convert.ToDecimal(shipmentItem.dimLength)* Convert.ToDecimal(shipmentItem.dimWidth)* Convert.ToDecimal(shipmentItem.dimHeight);

                    packingLine.ReferenceNumber = shipmentItem.barcode;
                    packingLine.MarksAndNos = shipmentItem.showSealNumber;

                    Country countryOforigin = new Country();
                    countryOforigin.Code = shipmentItem.originCountry;
                    packingLine.CountryOfOrigin = countryOforigin;

                    #region CUSTOMIZED FIELDS COLLECTION
                    List<CustomizedField> shipmentItemCustomizedFields = new List<CustomizedField>();

                    CustomizedField pickupDateCF = new CustomizedField();
                    pickupDateCF.Key = "Actual Pickup";
                    pickupDateCF.Value = shipmentItem.puActDate;
                    shipmentItemCustomizedFields.Add(pickupDateCF);

                    CustomizedField deliveryDateCF = new CustomizedField();
                    deliveryDateCF.Key = "Actual Delivery";
                    deliveryDateCF.Value = shipmentItem.dlvActDate;
                    shipmentItemCustomizedFields.Add(deliveryDateCF);

                    CustomizedField packageLineNetWeightCF = new CustomizedField();
                    packageLineNetWeightCF.Key = "net_weight";
                    packageLineNetWeightCF.Value = shipmentItem.uomNetWeight.ToString();
                    shipmentItemCustomizedFields.Add(packageLineNetWeightCF);

                    // Mapping (Bits sending 3 characters CW allows 2 characters)
                    CustomizedField packageLineUOMCF = new CustomizedField();
                    packageLineUOMCF.DataType = CustomizedFieldDataType.String;
                    packageLineUOMCF.Key = "uom";
                    packageLineUOMCF.Value = shipmentItem.uomCode;
                    shipmentItemCustomizedFields.Add(packageLineUOMCF);

                    shipmentItemCustomizedFields.RemoveAll(s => s.Value == null);
                    packingLine.CustomizedFieldCollection = shipmentItemCustomizedFields.ToArray();
                    #endregion

                    packings.Add(packingLine);
                }

                shipmentPackingLineCollection.PackingLine = packings.ToArray();
                cwShipment.PackingLineCollection = shipmentPackingLineCollection;
                #endregion

                int totalQunatity = shipment.shipmentItems.Sum(i => i.numberOfItems);
                decimal totalNetWeight = Convert.ToDecimal(shipment?.shipmentItems?.Sum(i => i.uomNetWeight));
                decimal totalGrossWeight = Convert.ToDecimal(shipment?.shipmentItems?.Sum(i => i.grossWeight));
                decimal totalChargableWeight = Convert.ToDecimal(shipment?.shipmentItems?.Sum(i => i.chargableWeight));
                decimal totalDimWeight = Convert.ToDecimal(shipment?.shipmentItems?.Sum(i => i.dimWeight));
                decimal totalInsurenceLiability = Convert.ToDecimal(shipment?.shipmentItems?.Sum(i => i.insuranceLiability));
                decimal totalCustomsLiability = Convert.ToDecimal(shipment?.shipmentItems?.Sum(i => i.customsLiability));
                decimal totalVolume = Convert.ToDecimal(shipment?.shipmentItems?.Sum(i => i.dimLength)) * Convert.ToDecimal(shipment?.shipmentItems?.Sum(i => i.dimWidth))* Convert.ToDecimal(shipment?.shipmentItems?.Sum(i => i.dimHeight));

                #region CUSTOMIZED FIELDS
                List<CustomizedField> shipmentCustomizedFields = new List<CustomizedField>();

                CustomizedField shipperReferenceCF = new CustomizedField();
                shipperReferenceCF.DataType = CustomizedFieldDataType.String;
                shipperReferenceCF.Key = "Shipper Reference";
                shipperReferenceCF.Value = shipment.shippersReference;
                shipmentCustomizedFields.Add(shipperReferenceCF);

                string chargeType = shipment.chargesType == "P" ? "PRE" : "COL";
                CustomizedField chargesTypeCF = new CustomizedField();
                chargesTypeCF.DataType = CustomizedFieldDataType.String;
                chargesTypeCF.Key = "Brinks Charges";
                chargesTypeCF.Value = chargeType;
                shipmentCustomizedFields.Add(chargesTypeCF);

                CustomizedField chargesAmountCF = new CustomizedField();
                chargesAmountCF.DataType = CustomizedFieldDataType.Decimal;
                chargesAmountCF.Key = "Charges Amount";
                chargesAmountCF.Value = shipment.chargesAmount.ToString();
                shipmentCustomizedFields.Add(chargesAmountCF);

                CustomizedField chargeCurrencyCF = new CustomizedField();
                chargeCurrencyCF.DataType = CustomizedFieldDataType.String;
                chargeCurrencyCF.Key = "Charges Currency";
                chargeCurrencyCF.Value = firstShipmentItem?.insurCurrencyCode;
                shipmentCustomizedFields.Add(chargeCurrencyCF);

                string userChargeType = shipment.userChargesType == "P" ? "PRE" : "COL";
                CustomizedField userChargesTypeCF = new CustomizedField();
                userChargesTypeCF.DataType = CustomizedFieldDataType.String;
                userChargesTypeCF.Key = "User/Clearance Fees";
                userChargesTypeCF.Value = userChargeType;
                shipmentCustomizedFields.Add(userChargesTypeCF);

                CustomizedField userAmountCF = new CustomizedField();
                userAmountCF.DataType = CustomizedFieldDataType.String;
                userAmountCF.Key = "User Amount";
                userAmountCF.Value = shipment.userAmount.ToString();
                shipmentCustomizedFields.Add(userAmountCF);

                //string showFlag = shipment.showFlag == "N" ? "No" : "S1";
                //CustomizedField showFlagCF = new CustomizedField();
                //showFlagCF.DataType = CustomizedFieldDataType.String;
                //showFlagCF.Key = "Show";
                //showFlagCF.Value = showFlag;
                //shipmentCustomizedFields.Add(showFlagCF);

                CustomizedField netWeightUnitCF = new CustomizedField();
                netWeightUnitCF.DataType = CustomizedFieldDataType.String;
                netWeightUnitCF.Key = "Net Weight UOM";
                netWeightUnitCF.Value = firstShipmentItem?.uomCode;
                shipmentCustomizedFields.Add(netWeightUnitCF);

                CustomizedField netWeightCF = new CustomizedField();
                netWeightCF.DataType = CustomizedFieldDataType.Decimal;
                netWeightCF.Key = "Net weight";
                netWeightCF.Value = totalNetWeight.ToString();
                shipmentCustomizedFields.Add(netWeightCF);

                CustomizedField waiver = new CustomizedField();
                waiver.DataType = CustomizedFieldDataType.String;
                waiver.Key = "Waiver";
                waiver.Value = shipment.waiverNumber;
                shipmentCustomizedFields.Add(waiver);

                CustomizedField originShipmentIdCF = new CustomizedField();
                originShipmentIdCF.DataType = CustomizedFieldDataType.String;
                originShipmentIdCF.Key = "Origin Shipment ID";
                originShipmentIdCF.Value = shipment.originShipmentId.ToString();
                shipmentCustomizedFields.Add(originShipmentIdCF);

                shipmentCustomizedFields.RemoveAll(s => s.Value == null);
                cwShipment.CustomizedFieldCollection = shipmentCustomizedFields.ToArray();
                #endregion

                #region BASIC REGISTRATION

                cwShipment.WayBillNumber = shipment.hawbNum;
                cwShipment.ContainerMode = new ContainerMode() { Code = "LSE" };

                ServiceLevel serviceLevel = new ServiceLevel();
                serviceLevel.Code = shipment.serviceType;
                cwShipment.ServiceLevel = serviceLevel;

                //List<EntryNumber> entryNumbers = new List<EntryNumber>();
                //EntryNumber entryNumber = new EntryNumber();
                //EntryType entryType = new EntryType();
                //entryType.Code = "ITN";
                //entryNumber.Type = entryType;
                //entryNumber.Number = shipment.itn;
                //entryNumbers.Add(entryNumber);
                //cwShipment.EntryNumberCollection = entryNumbers.ToArray();

                string? transportModeCWCode = _context.transportModes.Where(t => t.BrinksCode == shipment.modeOfTransport).FirstOrDefault()?.CWCode;
                CodeDescriptionPair transportMode = new CodeDescriptionPair();
                transportMode.Code = transportModeCWCode;
                cwShipment.TransportMode = transportMode;

                CodeDescriptionPair paymentMethod = new CodeDescriptionPair();
                paymentMethod.Code = shipment.chargesType;
                cwShipment.PaymentMethod = paymentMethod;

                IncoTerm incoTerm = new IncoTerm();
                incoTerm.Code = firstShipmentItem?.termCode;
                cwShipment.ShipmentIncoTerm = incoTerm;
                cwShipment.TotalWeightSpecified = true;
                cwShipment.TotalNoOfPacksSpecified = true;
                cwShipment.TotalNoOfPiecesSpecified = true;
                cwShipment.ActualChargeableSpecified = true;
                cwShipment.OuterPacksSpecified = true;
                cwShipment.TotalVolumeSpecified = true;

                UnitOfVolume totalUnitVolume = new UnitOfVolume();
                totalUnitVolume.Code = firstShipmentItem?.dimUOM == "in" ? "CI" : "CC";
                cwShipment.TotalVolumeUnit = totalUnitVolume;

                UnitOfWeight totalWeightUnit = new UnitOfWeight();
                totalWeightUnit.Code = "KG";//firstShipmentItem.grossWeightUomCode;
                cwShipment.TotalWeightUnit = totalWeightUnit;

                cwShipment.TotalWeight = totalGrossWeight;
                cwShipment.TotalVolume = totalVolume;
                cwShipment.ActualChargeable = totalChargableWeight;
                cwShipment.TotalNoOfPacks = totalQunatity;
                cwShipment.TotalNoOfPieces = totalQunatity;
                cwShipment.OuterPacks = totalQunatity;

                string? outerPackageCWCode = _context.packageTypes.Where(p => p.BrinksCode == firstShipmentItem.packageTypeCd)?.FirstOrDefault()?.CWCode;
                PackageType outerPackageType = new PackageType();
                outerPackageType.Code = outerPackageCWCode;
                cwShipment.OuterPacksPackageType = outerPackageType;
                cwShipment.TotalNoOfPacksPackageType = outerPackageType;

                Currency insurenceLiabilityCurrency = new Currency();
                insurenceLiabilityCurrency.Code = firstShipmentItem?.insurCurrencyCode;
                cwShipment.InsuranceValueCurrency = insurenceLiabilityCurrency;
                cwShipment.InsuranceValueSpecified = true;
                cwShipment.InsuranceValue = totalInsurenceLiability;

                cwShipment.GoodsValueSpecified = true;
                cwShipment.GoodsValue = totalCustomsLiability;
                cwShipment.GoodsDescription = firstShipmentItem?.commodityDescription;
                Currency goodsCurrency = new Currency();
                goodsCurrency.Code = firstShipmentItem?.customsCurrencyCode;
                cwShipment.GoodsValueCurrency = goodsCurrency;

                ShipmentLocalProcessing localProcessing = new ShipmentLocalProcessing();
                localProcessing.EstimatedPickup = firstShipmentItem?.puEstDate;
                localProcessing.PickupCartageCompleted = firstShipmentItem?.puActDate;
                localProcessing.EstimatedDelivery = firstShipmentItem?.dlvEstDate;
                localProcessing.DeliveryCartageCompleted = firstShipmentItem?.dlvActDate;
                cwShipment.LocalProcessing = localProcessing;
                #endregion

                universalShipmentData.Shipment = cwShipment;
                successMessage = shipmentId == null ? "Shipment Created in CW. " : "Shipment Updated in CW. ";
                string xml = Utilities.Serialize(universalShipmentData);
                var documentResponse = eAdaptor.Services.SendToCargowise(xml, _configuration.URI, _configuration.Username, _configuration.Password);
                if (documentResponse.Status == "ERROR")
                {
                    string errorMessage = documentResponse.Data.Data.FirstChild.InnerText;
                    dataResponse.Status = documentResponse.Status;

                    MatchCollection matchedError = Regex.Matches(errorMessage, "(Error)(.*)");
                    string[] groupedErrors = matchedError.GroupBy(x => x.Value).Select(y => y.Key).ToArray();
                    dataResponse.Message = string.Join(",", groupedErrors);

                    _logger.LogError("Error: {@Error} Request: {@Request}", dataResponse.Message, shipment);
                    return Ok(dataResponse);
                }
                else
                {

                    #region TRANSPORT BOOKING
                    string? responseShipmentId = Utilities.ReadUniversalEvent(documentResponse.Data.Data.OuterXml).Event.DataContext.DataSourceCollection.Where(s => s.Type == "ForwardingShipment").FirstOrDefault()?.Key;
                    var tranportBookingObj = _context.transportBookings.Where(t => t.HawbNumber == shipment.hawbNum).FirstOrDefault();
                    string? transportBooking = "";

                    #region TRANPORTBOOKIN INSERT OR UPDATE
                    if (tranportBookingObj != null)
                    {
                        //Update
                        transportBooking = tranportBookingObj.TBNumber;
                    }
                    else
                    {
                        //Insert
                        // Wait 15 sec for cw trigger to generate tranportbooking XML in the SFTP outbound service.
                        Thread.Sleep(15000);
                        string localDirectory = Path.Join(AppDomain.CurrentDomain.BaseDirectory, "tbProcessingFiles");

                        if (!Directory.Exists(localDirectory))
                            Directory.CreateDirectory(localDirectory);

                        if (!String.IsNullOrEmpty(_configuration.SftpUri))
                        {
                            GetFilesFromSFTP(_configuration.SftpUri, _configuration.SftpUsername, _configuration.SftpPassword, _configuration.SftpOutboundFolder, localDirectory);
                        }
                        else
                        {
                            GetFilesFromLocalServer(_configuration.SftpOutboundFolder, localDirectory);
                        }
                        string[] filePaths = Directory.GetFiles(localDirectory, "*.xml", SearchOption.TopDirectoryOnly);
                        foreach (var file in filePaths)
                        {
                            try
                            {
                                var trasportBookingXML = System.IO.File.ReadAllText(file);
                                UniversalShipmentData? transportBookingData = Utilities.ReadUniversalShipment(Utilities.getElementFromXML(trasportBookingXML, "Body"));

                                if (transportBookingData.Shipment.DataContext.ActionPurpose.Code == "TRB")
                                {
                                    if (transportBookingData.Shipment.SubShipmentCollection[0].DataContext.DataSourceCollection.Any(d => d.Key.Contains(responseShipmentId)))
                                    {
                                        transportBooking = transportBookingData.Shipment.DataContext.DataSourceCollection.Where(d => d.Type == "TransportBooking")?.FirstOrDefault().Key;
                                        TransportBooking dbTransportBooking = new TransportBooking
                                        {
                                            TBNumber = transportBooking,
                                            ShipmentNumber = responseShipmentId,
                                            HawbNumber = shipment.hawbNum
                                        };
                                        _context.Add(dbTransportBooking);
                                        _context.SaveChanges();

                                        if(!String.IsNullOrEmpty(_configuration.SftpUri))
                                        {
                                            // Backup to server
                                            string sourcePath = Path.Join(_configuration.SftpOutboundFolder, Path.GetFileName(file));
                                            MoveFileFTP(_configuration.SftpUri, _configuration.SftpUsername, _configuration.SftpPassword, sourcePath, _configuration.SftpBackupFolder);
                                        }
                                        else
                                        {
                                            string backupPath = Path.Join(_configuration.SftpBackupFolder, Path.GetFileName(file));
                                            System.IO.File.Move(file, backupPath);
                                        }
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError("Error: {@Error} Request: {@Request}", ex.Message, shipment);
                                continue;
                            }
                            finally
                            {
                                System.IO.File.Delete(file);
                            }

                        }
                    }
                    #endregion

                    if (transportBooking != "")
                    {
                        #region TRANPORT BOOKING SHIPMENT
                        UniversalShipmentData universalTransportData = new UniversalShipmentData();
                        Shipment tbShipment = new Shipment();

                        #region Data Context
                        DataContext tbDataContext = new DataContext();
                        DataTarget tbDataTarget = new DataTarget();
                        tbDataTarget.Type = "TransportBooking";
                        tbDataTarget.Key = transportBooking;

                        List<DataTarget> tbDataTargets = new List<DataTarget>();
                        tbDataTargets.Add(tbDataTarget);
                        tbDataContext.DataTargetCollection = tbDataTargets.ToArray();

                        tbDataContext.EnterpriseID = _configuration.EnterpriseId;
                        tbDataContext.ServerID = _configuration.ServerId;
                        tbDataContext.DataProvider = "TBAAPI";
                        #endregion

                        tbShipment.DataContext = tbDataContext;
                        tbShipment.ShipmentType = new CodeDescriptionPair { Code = "BKG" };

                        ShipmentPackingLineCollection transportPackingLineCollection = new ShipmentPackingLineCollection();
                        List<PackingLine> transportPackings = new List<PackingLine>();
                        //List<ShipmentInstruction> shipmentInstructions = new List<ShipmentInstruction>();

                        #region SHIPMENT ITEMS
                        int packlineCount = 0;
                        int instructionCount = 1;
                        foreach (var shipmentItem in shipment.shipmentItems)
                        {
                            #region PACKING LINE

                            PackingLine packingLine = new PackingLine();

                            packingLine.LinkSpecified = true;
                            packingLine.Link = packlineCount;

                            Commodity commodity = new Commodity();
                            commodity.Code = shipmentItem.globalCommodityCode;
                            packingLine.Commodity = commodity;

                            packingLine.GoodsDescription = shipmentItem.commodityDescription;

                            string? packaheTypeCodeCW = _context.packageTypes.Where(p => p.BrinksCode == shipmentItem.packageTypeCd)?.FirstOrDefault()?.CWCode;
                            PackageType packageType = new PackageType();
                            packageType.Code = packaheTypeCodeCW;
                            packingLine.PackType = packageType;


                            packingLine.PackQtySpecified = true;
                            packingLine.WeightSpecified = true;
                            packingLine.PackQty = Convert.ToInt64(shipmentItem.numberOfItems);
                            packingLine.Weight = Convert.ToDecimal(shipmentItem.grossWeight);

                            packingLine.LengthSpecified = true;
                            packingLine.WeightSpecified = true;
                            packingLine.WidthSpecified = true;
                            packingLine.HeightSpecified = true;
                            packingLine.Length = Convert.ToDecimal(shipmentItem.dimLength);
                            packingLine.Weight = Convert.ToDecimal(shipmentItem.dimWeight);
                            packingLine.Width = Convert.ToDecimal(shipmentItem.dimWidth);
                            packingLine.Height = Convert.ToDecimal(shipmentItem.dimLength);

                            packingLine.ReferenceNumber = shipmentItem.barcode;

                            Country countryOforigin = new Country();
                            countryOforigin.Code = shipmentItem.originCountry;
                            packingLine.CountryOfOrigin = countryOforigin;

                            string? unitOfLengthBitsCode = shipmentItem.dimUOM == "in" ? "IN" : "CM";
                            UnitOfLength unitOfLength = new UnitOfLength();
                            unitOfLength.Code = unitOfLengthBitsCode;
                            packingLine.LengthUnit = unitOfLength;

                            string? unitOfVolumeBitsCode = shipmentItem.dimUOM == "in" ? "CI" : "CC";
                            UnitOfVolume unitOfVolume = new UnitOfVolume();
                            unitOfVolume.Code = unitOfVolumeBitsCode;
                            packingLine.VolumeUnit = unitOfVolume;

                            // Mapping
                            UnitOfWeight unitOfWeight = new UnitOfWeight();
                            unitOfWeight.Code = shipmentItem.uomCode;
                            packingLine.WeightUnit = unitOfWeight;

                            transportPackings.Add(packingLine);
                            #endregion

                            #region INSTRUCTIONS
                            //ShipmentInstruction tbPickUpShipmentInstruction = new ShipmentInstruction();

                            //tbPickUpShipmentInstruction.SequenceSpecified = true;
                            //tbPickUpShipmentInstruction.Sequence = instructionCount;
                            //tbPickUpShipmentInstruction.Type = new CodeDescriptionPair() { Code = "PIC" };

                            OrganizationAddress pickupAddress = new OrganizationAddress();
                            pickupAddress.AddressType = "LocalCartageExporter";
                            pickupAddress.Address1 = shipmentItem.puAddress1;
                            pickupAddress.Address2 = shipmentItem.puAddress2;
                            pickupAddress.City = shipmentItem.puCity;
                            pickupAddress.CompanyName = shipmentItem.puName;
                            pickupAddress.Contact = shipmentItem.puContactName;
                            pickupAddress.Country = new Country() { Code = shipmentItem.puCountryCode };
                            pickupAddress.Mobile = shipmentItem.puMobileNumber;
                            pickupAddress.Postcode = shipmentItem.puPostalCode;

                            List<RegistrationNumber> pickupRegistrations = new List<RegistrationNumber>();
                            RegistrationNumber pickupRegistration = new RegistrationNumber();
                            pickupRegistration.Type = new RegistrationNumberType() { Code = "LSC" };
                            pickupRegistration.CountryOfIssue = new Country() { Code = shipmentItem.puCountryCode };
                            pickupRegistration.Value = shipmentItem.puGlobalCustomerCode;
                            pickupRegistrations.Add(pickupRegistration);
                            pickupAddress.RegistrationNumberCollection = pickupRegistrations.ToArray();
                            //tbPickUpShipmentInstruction.Address = pickupAddress;

                            //List<ShipmentInstructionInstructionPackingLineLink> pickupPackinglineLinks = new List<ShipmentInstructionInstructionPackingLineLink>();
                            //ShipmentInstructionInstructionPackingLineLink pickupPackinglineLink = new ShipmentInstructionInstructionPackingLineLink();
                            //pickupPackinglineLink.PackingLineLinkSpecified = true;
                            //pickupPackinglineLink.PackingLineLink = packlineCount;
                            //pickupPackinglineLink.QuantitySpecified = true;
                            //pickupPackinglineLink.Quantity = packlineCount + 1;

                            //pickupPackinglineLinks.Add(pickupPackinglineLink);
                            //tbPickUpShipmentInstruction.InstructionPackingLineLinkCollection = pickupPackinglineLinks.ToArray();

                            //shipmentInstructions.Add(tbPickUpShipmentInstruction);
                            //instructionCount++;

                            //ShipmentInstruction tbDeliveryShipmentInstruction = new ShipmentInstruction();

                            //tbDeliveryShipmentInstruction.SequenceSpecified = true;
                            //tbDeliveryShipmentInstruction.Sequence = instructionCount;
                            //tbDeliveryShipmentInstruction.Type = new CodeDescriptionPair() { Code = "DLV" };

                            OrganizationAddress deliveryAddress = new OrganizationAddress();
                            deliveryAddress.AddressType = "LocalCartageImporter";
                            deliveryAddress.Address1 = shipmentItem.dlvAddress1;
                            deliveryAddress.Address2 = shipmentItem.dlvAddress2;
                            deliveryAddress.City = shipmentItem.dlvCity;
                            deliveryAddress.CompanyName = shipmentItem.dlvName;
                            deliveryAddress.Contact = shipmentItem.dlvContactName;
                            deliveryAddress.Country = new Country() { Code = shipmentItem.dlvCountryCode };
                            deliveryAddress.Mobile = shipmentItem.dlvMobileNumber;
                            deliveryAddress.Postcode = shipmentItem.dlvPostalCode;

                            //List<RegistrationNumber> deliveryRegistrations = new List<RegistrationNumber>();
                            //RegistrationNumber deliveryRegistration = new RegistrationNumber();
                            //deliveryRegistration.Type = new RegistrationNumberType() { Code = "LSC" };
                            //deliveryRegistration.CountryOfIssue = new Country() { Code = shipmentItem.dlvCountryCode };
                            //deliveryRegistration.Value = shipmentItem.dlvGlobalCustomerCode;
                            //deliveryRegistrations.Add(deliveryRegistration);
                            //deliveryAddress.RegistrationNumberCollection = deliveryRegistrations.ToArray();
                            //tbDeliveryShipmentInstruction.Address = deliveryAddress;

                            //List<ShipmentInstructionInstructionPackingLineLink> deliveryPackinglineLinks = new List<ShipmentInstructionInstructionPackingLineLink>();
                            //ShipmentInstructionInstructionPackingLineLink deliveryPackinglineLink = new ShipmentInstructionInstructionPackingLineLink();
                            //deliveryPackinglineLink.PackingLineLinkSpecified = true;
                            //deliveryPackinglineLink.PackingLineLink = packlineCount;
                            //deliveryPackinglineLink.QuantitySpecified = true;
                            //deliveryPackinglineLink.Quantity = packlineCount + 1;
                            //deliveryPackinglineLinks.Add(deliveryPackinglineLink);
                            //tbDeliveryShipmentInstruction.InstructionPackingLineLinkCollection = deliveryPackinglineLinks.ToArray();
                            //shipmentInstructions.Add(tbDeliveryShipmentInstruction);
                            instructionCount++;
                            #endregion

                            packlineCount++;
                        }
                        #endregion

                        transportPackingLineCollection.PackingLine = transportPackings.ToArray();
                        tbShipment.PackingLineCollection = transportPackingLineCollection;

                        #region TRANPORT BOOKING SUBSHIPMENT
                        List<Shipment> tbSubshipments = new List<Shipment>();
                        Shipment tbSubshipment = new Shipment();
                        tbSubshipment.DataContext = tbDataContext;
                        tbSubshipment.ContainerMode = new ContainerMode() { Code = "LSE" };
                        //tbSubshipment.InstructionCollection = shipmentInstructions.ToArray();
                        tbSubshipments.Add(tbSubshipment);
                        tbShipment.SubShipmentCollection = tbSubshipments.ToArray();
                        #endregion

                        universalTransportData.Shipment = tbShipment;
                        #endregion

                        string updatedTransportXML = Utilities.Serialize(universalTransportData);
                        var transportResponse = eAdaptor.Services.SendToCargowise(updatedTransportXML, _configuration.URI, _configuration.Username, _configuration.Password);
                        if (transportResponse.Status == "ERROR")
                        {
                            string message = "Shipment created/updated with Id " + shipmentId + ". Unable to update the transport booking. Below are the reason." + transportResponse.Data.Data.FirstChild.InnerText.Replace("Error - ", "").Replace("Warning - ", ""); ;
                            dataResponse.Status = "ERROR";
                            dataResponse.Message = message;

                            _logger.LogError("Error: {@Error} Request: {@Request}", message, shipment);
                            return Ok(dataResponse);
                        }
                        else
                        {
                            dataResponse.Status = "SUCCESS";
                            dataResponse.Message = successMessage;

                            _logger.LogInformation(successMessage);
                            return Ok(dataResponse);
                        }
                    }
                    else
                    {
                        dataResponse.Status = "SUCCESS";
                        dataResponse.Message = successMessage;
                        _logger.LogInformation("Success: {@Success} Success: {@Request}", successMessage, shipment);
                    }
                    #endregion
                }
            }
            catch (Exception ex)
            {
                
                dataResponse.Status = "ERROR";
                dataResponse.Message = successMessage + ex.Message;
                _logger.LogError("Error: {@Error} Request: {@Request}", dataResponse.Message, shipment);
                return StatusCode(StatusCodes.Status500InternalServerError, dataResponse);
            }
            return Ok(dataResponse);
        }
        #endregion

        #region SHIPMENT HISTORY API
        /// <summary>
        /// Creates Shipment History.
        /// </summary>
        /// <param name="histories"></param>
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
        ///         "serverId": "42",
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
        public ActionResult<List<Response>> CreateShipmentHistory([FromBody] ShipmentHistory[] histories)
        {
            List<Response> dataResponses = new List<Response>();

            try
            {
                foreach (ShipmentHistory history in histories)
                {
                    Response dataResponse = new Response();
                    dataResponse.RequestId = history?.RequestId;
                    try
                    {
                        var validationResults = new List<ValidationResult>();
                        var validationContext = new ValidationContext(history);
                        var isValid = Validator.TryValidateObject(history, validationContext, validationResults);

                        if (isValid)
                        {
                            string? shipmentId = GetShipmentNumberByHawb(history.HawbNumber);
                            if (shipmentId != null)
                            {
                                string companyCode = null;
                                if(history.ServerId !=null)
                                {
                                    int serverId = Int32.Parse(history.ServerId);
                                    Site? site = _context.sites.Where(s => s.ServerID == serverId).FirstOrDefault();
                                    companyCode = site?.CompanyCode;
                                }
                                if (history.SiteCode != null)
                                {
                                    int siteCode = Int32.Parse(history.SiteCode);
                                    Site? site = _context.sites.Where(s => s.SiteCode == siteCode).FirstOrDefault();
                                    companyCode = site?.CompanyCode;
                                }
                                if (companyCode != null)
                                {
                                    ActionType? actionTypeObj = _context.actionTypes.Where(a => a.BrinksCode.ToLower() == history.ActionType.ToLower()).FirstOrDefault();
                                    string? eventType = actionTypeObj is null ? "Z00" : actionTypeObj.EventType;
                                    Events.UniversalEventData universalEvent = new Events.UniversalEventData();

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
                                    dataContext.Company = company;

                                    dataContext.EnterpriseID = _configuration.EnterpriseId;
                                    dataContext.ServerID = _configuration.ServerId;
                                    Events.Staff staff = new Events.Staff();
                                    staff.Code = history.UserId;
                                    dataContext.EventUser = staff;
                                    dataContext.DataProvider = "ShipmentHistoryAPI";
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
                                            Events.UniversalEventData? responseEvent = (Events.UniversalEventData?)serializer.Deserialize(reader);

                                            bool isError = responseEvent.Event.ContextCollection.Any(c => c.Type.Value.Contains("FailureReason"));
                                            if (isError)
                                            {
                                                string errorMessage = responseEvent.Event.ContextCollection.Where(c => c.Type.Value == "FailureReason").FirstOrDefault().Value;
                                                dataResponse.Status = "ERROR";
                                                MatchCollection matchedError = Regex.Matches(errorMessage, "(Error)(.*)");
                                                string[] groupedErrors = matchedError.GroupBy(x => x.Value).Select(y => y.Key).ToArray();
                                                dataResponse.Message = string.Join(",", groupedErrors);
                                                _logger.LogError("Error: {@Error} Request: {@Request}", dataResponse.Message, history);
                                            }
                                            else
                                            {
                                                string message = "Shipment history created.";

                                                #region UPDATING THE PACKAGE LINE USING TRACKING NUMBER

                                                UniversalShipmentData? universalShipmentData = GetShipmentById(shipmentId);
                                                if (universalShipmentData?.Shipment is not null)
                                                {
                                                    Shipment? shipment = universalShipmentData?.Shipment;

                                                    bool isConsol = shipment.DataContext.DataSourceCollection.Any(ds => ds.Type.Contains("ForwardingConsol"));
                                                    if (isConsol)
                                                    {
                                                        shipment = shipment.SubShipmentCollection.Where(s => s.DataContext.DataSourceCollection.Any(k => k.Key == shipmentId)).FirstOrDefault();
                                                        if(shipment == null)
                                                        {
                                                            shipment = universalShipmentData?.Shipment;
                                                        }
                                                    }

                                                    if (actionTypeObj is not null)
                                                    {
                                                        PackingLine? packingLineObject = shipment.PackingLineCollection?.PackingLine?.Where(p => p?.ReferenceNumber == history.TrackingNumber).FirstOrDefault();
                                                        if (packingLineObject is not null)
                                                        {
                                                            // Adding new customized field to packing line
                                                            if (packingLineObject.CustomizedFieldCollection is null)
                                                            {
                                                                List<CustomizedField> customizedFields = new List<CustomizedField>();
                                                                CustomizedField customizedField = new CustomizedField();
                                                                customizedField.DataType = CustomizedFieldDataType.String;
                                                                customizedField.Key = actionTypeObj.CWCode;
                                                                customizedField.Value = history.HistoryDate.ToString();
                                                                customizedFields.Add(customizedField);
                                                                packingLineObject.CustomizedFieldCollection = customizedFields.ToArray();
                                                            }
                                                            // Updating existing packing line
                                                            else
                                                            {

                                                                var updatePackLine = packingLineObject
                                                                    .CustomizedFieldCollection
                                                                    .Where(c => c.Key == actionTypeObj.CWCode)
                                                                    .FirstOrDefault();
                                                                if (updatePackLine is not null)
                                                                {
                                                                    packingLineObject
                                                                    .CustomizedFieldCollection
                                                                    .Where(c => c.Key == actionTypeObj.CWCode)
                                                                    .FirstOrDefault().Value = history.HistoryDate;

                                                                }
                                                                    
                                                            }
                                                        }
                                                        else
                                                        {
                                                            message += "Tracking Number " + history.TrackingNumber + " can't find.";
                                                        }
                                                        string universalShipmentDataXml = Utilities.Serialize(universalShipmentData);
                                                        universalShipmentDataXml = universalShipmentDataXml.Replace("DataSource", "DataTarget");
                                                        var universalShipmentDataResponse = eAdaptor.Services.SendToCargowise(universalShipmentDataXml, _configuration.URI, _configuration.Username, _configuration.Password);
                                                        if (universalShipmentDataResponse.Status == "SUCCESS")
                                                        {
                                                            message += "Successfully updated the packing item.";
                                                        }
                                                        else
                                                        {
                                                            message += "Unable to update the packing line with the traking number " + history.TrackingNumber;
                                                        }
                                                    }

                                                }

                                                #endregion

                                                dataResponse.Status = "SUCCESS";
                                                dataResponse.Message = message;
                                                _logger.LogInformation("Success: {@Success} Request: {@Request}", message, history);
                                            }
                                        }
                                    }
                                    else
                                    {
                                        string errorMessage = documentResponse.Data.Data.FirstChild.InnerText;
                                        dataResponse.Status = documentResponse.Status;
                                        MatchCollection matchedError = Regex.Matches(errorMessage, "(Error)(.*)");
                                        string[] groupedErrors = matchedError.GroupBy(x => x.Value).Select(y => y.Key).ToArray();
                                        dataResponse.Message = string.Join(",", groupedErrors);
                                        _logger.LogError("Error: {@Error} Request: {@Request}", dataResponse.Message, history);
                                    }
                                }
                                else
                                {
                                    dataResponse.Status = "ERROR";
                                    dataResponse.Message = "Please provide a valid Server ID or Site ID.";
                                    _logger.LogError("Error: {@Error} Request: {@Request}", dataResponse.Message, history);
                                }

                            }
                            else
                            {
                                dataResponse.Status = "NOTFOUND";
                                dataResponse.Message = String.Format("{0} Not Found.", history.HawbNumber);
                                _logger.LogError("Error: {@Error} Request: {@Request}", dataResponse.Message, history);
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
                    catch (Exception ex)
                    {
                        dataResponse.Status = "ERROR";
                        dataResponse.Message = ex.Message;
                        dataResponses.Add(dataResponse);
                        _logger.LogError("Error: {@Error} Request: {@Request}", dataResponse.Message, history);
                        continue;
                    }
                }
                return Ok(dataResponses);
            }
            catch (Exception ex)
            {
                Response dataResponse = new Response();
                dataResponse.Status = "ERROR";
                dataResponse.Message = ex.Message;
                dataResponses.Add(dataResponse);
                _logger.LogError("Error: {@Error}", dataResponse.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, dataResponses);
            }
        }
        #endregion

        #region GET SHIPMENT DETAILS BY SHIPMENT ID
        public UniversalShipmentData? GetShipmentById(string shipmentId)
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
                string outerXml = shipmentRequestResponse.Data.Data.OuterXml;
                string replace = "<Date><Type>DeliveryDueDate</Type><IsEstimate>false</IsEstimate><Value></Value></Date>";
                outerXml = outerXml.Replace(replace, "");
                if (!outerXml.Contains("MessageNumberCollection"))
                {
                    using (var reader = new StringReader(outerXml))
                    {
                        var serializer = new XmlSerializer(typeof(UniversalShipmentData));
                        response = (UniversalShipmentData?)serializer.Deserialize(reader);
                    }
                }
            }

            return response;
        }
        #endregion

        #region GET SHIPMENT NUMBER USING HOUSE BILL NUMBER
        public string? GetShipmentNumberByHawb(string hawb)
        {
            string? shipmentNumber = null;

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
                    Events.UniversalEventData? eventResponse = (Events.UniversalEventData?)serializer.Deserialize(reader);
                    shipmentNumber = eventResponse?.Event?.DataContext?.DataSourceCollection?.Where(d => d.Type == "ForwardingShipment")?.FirstOrDefault()?.Key;
                }
            }

            return shipmentNumber;
        }
        #endregion

        #region GET FILES FROM SFTP FOLDER (FOR TRANSPORT BOOKING)
        public static void GetFilesFromSFTP(string hostname, string username, string password, string remoteFolder, string localFolder)
        {

            WinSCP.SessionOptions sessionOptions = new WinSCP.SessionOptions
            {
                Protocol = Protocol.Sftp,
                HostName = hostname,
                UserName = username,
                Password = password,
                SshHostKeyFingerprint = "ssh-ed25519 255 faw0PNQCsw3K8cO5TdV7F8MgCOPLXNgmTLvvBl+lbYw"

            };
            using (Session session = new Session())
            {
                // Connect
                session.Open(sessionOptions);
                session.GetFilesToDirectory(remoteFolder, localFolder, "*.xml>=180S").Check();
            }
        }
        #endregion

        public static void GetFilesFromLocalServer(string inboundFolder,string outboundFolder)
        {
            DateTime thresholdValue = DateTime.Now.AddMinutes(-3);
            DirectoryInfo directoryInfo = new DirectoryInfo(inboundFolder);
            FileInfo[] files = directoryInfo.GetFiles().Where(i => i.LastWriteTime >= thresholdValue).ToArray();
            foreach (FileInfo file in files)
            {
                try
                {
                    string outputPath = Path.Combine(outboundFolder, file.Name);
                    System.IO.File.Copy(file.FullName, outputPath,true);
                }
                catch (Exception ex)
                {
                    continue;
                }

            }
        }

        #region MOVE FILE FROM SFTP FOLDER
        public static void MoveFileFTP(string hostname, string username, string password, string sourcePath, string destinationPath)
        {

            WinSCP.SessionOptions sessionOptions = new WinSCP.SessionOptions
            {
                Protocol = Protocol.Sftp,
                HostName = hostname,
                UserName = username,
                Password = password,
                SshHostKeyFingerprint = "ssh-ed25519 255 faw0PNQCsw3K8cO5TdV7F8MgCOPLXNgmTLvvBl+lbYw"

            };
            using (Session session = new Session())
            {
                // Connect
                session.Open(sessionOptions);
                session.MoveFile(sourcePath, destinationPath);
            }
        }
        #endregion

        #region SEARCH ORGANIZATION WITH LSC CODE
        public OrganizationData? SearchOrgWithRegNo(string regNo)
        {
            OrganizationData? organizationData = new OrganizationData();

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
                    NativeOrganization.Native? result = (NativeOrganization.Native?)serializer.Deserialize(reader);
                    string organization = result.Body.Any[0].OuterXml;
                    using (TextReader reader2 = new StringReader(organization))
                    {
                        var serializer2 = new XmlSerializer(typeof(OrganizationData));
                        organizationData = (OrganizationData?)serializer2.Deserialize(reader2);
                    }
                }
            }
            return organizationData;
        }
        #endregion

        #region CREATE ORGANIZATION
        public string CreateOrganization(Organization organization)
        {
            string? organizationCode = "";

            NativeOrganization.Native native = new NativeOrganization.Native();

            #region HEADER
            NativeHeader header = new NativeHeader();
            NativeOrganization.DataContext dataContext = new NativeOrganization.DataContext();
            NativeOrganization.Staff staff = new NativeOrganization.Staff();
            staff.Code = "CW1";
            dataContext.EventUser = staff;
            header.DataContext = dataContext;
            native.Header = header;
            #endregion

            NativeOrganization.NativeBody body = new NativeOrganization.NativeBody();
            OrganizationData organizationData = new OrganizationData();

            #region BASIC
            NativeOrganization.NativeOrganization nativeOrganization = new NativeOrganization.NativeOrganization();
            nativeOrganization.ActionSpecified = true;
            nativeOrganization.Action = NativeOrganization.Action.INSERT;
            nativeOrganization.FullName = organization.CompanyName;
            nativeOrganization.Language = "EN";
            nativeOrganization.IsConsigneeSpecified = organization.isConsignee;
            nativeOrganization.IsConsignee = organization.isConsignee;
            nativeOrganization.IsConsignorSpecified = organization.isConsignor;
            nativeOrganization.IsConsignor = organization.isConsignor;
            #endregion

            #region ADDRESS
            NativeOrganizationOrgAddress nativeOrgAddress = new NativeOrganizationOrgAddress();
            List<NativeOrganizationOrgAddress> nativeOrgAddresses = new List<NativeOrganizationOrgAddress>();
            nativeOrgAddress.ActionSpecified = true;
            nativeOrgAddress.Action = NativeOrganization.Action.INSERT;
            nativeOrgAddress.Address1 = organization.Address1;
            nativeOrgAddress.Address2 = organization.Address2;
            nativeOrgAddress.AdditionalAddressInformation = organization?.Address3;

            
            List<NativeOrganizationOrgAddressOrgAddressAdditionalInfo> additionalInfoAddresses = new List<NativeOrganizationOrgAddressOrgAddressAdditionalInfo>();
            if (organization.Address3 != null && organization.Address3 != "")
            {
                NativeOrganizationOrgAddressOrgAddressAdditionalInfo additionalInfoAddress3 = new NativeOrganizationOrgAddressOrgAddressAdditionalInfo();
                additionalInfoAddress3.ActionSpecified = true;
                additionalInfoAddress3.Action = NativeOrganization.Action.INSERT;
                additionalInfoAddress3.IsPrimarySpecified = true;
                additionalInfoAddress3.IsPrimary = true;
                additionalInfoAddress3.AdditionalInfo = organization?.Address3;
                additionalInfoAddresses.Add(additionalInfoAddress3);
            }
            if (organization.Address4 != null && organization.Address4 != "")
            {
                NativeOrganizationOrgAddressOrgAddressAdditionalInfo additionalInfoAddress4 = new NativeOrganizationOrgAddressOrgAddressAdditionalInfo();
                additionalInfoAddress4.ActionSpecified = true;
                additionalInfoAddress4.Action = NativeOrganization.Action.INSERT;
                additionalInfoAddress4.AdditionalInfo = organization?.Address4;
                additionalInfoAddresses.Add(additionalInfoAddress4);
            }
            nativeOrgAddress.OrgAddressAdditionalInfoCollection = additionalInfoAddresses.ToArray();

            nativeOrgAddress.City = organization.City;
            nativeOrgAddress.PostCode = organization.Postcode;
            nativeOrgAddress.State = organization.ProviceCode;
            NativeOrganizationOrgAddressCountryCode nativeOrgCountryCode = new NativeOrganizationOrgAddressCountryCode();
            nativeOrgCountryCode.TableName = "RefCountry";
            nativeOrgCountryCode.Code = organization.Country;
            nativeOrgAddress.CountryCode = nativeOrgCountryCode;

            nativeOrgAddress.Phone = organization.Phone;
            nativeOrgAddress.Mobile = organization.Mobile;
            nativeOrgAddress.Fax = organization.Fax;
            nativeOrgAddress.Email = organization.Email;
            nativeOrgAddress.Language = "EN";
            nativeOrgAddress.FCLEquipmentNeeded = "ANY";
            nativeOrgAddress.LCLEquipmentNeeded = "ANY";
            nativeOrgAddress.AIREquipmentNeeded = "ANY";
            List<NativeOrganizationOrgAddressOrgAddressCapability> nativeOrgAddressCapabilities = new List<NativeOrganizationOrgAddressOrgAddressCapability>();
            NativeOrganizationOrgAddressOrgAddressCapability nativeOrgAddressCapability = new NativeOrganizationOrgAddressOrgAddressCapability();
            nativeOrgAddressCapability.ActionSpecified = true;
            nativeOrgAddressCapability.Action = NativeOrganization.Action.INSERT;
            nativeOrgAddressCapability.IsMainAddressSpecified = true;
            nativeOrgAddressCapability.IsMainAddress = true;
            nativeOrgAddressCapability.AddressType = "OFC";
            nativeOrgAddressCapabilities.Add(nativeOrgAddressCapability);
            nativeOrgAddress.OrgAddressCapabilityCollection = nativeOrgAddressCapabilities.ToArray();

            nativeOrgAddresses.Add(nativeOrgAddress);
            nativeOrganization.OrgAddressCollection = nativeOrgAddresses.ToArray();
            #endregion

            #region PORT
            NativeOrganizationClosestPort nativeOrganizationClosestPort = new NativeOrganizationClosestPort();
            nativeOrganizationClosestPort.TableName = "RefUNLOCO";
            nativeOrganizationClosestPort.Code = organization.Unloco;
            nativeOrganization.ClosestPort = nativeOrganizationClosestPort;
            #endregion

            #region RESGISTRATION
            List<NativeOrganizationOrgCusCode> registrationCusCodes = new List<NativeOrganizationOrgCusCode>();

            NativeOrganizationOrgCusCode globalCustomerRegistrationCusCode = new NativeOrganizationOrgCusCode();
            globalCustomerRegistrationCusCode.ActionSpecified = true;
            globalCustomerRegistrationCusCode.Action = NativeOrganization.Action.INSERT;
            globalCustomerRegistrationCusCode.CodeType = "LSC";
            globalCustomerRegistrationCusCode.CustomsRegNo = organization.RegistrationNumber;
            NativeOrganizationOrgCusCodeCodeCountry cusCodeCountry = new NativeOrganizationOrgCusCodeCodeCountry();
            cusCodeCountry.Code = organization.Country;
            globalCustomerRegistrationCusCode.CodeCountry = cusCodeCountry;
            registrationCusCodes.Add(globalCustomerRegistrationCusCode);

            NativeOrganizationOrgCusCode eoriRegistrationCusCode = new NativeOrganizationOrgCusCode();
            eoriRegistrationCusCode.ActionSpecified = !string.IsNullOrEmpty(organization?.Eori);
            eoriRegistrationCusCode.Action = NativeOrganization.Action.INSERT;
            eoriRegistrationCusCode.CodeType = "EOR";
            eoriRegistrationCusCode.CustomsRegNo = organization.Eori;
            eoriRegistrationCusCode.CodeCountry = cusCodeCountry;
            registrationCusCodes.Add(eoriRegistrationCusCode);

            nativeOrganization.OrgCusCodeCollection = registrationCusCodes.ToArray();
            #endregion

            organizationData.OrgHeader = nativeOrganization;

            var serilaziedBody = Utilities.SerializeToXmlElement(organizationData);
            List<XmlElement> xmlElements = new List<XmlElement>();
            xmlElements.Add(serilaziedBody);
            body.Any = xmlElements.ToArray();
            native.Body = body;
            string xml = Utilities.Serialize(native);

            var response = eAdaptor.Services.SendToCargowise(xml, _configuration.URI, _configuration.Username, _configuration.Password);


            if (response.Status == "SUCCESS" && response.Data.Status == "PRS" && response.Data.ProcessingLog != null)
            {
                using (TextReader reader = new StringReader(response.Data.Data.OuterXml))
                {
                    var serializer = new XmlSerializer(typeof(Events.UniversalEventData));
                    Events.UniversalEventData? result = (Events.UniversalEventData?)serializer.Deserialize(reader);
                    organizationCode = result.Event.ContextCollection.Where(c => c.Type.Value == "EntityLocalCode").FirstOrDefault()?.Value;
                }
            }

            return organizationCode;
        }
        #endregion

        public class Organization
        {
            public bool isConsignor { get; set; } = false;
            public bool isConsignee { get; set; } = false;
            public string? CompanyName { get; set; }
            public string? Address1 { get; set; }
            public string? Address2 { get; set; }
            public string? Address3 { get; set; }
            public string? Address4 { get; set; }
            public string? Unloco { get; set; }
            public string? City { get; set; }
            public string? ProviceCode { get; set; }
            public string? Postcode { get; set; }
            public string? Country { get; set; }
            public string? Contact { get; set; }
            public string? Mobile { get; set; }
            public string? Phone { get; set; }
            public string? Email { get; set; }
            public string? Fax { get; set; }
            public string? RegistrationNumber { get; set; }
            public string? Eori { get; set; }

        }
    }
}
