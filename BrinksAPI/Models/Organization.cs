using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Reflection;

namespace BrinksAPI.Models
{
    public class Organization
    {
        public string? requestId { get; set; }
        public RiskCodes? RiskCodeDescription  { get; set; }
        [Required(ErrorMessage = "Customer Name is required.")]
        [StringLength(40)]
        public string? name { get; set; }
        [Required(ErrorMessage = "Customer Address1 is required.")]
        [StringLength(30)]
        public string? address1 { get; set; }
        [StringLength(30)]
        public string? address2 { get; set; }
        [StringLength(30)]
        public string? address3 { get; set; }
        [StringLength(30)]
        public string? address4 { get; set; }
        [Required(ErrorMessage = "Customer City is required.")]
        [StringLength(25)]
        public string? city { get; set; }
        [RequiredIf("countryCode", "US")]
        [StringLength(5)]
        public string? provinceCode { get; set; }
        [RequiredIf("countryCode", "US")]
        [StringLength(15)]
        public string? postalCode { get; set; }
        [StringLength(3)]
        public string? countryCode { get; set; }
        [StringLength(22)]
        public string? phoneNumber { get; set; }
        [StringLength(22)]
        public string? mobileNumber { get; set; }
        [StringLength(22)]
        public string? faxNumber { get; set; }
        [StringLength(255)]
        public string? emailAddress { get; set; }
        [StringLength(15)]
        public string? arAccountNumber { get; set; }
        [StringLength(15)]
        public string? apAccountNumber { get; set; }
        [StringLength(3)]
        public string? preferredCurrency { get; set; }
        [StringLength(25)]
        public string? billingAttention { get; set; }
        public string? dateCreated { get; set; }
        [StringLength(8)]
        public string? userId { get; set; }
        public string? notes { get; set; }
        public InvoiceTypes? invoiceType { get; set; }
        [StringLength(4)]
        public string? siteCode { get; set; }
        [StringLength(20)]
        [Required(ErrorMessage =("Customer Global Code is required."))]
        public string? globalCustomerCode { get; set; }
        [StringLength(20)]
        public string? invoiceGlobalCustomerCode { get; set; }
        [StringLength(20)]
        public string? brokerGlobalCustomerCode { get; set; }
        [StringLength(20)]
        public string? taxId { get; set; }
        [StringLength(2000)]
        public string? creditRiskNotes { get; set; }
        public YesOrNo? knownShipper { get; set; }
        [StringLength(10)]
        public string? customerGroupCode { get; set; }
        [StringLength(15)]
        public string? tsaValidationId { get; set; }
        public string? tsaDate { get; set; }
        public string? tsaType { get; set; }
        public string? locationVerifiedDate { get; set; }
        public YesOrNo? electronicInvoice { get; set; }
        public YesOrNo? addressValidatedFlag { get; set; }
        [StringLength(40)]
        public string? accountOwner { get; set; }
        [StringLength(255)]
        public string? einvoiceEmailAddress { get; set; }
        [StringLength(255)]
        public string? globalEntityName { get; set; }
        public YesOrNo? kycCreatedPrior2018 { get; set; }
        public YesOrNo? kycOpenProcCompleted { get; set; }
        [RequiredIf("kycOpenProcCompleted", YesOrNo.Y)]
        [StringLength(30)]
        public string? kycRefNbr { get; set; }
        [RequiredIf("kycOpenProcCompleted", YesOrNo.Y)]
        public string? kycVerifDate { get; set; }
        [RequiredIf("kycOpenProcCompleted", YesOrNo.Y)]
        [StringLength(50)]
        public string? kycApprovedBy { get; set; }
        [StringLength(30)]
        public string? kycOpeningStation { get; set; }
        [StringLength(10)]
        public string? lob { get; set; }
        public YesOrNo? allowCollect { get; set; }
        public YesOrNo? adyenPay { get; set; }
        [RequiredIf("adyenPay", YesOrNo.Y)]
 
        [StringLength(1)]
        public string? adyenPayPreference { get; set; }
        [StringLength(50)]
        public string? adyenTokenId { get; set; }
        [StringLength(50)]
        public string? adyenPayByLinkId { get; set; }

    }

    public enum YesOrNo{
        Y,
        N
    }
    public enum InvoiceTypes
    {
        S,
        C
    }

    public enum RiskCodes
    {
        CR1,
        CR2,
        CR3,
        CR4,
        CR5
    }


    /// <summary>
    /// Provides conditional <see cref="RequiredAttribute"/> 
    /// validation based on related property value.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public sealed class RequiredIfAttribute : RequiredAttribute
    {
        /// <summary>
        /// Gets or sets a value indicating whether other property's value should
        /// match or differ from provided other property's value (default is <c>false</c>).
        /// </summary>
        public bool IsInverted { get; set; } = false;

        /// <summary>
        /// Gets or sets the other property name that will be used during validation.
        /// </summary>
        /// <value>
        /// The other property name.
        /// </value>
        public string OtherProperty { get; private set; }

        /// <summary>
        /// Gets or sets the other property value that will be relevant for validation.
        /// </summary>
        /// <value>
        /// The other property value.
        /// </value>
        public object OtherPropertyValue { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="RequiredIfAttribute"/> class.
        /// </summary>
        /// <param name="otherProperty">The other property.</param>
        /// <param name="otherPropertyValue">The other property value.</param>
        public RequiredIfAttribute(string otherProperty, object otherPropertyValue)
            : base()
        {
            OtherProperty = otherProperty;
            OtherPropertyValue = otherPropertyValue;
        }

        protected override ValidationResult IsValid(
            object value,
            ValidationContext validationContext)
        {
            PropertyInfo otherPropertyInfo = validationContext
                .ObjectType.GetProperty(OtherProperty);
            if (otherPropertyInfo == null)
            {
                return new ValidationResult(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        "Could not find a property named {0}.",
                    validationContext.ObjectType, OtherProperty));
            }

            // Determine whether to run [Required] validation
            object actualOtherPropertyValue = otherPropertyInfo
                .GetValue(validationContext.ObjectInstance, null);
            if (!IsInverted && Equals(actualOtherPropertyValue, OtherPropertyValue) ||
                IsInverted && !Equals(actualOtherPropertyValue, OtherPropertyValue))
            {
                return base.IsValid(value, validationContext);
            }
            return default;
        }
    }
}
