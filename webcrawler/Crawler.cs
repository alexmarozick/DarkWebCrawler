/* Web Crawler / Scraper
 * Seth Tal, Juno Mayor, Alex Marozick
 * 01.14.2021
 * This file contains the main program execution pipeline for scraping web
 * data from specified sources.
*/

#region using statements
// .NET
using System;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net;
using System.Net.Http;

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

//torSharp 
using Knapcode.TorSharp;
#endregion




namespace ScrapeAndCrawl
{

#region CMD arg Parser

    /// <summary>
    /// Object containing defined cmd args to parse for the Web Scraper tool
    /// </summary>
    public class Options
    {
        [Option("file", Required=false, HelpText="Text file containing list of websites to crawl.")]
        public string InputFile { get; set; }

        [Option('s', "start", Required=false, HelpText="Starting URL to crawl from.")]
        public string StartingUri { get; set; }
    }

#endregion

#region Crawler Class

    /// <summary>
    /// Command Line Web Scraper. Scrapes hardcoded data from a starting
    /// uri or scrapes a list of websites written to a txt file. <br></br>
    /// Seth Tal, Juno Mayor, Alex Marozick.
    /// </summary>
    class Crawler
    {
        // PUBLIC CLASS MEMBERS
        // configure

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
                .MinimumLevel.Debug()
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
            //* INIT TORSHARP --------------------------------------------------------------------- 
            var settings = new TorSharpSettings
            {
                ZippedToolsDirectory = Path.Combine(Path.GetTempPath(), "TorZipped"),
                ExtractedToolsDirectory = Path.Combine(Path.GetTempPath(), "TorExtracted"),
                PrivoxySettings = { Port = 1337 },
                TorSettings =
                {
                    SocksPort = 1338,
                    ControlPort = 1339,
                    ControlPassword = "foobar",
                },
            };

            // download tools
            using (HttpClient hc = new HttpClient())
            {
                var fetcher = new TorSharpToolFetcher(settings, hc);
                var updates = await fetcher.CheckForUpdatesAsync();
                Console.WriteLine($"Current Privoxy: {updates.Privoxy.LocalVersion?.ToString() ?? "(none)"}");
                Console.WriteLine($" Latest Privoxy: {updates.Privoxy.LatestDownload.Version}");
                Console.WriteLine();
                Console.WriteLine($"Current Tor: {updates.Tor.LocalVersion?.ToString() ?? "(none)"}");
                Console.WriteLine($" Latest Tor: {updates.Tor.LatestDownload.Version}");
                Console.WriteLine();
                if (updates.HasUpdate)
                {
                    await fetcher.FetchAsync(updates);
                }
            }
            using (var proxy = new TorSharpProxy(settings))
            {
                var handler = new HttpClientHandler
                {
                    Proxy = new WebProxy(new Uri("http://localhost:" + settings.PrivoxySettings.Port))
                };

                // using (handler)
                using (var httpClient = new HttpClient(handler))
                {
                    var waiting = true;
                    while(waiting) {
                        //block untill we wait for TorSharp Proxy to be configured
                        await proxy.ConfigureAndStartAsync();
                        waiting = false;
                    }
                    
                    // * SETUP AND EXECUTE CRAWLER ========================================================
                    // Setup Crawler configuration
                    CrawlConfigurationX crawlConfig_RecursiveCrawl = new CrawlConfigurationX
                    {
                        MaxPagesToCrawl = 2,                             // Number of sites to crawl
                        IsJavascriptRenderingEnabled = true,              // Should crawler render JS?
                        JavascriptRenderingWaitTimeInMilliseconds = 10000, // How long to wait for js to process 
                        MaxConcurrentSiteCrawls = 1,                      // Only crawl a single site at a time
                        MaxRetryCount = 3
                        // ? MaxConcurrentThreads = 8                     // Logical processor count to avoid cpu thrashing
                    };

                    CrawlConfigurationX crawlConfig_SingleSiteCrawl = new CrawlConfigurationX
                    {
                        MaxPagesToCrawl = 1,                              // Number of sites to crawl
                        IsJavascriptRenderingEnabled = true,              // Should crawler render JS?
                        JavascriptRenderingWaitTimeInMilliseconds = 3000, // How long to wait for js to process 
                        MaxConcurrentSiteCrawls = 1                       // Only crawl a single site at a time
                        // ? MaxConcurrentThreads = 8                     // Logical processor count to avoid cpu thrashing
                    };

                    // ! Log.Logger.Information("TEST: " + parsedArgs.InputFile);

                    if (parsedArgs.InputFile == null)
                    {
                        // THIS IS -S
                        await DataScraper.Crawl(crawlConfig_RecursiveCrawl, handler, parsedArgs.StartingUri);
                    }
                    else
                    {
                        string inputFilePath = @parsedArgs.InputFile;

                        var sitesToCrawl = GenerateSiteList(inputFilePath);

                        for (int i = 0; i < sitesToCrawl.Count; i++)
                        {
                            // Crawl
                            await DataScraper.Crawl(crawlConfig_RecursiveCrawl, handler, sitesToCrawl[i]);
                        }
                    }

                    // Check if cached Data
                    if (DataScraper.dataDocuments.Count > 0)
                    {
                        Log.Logger.Debug("Number of documents generated: " + DataScraper.dataDocuments.Count.ToString());
                        // Fetch data
                        for (int i = 0; i < DataScraper.dataDocuments.Count; i++)
                        {
                            // ? Log.Logger.Information(DataScraper.dataDocuments[i].ToJson());
                            // TODO mongoDB add document ( DataScraper.dataDocuments[i])
                            // TODO determine what collection to place document in -- based on cli flag? 
                            //if server info 
                                //call server info parse function

                            
                            var client = new MongoClient("mongodb+srv://test-user_01:vVzppZ1Sz6PzE3Mx@cluster0.bvnvt.mongodb.net/Cluster0?retryWrites=true&w=majority");
                            var database = client.GetDatabase("test");

                            // TODO - figure out a better way to handle this
                            // if (database.GetCollection<BsonDocument>("Test Collection [wikipedia]").Exists())
                            // {

                            // }
                            // else
                            //    database.CreateCollection("Test Collection [wikipedia]");

                            var collection = database.GetCollection<BsonDocument>("Test Collection [wikipedia]");

                            if(collection != null)
                            {
                                await collection.InsertOneAsync(DataScraper.dataDocuments[i]);
                            }

                        }
                    }
                }

                    // var client = new MongoClient("mongodb+srv://<username>:<password>@<cluster-address>/test?w=majority");
                    // var database = client.GetDatabase("test");
                    //Stop Torsharp
                    proxy.Stop();
            }
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

        /// <summary>
        /// Generates a list of website names to crawl from with a txt file as input
        /// </summary>
        static List<string> GenerateSiteList(string fileName)
        {
            List<string> result = new List<string>();

            if (!File.Exists(fileName))
            {
                throw new InvalidOperationException("File at " + fileName + " does not exist.");
            }
            else
            {
                using (StreamReader sr = File.OpenText(fileName))
                {
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        Console.WriteLine("Adding " + line + " to list of files to crawl from...");
                        result.Add(line);
                    }
                }
            }

            return result;
        }
        // ========================================================================================
        // ========================================================================================
        // ========================================================================================
    }
}
#endregion
