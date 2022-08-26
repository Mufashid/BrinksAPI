using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace BrinksAPI.Models
{
    [Keyless]
    public class Document
    {
        public string? RequestId { get; set; }
        [Required]
        [EnumDataType(typeof(DocumentType))]
        public DocumentType? DocumentTypeCode { get; set; }
        [Required]
        [StringLength(100)]
        public string? FileName { get; set; }
        [Required]
        public DocumentReferenceType? DocumentReference { get; set; }

        [Required]
        [StringLength(20)]
        public string? DocumentReferenceId { get; set; }

        [Required]
        public byte[]? DocumentContent { get; set; }

        [Required]
        public DocumentFormatType? DocumentFormat { get; set; }
        [Required]
        [StringLength(255)]
        public string? DocumentDescription { get; set; }

        [StringLength(50)]
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
        JPG,
        GIF,
        BMP,
        XML,
        TXT,
        DOCX,
        XLSX,
        TIFF,
        TIF,
        BAK
    }
    public enum DocumentReferenceType
    {
        SHIPMENT,
        MAWB,
        CUSTOMER
    }
}
