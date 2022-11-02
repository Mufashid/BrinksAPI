using System.Collections;
using System.ComponentModel.DataAnnotations;

namespace BrinksAPI.Models
{
    #region SHIPMENT HISTORY
    public class ShipmentHistory
    {
        public string? RequestId { get; set; }

        [Required]
        [StringLength(16)]
        public string? TrackingNumber { get; set; }
        [StringLength(50)]
        public string? UserId { get; set; }
        [StringLength(5)]
        public string? ActionType { get; set; }
        [StringLength(5)]
        public string? AreaType { get; set; }
        [Required]
        [StringLength(2000)]
        public string? HistoryDetails { get; set; }
        [Required]
        public string? HistoryDate { get; set; }
        [StringLength(4)]
        public string? SiteCode { get; set; }
        [Required]
        [StringLength(5)]
        public string? ServerId { get; set; }
        [StringLength(11)]
        [Required]
        public string? HawbNumber { get; set; }
    }
    #endregion

    #region SHIPMENT
    public class Shipment
    {
    
        [StringLength(5)]
        public string? originServerId { get; set; }

        [RegularExpression(@"^(0|-?\d{0,18}(\.\d{0,2})?)$")]
        public string? originShipmentId { get; set; }

        [StringLength(8)]
        public string? userId { get; set; }
        [Required]
        [StringLength(11)]
        public string? hawbNum { get; set; }
        [Required]
        [StringLength(2)]
        public string? serviceType { get; set; }
        
        [StringLength(30)]
        public string? shippersReference { get; set; }
        [Required]
        [StringLength(20)]
        public string? shipperGlobalCustomerCode { get; set; }
        [Required]
        [StringLength(40)]
        public string? shipperName { get; set; }
        [Required]
        [StringLength(30)]
        public string? shipperAddress1 { get; set; }
        [StringLength(30)]
        public string? shipperAddress2 { get; set; }
        [StringLength(30)]
        public string? shipperAddress3 { get; set; }
        [StringLength(30)]
        public string? shipperAddress4 { get; set; }
        [Required]
        [StringLength(25)]
        public string? shipperCity { get; set; }

        [StringLength(5)]
        public string? shipperProvinceCode { get; set; }

        [StringLength(15)]
        public string? shipperPostalCode { get; set; }
        [Required]
        [StringLength(3)]
        public string? shipperCountryCode { get; set; }
        
        public string? shipperMobileNumber { get; set; }
        [Required]
        [StringLength(22)]
        public string? shipperPhoneNumber { get; set; }
        public string? shipperContactName { get; set; }
        [Required]
        [StringLength(25)]
        public string? consigneeGlobalCustomerCode { get; set; }
        [Required]
        [StringLength(40)]
        public string? consigneeName { get; set; }
        [Required]
        [StringLength(30)]
        public string? consigneeAddress1 { get; set; }
        [StringLength(30)]
        public string? consigneeAddress2 { get; set; }
        [StringLength(30)]
        public string? consigneeAddress3 { get; set; }
        [StringLength(30)]
        public string? consigneeAddress4 { get; set; }
        [Required]
        [StringLength(25)]
        public string? consigneeCity { get; set; }

        [StringLength(5)]
        public string? consigneeProvinceCode { get; set; }

        [StringLength(15)]
        public string? consigneePostalCode { get; set; }
        [Required]
        [StringLength(3)]
        public string? consigneeCountryCode { get; set; }
        public string? consigneeContactName { get; set; }
        [Required]
        [StringLength(22)]
        public string? consigneePhoneNumber { get; set; }
        public string? consigneeMobileNumber { get; set; }
        public string? readyAt { get; set; }
        [Required]
        [StringLength(1)]
        public string? modeOfTransport { get; set; }

        [StringLength(15)]
        public string? itn { get; set; }
        [Required]
        [StringLength(1)]
        public string? userChargesType { get; set; }
        public string? notifyOnPickUpFlag { get; set; }
        public string? notifyOnPickupEmailAddress { get; set; }
        public string? notifyOnExceptionFlag { get; set; }
        public string? notifyOnExceptionEmailAddress { get; set; }
        public string? notifyOnDeliveryFlag { get; set; }
        public string? notifyOnDeliveryEmailAddress { get; set; }
        public string? residentialDeliveryFlag { get; set; }
        public string? puNotes { get; set; }
        public string? dlvNotes { get; set; }
        [Required]
        [StringLength(1)]
        public string? chargesType { get; set; }
        public string? windowPickupFlag { get; set; }
        public string? windowDeliveryFlag { get; set; }
        public List<ShipmentItem>? shipmentItems { get; set; }
        [StringLength(3)]
        public string? deliveryAirportCode { get; set; }
        [StringLength(3)]
        public string? pickupAirportCode { get; set; }

        [StringLength(1)]
        public string? offshore { get; set; }
        public float chargesAmount { get; set; }
        public float userAmount { get; set; }
        public string? dateCreated { get; set; }
        public string? lastUpdated { get; set; }
        public string? notes { get; set; }
        [StringLength(1)]
        public string? showFlag { get; set; }
        [StringLength(1)]
        public string? holdFlag { get; set; }
        [StringLength(1)]
        public string? lowValueFlag { get; set; }

