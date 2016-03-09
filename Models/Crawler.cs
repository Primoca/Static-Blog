using HtmlAgilityPack;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using System.Web;
using System.Web.Hosting;
using System.Timers;
//using log4net;
using System.IO;
using Microsoft.WindowsAzure;
using Amazon.S3;
using Static_Blog.Models;

namespace Static_Blog.Models
{
    public class Logg
    {
        public List<string> entires = new List<string>();
        public void Debug(string s)
        {
            entires.Add(s);
        }
        public void Error(string s, Exception ex)
        {
            entires.Add(s + ex.Message);
        }
    }

    public class Crawler : IRegisteredObject
    {
        public static readonly AmazonS3Client S3 = new AmazonS3Client();
        //private static readonly ILog log = LogManager.GetLogger(typeof(Crawler));
        public Logg log = new Logg();

        CancellationTokenSource _cts = new CancellationTokenSource();
        ConcurrentDictionary<string, Uri> _crawledLinks = new ConcurrentDictionary<string, Uri>();
        ConcurrentBag<string> _errorLinks = new ConcurrentBag<string>();
        HashSet<string> _remainingLinks = new HashSet<string>();
        System.Timers.Timer _timer = new System.Timers.Timer(600000);
        Guid _id;
        bool _isCompleted;
        CancelledReason _cancelledReason = CancelledReason.None;
        Uri _startUrl;
        string _rootAction;
        public Guid Id { get { return _id; } }
        public bool IsCompleted { get { return _isCompleted; } }
        public CancelledReason CancelReason { get { return _cancelledReason; } }
        public string CancelReasonCssClass
        {
            get
            {
                switch (CancelReason)
                {
                    case CancelledReason.None:
                        return "success";
                    case CancelledReason.User:
                    case CancelledReason.Timeout:
                    case CancelledReason.AppDomainShutdown:
                    case CancelledReason.MaxLinks:
                    case CancelledReason.Error:
                        return "error";
                    default:
                        throw new NotImplementedException();
                }
            }
        }
        public string[] CrawledUrls { get { return _crawledLinks.Keys.ToArray(); } }
        public Uri StartUrl { get { return _startUrl; } }
        public IEnumerable<string> ErrorUrls { get { return _errorLinks; } }

        public Crawler()
        {
            _id = Guid.NewGuid();
            _timer.AutoReset = false;
            _timer.Elapsed += timer_Elapsed;
            _rootAction = "index.html";
        }

        public static string GetCrawlerKey(Guid id, Guid appId)
        {
            return "CrawlerId:" + id.ToString() + " AppId:" + appId.ToString();
        }

        void timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            //log.Warn("Timeout cancel: " + CrawlerKey);
            _cancelledReason = CancelledReason.Timeout;
            _cts.Cancel();
        }

        public void Cancel()
        {
            //log.Warn("User cancel: " + CrawlerKey);
            _cancelledReason = CancelledReason.User;
            _cts.Cancel();
        }

        public void Stop(bool immediate)
        {
            //log.Warn("AppDomain shutdown cancel: " + CrawlerKey);
            _cancelledReason = CancelledReason.AppDomainShutdown;
            _cts.Cancel();
        }

