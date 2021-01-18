#region using statements
// .NET
using System;
using System.Threading.Tasks;

// Abot2
using Abot2.Core;      // Core components <change this comment later this is a bad description>
using Abot2.Crawler;   // Namespace where Crawler objects are defined
using Abot2.Poco;      //

// AbotX2
using AbotX2.Crawler;  //
using AbotX2.Parallel; //
using AbotX2.Poco;     //

// Logger
using Serilog;         // Serilog provides diagnostic logging to files

//Linq ?? 
#endregion



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
            #region Normal Polite Crawler
            Log.Logger.Information("Running Abot2 polite crawler...\n");
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

            // Subscribes method "PageCrawlMethod" to the PageCrawlCompleted event
            //PageCrawlMethod is now executed on PageCrawlCompleted
            crawler.PageCrawlCompleted += PageCrawlMethod;

            // Change the URL inside of the Uri object to have this crawler crawl somewhere else
            var crawlResult = await crawler.CrawlAsync(new Uri(uriToCrawl));
            #endregion
            
            #region X Crawler
            
            // Log.Logger.Information("Running new X crawler...\n");
            
            // var configX = new CrawlConfigurationX
            // {
            //     MaxPagesToCrawl = 10,
            //     IsJavascriptRenderingEnabled = true,
            //     JavascriptRenderingWaitTimeInMilliseconds = 3000, //How long to wait for js to process 
            //     MaxConcurrentSiteCrawls = 1,                      //Only crawl a single site at a time
            //     MaxConcurrentThreads = 8,                         //Logical processor count to avoid cpu thrashing
            // };

            // var crawlerX = new CrawlerX(configX);
            
            // crawlerX.ShouldRenderPageJavascript((CrawledPage, CrawlContext) =>
            // {
            //     if (CrawledPage.Uri.AbsoluteUri.Contains("ghost"))
            //         return new CrawlDecision { Allow = false, Reason = "scared to render ghost javascript." };
                
            //     return new CrawlDecision { Allow = true };
            // });

            // crawlerX.PageCrawlCompleted += PageCrawlMethod;

            // var crawlerXTask = await crawlerX.CrawlAsync(new Uri(uriToCrawl));
            
            #endregion
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

        private static void PageCrawlMethod(object sender, PageCrawlCompletedArgs e)
        {
            var httpStatus = e.CrawledPage.HttpResponseMessage.StatusCode;
            var rawPageText = e.CrawledPage.Content.Text;
            Log.Logger.Information(rawPageText);
        }
    }
#endregion
}