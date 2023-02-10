using BrinksAPI.Auth;
using BrinksAPI.Helpers;
using BrinksAPI.Interfaces;
using BrinksAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Serialization;

namespace BrinksAPI.Controllers
{
    [Authorize]
    public class MawbController : Controller
    {
        private readonly ILogger<MawbController> _logger;
        private readonly IConfigManager _configuration;
        private readonly ApplicationDbContext _context;

        public MawbController(IConfigManager configuaration, ApplicationDbContext context, ILogger<MawbController> logger)
        {
            _configuration = configuaration;
            _context = context;
            _logger = logger;
        }


        /// <summary>
        /// Creates MAWB History.
        /// </summary>
        /// <param name="mawbs"></param>
        /// <returns>A newly created MAWB</returns>
        /// <remarks>
        /// Sample request:
        ///
        ///     POST /api/mawb/history
        ///     [{
        ///         "requestId": "12345678",
        ///         "mawbNumber": "176-2222222",
        ///         "historyDetails": "This is a test event details for first object",
        ///         "historyDate": "2022-05-21 10:00:00",
        ///         "serverId": "41",
        ///         "historyCode": "DEP"
        ///     },
        ///     {
        ///         "requestId": "12345679",
        ///         "mawbNumber": "176-2222223",
        ///         "historyDetails": "This is a test event details for second object",
        ///         "historyDate": "2022-05-21 10:00:00",
        ///         "serverId": "42",
        ///         "historyCode": "DEP"
        ///     }]
        /// </remarks>
        /// <response code="200">Success</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="500">Internal server error</response>
        [HttpPost]
        [ProducesResponseType(200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(500)]
        [Route("api/mawb/history")]
        public ActionResult <List<Response>> UpdateMawbHistory([FromBody] Mawb[] mawbs)
        {
            List<Response> dataResponses = new List<Response>();
            try
            {
                foreach (Mawb mawb in mawbs)
                {
                    Response dataResponse = new Response();
                    dataResponse.RequestId = mawb?.requestId;
                    try
                    {
                        var validationResults = new List<ValidationResult>();
                        var validationContext = new ValidationContext(mawb);
                        var isValid = Validator.TryValidateObject(mawb, validationContext, validationResults);


                        if (isValid)
                        {
                            string? historyCode = _context.eventCodes.Where(e => e.BrinksCode == mawb.historyCode).FirstOrDefault()?.CWCode;
                            historyCode = historyCode == null ? "Z77" : historyCode; // Default history code

                            #region UNIVERSAL EVENT
                            Events.UniversalEventData universalEvent = new Events.UniversalEventData();

                            #region DataContext
                            Events.Event @event = new Events.Event();
                            Events.DataContext dataContext = new Events.DataContext();
                            List<Events.DataTarget> dataTargets = new List<Events.DataTarget>();
                            Events.DataTarget dataTarget = new Events.DataTarget();
                            dataTarget.Type = "ForwardingConsol";
                            dataTargets.Add(dataTarget);
                            dataContext.DataTargetCollection = dataTargets.ToArray();

                            dataContext.EnterpriseID = _configuration.EnterpriseId;
                            dataContext.ServerID = _configuration.ServerId;
                            @event.DataContext = dataContext;
                            #endregion

                            #region Event
                            @event.EventTime = mawb.historyDate;
                            @event.EventType = historyCode;
                            @event.EventReference = mawb.historyDetails;
                            #endregion

                            #region Contexts
                            List<Events.Context> contexts = new List<Events.Context>();
                            Events.Context context = new Events.Context();
                            Events.ContextType type = new Events.ContextType();
                            type.Value = "MAWBNumber";
                            context.Type = type;
                            context.Value = mawb.mawbNumber;
                            contexts.Add(context);
                            @event.ContextCollection = contexts.ToArray();
                            #endregion

                            universalEvent.Event = @event; 
                            #endregion

                            string xml = Utilities.Serialize(universalEvent);

                            var documentResponse = eAdaptor.Services.SendToCargowise(xml, _configuration.URI, _configuration.Username, _configuration.Password);

                            if (documentResponse.Status == "SUCCESS")
                            {
                                using (var reader = new StringReader(documentResponse.Data.Data.OuterXml))
                                {
                                    var serializer = new XmlSerializer(typeof(Events.UniversalEventData));
                                    Events.UniversalEventData? responseEvent = (Events.UniversalEventData?)serializer.Deserialize(reader);

                                    bool isError = responseEvent.Event.ContextCollection.Any(c => c.Type.Value.Contains("FailureReason"));
                                    if (isError)
                                    {
                                        string errorMessage = responseEvent.Event.ContextCollection.Where(c => c.Type.Value == "FailureReason").FirstOrDefault().Value.Replace("Error - ", "").Replace("Warning - ", "");
                                        
                                        if (errorMessage.Contains("No Module found a Business Entity to link this Universal Event to."))
                                        {
                                            dataResponse.Status = "NOTFOUND";
                                            dataResponse.Message = String.Format("{0} Not Found.", mawb.mawbNumber);
                                            _logger.LogError("Error: {@Error} Request: {@Request}", dataResponse.Message, mawb);
                                        }

                                        else
                                        {
                                            dataResponse.Status = "ERROR";
                                            MatchCollection matchedError = Regex.Matches(errorMessage, "(Error)(.*)");
                                            string[] groupedErrors = matchedError.GroupBy(x => x.Value).Select(y => y.Key).ToArray();
                                            dataResponse.Message = string.Join(",", groupedErrors);
                                            _logger.LogError("Error: {@Error} Request: {@Request}", dataResponse.Message, mawb);


                                        }
                                    }
                                    else
                                    {
                                        dataResponse.Status = "SUCCESS";
                                        dataResponse.Message = "Mawb History Created Sucessfully";
                                        _logger.LogInformation("Success: {@Success} Request: {@Request}", dataResponse.Message, mawb);
                                    }
                                }
                            }
                            else
                            {
                                dataResponse.Status = "ERROR";
                                string notValidEventCodeMsg = "Cannot import XML Event unless it has a valid code.";
                                string responseErrorMsg = documentResponse.Data.Data.FirstChild.InnerText;
                                if (responseErrorMsg.Contains(notValidEventCodeMsg))
                                {
                                    dataResponse.Message = mawb.historyCode + " Is not a valid history code.";
                                    _logger.LogError("Error: {@Error} Request: {@Request}", dataResponse.Message, mawb);
                                }
                                else
                                {
                                    MatchCollection matchedError = Regex.Matches(responseErrorMsg, "(Error)(.*)");
                                    string[] groupedErrors = matchedError.GroupBy(x => x.Value).Select(y => y.Key).ToArray();
                                    dataResponse.Message = string.Join(",", groupedErrors);
                                    _logger.LogError("Error: {@Error} Request: {@Request}", dataResponse.Message, mawb);

                                }
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
                    catch (Exception ex)
                    {
                        dataResponse.Status = "ERROR";
                        dataResponse.Message = ex.Message;
                        _logger.LogError("Error: {@Error} Request: {@Request}", ex.Message, mawb);
                        dataResponses.Add(dataResponse);
                        continue;
                    }
                }

                return Ok(dataResponses);
            }
            catch (Exception ex)
            {
                Response dataResponse = new Response();
                dataResponse.Status = "ERROR";
                dataResponse.Message = ex.Message;
                dataResponses.Add(dataResponse);
                _logger.LogError("Error: {@Error}", dataResponse.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, dataResponses);
            }
        }


    }
}
