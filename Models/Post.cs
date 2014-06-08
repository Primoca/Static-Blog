using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Caching;
using System.Threading;
using System.Web;

namespace Static_Blog.Models
{
	public class Post
	{
		public string MarkdownText { get; set; }
		public DateTime Date { get; set; }
		public string Tags { get; set; }
		public List<string> TagList { get { return string.IsNullOrEmpty(Tags) ? new List<string>() : Tags.Split(',').Select(s => s.Trim()).ToList(); } }
		public string Title { get; set; }
		public string Author { get; set; }

		public string FullId { get { return DateId + TitleId + "/"; } }
		public string StorageId { get { return "posts/" + DateId + TitleId + ".txt"; } }
		public string DateId { get { return Date.ToString("yyyy/MM/"); } }
		public string TitleId { get { return Title.ToLower().Replace(' ', '-'); } }

		[JsonIgnore]
		public HtmlString HtmlText { get { return new HtmlString(new MarkdownSharp.Markdown().Transform(MarkdownText)); } }
		[JsonIgnore]
		public HtmlString HtmlShort
		{
			get
			{
				var space = MarkdownText.IndexOf(' ', MarkdownText.Length > 400 ? 400 : MarkdownText.Length / 2);
				var tag = MarkdownText.IndexOf('<');
				if (tag > 0)
				{
					space = Math.Min(tag, space);
				}
				return new HtmlString(new MarkdownSharp.Markdown().Transform(MarkdownText.Substring(0, space)));
			}
		}

		private static CloudBlobContainer GetContainer()
		{
			CloudStorageAccount storageAccount = CloudStorageAccount.Parse(CloudConfigurationManager.GetSetting("StorageConnectionString"));
			var cl = storageAccount.CreateCloudBlobClient();
			return cl.GetContainerReference("blog");
		}

		public static void Save(Post post)
		{
			var c = GetContainer();
			var b = c.GetBlockBlobReference(post.StorageId);
			b.UploadText(JsonConvert.SerializeObject(post));
		}

		public static Post Get(string id)
		{
			var c = GetContainer();
			var b = c.GetBlockBlobReference("posts/" + id + ".txt");
			return JsonConvert.DeserializeObject<Post>(b.DownloadText());
		}

		private static readonly SemaphoreSlim _Lock = new SemaphoreSlim(1);
		private static readonly string _allCacheKey = "allposts";
		private static Post[] GetAllCached()
		{
			_Lock.Wait();
			try
			{
				var all = (Post[])MemoryCache.Default.Get(_allCacheKey);
				if (all == null)
				{
					var c = GetContainer();
					var blobs = c.ListBlobs(prefix: "posts/",useFlatBlobListing: true);
					all = blobs.Select(x => JsonConvert.DeserializeObject<Post>(((CloudBlockBlob)x).DownloadText())).OrderByDescending(x => x.Date).ToArray();
					MemoryCache.Default.Add(_allCacheKey, all, DateTimeOffset.Now.AddSeconds(30));
				}
				return all;
			}
			finally
			{
				_Lock.Release();
			}
		}

		public static Post[] Get(int? year, int? month)
		{
			var posts = GetAllCached();
			if (!year.HasValue)
			{
				return posts;
			}
			if (!month.HasValue)
			{
				return posts.Where(x => x.Date.Year == year.Value).OrderByDescending(x => x.Date).ToArray();
			}
			return posts.Where(x => x.Date.Year == year.Value && x.Date.Month == month.Value).OrderByDescending(x => x.Date).ToArray();
		}

		public static string[] GetTags()
		{
			return Get(null, null).SelectMany(x => x.TagList).Distinct().OrderBy(x => x).ToArray();
		}

		public static Post[] GetTag(string tag)
		{
			return Get(null, null).Where(x => x.TagList.Contains(tag)).OrderByDescending(x => x.Date).ToArray();
		}

		public static Dictionary<int, int[]> GetArchive()
		{
			return GetAllCached().GroupBy(x => x.Date.Year).ToDictionary(x => x.Key, x => x.Select(y => y.Date.Month).Distinct().ToArray());
		}

		public static string[] GetAssets()
		{
			var c = GetContainer();
			var blobs = c.ListBlobs(prefix: "assets/", useFlatBlobListing: true);
			return blobs.Select(x => (CloudBlockBlob)x).Select(x => x.Name).ToArray();
		}

		public static void SaveAsset(string name, Stream s)
		{
			var c = GetContainer();
			var b = c.GetBlockBlobReference("assets/" + DateTime.Now.Year + "/" + name);
			b.UploadFromStream(s);
		}

		public static Stream GetAsset(string id)
		{
			var c = GetContainer();
			var b = c.GetBlockBlobReference("assets/" + id);
			var m = new MemoryStream();
			b.DownloadToStream(m);
			m.Position = 0;
			return m;
		}
	}
}