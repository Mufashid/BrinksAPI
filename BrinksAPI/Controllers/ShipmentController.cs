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
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;

using WinSCP;

namespace BrinksAPI.Controllers
{
    [Authorize]
    //[ApiController]
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

                Random random = new Random();
                long originShipmentId = random.Next(30000000, 39999999);// 30 million

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

                cwShipment.DataContext = dataContext;
                #endregion

                #region PORT
                var loadingPort = _context.sites.Where(s => s.SiteCode.ToString() == shipment.pickupSiteCode).FirstOrDefault();
                UNLOCO portOfLoading = new UNLOCO();
                portOfLoading.Code = loadingPort?.Country + loadingPort?.Airport;
                cwShipment.PortOfOrigin = portOfLoading;
                cwShipment.PortOfLoading = portOfLoading;

                var dischargePort = _context.sites.Where(s => s.SiteCode.ToString() == shipment.deliverySiteCode).FirstOrDefault();
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
                OrganizationData shipperOrganizationData = SearchOrgWithRegNo(shipment.shipperGlobalCustomerCode);
                OrganizationAddress shipperAddress = new OrganizationAddress();
                shipperAddress.AddressType = "ConsignorDocumentaryAddress";
                if (shipperOrganizationData.OrgHeader == null)
                {
                    shipperAddress.AddressOverrideSpecified = true;
                    shipperAddress.AddressOverride = true;

                    shipperAddress.CompanyName = shipment.shipperName;
                    shipperAddress.Address1 = shipment.shipperAddress1;
                    shipperAddress.Address2 = shipment.shipperAddress2;
                    shipperAddress.AdditionalAddressInformation = shipment.shipperAddress3 + shipment.shipperAddress4;
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
                    consigneeAddress.AddressOverrideSpecified = true;
                    consigneeAddress.AddressOverride = true;
                    consigneeAddress.CompanyName = shipment.consigneeName;
                    consigneeAddress.Address1 = shipment.consigneeAddress1;
                    consigneeAddress.Address2 = shipment.consigneeAddress2;
                    consigneeAddress.AdditionalAddressInformation = shipment.consigneeAddress3 + shipment.consigneeAddress4;
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

                }
                else
                {
                    consigneeAddress.OrganizationCode = consigneeOrganizationData.OrgHeader.Code;
                }
                organizationAddresses.Add(consigneeAddress);
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

                    //packingLine.OutturnedWeightSpecified = true;
                    //packingLine.OutturnedLengthSpecified = true;
                    //packingLine.OutturnedWidthSpecified = true;
                    //packingLine.OutturnedHeightSpecified = true;
                    //packingLine.OutturnedWeight = Convert.ToDecimal(shipmentItem.dimWeight);
                    //packingLine.OutturnedLength = Convert.ToDecimal(shipmentItem.dimLength);
                    //packingLine.OutturnedWidth = Convert.ToDecimal(shipmentItem.dimWidth);
                    //packingLine.OutturnedHeight = Convert.ToDecimal(shipmentItem.dimHeight);

                    packingLine.ReferenceNumber = shipmentItem.barcode;
                    packingLine.MarksAndNos = shipmentItem.showSealNumber;

                    Country countryOforigin = new Country();
                    countryOforigin.Code = shipmentItem.originCountry;
                    packingLine.CountryOfOrigin = countryOforigin;


                    #region CUSTOMIZED FIELDS COLLECTION
                    List<CustomizedField> shipmentItemCustomizedFields = new List<CustomizedField>();

                    CustomizedField pickupDateCF = new CustomizedField();
                    pickupDateCF.Key = "pickup_date";
                    pickupDateCF.Value = shipmentItem.puDate;
                    shipmentItemCustomizedFields.Add(pickupDateCF);

                    CustomizedField deliveryDateCF = new CustomizedField();
                    deliveryDateCF.Key = "delivery_date";
                    deliveryDateCF.Value = shipmentItem.dlvDate;
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
                decimal totalChargableWeight = Convert.ToDecimal(shipment?.shipmentItems?.Sum(i => i.chargeableWeight));
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

                string showFlag = shipment.showFlag == "N" ? "No" : "S1";
                CustomizedField showFlagCF = new CustomizedField();
                showFlagCF.DataType = CustomizedFieldDataType.String;
                showFlagCF.Key = "Show";
                showFlagCF.Value = showFlag;
                shipmentCustomizedFields.Add(showFlagCF);

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
                originShipmentIdCF.Value = originShipmentId.ToString();
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
                #endregion

