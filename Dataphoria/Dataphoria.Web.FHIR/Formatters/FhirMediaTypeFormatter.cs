﻿using Alphora.Dataphor.Dataphoria.Web.FHIR.ExceptionHandling;
using Alphora.Dataphor.Dataphoria.Web.FHIR.Extensions;
using Alphora.Dataphor.Dataphoria.Web.FHIR.Models;
using Hl7.Fhir.Model;
using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.Text;

namespace Alphora.Dataphor.Dataphoria.Web.FHIR.Formatters
{
	public abstract class FhirMediaTypeFormatter : MediaTypeFormatter
	{
		public FhirMediaTypeFormatter() : base()
		{
			this.SupportedEncodings.Clear();
			this.SupportedEncodings.Add(Encoding.UTF8);
		}

		protected Interaction entry = null;
		protected HttpRequestMessage requestMessage;

		private void setEntryHeaders(HttpContentHeaders headers)
		{
			if (entry != null)
			{
				headers.LastModified = entry.When;
				// todo: header.contentlocation
				//headers.ContentLocation = entry.Key.ToUri(Localhost.Base); dit moet door de exporter gezet worden.

				if (entry.Resource is Binary)
				{
					Binary binary = (Binary)entry.Resource;
					headers.ContentType = new MediaTypeHeaderValue(binary.ContentType);
				}
			}
		}

		public override bool CanReadType(Type type)
		{
			bool can = typeof(Resource).IsAssignableFrom(type);  /* || type == typeof(Bundle) || (type == typeof(TagList) ) */
			return can;
		}

		public override bool CanWriteType(Type type)
		{
			return typeof(Resource).IsAssignableFrom(type);
		}

		public override void SetDefaultContentHeaders(Type type, HttpContentHeaders headers, MediaTypeHeaderValue mediaType)
		{
			base.SetDefaultContentHeaders(type, headers, mediaType);
			setEntryHeaders(headers);
		}

		public override MediaTypeFormatter GetPerRequestFormatterInstance(Type type, HttpRequestMessage request, MediaTypeHeaderValue mediaType)
		{
			this.entry = request.GetEntry();
			this.requestMessage = request;
			return base.GetPerRequestFormatterInstance(type, request, mediaType);
		}

		protected string ReadBodyFromStream(Stream readStream, HttpContent content)
		{
			var charset = content.Headers.ContentType.CharSet ?? Encoding.UTF8.HeaderName;
			var encoding = Encoding.GetEncoding(charset);

			if (encoding != Encoding.UTF8)
				throw ExceptionHandling.Error.BadRequest("FHIR supports UTF-8 encoding exclusively, not " + encoding.WebName);

			StreamReader reader = new StreamReader(readStream, Encoding.UTF8, true);
			return reader.ReadToEnd();
		}

	}
}