        public async Task Crawl(Uri start, string basicUser, string basicPassword)
        {
            try
            {
                _startUrl = start;
                _timer.Enabled = true;
                HostingEnvironment.RegisterObject(this);

                var broadcastBlock = new BroadcastBlock<UriItem>(li => li, new DataflowBlockOptions { CancellationToken = _cts.Token });

                Func<UriItem, Task<IEnumerable<UriItem>>> downloadFromLink =
                    async link =>
                    {
                        var nextLinks = new UriItem[] { };
                        try
                        {
                            log.Debug(link.AbsoluteUri);
                            if (_crawledLinks.Count >= 10000)
                            {
                                //log.Warn("Max Links cancel: " + _crawledLinks.Count + " " + CrawlerKey + " " + start.AbsoluteUri);
                                _cancelledReason = CancelledReason.MaxLinks;
                                _cts.Cancel();
                            }

                            if (link.Level < 20 && !_cts.IsCancellationRequested && _crawledLinks.TryAdd(link.AbsoluteUri, link))
                            {
                                using (var client = new HttpClient(new HttpClientHandler { AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate, AllowAutoRedirect = false }))
                                {
                                    client.DefaultRequestHeaders.UserAgent.ParseAdd("PrimocaCrawler (http://www.primoca.com)");
                                    if (!string.IsNullOrEmpty(basicUser) || !string.IsNullOrEmpty(basicPassword))
                                    {
                                        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.ASCII.GetBytes(basicUser + ":" + basicPassword)));
                                    }
                                    HttpResponseMessage r = null;
                                    try
                                    {
                                        r = await client.GetAsync(link, HttpCompletionOption.ResponseHeadersRead, _cts.Token);
                                    }
                                    catch (Exception ex)
                                    {
                                        log.Error(link.AbsoluteUri, ex);
                                        _errorLinks.Add(link.AbsoluteUri);
                                    }
                                    if (r != null)
                                    {
                                        if (r.IsSuccessStatusCode)
                                        {
                                            using (var s = await r.Content.ReadAsStreamAsync())
                                            {
                                                try {
                                                    var pst = s;
                                                    if (r.Content.Headers.ContentType != null && r.Content.Headers.ContentType.MediaType != null &&
                                                            (r.Content.Headers.ContentType.MediaType.StartsWith("text/html", StringComparison.OrdinalIgnoreCase)
                                                            || r.Content.Headers.ContentType.MediaType.StartsWith("text/css", StringComparison.OrdinalIgnoreCase)
                                                            || r.Content.Headers.ContentType.MediaType.StartsWith("text/xml", StringComparison.OrdinalIgnoreCase)))
                                                    {
                                                        var html = await new StreamContent(s).ReadAsStringAsync();
                                                        //var modHtml = Regex.Replace(html, start.GetLeftPart(UriPartial.Authority), "http://" + _app.ActiveHostname, RegexOptions.IgnoreCase);
                                                        //if (!Object.ReferenceEquals(html, modHtml))
                                                        //{
                                                        //	pst = new MemoryStream(Encoding.UTF8.GetBytes(modHtml));
                                                        //}
                                                        pst = new MemoryStream(Encoding.UTF8.GetBytes(html));
                                                        nextLinks = Find(html, r.RequestMessage.RequestUri).Where(x => !_crawledLinks.ContainsKey(x.AbsoluteUri)).Select(x => new UriItem(x, link.Level + 1)).ToArray();
                                                    }
                                                    else
                                                    {
                                                        pst = new MemoryStream(await new StreamContent(s).ReadAsByteArrayAsync());
                                                    }
                                                    var request = new Amazon.S3.Model.PutObjectRequest
                                                    {
                                                        BucketName = CloudConfigurationManager.GetSetting("bucket"),
                                                        InputStream = pst,
                                                        Key = (link.PathAndQuery.EndsWith("/") ? (link.PathAndQuery + _rootAction) : link.PathAndQuery).Remove(0, 1)
                                                    };
                                                    var p = S3.PutObject(request);
                                                }
                                                catch(Exception ex)
                                                {
                                                    log.Error("Error:" + r.StatusCode + " " + link.AbsoluteUri, ex);
                                                    throw ex;
                                                }
                                            }
                                        }
                                        else if ((int)r.StatusCode >= 400)
                                        {
                                            log.Debug("Crawl Error code: " + r.StatusCode + " " + link.AbsoluteUri);
                                            _errorLinks.Add(link.AbsoluteUri);
                                        }
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            ((IDataflowBlock)broadcastBlock).Fault(ex);
                        }
                        finally
                        {
                            lock (_remainingLinks)
                            {
                                _remainingLinks.Remove(link.AbsoluteUri);
                                foreach (var l in nextLinks)
                                {
                                    _remainingLinks.Add(l.AbsoluteUri);
                                }
                                if (_remainingLinks.Count == 0)
                                {
                                    broadcastBlock.Complete();
                                }
                            }
                        }
                        return nextLinks;
                    };

                var linkFinderBlock = new TransformManyBlock<UriItem, UriItem>(downloadFromLink, new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = 4 });

                linkFinderBlock.LinkTo(broadcastBlock);
                broadcastBlock.LinkTo(linkFinderBlock);
                _remainingLinks.Add(start.AbsoluteUri);
                broadcastBlock.Post(new UriItem(start, 0));

                await broadcastBlock.Completion;
            }
            catch (Exception ex)
            {
                if (!(ex is TaskCanceledException))
                {
                    _cancelledReason = CancelledReason.Error;
                    log.Error(start.AbsoluteUri, ex);
                }
            }

            HostingEnvironment.UnregisterObject(this);
            _timer.Enabled = false;
            _timer.Dispose();
            _isCompleted = true;
        }

