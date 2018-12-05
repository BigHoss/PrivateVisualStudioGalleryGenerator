namespace UpdateAtomFeed
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.IO.Compression;
	using System.Linq;
	using System.Net;
	using System.Reflection;
	using System.ServiceModel.Syndication;
	using System.Xml;
	using System.Xml.Linq;

	class Program
	{
		const string FeedTitle = "MyPrivateVsixGallery";
		const string FeedId = "MyPrivateVsixGalleryV1";
		const string FeedFileName = "MyPrivateVsixGallery.xml";
		const string ExtensionVsixManifest = "extension.vsixmanifest";

		class FeedEntryInfo
		{
			public string Id { get; set; }
			public string Author { get; set; }
			public string Title { get; set; }
			public string Description { get; set; }
			public string Version { get; set; }
		}

		static void UpdateFeed(string vsixPath, string destination)
		{
			Atom10FeedFormatter feed = GenerateFeed(vsixPath, destination);
			WriteFeed(feed, destination);
		}
		static Atom10FeedFormatter GenerateFeed(string vsixPath, string destination)
		{
			SyndicationFeed feed = new SyndicationFeed
			{
				Title = new TextSyndicationContent(FeedTitle),
				Id = FeedId
			};
			List<SyndicationItem> items = new List<SyndicationItem> { GenerateFeedEntry(vsixPath, destination) };
			AddFeedEntries(items, destination);
			feed.Items = items;
			Atom10FeedFormatter result = new Atom10FeedFormatter(feed);
			Console.WriteLine("Feed generated");
			return result;
		}
		static void AddFeedEntries(List<SyndicationItem> items, string destination)
		{
			HashSet<string> ids = new HashSet<string>();
			foreach (SyndicationItem item in items)
			{
				ids.Add(item.Id);
			}

			string destinationFeedPath = Path.Combine(destination, FeedFileName);
			if (!File.Exists(destinationFeedPath))
			{
				return;
			}

			using (Stream stream = File.Open(destinationFeedPath, FileMode.Open))
			{
				XmlReader xmlReader = XmlReader.Create(stream);
				SyndicationFeed feed = SyndicationFeed.Load(xmlReader);
				foreach (SyndicationItem item in feed.Items)
				{
					if (!ids.Contains(item.Id))
					{
						items.Add(item);
					}
				}
			}
		}
		static XElement GetElement(XElement element, string name)
		{
			return GetElement(element.Elements(), name);
		}
		static XElement GetElement(IEnumerable<XElement> elements, string name)
		{
			return elements.FirstOrDefault(e => e.Name.LocalName == name);
		}
		static string GetAttributeValue(XElement identityElement, string name)
		{
			return identityElement.Attributes(name).First().Value;
		}
		static FeedEntryInfo GetFeedEntryInfo(string vsixPath, string destination)
		{
			string fileName = Path.GetFileName(vsixPath);
			string destinationPath = Path.Combine(destination, fileName ?? throw new ArgumentNullException(nameof(vsixPath)));
			using (ZipArchive zipArchive = ZipFile.Open(destinationPath, ZipArchiveMode.Read))
			{
				ZipArchiveEntry entry = zipArchive.GetEntry(ExtensionVsixManifest);
				XDocument doc;
				using (Stream stream = entry?.Open())
				{
					doc = XDocument.Load(stream);
				}

				XElement metadataElement = GetElement(doc.Root, "Metadata");
				XElement identityElement = GetElement(metadataElement, "Identity");

				return new FeedEntryInfo()
				{
					Id = GetAttributeValue(identityElement, "Id"),
					Author = GetAttributeValue(identityElement, "Publisher"),
					Title = GetElement(metadataElement, "DisplayName").Value,
					Description = GetElement(metadataElement, "Description").Value,
					Version = GetAttributeValue(identityElement, "Version")
				};
			}
		}
		static SyndicationItem GenerateFeedEntry(string vsixPath, string destination)
		{
			SyndicationItem feedEntry = new SyndicationItem();
			string fileName = Path.GetFileName(vsixPath);
			feedEntry.Content = new UrlSyndicationContent(new Uri(fileName ?? throw new ArgumentNullException(nameof(vsixPath)), UriKind.Relative), "octet/stream");
			var feedEntryInfo = GetFeedEntryInfo(vsixPath, destination);
			UpdateFeedEntry(feedEntry, feedEntryInfo);
			return feedEntry;
		}
		static void UpdateFeedEntry(SyndicationItem feedEntry, FeedEntryInfo info)
		{
			string id = info.Id;
			string author = info.Author;
			string title = info.Title;
			string description = info.Description;
			string version = info.Version;

			string ns = @"http://schemas.microsoft.com/developer/vsx-syndication-schema/2010";
			feedEntry.Id = id;
			feedEntry.Authors.Add(new SyndicationPerson()
			{
				Name = author
			});
			feedEntry.Title = new TextSyndicationContent(title);
			feedEntry.Summary = new TextSyndicationContent(description);

			var vsixElement = new XElement(XName.Get("Vsix", ns),
														   new XElement(XName.Get("Id", ns), id),
														   new XElement(XName.Get("Version", ns), version)).
												  CreateReader();
			feedEntry.ElementExtensions.Add(vsixElement);
		}
		static void WriteFeed(Atom10FeedFormatter feed, string destination)
		{
			XmlWriterSettings settings = new XmlWriterSettings {OmitXmlDeclaration = false, Indent = true};
			string destinationFeedPath = Path.Combine(destination, FeedFileName);
			if (File.Exists(destinationFeedPath))
			{
				File.Delete(destinationFeedPath);
			}

			using (XmlWriter xmlWriter = XmlWriter.Create(destinationFeedPath, settings))
			{
				feed.WriteTo(xmlWriter);
			}

			Console.WriteLine("Feed written");
		}
		static void CopyToDestination(string sourcePath, string destinationFolder)
		{
			if (string.IsNullOrEmpty(sourcePath) || string.IsNullOrEmpty(destinationFolder))
			{
				return;
			}

			string vsixName = Path.GetFileName(sourcePath);
			if (!File.Exists(sourcePath))
			{
				string sourceDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
				if (sourceDir != null) sourcePath = Path.Combine(sourceDir, vsixName);
			}
			if (string.IsNullOrEmpty(sourcePath) && !File.Exists(sourcePath))
			{
				return;
			}

			string destinationFile = Path.Combine(destinationFolder, vsixName);
			if (File.Exists(destinationFile))
			{
				File.Delete(destinationFile);
				Console.WriteLine("File deleted");
			}

			File.Copy(sourcePath, destinationFile);
			Console.WriteLine("File copied");
		}
		/// <summary>
		///
		/// </summary>
		/// <param name="args">%1 => VSIX Path, %2 => Destinationpath(URI/networkpath</param>
		static void Main(string[] args)
		{
			Console.WriteLine(string.Join(", ", args));
			string vsixPath = args[0];
			string destination = args[1];
			try
			{
				if (destination.StartsWith("\\\\"))
				{
					string userName = args[2];
					string passWord = args[3];

					NetworkCredential theNetworkCredential = new NetworkCredential(userName, passWord);
					CredentialCache unused = new CredentialCache { { new Uri(destination), "Basic", theNetworkCredential } };
				}
				CopyToDestination(vsixPath, destination);
				UpdateFeed(vsixPath, destination);
			}
			catch (Exception e)
			{
				Console.WriteLine(e);
				throw;
			}
		}
	}
}