        [StringLength(20)]
        public string? waiverNumber { get; set; }
        [StringLength(1)]
        public string? tracer { get; set; }
        [StringLength(3)]
        public string? uscsStatusCode { get; set; }
        [StringLength(1)]
        public string? amsSentFlag { get; set; }
        public string? amsSentDate { get; set; }
        [StringLength(1)]
        public string? trackingStatus { get; set; }
    }

    public class ShipmentItem
    {
        [StringLength(5)]
        public string? originServerId { get; set; }

        [RegularExpression(@"^(0|-?\d{0,18}(\.\d{0,2})?)$")]
        public string? originShipmentItemId { get; set; }
        [Required]
        [StringLength(16)]
        public string? barcode { get; set; }
        public string? dlvDate { get; set; }
        [Required]
        public string? dlvEstDate { get; set; }
        [Required]
        [StringLength(4)]
        public string? globalCommodityCode { get; set; }
        [Required]
        [StringLength(25)]
        public string? commodityDescription { get; set; }
        [Required]
        public float insuranceLiability { get; set; }
        public float grossWeight { get; set; }
        [Required]
        [StringLength(4)]
        public string? uomCode { get; set; }
        public double uomNetWeight { get; set; }
        [StringLength(45)]
        public string? poNumber { get; set; }
        [StringLength(20)]
        public string? puInvoiceNumber { get; set; }
        public string? contentQuantity { get; set; }
        public float dimLength { get; set; }
        public float dimWidth { get; set; }
        public float dimHeight { get; set; }
        public float dimWeight { get; set; }
        public float? chargableWeight { get; set; }
        public string? dimUOM { get; set; }
        public double customerWeight { get; set; }
        public string? codAmount { get; set; }
        public string? codTypeCode { get; set; }
        [StringLength(15)]
        public string? showSealNumber { get; set; }

        [StringLength(6)]
        public string? packageTypeCd { get; set; }
        [Required]
        public string? puDate { get; set; }
        [Required]
        [StringLength(4)]
        public string? insurCurrencyCode { get; set; }
        public float customsLiability { get; set; }
        [Required]
        public float customsLiabilityUsd { get; set; }
        public string? customsCurrencyCode { get; set; }
        public string? originCountry { get; set; }
        public string? usScheduleBCode { get; set; }
        public string? exportOrigin { get; set; }
        public string? exportLicenseNumber { get; set; }
        public string? exportLicenseValue { get; set; }
        public string? licenseCode { get; set; }
        public string? destinationCustomsCode { get; set; }

        [StringLength(4)]
        public string? customsCode { get; set; }
        [Required]
        [StringLength(20)]
        public string? puGlobalCustomerCode { get; set; }
        [Required]
        [StringLength(40)]
        public string? puName { get; set; }
        [Required]
        [StringLength(40)]
        public string? puContactName { get; set; }
        [Required]
        [StringLength(30)]
        public string? puAddress1 { get; set; }
        [StringLength(30)]
        public string? puAddress2 { get; set; }
        [StringLength(30)]
        public string? puAddress3 { get; set; }
        [StringLength(30)]
        public string? puAddress4 { get; set; }
        [Required]
        [StringLength(25)]
        public string? puCity { get; set; }

        [StringLength(5)]
        public string? puProvinceCode { get; set; }
 
        [StringLength(15)]
        public string? puPostalCode { get; set; }
        [Required]
        [StringLength(3)]
        public string? puCountryCode { get; set; }
        [Required]
        [StringLength(22)]
        public string? puPhoneNumber { get; set; }
        public string? puMobileNumber { get; set; }
        [StringLength(255)]
        public string? puEmailAddress { get; set; }
        public string? puGcc { get; set; }
        [Required]
        [StringLength(25)]
        public string? dlvGlobalCustomerCode { get; set; }
        [Required]
        [StringLength(40)]
        public string? dlvName { get; set; }
 
        [StringLength(40)]
        public string? dlvContactName { get; set; }
        [Required]
        [StringLength(30)]
        public string? dlvAddress1 { get; set; }
        [StringLength(30)]
        public string? dlvAddress2 { get; set; }
        [StringLength(30)]
        public string? dlvAddress3 { get; set; }
        [StringLength(30)]
        public string? dlvAddress4 { get; set; }
        [Required]
        [StringLength(25)]
        public string? dlvCity { get; set; }

        [StringLength(5)]
        public string? dlvProvinceCode { get; set; }
   
        [StringLength(15)]
        public string? dlvPostalCode { get; set; }
        [Required]
        [StringLength(3)]
        public string? dlvCountryCode { get; set; }
        public string? dlvMobileNumber { get; set; }
     
        [StringLength(22)]
        public string? dlvPhoneNumber { get; set; }
        public string? dlvFaxNumber { get; set; }
        [StringLength(255)]
        public string? dlvEmailAddress { get; set; }
        public string? dlvGcc { get; set; }
        [Required]
        public int numberOfItems { get; set; }
        public string? uscsDlvCustomerType { get; set; }
        [Required]
        [StringLength(3)]
        public string? termCode { get; set; }
        public float transportExpense { get; set; }
        public float insuranceExpense { get; set; }
        public string? puInvoiceDate { get; set; }
        [Required]
        public float insuranceLiabilityUsd { get; set; }
        [StringLength(3)]
        public string? uscsStatusCode { get; set; }
        [StringLength(2)]
        public string? amsStatusCode { get; set; }
        public string? amsStatusDate { get; set; }
   
    }
    #endregion
}
