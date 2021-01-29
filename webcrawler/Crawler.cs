/* Web Crawler / Scraper
 * Seth Tal, Juno Mayor, Alex Marozick
 * 01.14.2021
 * This file contains the main program execution pipeline for scraping web
 * data from specified sources.
*/

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

    /// <summary>
    /// Object containing defined cmd args to parse for the Web Scraper tool
    /// </summary>
    public class Options
    {
        [Option('f', "file", Required=false, HelpText="Text file containing list of websites to crawl.")]
        public string InputFile { get; set; }

        [Option('s', "start", Required=false, HelpText="Starting URL to crawl from.")]
        public string StartingUri { get; set; }
    }

#endregion

#region Crawler Class

    /// <summary>
    /// Seth Tal, Juno Mayor, Alex Marozick.
    /// Command Line Web Scraper. Scrapes hardcoded data from a starting
    /// uri or scrapes a list of websites written to a txt file.
    /// </summary>
    class Crawler
    {
        // PUBLIC CLASS MEMBERS

        // will contain parsed arguement data from the command line
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
                .WithParsed<Options>(ParseSuccessHandler)
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

        /// <summary>
        /// Handles parsed "options" if the Parser parses succesfully.
        /// </summary>
        /// <param name="options"> Contains the parsed arguement data. </param>
        static void ParseSuccessHandler(Options options)
        {
            if (options.StartingUri != null || options.StartingUri != "")
            {
                Log.Logger.Information("Starting Crawl From: " + options.StartingUri);
            }

            // Store parsed args in static class reference defined above
            parsedArgs = options;
        }

        /// <summary>
        /// Error handler if Parser parses unsuccessfully.
        /// </summary>
        /// <param name="error"> IDK what this does yet. </param>
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