Static-Blog
===========

ASP.NET MVC website for generating static files for [http://blog.primoca.com](http://blog.primoca.com)

Architecture 
-------
Most existing static website generators strive to be a standalone tool. I find that a bit too rigid, there's more power and flexibility in decoupling the page rendering from the saving of output. I created a dynamic web app that runs locally then use a generic web crawler to scrape the output. The benefit is I can use the full capability of the web development tools I already know and test and debug just like any other web app. It also opens up more options like you could store content in a DB or deploy the web app to a staging server and scrape it from anywhere.

Features
------------
+ Browser based editing using [Markdown](http://en.wikipedia.org/wiki/Markdown)
+ Indexing of posts by Year/Month and Tags
+ RSS feed
+ Text file based storage

Crawler
---------
I use [HTTrack](http://www.httrack.com/) to crawl the web app and save the output to static files. Here are the configuration options I changed so the site would generate correctly:

+ Links -> Attempt to detect all links (even in unknown tags/javascript code) -> **Unchecked**
+ Browser ID -> HTML footer -> **(none)**
+ Experts Only -> Rewrite Links: internal / external -> **Original URL / Original URL**

MIT License
--------
