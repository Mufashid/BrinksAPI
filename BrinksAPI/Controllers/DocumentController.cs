using BrinksAPI.Models;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using eAdaptor;
using BrinksAPI.Helpers;
using System.Xml.Serialization;
using BrinksAPI.Auth;
using BrinksAPI.Interfaces;
using Microsoft.AspNetCore.Authorization;
using NativeRequest;
using NativeOrganization;

namespace BrinksAPI.Controllers
{
    [Authorize]
    public class DocumentController : Controller
    {
        private readonly ILogger<BillingController> _logger;
        private readonly IConfigManager _configuration;
        private readonly ApplicationDbContext _context;
        public DocumentController(IConfigManager configuration, ApplicationDbContext applicationDbContext, ILogger<BillingController> logger)
        {
            _configuration = configuration;
            _context = applicationDbContext;
            _logger = logger;
        }

        

        #region CREATE DOCUMENT
        /// <summary>
        /// Creates a Documents in consol or shipment.
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
        /// <response code="401">Unauthorized</response>
        /// <response code="500">Internal server error</response>
        [HttpPost]
        [Route("api/document/")]
        [ProducesResponseType(200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(500)]
        public ActionResult<Response> Create([FromBody] Document document)
        {
            Response dataResponse = new Response();
            try
            {
                dataResponse.RequestId = document?.RequestId;

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
                    _logger.LogError(errorString);
                    dataResponse.Status = "ERROR";
                    dataResponse.Message = errorString;

                    return Ok(dataResponse);
                }
                #endregion

                string requestXML = "";

                switch (document.DocumentReference)
                {
                    case DocumentReferenceType.SHIPMENT:
                        string shipmentId = GetShipmentIdByReferenceNumber(DocumentReferenceType.SHIPMENT, document?.DocumentReferenceId);
                        if(shipmentId != null && shipmentId != "")
                        {
                            requestXML = CreateShipmentDocumentXML(shipmentId,document);
                        }
                        else
                        {
                            _logger.LogError(String.Format("House Bill {0} not found.", document.DocumentReferenceId));
                            dataResponse.Status = "NOTFOUND";
                            dataResponse.Message = String.Format("{0} Not Found.", document?.DocumentReferenceId);
                            return Ok(dataResponse);
                        }
                        break;
                    case DocumentReferenceType.MAWB:
                        string consolId = GetShipmentIdByReferenceNumber(DocumentReferenceType.MAWB, document?.DocumentReferenceId);
                        if (consolId != null && consolId != "")
                        {
                           requestXML = CreateShipmentDocumentXML(consolId,document);
                        }
                        else
                        {
                            _logger.LogError(String.Format("MAWB {0} not found.",document.DocumentReferenceId));
                            dataResponse.Status = "NOTFOUND";
                            dataResponse.Message = String.Format("{0} Not Found.", document?.DocumentReferenceId);
                            return Ok(dataResponse);
                        }
                        break;
                    case DocumentReferenceType.CUSTOMER:
                        string orgCode = GetOrgCodeByRegNumber(document?.DocumentReferenceId);

                        if (orgCode != null && orgCode != "")
                        {
                            requestXML = CreateOrganizationDocumentXML(orgCode, document);
                        }
                        else
                        {
                            _logger.LogError(String.Format("Global Cuatomer Code {0} not found.", document.DocumentReferenceId));
                            dataResponse.Status = "NOTFOUND";
                            dataResponse.Message = String.Format("{0} Not Found.", document?.DocumentReferenceId);
                            return Ok(dataResponse);
                        }
                        break;
                }

                #region SENDING TO CW
                var documentResponse = eAdaptor.Services.SendToCargowise(requestXML, _configuration.URI, _configuration.Username, _configuration.Password);
                if (documentResponse.Status == "SUCCESS")
                {
                    _logger.LogInformation(String.Format("Sucessfully created the document. Request: {0}", document));
                    dataResponse.Status = "SUCCESS";
                    dataResponse.Message = "Successfully created the document.";
                    return Ok(dataResponse);
                }
                else
                {
                    string errorMessage = documentResponse.Data.Data.FirstChild.InnerText.Replace("Error - ", "").Replace("Warning - ", "");
                    _logger.LogError(errorMessage);
                    dataResponse.Status = "ERROR";
                    dataResponse.Message = errorMessage;
                    return Ok(dataResponse);
                } 
                #endregion

            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                dataResponse.Status = "ERROR";
                dataResponse.Message = ex.Message;
                return StatusCode(StatusCodes.Status500InternalServerError, dataResponse);
            }
        }
        #endregion


