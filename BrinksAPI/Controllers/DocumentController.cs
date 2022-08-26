using BrinksAPI.Models;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using eAdaptor;
using BrinksAPI.Helpers;
using System.Xml.Serialization;
using BrinksAPI.Auth;
using BrinksAPI.Interfaces;
using Microsoft.AspNetCore.Authorization;

namespace BrinksAPI.Controllers
{
    [Authorize]
    public class DocumentController : Controller
    {
        private readonly IConfigManager Configuration;
        private readonly ApplicationDbContext _context;
        public DocumentController(IConfigManager _configuration, ApplicationDbContext applicationDbContext)
        {
            Configuration = _configuration;
            _context = applicationDbContext;
        }

        #region GET DOCUMENT
        [HttpGet]
        [Route("api/document/")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public IActionResult Get(Document document)
        {

            string test = this.Configuration.URI;
            string tesr3 = this.Configuration.Username;
            return Ok();
        }
        #endregion

        #region CREATE DOCUMENT
        /// <summary>
        /// Creates a Documents.
        /// </summary>
        /// <param name="document"></param>
        /// <returns>A newly created Document</returns>
        /// <remarks>
        /// Sample request:
        ///
        ///     POST /api/document
        ///     {
        ///        "requestId":"123456",
        ///        "documentTypeCode":"OTH",
        ///        "fileName":"Tiny PreAlert.txt",
        ///        "documentReference":"MAWB",
        ///        "documentReferenceId":"12345678",
        ///        "documentContent":"SGV5ISBXYWtlIFVwIQ==",
        ///        "documentFormat":"PDF",
        ///        "documentDescription":"Pre Alert",
        ///        "userId":"HJ"
        ///        
        ///     }
        ///
        /// </remarks>
        /// <response code="200">Success</response>
        /// <response code="400">Data not valid</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="500">Internal server error</response>
        [HttpPost]
        [Route("api/document/")]
        [ProducesResponseType(200)]
        public ActionResult<DocumentResponse> Create([FromBody]Document document)
        {
            DocumentResponse dataResponse = new DocumentResponse();
            try
            {
                dataResponse.RequestId = document?.RequestId;
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
                UniversalShipmentData universalShipmentData = new UniversalShipmentData();
                Shipment shipment = new Shipment();

                string? documentReferenceId = document.DocumentReferenceId;
                if (document.DocumentReference == DocumentReferenceType.MAWB)
                {
                    Events.UniversalEventData universalEventData = new Events.UniversalEventData();
                    Events.Event @event = new Events.Event();

                    #region Data Context
                    Events.DataContext eventDataContext = new Events.DataContext();
                    Events.DataTarget eventDataTarget = new Events.DataTarget();
                    eventDataTarget.Type = "ForwardingShipment";

                    List<Events.DataTarget> eventDataTargets = new List<Events.DataTarget>();
                    eventDataTargets.Add(eventDataTarget);
                    eventDataContext.DataTargetCollection = eventDataTargets.ToArray();

                    Events.Company eventCompany = new Events.Company();
                    eventCompany.Code =Configuration.CompanyCode;
                    eventDataContext.Company = eventCompany;

                    eventDataContext.DataProvider = Configuration.ServiceDataProvider;
                    eventDataContext.EnterpriseID = Configuration.EnterpriseId;
                    eventDataContext.ServerID = Configuration.ServerId;

                    @event.DataContext = eventDataContext;
                    #endregion

                    #region Event
                    @event.EventTime = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fff");
                    @event.EventType = "Z00";
                    @event.IsEstimate = true;
                    #endregion

                    #region Contexts
                    List<Events.Context> contexts = new List<Events.Context>();
                    Events.Context context = new Events.Context();
                    Events.ContextType contextType = new Events.ContextType();
                    contextType.Value = "HAWBNumber";
                    context.Type = contextType;
                    context.Value = document.DocumentReferenceId;
                    contexts.Add(context);
                    @event.ContextCollection = contexts.ToArray(); 
                    #endregion

                    universalEventData.Event = @event;

                    string eventXML = Utilities.Serialize(universalEventData);
                    var eventResponse =eAdaptor.Services.SendToCargowise(eventXML,Configuration.URI, Configuration.Username, Configuration.Password);
                    if (eventResponse.Status != "ERROR" && eventResponse.Data.Status == "PRS")
                    {
                        string stringXML = eventResponse.Data.Data.OuterXml.ToString();
                        Events.UniversalEventData eventResult = new Events.UniversalEventData();
                        using (TextReader reader = new StringReader(stringXML))
                        {
                            var serializer = new XmlSerializer(typeof(Events.UniversalEventData));
                            eventResult = (Events.UniversalEventData)serializer.Deserialize(reader);
                        }
                        bool isHouseBill = eventResult.Event.EventType == "DIM";
                        if (isHouseBill)
                        {
                            documentReferenceId = eventResult.Event.DataContext.DataSourceCollection.Where(s => s.Type == "ForwardingShipment").First().Key;
                        }
                        else
                        {
                            dataResponse.Status = "Not Found";
                            dataResponse.Message = String.Format("Housbill {0} Number is invalid.", documentReferenceId);
                            return Ok(dataResponse);
                        }


                    }
                }


                #region Data Context
                DataContext dataContext = new DataContext();
                DataTarget dataTarget = new DataTarget();
                dataTarget.Type = "ForwardingShipment";
                dataTarget.Key = documentReferenceId;

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

                universalShipmentData.Shipment = shipment;
                string shipmentXML = Utilities.Serialize(universalShipmentData);
                var shipmentResponse = eAdaptor.Services.SendToCargowise(shipmentXML, Configuration.URI, Configuration.Username, Configuration.Password);
                if (shipmentResponse.Status == "ERROR")
                {
                    dataResponse.Status = "Not Found";
                    dataResponse.Message = String.Format("Shipment {0} not found in the CW.", documentReferenceId);
                    return Ok(dataResponse);
                }


                #region Document
                List<AttachedDocument> attachedDocuments = new List<AttachedDocument>();
                AttachedDocument attachedDocument = new AttachedDocument();

                //string fileName = document.CWDocumentId + "_" + document.FileName;
                attachedDocument.FileName = document.FileName;
                attachedDocument.ImageData = document.DocumentContent;

                var documentTypeInDB = _context.documentTypes
                    .Where(d => d.BrinksCode == document.DocumentTypeCode.Value.ToString()).FirstOrDefault();


                DocumentType documentType = new DocumentType();
                documentType.Code = documentTypeInDB != null ? documentTypeInDB.CWCode : "OTH";
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
                var documentResponse = eAdaptor.Services.SendToCargowise(xml,Configuration.URI, Configuration.Username, Configuration.Password);
                dataResponse.Status = "SUCCESS";
                dataResponse.Message = "Successfully created the document.";
                //dataResponse.Data = document;

            }
            catch (Exception ex)
            {
                dataResponse.Status = "Internal Error";
                dataResponse.Message = ex.Message;
                return Ok(ex.Message);
            }
            return Ok(dataResponse);
        }
        #endregion

        
    }
}