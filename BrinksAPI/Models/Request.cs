using System.ComponentModel.DataAnnotations;

namespace BrinksAPI.Models
{
    #region Document
    public class BrinksDocument
    {
        [Required]
        public string? RequestId { get; set; }
        [Required]
        public string? CWDocumentId { get; set; }
        [Required]
        //[MaxLength(5)]
        [EnumDataType(typeof(DocumentType))]
        public DocumentType DocumentTypeCode { get; set; }
        [Required]
        [MaxLength(100)]
        public string? FileName { get; set; }
        [Required]
        //[MaxLength(20)]
        public DocumentReferenceType DocumentReference { get; set; }

        [Required]
        [MaxLength(20)]
        public string? DocumentReferenceId { get; set; }

        [Required]
        public byte[]? DocumentContent { get; set; }

        [Required]
        //[MaxLength(4)]
        public DocumentFormatType DocumentFormat { get; set; }
        [Required]
        [MaxLength(255)]
        public string? DocumentDescription { get; set; }

        [MaxLength(50)]
        public string? UserId { get; set; }

    }


    public enum DocumentType
    {
        AL,
        BOL,
        BR,
        CI,
        CN,
        COO,
        CSI,
        CTS,
        EEI,
        FAGSP,
        FRMD,
        GC,
        IMAGE,
        JTEPA,
        KPC,
        LIC,
        MCEUR,
        OTH,
        PFI,
        PID,
        PL,
        POA,
        TIB,
        TR
    }
    public enum DocumentFormatType
    {
        PDF,
        PNG,
        JPEG,
        GIF,
        BMP,
        XML,
        TXT,
        DOCX,
        XLSX
    }
    public enum DocumentReferenceType
    {
        SHIPMENT,
        MAWB,
        CUSTOMER
    }
    #endregion
}
