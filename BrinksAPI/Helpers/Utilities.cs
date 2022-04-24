using System.Xml.Serialization;

namespace BrinksAPI.Helpers
{
    public class Utilities
    {
        #region Serialize XML Objects
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
    }
}
