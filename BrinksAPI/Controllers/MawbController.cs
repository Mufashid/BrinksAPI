using BrinksAPI.Auth;
using BrinksAPI.Helpers;
using BrinksAPI.Interfaces;
using BrinksAPI.Models;
using Microsoft.AspNetCore.Mvc;
using System.Xml.Serialization;

namespace BrinksAPI.Controllers
{
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
        ///     {
        ///         "requestId": "12345678",
        ///         "mawbNumber": "176-2222222",
        ///         "historyDetails": "This is a test event details",
        ///         "historyDate": "2022-05-21 10:00:00",
        ///         "serverId": "TRN",
        ///         "historyCode": "DEP"
        ///     }
        /// </remarks>
        /// <response code="200">Success</response>
        /// <response code="400">Data not valid</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="500">Internal server error</response>
        [HttpPost]
        [Route("api/mawb/history")]
        public IActionResult UpdateMawbHistory([FromBody] Mawb mawb)
        {
            Response dataResponse = new Response();
            try
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
                company.Code = "";
                dataContext.Company = company;
                dataContext.EnterpriseID = "";
                dataContext.ServerID = mawb.serverId;
                @event.DataContext = dataContext;
                #endregion

                @event.EventTime = mawb.historyDate;
                @event.EventType = mawb.historyCode;
                @event.EventReference = mawb.historyDetails;

                #region Context
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

                if(documentResponse.Status == "SUCCESS")
                {
                    using (var reader = new StringReader(documentResponse.Data.Data.OuterXml))
                    {
                        var serializer = new XmlSerializer(typeof(Events.UniversalEventData));
                        Events.UniversalEventData responseEvent = (Events.UniversalEventData)serializer.Deserialize(reader);

                        bool isError = responseEvent.Event.ContextCollection.Any(c => c.Type.Value.Contains("FailureReason"));
                        string messageData = isError ? responseEvent.Event.ContextCollection.Where(c => c.Type.Value == "FailureReason").FirstOrDefault().Value : documentResponse.Data.Data.OuterXml;
                        dataResponse.Status = isError ? "ERROR" : "SUCCESS";
                        dataResponse.Message = isError ? "Please fix the errors." : "MAWB Created Successfully.";
                        dataResponse.Data = messageData;
                        return isError?BadRequest(dataResponse):Ok(dataResponse);
                    }
                }

                dataResponse.Status = documentResponse.Status;
                dataResponse.Message = documentResponse.Message;
                dataResponse.Data = documentResponse.Data.Data.OuterXml;
                return BadRequest(dataResponse);

            }
            catch (Exception ex)
            {
                dataResponse.Status = "Internal Error";
                dataResponse.Message = ex.Message;
                return BadRequest(ex.Message);
            }
        }
    }
}
