#region Using Statements
// .NET
using System;
using System.Threading.Tasks;

// AbotX2
using AbotX2.Crawler;  //
using AbotX2.Parallel; //
using AbotX2.Poco;     //

// Logger
using Serilog;         // Serilog provides diagnostic logging to files
#endregion


namespace ScrapeAndCrawl
{
    /// <summary>
    /// Container for storing specifc kinds of data. Used in conjuction
    /// with the DataScraper object.
    /// </summary>
    class DataContainer
    {

    }

    /// <summary>
    /// Utilizing AbotX (for js rendering) this object scrapes specified
    /// websites for keywords, uri links, and other hardcoded data. This
    /// data is then compiled into a data container to interface with.
    /// </summary>
    class DataScraper
    {
        /* ========== Public Members ========= */
        public DataContainer data;

        /* ========== Private Members ======== */


        /* ======= Class Constructors ======== */
        public DataScraper()
        {
            data = new DataContainer();
        }
        // public DataCrawler(CrawlConfigurationX configX) {}


        /* ====== Class Methods {Public} ===== */
        // public static Task CrawlSingleSite() {}
        // public static Task CrawlMultipleSites() {}

        /* ====== Class Methods {Private} ==== */
    }
}