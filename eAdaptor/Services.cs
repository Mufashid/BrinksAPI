using eAdaptor.Entities;
using System.IO.Compression;
using System.Net;
using System.Text;
using System.Xml.Serialization;

namespace eAdaptor
{
    public class Services
    {
        #region Send XML to CW
        public static XMLDataResponse SendToCargowise(string xml, string uri,string username, string password)
        {
            XMLDataResponse responseData = new XMLDataResponse();
            try
            {
                var client = new HttpXmlClient(new Uri(uri), true, username, password);
                using (var sourceStream = new MemoryStream(Encoding.UTF8.GetBytes(xml)))
                {
                    var response = client.Post(sourceStream);
                    //var responseStatus = response.StatusCode;
                    if (response.IsSuccessStatusCode)
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
                                var serializer = new XmlSerializer(typeof(UniversalResponseData));
                                UniversalResponseData result = (UniversalResponseData)serializer.Deserialize(reader);

                                bool isError = result.Data.InnerText.Contains("Error") || result.Status!= "PRS";
                                responseData.Status = isError ? "ERROR" : "SUCCESS";
                                responseData.Message = isError ? "Please fix the errors." : "Successfull";
                                responseData.Data = result;
                            }
                        }
                    }
                    else
                    {
                        responseData.Status = "ERROR";
                        responseData.Message = response.ReasonPhrase;
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

        #region Send XML to CW
        public static XMLDataResponse SendToCargowise2(string xml, string uri, string username, string password)
        {
            XMLDataResponse responseData = new XMLDataResponse();
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
                                var serializer = new XmlSerializer(typeof(UniversalResponseData));
                                UniversalResponseData result = (UniversalResponseData)serializer.Deserialize(reader);

                                bool isError = result.Data.InnerText.Contains("Error");
                                responseData.Status = isError ? "ERROR" : "SUCCESS";
                                responseData.Message = isError ? "Please fix the errors." : "Successfull";
                                responseData.Data = result;
                            }
                        }
                    }
                    else
                    {
                        responseData.Status = "ERROR";
                        responseData.Message = response.ReasonPhrase;
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
