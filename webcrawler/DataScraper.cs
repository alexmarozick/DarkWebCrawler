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
        // ? public DataScraper() {}
        // ? public DataCrawler(CrawlConfigurationX configX) {}

#region Public Class Methods
        /* ================================= Class Methods {Public} ============================ */

        /// <summary>
        /// Static method for crawling. Pass in a configuration
        /// (i.e. specify how many sites to crawl, whether or not to 
        /// render js, etc) then creates and executes crawler
        /// </summary>
        public static async Task Crawl(CrawlConfigurationX configX, string uriToCrawl = "http://google.com")
        {
            // 'using' sets up scope for crawlerX object to be used
            // disposes of object at end of scope. (i.e. close-curly-brace)
            // I saw this used in the github example. Maybe its good practice??
            using (var crawlerX = new CrawlerX(configX))
            {
                crawlerX.ShouldRenderPageJavascript((CrawledPage, CrawlContext) =>
                {
                    if (CrawledPage.Uri.AbsoluteUri.Contains("ghost"))
                        return new CrawlDecision { Allow = false, Reason = "scared to render ghost javascript." };

                    return new CrawlDecision { Allow = true };
                });
                //add handler to be called when the crawl for that page is complete
                crawlerX.PageCrawlCompleted += PageCrawlHandler;

                await crawlerX.CrawlAsync(new Uri(uriToCrawl));
            }
        }
        
        // ? public static async Task CrawlWithSpecifiedConfig(CrawlConfigurationX configX, string uriToCrawlFrom) {}
#endregion

#region Private Class Methods
        /* ================================= Class Methods {Private} =========================== */

        /// <summary>
        /// Handles the PageCrawlCompleted event called by a given Crawler.
        /// Will parse each website and store data in a Bson Document.
        /// </summary>
        private static void PageCrawlHandler(object sender, PageCrawlCompletedArgs e)
        {
            var httpStatus = e.CrawledPage.HttpResponseMessage.StatusCode;
            var rawPageText = e.CrawledPage.Content.Text;
    
            // TODO:
            // * pase page content into data we want...

            // ? add private parsing methods to class and use them here

            // * Convert to BSON Doc and add to dataDocuments
            var bson = new BsonDocument
            {
                {"name", "test page"},
                {"raw", rawPageText}
            };

            dataDocuments.Add(bson);

            // ? Log.Logger.Information(rawPageText);
        }
#endregion
    }
}