using eAdaptor.Entities;
using System.IO.Compression;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Xml;
using System.Xml.Linq;
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
                    if (response.IsSuccessStatusCode)
                    {
                        if (response.Content != null)
                        {
                            var stream = response.Content.ReadAsStreamAsync().Result;

                            if (response.Content.Headers.ContentEncoding.Contains("gzip", StringComparer.InvariantCultureIgnoreCase))
                            {
                                stream = new GZipStream(stream, CompressionMode.Decompress);
                            }

                            XmlSerializer serializer = new XmlSerializer(typeof(UniversalResponseData));
                            using (StreamReader reader = new StreamReader(stream))
                            {
                                UniversalResponseData? result = (UniversalResponseData?)serializer.Deserialize(reader);
                                bool isError = result.Data.InnerText.Contains("Error") || result.Status != "PRS";
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

        #region CHAT GPT
        public static async Task<XMLDataResponse> SendToCargowise3(string xml, string uri, string username, string password)
        {
            var responseData = new XMLDataResponse();
            try
            {
                using (var handler = new HttpClientHandler())
                {
                    handler.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;

                    using (var client = new HttpClient(handler))
                    {
                        client.BaseAddress = new Uri(uri);
                        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.ASCII.GetBytes($"{username}:{password}")));

                        using (var sourceStream = new MemoryStream(Encoding.UTF8.GetBytes(xml)))
                        {
                            var response = await client.PostAsync("", new StreamContent(sourceStream));

                            if (response.IsSuccessStatusCode)
                            {
                                var contentStream = await response.Content.ReadAsStreamAsync();

                                using (var reader = new StreamReader(contentStream))
                                {
                                    var serializer = new XmlSerializer(typeof(UniversalResponseData));
                                    UniversalResponseData result = (UniversalResponseData)serializer.Deserialize(reader);

                                    var isError = string.IsNullOrEmpty(result.Data.InnerText) || result.Data.InnerText.Contains("Error") || result.Status != "PRS";
                                    responseData.Status = isError ? "ERROR" : "SUCCESS";
                                    responseData.Message = isError ? "Please fix the errors." : "Successful";
                                    responseData.Data = result;
                                }
                            }
                            else
                            {
                                responseData.Status = "ERROR";
                                responseData.Message = response.ReasonPhrase;
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


        #region Send XML to CW(Only for organization )
        // This function is only used in organization because while getting serialization wrong if the response is failure from CW side
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
                                string xmlString = reader.ReadToEnd();
                                XmlDocument doc = new XmlDocument();
                                doc.LoadXml(xmlString);

                                string? status =  doc.GetElementsByTagName("Status")[0]?.FirstChild?.InnerText;
                                string? data = doc.GetElementsByTagName("Data")[0]?.FirstChild?.InnerText;
                                string? processingLog = doc.GetElementsByTagName("ProcessingLog")[0]?.FirstChild?.InnerText;

                                bool isError = processingLog.Contains("Error");
                                responseData.Status = isError ? "ERROR" : "SUCCESS";
                                responseData.Message = processingLog;
                                responseData.Data = null;
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
