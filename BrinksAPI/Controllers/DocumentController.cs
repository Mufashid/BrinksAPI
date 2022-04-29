using BrinksAPI.Models;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using eAdaptor;
using BrinksAPI.Helpers;
using System.Xml.Serialization;
using BrinksAPI.Auth;
using BrinksAPI.Interfaces;

namespace BrinksAPI.Controllers
{
    public class DocumentController : Controller
    {
        private readonly IConfigManager Configuration;
        private readonly ApplicationDbContext _context;
        public DocumentController(IConfigManager _configuration, ApplicationDbContext applicationDbContext)
        {
            Configuration = _configuration;
            _context = applicationDbContext;
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

        #region CREATE DOCUMENT POST /api/document
        /// <summary>
        /// Creates a Documents.
        /// </summary>
        /// <param name="item"></param>
        /// <returns>A newly created TodoItem</returns>
        /// <remarks>
        /// Sample request:
        ///
        ///     POST /Todo
        ///     {
        ///        "id": 1,
        ///        "name": "Item #1",
        ///        "isComplete": true
        ///     }
        ///
        /// </remarks>
        /// <response code="201">Returns the newly created document</response>
        /// <response code="400">If the item is null</response>
        [HttpPost]
        [Route("api/document/")]
        public IActionResult Create(BrinksDocument document)
        {
            Response dataResponse = new Response();
            try
            {
                if (!ModelState.IsValid)
                {
                    //dataResponse.Status = "Validation Error";
                    //dataResponse.Message = String.Format("{0} Error found", ModelState.ErrorCount);
                    //dataResponse.Data = ModelState.ToString();
                    return BadRequest(ModelState);
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

                    @event.EventTime = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fff");
                    @event.EventType = "Z00";
                    @event.IsEstimate = true;

                    List<Events.Context> contexts = new List<Events.Context>();
                    Events.Context context = new Events.Context();
                    Events.ContextType contextType = new Events.ContextType();
                    contextType.Value = "HAWBNumber";
                    context.Type = contextType;
                    context.Value = document.DocumentReferenceId;
                    contexts.Add(context);

                    @event.ContextCollection = contexts.ToArray();
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
                            return BadRequest(dataResponse);
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
                    return NotFound(dataResponse);
                }


                #region Document
                List<AttachedDocument> attachedDocuments = new List<AttachedDocument>();
                AttachedDocument attachedDocument = new AttachedDocument();

                attachedDocument.FileName = document.FileName;
                attachedDocument.ImageData = document.DocumentContent;
                
                string documentTypeInDB = _context.documentTypes
                    .Where(d => d.BrinksCode == document.DocumentTypeCode.Value.ToString())
                    .SingleOrDefault()
                    .CWCode;
                DocumentType documentType = new DocumentType();
                documentType.Code = documentTypeInDB;
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
                dataResponse.Data = documentResponse.Data.Data.OuterXml;

            }
            catch (Exception ex)
            {
                dataResponse.Status = "Internal Error";
                dataResponse.Message = ex.Message;
                return BadRequest(ex.Message);
            }
            return Created("",dataResponse);
        }
        #endregion

        
    }
}