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

//mongoDB
using MongoDB.Bson;
using MongoDB.Driver;
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
            // if (args.Length == 0)
            // {
            //     await SimpleCrawler();
            //     await SinglePageRequest();
            // }
            // else // use provided url passed in as an arg
            // {
            //     await SimpleCrawler(args[0]);
            //     await SinglePageRequest(args[0]);
            // }

            DataScraper ds = new DataScraper();

            await DataScraper.CrawlSingleSite(args[0]);

            if (DataScraper.dataDocuments.Count > 0)
            {
                for (int i = 0; i < DataScraper.dataDocuments.Count; i++)
                {
                    Log.Logger.Information(DataScraper.dataDocuments[i].ToJson());
                    // mongoDB add document ( DataScraper.dataDocuments[i])
                }
            }

            // var client = new MongoClient("mongodb+srv://<username>:<password>@<cluster-address>/test?w=majority");
            // var database = client.GetDatabase("test");
        }

        /// <summary>
        /// Method that crawls starting from the passed in url
        /// </summary>
        /// <param name="uriToCrawl"> String representing the url to start crawling from </param>
        private static async Task SimpleCrawler(string uriToCrawl = "http://google.com")
        {   
            #region X Crawler
            
            Log.Logger.Information("Running new X crawler...\n");
            
            var configX = new CrawlConfigurationX
            {
                MaxPagesToCrawl = 10,
                IsJavascriptRenderingEnabled = true,
                JavascriptRenderingWaitTimeInMilliseconds = 3000, //How long to wait for js to process 
                MaxConcurrentSiteCrawls = 1,                      //Only crawl a single site at a time
                MaxConcurrentThreads = 8,                         //Logical processor count to avoid cpu thrashing
            };

            var crawlerX = new CrawlerX(configX);
            
            crawlerX.ShouldRenderPageJavascript((CrawledPage, CrawlContext) =>
            {
                if (CrawledPage.Uri.AbsoluteUri.Contains("ghost"))
                    return new CrawlDecision { Allow = false, Reason = "scared to render ghost javascript." };

                return new CrawlDecision { Allow = true };
            });

            crawlerX.PageCrawlCompleted += PageCrawlMethod;

            var crawlerXTask = await crawlerX.CrawlAsync(new Uri(uriToCrawl));
            
            #endregion

            #region AbotX Crawler Demo (from github)

            var pathToPhantomJSExeFolder = @"C:\Users\Setht\.nuget\packages\phantomjs\2.1.1\tools\phantomjs";
            var config = new CrawlConfigurationX
            {
                IsJavascriptRenderingEnabled = true,
                JavascriptRendererPath = pathToPhantomJSExeFolder,
                IsSendingCookiesEnabled = true,
                MaxConcurrentThreads = 1,
                MaxPagesToCrawl = 1,
                JavascriptRenderingWaitTimeInMilliseconds = 3000,
                CrawlTimeoutSeconds = 20
            };

            using (var crawler = new CrawlerX(config))
            {
                crawler.PageCrawlCompleted += PageCrawlMethod;

                await crawler.CrawlAsync(new Uri(uriToCrawl));
            }

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