using BrinksAPI.Models;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using eAdaptor;
using BrinksAPI.Helpers;
using Cargowise;

namespace BrinksAPI.Controllers
{
    public class DocumentController : Controller
    {
        private readonly IConfigManager Configuration;

        public DocumentController(IConfigManager _configuration)
        {
            Configuration = _configuration;
        }

        #region GET DOCUMENT GET /api/document/
        [HttpGet]
        [Route("api/document/")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public IActionResult Get(BrinksDocument document)
        {

            string test = this.Configuration.URI;
            string tesr3 = this.Configuration.Username;
            return Ok();
        }
        #endregion

        #region Create Document POST /api/document
        [HttpPost]
        [Route("api/document/")]
        public IActionResult Create(BrinksDocument document)
        {
            string response;
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest();

                UniversalShipmentData universalShipmentData = new UniversalShipmentData();
                Shipment shipment = new Shipment();

                if(document.DocumentReference == DocumentReferenceType.SHIPMENT)
                {

                }
                else if(document.DocumentReference == DocumentReferenceType.MAWB)
                { 
                
                }
                else if(document.DocumentReference == DocumentReferenceType.CUSTOMER){

                }
                #region Data Context
                DataContext dataContext = new DataContext();
                DataTarget dataTarget = new DataTarget();
                dataTarget.Type = "ForwardingShipment";
                dataTarget.Key = document.DocumentReferenceId;

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

                //universalShipmentData.Shipment = shipment;
                //string shipmentXML = Utilities.Serialize(universalShipmentData);
                //dataResponse = SendToCargowise(shipmentXML, username, password);
                //if (dataResponse.Status == "ERROR")
                //{
                //    dataResponse.Status = "Not Found";
                //    dataResponse.Message = String.Format("{0} not found", id);
                //    dataResponse.data = null;
                //    return Content(HttpStatusCode.NotFound, dataResponse);
                //}


                #region Document
                List<AttachedDocument> attachedDocuments = new List<AttachedDocument>();
                AttachedDocument attachedDocument = new AttachedDocument();
                attachedDocument.FileName = document.FileName;
                attachedDocument.ImageData = document.DocumentContent;
                
                DocumentType documentType = new DocumentType();
                documentType.Code = document.DocumentFormat.ToString();
                documentType.Description = document.DocumentDescription;
                attachedDocument.Type = documentType;
                attachedDocument.IsPublishedSpecified = true;
                attachedDocument.IsPublished = true;
                 attachedDocument.SaveDateUTC = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.f");
                Staff staff = new Staff();
                staff.Code = document.UserId;
                attachedDocument.SavedBy = staff;
                attachedDocuments.Add(attachedDocument);
                shipment.AttachedDocumentCollection = attachedDocuments.ToArray();
                #endregion

                universalShipmentData.Shipment = shipment;

                string xml = Utilities.Serialize(universalShipmentData);
                response = Services.SendToCargowise(xml,Configuration.URI, Configuration.Username, Configuration.Password);

            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
            return Ok(response);
        } 
        #endregion
    }
}