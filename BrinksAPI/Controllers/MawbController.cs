using BrinksAPI.Auth;
using BrinksAPI.Helpers;
using BrinksAPI.Interfaces;
using BrinksAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;
using System.Xml;
using System.Xml.Serialization;

namespace BrinksAPI.Controllers
{
    [Authorize]
    public class MawbController : Controller
    {
        private readonly IConfigManager _configuration;
        private readonly ApplicationDbContext _context;

        public MawbController(IConfigManager configuaration, ApplicationDbContext context)
        {
            _configuration = configuaration;
            _context = context;
        }
       

        /// <summary>
        /// Creates MAWB History.
        /// </summary>
        /// <param name="mawb"></param>
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
        ///         "serverId": "TRN",
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
        public ActionResult <List<MawbResponse>> UpdateMawbHistory([FromBody] Mawb[] mawbs)
        {
            List<MawbResponse> dataResponses = new List<MawbResponse>();
            try
            {
                foreach (Mawb mawb in mawbs)
                {
                    MawbResponse dataResponse = new MawbResponse();
                    dataResponse.RequestId = mawb?.requestId;
                    try
                    {
                        var validationResults = new List<ValidationResult>();
                        var validationContext = new ValidationContext(mawb);
                        var isValid = Validator.TryValidateObject(mawb, validationContext, validationResults);

                        if (isValid)
                        {
                            var site = _context.sites.Where(s => s.ServerID == Int32.Parse(mawb.serverId)).FirstOrDefault();
                            if (site != null)
                            {
                                Events.UniversalEventData universalEvent = new Events.UniversalEventData();
                                #region DataContext
                                Events.Event @event = new Events.Event();
                                Events.DataContext dataContext = new Events.DataContext();
                                List<Events.DataTarget> dataTargets = new List<Events.DataTarget>();
                                Events.DataTarget dataTarget = new Events.DataTarget();
                                dataTarget.Type = "ForwardingConsol";
                                dataTargets.Add(dataTarget);
                                dataContext.DataTargetCollection = dataTargets.ToArray();

                                Events.Company company = new Events.Company();
                                company.Code = _configuration.CompanyCode;
                                Events.Country country = new Events.Country();
                                country.Code = site.Country;
                                company.Country = country;
                                dataContext.Company = company;

                                dataContext.EnterpriseID = _configuration.EnterpriseId;
                                dataContext.ServerID = _configuration.ServerId;
                                @event.DataContext = dataContext;
                                #endregion

                                #region Event
                                @event.EventTime = mawb.historyDate;
                                @event.EventType = mawb.historyCode;
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

                                string xml = Utilities.Serialize(universalEvent);

                                var documentResponse = eAdaptor.Services.SendToCargowise(xml, _configuration.URI, _configuration.Username, _configuration.Password);

                                if (documentResponse.Status == "SUCCESS")
                                {
                                    using (var reader = new StringReader(documentResponse.Data.Data.OuterXml))
                                    {
                                        var serializer = new XmlSerializer(typeof(Events.UniversalEventData));
                                        Events.UniversalEventData responseEvent = (Events.UniversalEventData)serializer.Deserialize(reader);

                                        bool isError = responseEvent.Event.ContextCollection.Any(c => c.Type.Value.Contains("FailureReason"));
                                        if (isError)
                                        {
                                            string errorMessage = responseEvent.Event.ContextCollection.Where(c => c.Type.Value == "FailureReason").FirstOrDefault().Value.Replace("Error - ", "").Replace("Warning - ", "");
                                            dataResponse.Status = "ERROR";
                                            if (errorMessage == "No Module found a Business Entity to link this Universal Event to.")
                                                dataResponse.Message = String.Format("{0} - Mawb does not exist", mawb.mawbNumber);
                                            else
                                                dataResponse.Message = errorMessage;
                                        }
                                        else
                                        {
                                            dataResponse.Status = isError ? "ERROR" : "SUCCESS";
                                            dataResponse.Message = isError ? "Please fix the errors." : "Mawb History Created Sucessfully";
                                        }
                                    }
                                }
                                else
                                {
                                    dataResponse.Status = documentResponse.Status;
                                    dataResponse.Message = documentResponse.Data.Data.FirstChild.InnerText.Replace("Error - ", "").Replace("Warning - ", "");

                                }
                            }
                            else
                            {
                                dataResponse.Status = "ERROR";
                                dataResponse.Message = "Server ID " + mawb.serverId + " is not found in mapping DB.";
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
                        continue;
                    }
                }

                return Ok(dataResponses);
            }
            catch (Exception ex)
            {
                MawbResponse dataResponse = new MawbResponse();
                dataResponse.Status = "ERROR";
                dataResponse.Message = ex.Message;
                dataResponses.Add(dataResponse);
                return StatusCode(StatusCodes.Status500InternalServerError, dataResponses);
            }
        }
    }
}