        public static IEnumerable<Uri> Find(string html, Uri url)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(html);
            var baseNodes = doc.DocumentNode.SelectNodes("//base[@href]");
            string baseHREF = baseNodes == null ? "" : baseNodes.Select(x => x.Attributes["href"].Value).FirstOrDefault();

            var links = new List<string>();
            var aNodes = doc.DocumentNode.SelectNodes("//a[@href] | //link[@href]");
            if (aNodes != null)
            {
                links.AddRange(aNodes.Select(x => x.Attributes["href"].Value));
            }
            var scriptNodes = doc.DocumentNode.SelectNodes("//script[@src] | //img[@src]");
            if (scriptNodes != null)
            {
                links.AddRange(scriptNodes.Select(x => x.Attributes["src"].Value));
            }

            int index = 0;
            while (index >= 0)
            {
                index = html.IndexOf("url(", index, StringComparison.OrdinalIgnoreCase);
                if (index >= 0)
                {
                    int endindex = html.IndexOf(")", index + 4);
                    if (endindex >= 0)
                    {
                        int start = index + 4 + ((html[index + 4] == '"' || html[index + 4] == '\'') ? 1 : 0);
                        links.Add(html.Substring(start, endindex - start).TrimEnd('\'', '"'));
                        index = endindex;
                    }
                }
            }

            var distinctLinks = new Dictionary<string, Uri>();
            foreach (var l in links)
            {
                try
                {
                    var b = string.IsNullOrEmpty(baseHREF) ? url : new Uri(baseHREF);
                    var u = new Uri(b, l);
                    var uNoFragment = string.IsNullOrEmpty(u.Fragment) ? u.AbsoluteUri : u.AbsoluteUri.Replace(u.Fragment, "");
                    if (!distinctLinks.ContainsKey(uNoFragment) && b.GetLeftPart(UriPartial.Authority).Equals(u.GetLeftPart(UriPartial.Authority), StringComparison.OrdinalIgnoreCase))
                    {
                        distinctLinks.Add(uNoFragment, new Uri(uNoFragment));
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception("baseHREF:" + baseHREF + " URL:" + url.AbsoluteUri + " link:" + l, ex);
                }

            }
            return distinctLinks.Values;
        }

        private class UriItem : Uri
        {
            int _level = 0;

            public UriItem(string uriString, int level)
                : base(uriString)
            {
                _level = level;
            }

            public UriItem(Uri uri, int level)
                : this(uri.AbsoluteUri, level)
            {

            }
            public int Level { get { return _level; } }
        }

        public enum CancelledReason
        {
            None,
            Timeout,
            User,
            AppDomainShutdown,
            MaxLinks,
            Error
        }
    }

    public class md : HttpClientHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return base.SendAsync(request, cancellationToken);
        }
    }
}