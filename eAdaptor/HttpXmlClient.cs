using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Web;


	public class HttpXmlClient
	{
		public HttpXmlClient(Uri requestUri, bool requestCompressedResponse, string username, string password)
		{
			this.requestUri = requestUri;
			this.requestCompressedResponse = requestCompressedResponse;
			this.username = username;
			this.password = password;
		}
		readonly Uri requestUri;
		readonly bool requestCompressedResponse;
		readonly string username;
		readonly string password;

		public HttpResponseMessage Post(Stream content)
		{
			return Request(HttpMethod.Post, content);
		}

		HttpResponseMessage Request(HttpMethod method, Stream content)
		{
			using (var client = new HttpClient() { Timeout = TimeSpan.FromSeconds(250) })
			{
				var request = new HttpRequestMessage(method, requestUri);
				request.Content = new StreamContent(content);
				if (requestCompressedResponse)
				{
					request.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));
				}
				var authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(
					Encoding.GetEncoding("iso-8859-1").GetBytes(string.Format("{0}:{1}", username, password))));
				request.Headers.Authorization = authorization;

				var send = client.SendAsync(request);
				return send.Result;
			}
		}
	}