                universalShipmentData.Shipment = cwShipment;
                successMessage = shipmentId == null ? "Shipment Created in CW. " : "Shipment Updated in CW. ";
                string xml = Utilities.Serialize(universalShipmentData);
                var documentResponse = eAdaptor.Services.SendToCargowise(xml, _configuration.URI, _configuration.Username, _configuration.Password);
                if (documentResponse.Status == "ERROR")
                {
                    string errorMessage = documentResponse.Data.Data.FirstChild.InnerText.Replace("Error - ", "").Replace("Warning - ", "");
                    dataResponse.Status = documentResponse.Status;
                    dataResponse.Message = errorMessage;

                    _logger.LogError("Error: {@Error} Request: {@Request}", errorMessage, shipment);
                    return Ok(dataResponse);
                }
                else
                {

                    #region TRANSPORT BOOKING
                    string responseShipmentId = Utilities.ReadUniversalEvent(documentResponse.Data.Data.OuterXml).Event.DataContext.DataSourceCollection.Where(s => s.Type == "ForwardingShipment").FirstOrDefault().Key;
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
                        GetFilesFromSFTP(_configuration.SftpUri, _configuration.SftpUsername, _configuration.SftpPassword, _configuration.SftpOutboundFolder, localDirectory);
                        string[] filePaths = Directory.GetFiles(localDirectory, "*.xml", SearchOption.TopDirectoryOnly);
                        foreach (var file in filePaths)
                        {
                            try
                            {
                                var trasportBookingXML = System.IO.File.ReadAllText(file);
                                UniversalShipmentData transportBookingData = Utilities.ReadUniversalShipment(Utilities.getElementFromXML(trasportBookingXML, "Body"));

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

                                        // Backup to server
                                        string sourcePath = Path.Join(_configuration.SftpOutboundFolder, Path.GetFileName(file));
                                        //string destinationPath = Path.Join(_configuration.SftpBackupFolder, Path.GetFileName(file));
                                        MoveFileFTP(_configuration.SftpUri, _configuration.SftpUsername, _configuration.SftpPassword, sourcePath, _configuration.SftpBackupFolder);
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
                        #endregion

                        tbShipment.DataContext = tbDataContext;
                        tbShipment.ShipmentType = new CodeDescriptionPair { Code = "BKG" };

                        ShipmentPackingLineCollection transportPackingLineCollection = new ShipmentPackingLineCollection();
                        List<PackingLine> transportPackings = new List<PackingLine>();
                        List<ShipmentInstruction> shipmentInstructions = new List<ShipmentInstruction>();

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

                            //packingLine.LengthSpecified = true;
                            //packingLine.WeightSpecified = true;
                            //packingLine.WidthSpecified = true;
                            //packingLine.HeightSpecified = true;
                            //packingLine.Length = Convert.ToDecimal(shipmentItem.dimLength);
                            //packingLine.Weight = Convert.ToDecimal(shipmentItem.dimWeight);
                            //packingLine.Width = Convert.ToDecimal(shipmentItem.dimWidth);
                            //packingLine.Height = Convert.ToDecimal(shipmentItem.dimLength);

                            packingLine.OutturnedWeightSpecified = true;
                            packingLine.OutturnedLengthSpecified = true;
                            packingLine.OutturnedWidthSpecified = true;
                            packingLine.OutturnedHeightSpecified = true;
                            packingLine.OutturnedWeight = Convert.ToDecimal(shipmentItem.dimWeight);
                            packingLine.OutturnedLength = Convert.ToDecimal(shipmentItem.dimLength);
                            packingLine.OutturnedWidth = Convert.ToDecimal(shipmentItem.dimWidth);
                            packingLine.OutturnedHeight = Convert.ToDecimal(shipmentItem.dimHeight);

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
                            ShipmentInstruction tbPickUpShipmentInstruction = new ShipmentInstruction();

                            tbPickUpShipmentInstruction.SequenceSpecified = true;
                            tbPickUpShipmentInstruction.Sequence = instructionCount;
                            tbPickUpShipmentInstruction.Type = new CodeDescriptionPair() { Code = "PIC" };

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
                            tbPickUpShipmentInstruction.Address = pickupAddress;

                            List<ShipmentInstructionInstructionPackingLineLink> pickupPackinglineLinks = new List<ShipmentInstructionInstructionPackingLineLink>();
                            ShipmentInstructionInstructionPackingLineLink pickupPackinglineLink = new ShipmentInstructionInstructionPackingLineLink();
                            pickupPackinglineLink.PackingLineLinkSpecified = true;
                            pickupPackinglineLink.PackingLineLink = packlineCount;
                            pickupPackinglineLink.QuantitySpecified = true;
                            pickupPackinglineLink.Quantity = packlineCount + 1;

                            pickupPackinglineLinks.Add(pickupPackinglineLink);
                            tbPickUpShipmentInstruction.InstructionPackingLineLinkCollection = pickupPackinglineLinks.ToArray();

                            shipmentInstructions.Add(tbPickUpShipmentInstruction);
                            instructionCount++;

                            ShipmentInstruction tbDeliveryShipmentInstruction = new ShipmentInstruction();

                            tbDeliveryShipmentInstruction.SequenceSpecified = true;
                            tbDeliveryShipmentInstruction.Sequence = instructionCount;
                            tbDeliveryShipmentInstruction.Type = new CodeDescriptionPair() { Code = "DLV" };

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

                            List<RegistrationNumber> deliveryRegistrations = new List<RegistrationNumber>();
                            RegistrationNumber deliveryRegistration = new RegistrationNumber();
                            deliveryRegistration.Type = new RegistrationNumberType() { Code = "LSC" };
                            deliveryRegistration.CountryOfIssue = new Country() { Code = shipmentItem.dlvCountryCode };
                            deliveryRegistration.Value = shipmentItem.dlvGlobalCustomerCode;
                            deliveryRegistrations.Add(deliveryRegistration);
                            deliveryAddress.RegistrationNumberCollection = deliveryRegistrations.ToArray();
                            tbDeliveryShipmentInstruction.Address = deliveryAddress;

                            List<ShipmentInstructionInstructionPackingLineLink> deliveryPackinglineLinks = new List<ShipmentInstructionInstructionPackingLineLink>();
                            ShipmentInstructionInstructionPackingLineLink deliveryPackinglineLink = new ShipmentInstructionInstructionPackingLineLink();
                            deliveryPackinglineLink.PackingLineLinkSpecified = true;
                            deliveryPackinglineLink.PackingLineLink = packlineCount;
                            deliveryPackinglineLink.QuantitySpecified = true;
                            deliveryPackinglineLink.Quantity = packlineCount + 1;
                            deliveryPackinglineLinks.Add(deliveryPackinglineLink);
                            tbDeliveryShipmentInstruction.InstructionPackingLineLinkCollection = deliveryPackinglineLinks.ToArray();
                            shipmentInstructions.Add(tbDeliveryShipmentInstruction);
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
                        tbSubshipment.InstructionCollection = shipmentInstructions.ToArray();
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
                        _logger.LogInformation(successMessage);
                    }
                    #endregion

                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Error: {@Error} Request: {@Request}", ex.Message, shipment);
                dataResponse.Status = "ERROR";
                dataResponse.Message = successMessage + ex.Message;
                return StatusCode(StatusCodes.Status500InternalServerError, dataResponse);
            }
            return Ok(dataResponse);
        }
        #endregion

