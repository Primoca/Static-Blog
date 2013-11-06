using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
		public string DateId { get { return Date.ToString("yyyy/MM/"); } }
		public string TitleId { get { return Title.ToLower().Replace(' ', '-'); } }

		public HtmlString HtmlText { get { return new HtmlString(new MarkdownSharp.Markdown().Transform(MarkdownText)); } }
		public HtmlString HtmlShort { get { return new HtmlString(new MarkdownSharp.Markdown().Transform(MarkdownText.Substring(0, MarkdownText.IndexOf(' ', 400)) + "...")); } }

		private static string GetPostsPath()
		{
			return HttpContext.Current.Server.MapPath("~/Content/posts") + "/";
		}

		public static void Save(Post post)
		{
			var path = GetPostsPath() + post.DateId;
			Directory.CreateDirectory(path);
			File.WriteAllText(path + post.TitleId + ".txt", JsonConvert.SerializeObject(post));
		}

		public static Post Get(string id)
		{
			return JsonConvert.DeserializeObject<Post>(File.ReadAllText(GetPostsPath() + id + ".txt"));
		}

		public static Post[] Get(int? year, int? month)
		{
			var posts = Directory.GetFiles(GetPostsPath(), "*.*", SearchOption.AllDirectories).Select(x => JsonConvert.DeserializeObject<Post>(File.ReadAllText(x))).OrderByDescending(x => x.Date).ToArray();
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

		public static Dictionary<string, string[]> GetArchive()
		{
			return Directory.GetDirectories(GetPostsPath()).OrderByDescending(x => x).ToDictionary(x => x.Replace(GetPostsPath(), ""), x => Directory.GetDirectories(x).OrderByDescending(s => s).Select(s => s.Replace(x + "\\", "")).ToArray());
		}
	}
}