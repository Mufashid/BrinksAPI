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
        public static XmlElement SerializeToXmlElement(object o)
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

        public static UniversalShipmentData ReadUniversalShipment(string xml)
        {
            XmlSerializer ser = new XmlSerializer(typeof(UniversalShipmentData));
            UniversalShipmentData? s = new UniversalShipmentData();
            using (TextReader reader = new StringReader(xml))
            {
                s = (UniversalShipmentData)ser.Deserialize(reader);
            }
            return s;
        }

        public static Events.UniversalEventData ReadUniversalEvent(string xml)
        {
            XmlSerializer ser = new XmlSerializer(typeof(Events.UniversalEventData));
            Events.UniversalEventData? s = new Events.UniversalEventData();
            using (TextReader reader = new StringReader(xml))
            {
                s = (Events.UniversalEventData)ser.Deserialize(reader);
            }
            return s;
        }
    }
}
