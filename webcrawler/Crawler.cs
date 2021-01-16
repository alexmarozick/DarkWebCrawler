using System;
using System.Threading.Tasks;
using Abot2.Core;
using Abot2.Crawler;
using Abot2.Poco;
using Serilog;


namespace ScrapeAndCrawl
{
#region Crawler Class

    /// <summary>
    /// Simple webcrawler demo as shown in Abot repo. Run using: dotnet run "urlToCrawlFrom".
    /// By default it will crawl the Google.com homepage.
    /// </summary>
    class Crawler
    {
        static async Task Main(string[] args)
        {
            // "Log" from Serilog namespace
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .WriteTo.Console()
                .CreateLogger();
            
            Log.Logger.Information("Minimal Crawler Demo start...");

            // If a url is not provided then use default url
            if (args.Length == 0)
            {
                await SimpleCrawler();
                await SinglePageRequest();
            }
            else // use provided url passed in as an arg
            {
                await SimpleCrawler(args[0]);
                await SinglePageRequest(args[0]);
            }
        }

        /// <summary>
        /// Method that crawls starting from the passed in url
        /// </summary>
        /// <param name="uriToCrawl"> String representing the url to start crawling from </param>
        private static async Task SimpleCrawler(string uriToCrawl = "http://google.com")
        {
            // "CrawlConfiguration" from Abot2.Poco namespace
            // For specific configuration requirements the use of a "CrawlConfiguration" object
            // can be used when creating crawlers like the one bellow...
            var config = new CrawlConfiguration
            {
                MaxPagesToCrawl = 10, //Only crawl 10 pages
                MinCrawlDelayPerDomainMilliSeconds = 3000 //Wait this many millisecs between requests
            };

            // "PoliteWebCrawler" from Abot2.Crawler namespace
            // This creates a new "PoliteWebCrawler" object with configuration defined above
            var crawler = new PoliteWebCrawler(config);

            // Subscribes method "PageCrawlCompleted" to the PageCrawlCompleted event
            crawler.PageCrawlCompleted += PageCrawlCompleted;

            // Change the URL inside of the Uri object to have this crawler crawl somewhere else
            var crawlResult = await crawler.CrawlAsync(new Uri(uriToCrawl));
        }
        
        /// <summary>
        /// An asyncronous single page request to ping a single page from the uriToRequest arg
        /// </summary>
        private static async Task SinglePageRequest(string uriToRequest = "https://google.com")
        {
            var pageRequester = new PageRequester(new CrawlConfiguration(), new WebContentExtractor());

            var crawledPage = await pageRequester.MakeRequestAsync(new Uri(uriToRequest));
            Log.Logger.Information("{result}", new
            {
                url = crawledPage.Uri,
                status = Convert.ToInt32(crawledPage.HttpResponseMessage.StatusCode)
            });
        }

        private static void PageCrawlCompleted(object sender, PageCrawlCompletedArgs e)
        {
            var httpStatus = e.CrawledPage.HttpResponseMessage.StatusCode;
            var rawPageText = e.CrawledPage.Content.Text;
        }
    }
#endregion
}