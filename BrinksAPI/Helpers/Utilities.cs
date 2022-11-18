using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace BrinksAPI.Helpers
{
    public class Utilities
    {
        #region Serialize XML Element to String
        public static string Serialize<T>(T dataToSerialize)
        {
            try
            {
                var stringwriter = new System.IO.StringWriter();
                var serializer = new XmlSerializer(typeof(T));
                serializer.Serialize(stringwriter, dataToSerialize);
                return stringwriter.ToString();
            }
            catch
            {
                throw;
            }
        }
        #endregion

        #region Serialize Object to XML Element
        public static XmlElement? SerializeToXmlElement(object o)
        {
            XmlDocument doc = new XmlDocument();
            using (XmlWriter writer = doc.CreateNavigator().AppendChild())
            {
                new XmlSerializer(o.GetType()).Serialize(writer, o);
            }
            
            return doc.DocumentElement;
        }
        #endregion

        public static string getElementFromXML(string xml,string elementName)
        {
            XmlDocument xDoc = new XmlDocument();
            xDoc.LoadXml(xml);
            XmlNodeList elemlist = xDoc.GetElementsByTagName(elementName);
            return elemlist[0].InnerXml;
        }

        public static UniversalShipmentData? ReadUniversalShipment(string xml)
        {
            XmlSerializer ser = new XmlSerializer(typeof(UniversalShipmentData));
            UniversalShipmentData? s = new UniversalShipmentData();
            using (TextReader reader = new StringReader(xml))
            {
                s = (UniversalShipmentData?)ser.Deserialize(reader);
            }
            return s;
        }

        public static Events.UniversalEventData? ReadUniversalEvent(string xml)
        {
            XmlSerializer ser = new XmlSerializer(typeof(Events.UniversalEventData));
            Events.UniversalEventData? s = new Events.UniversalEventData();
            using (TextReader reader = new StringReader(xml))
            {
                s = (Events.UniversalEventData?)ser.Deserialize(reader);
            }
            return s;
        }

        public static string GetToken(string url,string username,string password)
        {
            var credential = new
            {
                username = username,
                password = password,
            };
            string jsonData = JsonConvert.SerializeObject(credential);
            dynamic response = JObject.Parse(PostRequest(url, "", jsonData).Item2);
            return response.token;
        }
        public static Tuple<HttpStatusCode, string> PostRequest(string url,string token,string data)
        {
            string? result = null;
            HttpWebRequest httpRequest = (HttpWebRequest)WebRequest.Create(url);
            httpRequest.Method = "POST";
            httpRequest.Accept = "application/json";
            httpRequest.ContentType = "application/json";
            httpRequest.Headers["Authorization"] = "Bearer " + token;
            
            using (var streamWriter = new StreamWriter(httpRequest.GetRequestStream()))
            {
                streamWriter.Write(data);
            }

            HttpWebResponse httpResponse = (HttpWebResponse)httpRequest.GetResponse();
            using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
            {
                result = streamReader.ReadToEnd();
            }
            return Tuple.Create(httpResponse.StatusCode,result);
        }


    }
}
