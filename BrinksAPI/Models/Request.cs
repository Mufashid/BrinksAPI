using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace BrinksAPI.Models
{

    #region MultiShipment
    public class Shipper
    {
        public string? name { get; set; }
        public string? address1 { get; set; }
        public string? address2 { get; set; }
        public string? city { get; set; }
        public string? provinceCode { get; set; }
        public string? postalCode { get; set; }
        public string? countryCode { get; set; }
        public string? phoneNumber { get; set; }
    }

    public class Consignee
    {
        public string? name { get; set; }
        public string? address1 { get; set; }
        public string? address2 { get; set; }
        public string? city { get; set; }
        public string? provinceCode { get; set; }
        public string? postalCode { get; set; }
        public string? countryCode { get; set; }
        public string? contactName { get; set; }
        public string? phoneNumber { get; set; }
    }

    public class PickupLocation
    {
        public string? name { get; set; }
        public string? address1 { get; set; }
        public string? address2 { get; set; }
        public string? city { get; set; }
        public string? provinceCode { get; set; }
        public string? postalCode { get; set; }
        public string? countryCode { get; set; }
        public string? contactName { get; set; }
        public string? phoneNumber { get; set; }
    }

    public class DeliveryLocation
    {
        public string? name { get; set; }
        public string? address1 { get; set; }
        public string? address2 { get; set; }
        public string? city { get; set; }
        public string? provinceCode { get; set; }
        public string? postalCode { get; set; }
        public string? countryCode { get; set; }
        public string? contactName { get; set; }
        public string? phoneNumber { get; set; }
    }

    public class ShipmentLabelRequest
    {
        public string? labelType { get; set; }
        public string? printOption { get; set; }
    }

    public class ShipmentItem
    {
        public string? insuranceLiabilityCurrencyCode { get; set; }
        public double? insuranceLiabilityValue { get; set; }
        public string? insuranceLiabilityValueInUSD { get; set; }
        public int? commodityId { get; set; }
        public double? packageWeight { get; set; }
        public string? packageWeightUOM { get; set; }
        public double? netWeight { get; set; }
        public string? netWeightUOM { get; set; }
        public string? packageTypeCode { get; set; }
        public string? contentQuantity { get; set; }
        public string? licenseCode { get; set; }
        public string? uscsScheduleBCode { get; set; }
        public string? termCode { get; set; }

        public string? requestLabelImage { get; set; }
        [Required]
        public PickupLocation? pickupLocation { get; set; }
        [Required]
        public DeliveryLocation? deliveryLocation { get; set; }
    }

    public class ShipInfo
    {
        public string? billingType { get; set; }
        [Required]
        public Shipper? shipper { get; set; }
        [Required]
        public Consignee? consignee { get; set; }

        public PickupLocation? pickupLocation { get; set; }

        public DeliveryLocation? deliveryLocation { get; set; }
        public string? serviceType { get; set; }
        public string? requestLabelImage { get; set; }
        public string? modeOfTransportCode { get; set; }
        public string? localServiceType { get; set; }
        public string? pickupDate { get; set; }
        public string? pickupTime { get; set; }
        public string? deliveryDate { get; set; }
        public string? deliveryTime { get; set; }
        public string? reference { get; set; }
        public string? shipmentNotes { get; set; }
        public string? pickupNotes { get; set; }
        public string? deliveryNotes { get; set; }
        public ShipmentLabelRequest? shipmentLabelRequest { get; set; }
        [Required]
        public List<ShipmentItem>? shipmentItems { get; set; }
    }

    public class BrinksMultipleShipment
    {
        [Display(Name ="Request ID")]
        public string? requestId { get; set; }
        [Required]
        public ShipInfo? shipInfo { get; set; }
    }
    #endregion

    public class BrinksSingleShipment
    {
        public class Shipper
        {
            public string? name { get; set; }
            public string? address1 { get; set; }
            public string? address2 { get; set; }
            public string? city { get; set; }
            public string? provinceCode { get; set; }
            public string? postalCode { get; set; }
            public string? countryCode { get; set; }
            public string? phoneNumber { get; set; }
        }

        public class Consignee
        {
            public string? name { get; set; }
            public string? address1 { get; set; }
            public string? address2 { get; set; }
            public string? city { get; set; }
            public string? provinceCode { get; set; }
            public string? postalCode { get; set; }
            public string? countryCode { get; set; }
            public string? contactName { get; set; }
            public string? phoneNumber { get; set; }
        }

        public class PickupLocation
        {
            public string? name { get; set; }
            public string? address1 { get; set; }
            public string? address2 { get; set; }
            public string? city { get; set; }
            public string? provinceCode { get; set; }
            public string? postalCode { get; set; }
            public string? countryCode { get; set; }
            public string? contactName { get; set; }
            public string? phoneNumber { get; set; }
        }

        public class DeliveryLocation
        {
            public string? name { get; set; }
            public string? address1 { get; set; }
            public string? address2 { get; set; }
            public string? city { get; set; }
            public string? provinceCode { get; set; }
            public string? postalCode { get; set; }
            public string? countryCode { get; set; }
            public string? contactName { get; set; }
            public string? phoneNumber { get; set; }
        }

        public class ShipmentLabelRequest
        {
            public string? labelType { get; set; }
            public string? printOption { get; set; }
        }

        public class ShipmentItem
        {
            public string? insuranceLiabilityCurrencyCode { get; set; }
            public string? insuranceLiabilityValue { get; set; }
            public string? insuranceLiabilityValueInUSD { get; set; }
            public string? commodityId { get; set; }
            public string? packageWeight { get; set; }
            public string? packageWeightUOM { get; set; }
            public string? netWeight { get; set; }
            public string? netWeightUOM { get; set; }
            public string? packageTypeCode { get; set; }
            public string? contentQuantity { get; set; }
            public string? licenseCode { get; set; }
            public string? uscsScheduleBCode { get; set; }
            public string? termCode { get; set; }
        }

        public class ShipInfo
        {
            public string? billingType { get; set; }
            public Shipper? shipper { get; set; }
            public Consignee? consignee { get; set; }
            public PickupLocation? pickupLocation { get; set; }
            public DeliveryLocation? deliveryLocation { get; set; }
            public string? serviceType { get; set; }
            public string? requestLabelImage { get; set; }
            public string? modeOfTransportCode { get; set; }
            public string? localServiceType { get; set; }
            public string? pickupDate { get; set; }
            public string? pickupTime { get; set; }
            public string? deliveryDate { get; set; }
            public string? deliveryTime { get; set; }
            public string? reference { get; set; }
            public string? shipmentNotes { get; set; }
            public string? pickupNotes { get; set; }
            public string? deliveryNotes { get; set; }
            public ShipmentLabelRequest? shipmentLabelRequest { get; set; }
            public List<ShipmentItem>? shipmentItems { get; set; }
        }

        public class Root
        {
            public string? requestId { get; set; }
            public ShipInfo? shipInfo { get; set; }
        }


    }

}