        public string GetOrgCodeByRegNumber(string refNumber)
        {
            OrganizationData organizationData = new OrganizationData();

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
            criteriaType2.Value = refNumber;
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
                    NativeOrganization.Native result = (NativeOrganization.Native)serializer.Deserialize(reader);
                    string organization = result.Body.Any[0].OuterXml;
                    using (TextReader reader2 = new StringReader(organization))
                    {
                        var serializer2 = new XmlSerializer(typeof(OrganizationData));
                        organizationData = (OrganizationData)serializer2.Deserialize(reader2);
                    }
                }
            }
            return organizationData?.OrgHeader?.Code;
        }

        public string GetShipmentIdByReferenceNumber(DocumentReferenceType referenceType, string referenceNumber)
        {
            string? shipmentNumber = "";
            Events.UniversalEventData universalEvent = new Events.UniversalEventData();
            Events.Event @event = new Events.Event();

            #region DATA CONTEXT
            string dataTargetType = referenceType == DocumentReferenceType.SHIPMENT ? "ForwardingShipment" : "ForwardingConsol";
            Events.DataContext eventDataContext = new Events.DataContext();
            List<Events.DataTarget> dataTargets = new List<Events.DataTarget>();
            Events.DataTarget dataTarget = new Events.DataTarget();
            dataTarget.Type = dataTargetType;
            dataTargets.Add(dataTarget);
            eventDataContext.DataTargetCollection = dataTargets.ToArray();
            @event.DataContext = eventDataContext;
            #endregion

            #region EVENT DEATAIL
            @event.EventTime = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss");
            @event.EventType = "Z00";
            #endregion

            #region CONTEXT COLLECTION
            string contextType = referenceType == DocumentReferenceType.SHIPMENT ? "HAWBNumber" : "MAWBNumber";
            List<Events.Context> contexts = new List<Events.Context>();
            Events.Context context = new Events.Context();
            Events.ContextType type = new Events.ContextType();
            type.Value = contextType;
            context.Type = type;
            context.Value = referenceNumber;
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
                    shipmentNumber = eventResponse?.Event?.DataContext?.DataSourceCollection?.Where(d => d.Type == dataTargetType)?.FirstOrDefault()?.Key;
                }
            }
            return shipmentNumber;
        }
        public string CreateShipmentDocumentXML(string id,Document document)
        {
            UniversalShipmentData universalShipmentData = new UniversalShipmentData();
            Shipment shipment = new Shipment();

            #region DATA CONTEXT
            string dataTargetType = document.DocumentReference == DocumentReferenceType.SHIPMENT ? "ForwardingShipment" : "ForwardingConsol";
            DataContext dataContext = new DataContext();
            DataTarget dataTarget = new DataTarget();
            dataTarget.Type = dataTargetType;
            dataTarget.Key = id;
            List<DataTarget> dataTargets = new List<DataTarget>();
            dataTargets.Add(dataTarget);
            dataContext.DataTargetCollection = dataTargets.ToArray();
            Company company = new Company();
            company.Code = _configuration.CompanyCode;
            dataContext.Company = company;
            dataContext.EnterpriseID = _configuration.EnterpriseId;
            dataContext.ServerID = _configuration.ServerId;
            shipment.DataContext = dataContext;
            #endregion

            #region DOCUMENT
            List<AttachedDocument> attachedDocuments = new List<AttachedDocument>();
            AttachedDocument attachedDocument = new AttachedDocument();

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

            return Utilities.Serialize(universalShipmentData);
        }

        public string CreateOrganizationDocumentXML(string orgCode, Document document)
        {
            return "";
        }
    }
}