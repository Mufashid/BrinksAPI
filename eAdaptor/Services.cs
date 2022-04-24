using System.IO.Compression;
using System.Net;
using System.Text;

namespace eAdaptor
{
    public class Services
    {
        #region Send XML to CW
        public static string SendToCargowise(string xml, string uri,string username, string password)
        {
            string responseData = "";
            try
            {
                var client = new HttpXmlClient(new Uri(uri), true, username, password);
                using (var sourceStream = new MemoryStream(Encoding.UTF8.GetBytes(xml)))
                {
                    var response = client.Post(sourceStream);
                    var responseStatus = response.StatusCode;
                    if (responseStatus == HttpStatusCode.OK)
                    {

                        if (response.Content != null)
                        {
                            var stream = response.Content.ReadAsStreamAsync().Result;

                            if (response.Content.Headers.ContentEncoding.Contains("gzip", StringComparer.InvariantCultureIgnoreCase))
                            {
                                stream = new GZipStream(stream, CompressionMode.Decompress);
                            }

                            using (var reader = new StreamReader(stream))
                            {
                                responseData = reader.ReadToEnd();
                            }
                        }
                    }
                }
            }

            catch
            {
                throw;
            }
            return responseData;

        }
        #endregion
    }
}