        #region SHIPMENT HISTORY API
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
                                    companyCode = site?.CompanyCode;
                                }
                                else
                                {
                                    actionType = "Picked up date";
                                    eventType = "Z00";
                                    companyCode = site?.CompanyCode;
                                }
                                if (history.SiteCode != null)
                                {
                                    int siteCode = Int32.Parse(history.SiteCode);
                                    site = _context.sites.Where(s => s.SiteCode == siteCode).FirstOrDefault();
                                    companyCode = site?.CompanyCode;
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
                                        Events.UniversalEventData? responseEvent = (Events.UniversalEventData)serializer.Deserialize(reader);

                                        bool isError = responseEvent.Event.ContextCollection.Any(c => c.Type.Value.Contains("FailureReason"));
                                        if (isError)
                                        {
                                            string errorMessage = responseEvent.Event.ContextCollection
                                                .Where(c => c.Type.Value == "FailureReason")
                                                .FirstOrDefault().Value
                                                .Replace("Error - ", "")
                                                .Replace("Warning - ", "");
                                            
                                            if (errorMessage.Contains("No Module found a Business Entity to link this Universal Event to."))
                                            {
                                                dataResponse.Status = "NOTFOUND";
                                                dataResponse.Message = String.Format("{0} Not Found.", history.HawbNumber);
                                            }

                                            else
                                            {
                                                dataResponse.Status = "ERROR";
                                                dataResponse.Message = errorMessage;
                                            }
                                        }
                                        else
                                        {
                                            string message = "Shipment history created.";
                                            string shipmentId = responseEvent.Event.DataContext.DataSourceCollection.Where(d => d.Type == "ForwardingShipment").FirstOrDefault().Key;
                                            if (history.TrackingNumber != null && shipmentId != null)
                                            {
                                                UniversalShipmentData universalShipmentData = GetShipmentById(shipmentId);
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
                                                            message += "Tracking Number " + history.TrackingNumber + " can't find.";
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
                                                            message += "Tracking Number " + history.TrackingNumber + " can't find.";
                                                        else
                                                        {
                                                            if (packingLineObject.CustomizedFieldCollection is null)
                                                            {
                                                                
                                                                List<CustomizedField> customizedFields = new List<CustomizedField>();
                                                                CustomizedField customizedField = new CustomizedField();
                                                                customizedField.DataType = CustomizedFieldDataType.String;
                                                                customizedField.Key = actionType;
                                                                customizedField.Value = history.HistoryDate.ToString();
                                                                customizedFields.Add(customizedField);
                                                                packingLineObject.CustomizedFieldCollection = customizedFields.ToArray();
                                                            }
                                                            else
                                                            {
                                                                packingLineObject
                                                                    .CustomizedFieldCollection
                                                                    .Where(c => c.Key == actionType)
                                                                    .FirstOrDefault()
                                                                    .Value = history.HistoryDate;
                                                            }
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
                using (var reader = new StringReader(shipmentRequestResponse.Data.Data.OuterXml))
                {
                    var serializer = new XmlSerializer(typeof(UniversalShipmentData));
                    response = (UniversalShipmentData?)serializer.Deserialize(reader);
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

    }
}
