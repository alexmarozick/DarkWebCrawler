#region Using Statements
// .NET
using System;
using System.Collections.Generic;
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

//BSON for mongo
using MongoDB.Bson;
#endregion


namespace ScrapeAndCrawl
{

    /// <summary>
    /// Utilizing AbotX (for js rendering) this object scrapes specified
    /// websites for keywords, uri links, and other hardcoded data. This
    /// data is then compiled into a data container to interface with.
    /// </summary>
    class DataScraper
    {
        /* ========== Public Members ========= */
        public static List<BsonDocument> dataDocuments = new List<BsonDocument>();

        /* ========== Private Members ======== */


        /* ======= Class Constructors ======== */
        public DataScraper() {}

        // ? public DataCrawler(CrawlConfigurationX configX) {}

#region Public Class Methods
        /* ================================= Class Methods {Public} ============================ */
        public static async Task CrawlSingleSite(string uriToCrawl = "http://google.com")
        {
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
            //add handler to be called when the crawl for that page is complete
            crawlerX.PageCrawlCompleted += SinglePageHandler;

            await crawlerX.CrawlAsync(new Uri(uriToCrawl));
        }

        // ? public static async Task CrawlMultipleSites(int numSites) {}
        // ? public static async Task Crawl() {}
#endregion

#region Private Class Methods
        /* ================================= Class Methods {Private} =========================== */

        /// <summary>
        /// Page Crawl Handler for Crawling a Single Page. Will parse the
        /// webpage and collect data?
        /// </summary>
        private static void SinglePageHandler(object sender, PageCrawlCompletedArgs e)
        {
            var httpStatus = e.CrawledPage.HttpResponseMessage.StatusCode;
            var rawPageText = e.CrawledPage.Content.Text;
    
            // TODO:
            // * do something with the raw content... 
            var bson = new BsonDocument
            {
                {"name", "test page"},
                {"raw", rawPageText}
            };

            dataDocuments.Add(bson);

            // * write to the dataContainer

            Log.Logger.Information(rawPageText);
        }
#endregion
    }
}