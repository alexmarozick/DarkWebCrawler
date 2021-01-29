#region using statements
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

// Command Line Arguement Parser
using CommandLine;

//mongoDB
using MongoDB.Bson;
using MongoDB.Driver;
#endregion


namespace ScrapeAndCrawl
{
#region CMD arg Parser

    public class Options
    {
        [Option('f', "file", Required=false, HelpText="Text file containing list of websites to crawl.")]
        public string InputFile { get; set; }

        [Option('s', "start", Required=true, HelpText="Starting URL to crawl from.")]
        public string StartingUri { get; set; }
    }

#endregion

#region Crawler Class

    /// <summary>
    /// Main program container 
    /// </summary>
    class Crawler
    {
        // PUBLIC CLASS MEMBERS
        static Options parsedArgs;

        // PRIVATE CLASS MEMBERS

        // MAIN ===================================================================================
        // ========================================================================================
        // ========================================================================================
        /// <param name="args"> Command line arguements passed to executable. </param>
        static async Task Main(string[] args)
        {
            // * SET UP LOGGER --------------------------------------------------------------------
            // "Log" from Serilog namespace
            // Configure the logging tool for nice command line prints/formats
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .WriteTo.Console()
                .WriteTo.File("./log/log.txt")
                .CreateLogger();

            Log.Logger.Information("Darkweb Data Scraper start...");
            // * ----------------------------------------------------------------------------------

            // * PARSE COMMAND LINE ARGS ----------------------------------------------------------
            // Uses CommandLine to parse predefined command line args
            Parser.Default.ParseArguments<Options>(args)
                .WithParsed<Options>(o =>
                {
                    if (o.StartingUri != null || o.StartingUri != "")
                    {
                        Log.Logger.Information("Starting Crawl From: " + o.StartingUri);
                    }

                    parsedArgs = o;
                })
                .WithNotParsed<Options>(ParseErrorHandler);

            // "parsedArgs" static member contains parsed command line args
            // * ----------------------------------------------------------------------------------

            // * SETUP AND EXECUTE CRAWLER ========================================================
            // Setup Crawler configuration
            CrawlConfigurationX crawlConfig = new CrawlConfigurationX
            {
                MaxPagesToCrawl = 10,                             // Number of sites to crawl
                IsJavascriptRenderingEnabled = true,              // Should crawler render JS?
                JavascriptRenderingWaitTimeInMilliseconds = 3000, // How long to wait for js to process 
                MaxConcurrentSiteCrawls = 1                      // Only crawl a single site at a time
                // ? MaxConcurrentThreads = 8                         // Logical processor count to avoid cpu thrashing
            };

            // Crawl
            await DataScraper.Crawl(crawlConfig, parsedArgs.StartingUri);

            // Check if cached Data
            if (DataScraper.dataDocuments.Count > 0)
            {
                // Fetch data
                for (int i = 0; i < DataScraper.dataDocuments.Count; i++)
                {
                    // ! Log.Logger.Information(DataScraper.dataDocuments[i].ToJson());
                    // TODO mongoDB add document ( DataScraper.dataDocuments[i])
                }
            }

            // var client = new MongoClient("mongodb+srv://<username>:<password>@<cluster-address>/test?w=majority");
            // var database = client.GetDatabase("test");
            // * ==================================================================================
        }
        // ========================================================================================
        // ========================================================================================
        // ========================================================================================


        // CLASS METHODS ==========================================================================
        // ========================================================================================
        // ========================================================================================

        static void ParseErrorHandler(IEnumerable<Error> error)
        {
            // ? If error handling args then hault execution of program
            System.Environment.Exit(0);
        }

        // ========================================================================================
        // ========================================================================================
        // ========================================================================================
    }
#endregion
}