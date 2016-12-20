﻿using Alphora.Dataphor.Dataphoria.Web.FHIR.Models;
using Hl7.Fhir.Model;
using System.Collections.Generic;

namespace Alphora.Dataphor.Dataphoria.Web.FHIR.Extensions
{
	public static class FhirModelExtensions
	{
		public static void Append(this Bundle bundle, Resource resource)
		{
			bundle.Entry.Add(CreateEntryForResource(resource));
		}

		public static void Append(this Bundle bundle, Bundle.HTTPVerb method, Resource resource)
		{
			Bundle.EntryComponent entry = CreateEntryForResource(resource);

			if (entry.Request == null) entry.Request = new Bundle.RequestComponent();
			entry.Request.Method = method;
			bundle.Entry.Add(entry);
		}

		private static Bundle.EntryComponent CreateEntryForResource(Resource resource)
		{
			var entry = new Bundle.EntryComponent();
			entry.Resource = resource;
			//            entry.FullUrl = resource.ResourceIdentity().ToString();
			entry.FullUrl = resource.ExtractKey().ToUriString();
			return entry;
		}

		public static void Append(this Bundle bundle, IEnumerable<Resource> resources)
		{
			foreach (Resource resource in resources)
			{
				bundle.Append(resource);
			}
		}

		public static void Append(this Bundle bundle, Bundle.HTTPVerb method, IEnumerable<Resource> resources)
		{
			foreach (Resource resource in resources)
			{
				bundle.Append(method, resource);
			}
		}

		public static Bundle Append(this Bundle bundle, Interaction interaction)
		{
			// API: The api should have a function for this. AddResourceEntry doesn't cut it.
			// Might TransactionBuilder be better suitable?

			Bundle.EntryComponent entry;
			switch (bundle.Type)
			{
				case Bundle.BundleType.History: entry = interaction.ToTransactionEntry(); break;
				case Bundle.BundleType.Searchset: entry = interaction.TranslateToSparseEntry(); break;
				default: entry = interaction.TranslateToSparseEntry(); break;
			}
			bundle.Entry.Add(entry);

			return bundle;
		}

		public static Bundle Append(this Bundle bundle, IEnumerable<Interaction> interactions)
		{
			foreach (Interaction interaction in interactions)
			{
				// BALLOT: whether to send transactionResponse components... not a very clean solution
				bundle.Append(interaction);
			}

			// NB! Total can not be set by counting bundle elements, because total is about the snapshot total
			// bundle.Total = bundle.Entry.Count();

			return bundle;
		}

		public static IList<Interaction> GetInteractions(this ILocalhost localhost, Bundle bundle)
		{
			var interactions = new List<Interaction>();
			foreach (var entry in bundle.Entry)
			{
				Interaction interaction = localhost.ToInteraction(entry);
				interactions.Add(interaction);
			}
			return interactions;
		}

	}
}