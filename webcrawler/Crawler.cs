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
    /// Main program container 
    /// </summary>
    class Crawler
    {
        /// <param name="args"> Command line arguements passed to executable. </param>
        static async Task Main(string[] args)
        {
            // "Log" from Serilog namespace
            // Configure the logging tool for nice command line prints/formats
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .WriteTo.Console()
                .WriteTo.File("./log/log.txt")
                .CreateLogger();

            Log.Logger.Information("Minimal Crawler Demo start...");

            CrawlConfigurationX crawlConfig = new CrawlConfigurationX
            {
                MaxPagesToCrawl = 10,                             // Number of sites to crawl
                IsJavascriptRenderingEnabled = true,              // Should crawler render JS?
                JavascriptRenderingWaitTimeInMilliseconds = 3000, // How long to wait for js to process 
                MaxConcurrentSiteCrawls = 1                      // Only crawl a single site at a time
                // ? MaxConcurrentThreads = 8                         // Logical processor count to avoid cpu thrashing
            };

            await DataScraper.Crawl(crawlConfig, args[0]);

            if (DataScraper.dataDocuments.Count > 0)
            {
                for (int i = 0; i < DataScraper.dataDocuments.Count; i++)
                {
                    Log.Logger.Information(DataScraper.dataDocuments[i].ToJson());
                    // TODO mongoDB add document ( DataScraper.dataDocuments[i])
                }
            }

            // var client = new MongoClient("mongodb+srv://<username>:<password>@<cluster-address>/test?w=majority");
            // var database = client.GetDatabase("test");
        }
    }
#endregion
}