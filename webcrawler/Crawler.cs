/* Web Data Scraper
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
using System.Linq;
using System.Text.RegularExpressions;
using System.Text;

// Abot2
// using Abot2.Core;      // Core components <change this comment later this is a bad description>
// using Abot2.Crawler;   // Namespace where Crawler objects are defined
// using Abot2.Poco;      //

// AbotX2
// using AbotX2.Crawler;  //
// using AbotX2.Parallel; //
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


// Scrape And Crawl
using ScrapeAndCrawl.Extensions;
#endregion


namespace ScrapeAndCrawl
{

    /// <summary>
    /// Object containing defined cmd args to parse for the Web Scraper tool
    /// </summary>
    public class Options
    {
        [Option('s', "single", Required=false, HelpText="Crawl a single URL. Specify the URL.")]
        public string StartingUri { get; set; }

        [Option('m', "multi", Required=false, HelpText="Crawl multiple URLs. Pass input file containing URLs.")]
        public string InputFile { get; set; }

        [Option('h', "handler", Group = "Page Handler", HelpText = "Specify page handler type:\n* wordFrequency\n* sentimentAnalysis")]
        public PageHandlerType handlerType { get; set; }
}

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
        /// <param name="args"> Command line arguements passed to executable. </param>
        static async Task Main(string[] args)
        {
            // Creates a Logger object from Serilog. Writes up to Debug level prints.   
            SetupLogger();

            Log.Logger.Information("Darkweb Data Scraper start...");

            // Parses command line arguements and stores them in "parsedArgs"
            SetupParser(args);

            // I made this function to move this setup out of main.
            // Returns a TorSharpSettings object for use with TorSharp.
            var settings = SetupTorSharpSettings();

            // Idk exactly how this works but like... its for torsharp
            // its uh... setting up torsharp "tools"...also its asyncronous
            await SetupTorSharpTools(settings);

            // * starts tor proxy -----------------------------------------------------------------
            using (var proxy = new TorSharpProxy(settings))
            {
                var waiting = true;
                while(waiting) {
                    // block untill we wait for TorSharp Proxy to be configured
                    await proxy.ConfigureAndStartAsync();
                    waiting = false;
                }
                
                // * SETUP AND EXECUTE CRAWLER ================================================
                // Setup Crawler configuration
                CrawlConfigurationX crawlConfig = new CrawlConfigurationX
                {
                    // Read up on what AutoThrottling and Decelerator is doing....
                    // Also consider commenting them out...
                    AutoThrottling = new AutoThrottlingConfig
                    {
                        IsEnabled = true,
                        ThresholdHigh = 10,                            //default
                        ThresholdMed = 5,                              //default
                        ThresholdTimeInMilliseconds = 5000,            //default
                        MinAdjustmentWaitTimeInSecs = 30               //default
                    },
                    Decelerator = new DeceleratorConfig
                    {
                        ConcurrentSiteCrawlsDecrement = 2,             //default
                        ConcurrentRequestDecrement = 2,                //default
                        DelayIncrementInMilliseconds = 2000,           //default
                        MaxDelayInMilliseconds = 15000,                //default
                        ConcurrentSiteCrawlsMin = 1,                   //default
                        ConcurrentRequestMin = 1                       //default
                    },
                    // ..............................................................

                    MaxPagesToCrawl = 30,
                    MaxCrawlDepth = 1,                           // Number of sites to crawl
                    IsJavascriptRenderingEnabled = true,               // Should crawler render JS?
                    JavascriptRenderingWaitTimeInMilliseconds = 2000, // How long to wait for js to process 
                    MaxConcurrentSiteCrawls = 1,                       // Only crawl a single site at a time
                    MaxRetryCount = 3                                  // Retries to connect and crawl site 'x' times
                };

                if (parsedArgs.InputFile == null) // THIS IS "-s"
                {
                    var handler = new HttpClientHandler
                    {
                        Proxy = new WebProxy(new Uri("http://localhost:" + settings.PrivoxySettings.Port))
                    };
                    await DataScraper.Crawl(crawlConfig, handler, parsedArgs.handlerType, parsedArgs.StartingUri);
                    BuildBsonDocument(DataScraper.allParsedText, parsedArgs.StartingUri);
                    //reset vals for next crawl
                    DataScraper.allParsedText = new List<string>();
                    DataScraper.siteTitle = "";
                }
                else // THIS IS "--file"
                {
                    var client = new MongoClient("mongodb://test-user_01:vVzppZ1Sz6PzE3Mx@cluster0-shard-00-00.bvnvt.mongodb.net:27017,cluster0-shard-00-01.bvnvt.mongodb.net:27017,cluster0-shard-00-02.bvnvt.mongodb.net:27017/myFirstDatabase?ssl=true&replicaSet=atlas-lfat71-shard-0&authSource=admin&retryWrites=true&w=majority");
                    //var client = new MongoClient("mongodb+srv://test-user_01:vVzppZ1Sz6PzE3Mx@cluster0.bvnvt.mongodb.net/Cluster0?retryWrites=true&w=majority");
                    var database = client.GetDatabase("test");
                    var collection = database.GetCollection<BsonDocument>("onion-test-3");
                    string inputFilePath = @parsedArgs.InputFile;

                    var sitesToCrawl = GenerateSiteList(inputFilePath);

                    for (int i = 0; i < sitesToCrawl.Count; i++)
                    {
                        var handler = new HttpClientHandler
                        {
                            Proxy = new WebProxy(new Uri("http://localhost:" + settings.PrivoxySettings.Port))
                        };

                        // Crawl
                        await DataScraper.Crawl(crawlConfig, handler, parsedArgs.handlerType, sitesToCrawl[i]);
                        //build BSON Doucment 
                        BuildBsonDocument(DataScraper.allParsedText,sitesToCrawl[i]);
                        //reset vals for next crawl
                        collection.InsertMany(DataScraper.dataDocuments);
                        DataScraper.allParsedText = new List<string>();
                        DataScraper.siteTitle = "";
                        DataScraper.dataDocuments = new List<BsonDocument>();
                    }
                }
                // * ==========================================================================

                // Check if any cached data exists
                // if (DataScraper.dataDocuments.Count > 0)
                // {

                //     Log.Logger.Debug("Number of documents generated: " + DataScraper.dataDocuments.Count.ToString());

                //     // Setup connection with MongoDB database
                //     var client = new MongoClient("mongodb://test-user_01:vVzppZ1Sz6PzE3Mx@cluster0-shard-00-00.bvnvt.mongodb.net:27017,cluster0-shard-00-01.bvnvt.mongodb.net:27017,cluster0-shard-00-02.bvnvt.mongodb.net:27017/myFirstDatabase?ssl=true&replicaSet=atlas-lfat71-shard-0&authSource=admin&retryWrites=true&w=majority");
                //     //var client = new MongoClient("mongodb+srv://test-user_01:vVzppZ1Sz6PzE3Mx@cluster0.bvnvt.mongodb.net/Cluster0?retryWrites=true&w=majority");
                //     var database = client.GetDatabase("test");
                //     var collection = database.GetCollection<BsonDocument>("onion-test-3");

                //     collection.InsertMany(DataScraper.dataDocuments);
                // }

                // Stop the TorSharp tools so that the proxy is no longer listening on the configured port.
                proxy.Stop();
            }
            // * ----------------------------------------------------------------------------------
        }

        // ========================================================================================
        // ========================================================================================
        // ========================================================================================


        // CLASS METHODS ==========================================================================
        // ========================================================================================
        // ========================================================================================

        /// <summary>
        /// Sets up Logger in Serilog.
        /// </summary>
        static void SetupLogger()
        {
            // "Log" from Serilog namespace
            // Configure the logging tool for nice console logging (formatted printing)
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()              // Write up to Debug level logging
                .WriteTo.Console()                 // Write to console
                .WriteTo.File("./log/log.txt")     // Write to a log file
                .CreateLogger();                   // Instantiate the Logger
        }

        /// <summary> TODO: add description. </summary>
        static void SetupParser(string[] args)
        {
            // Uses CommandLine to parse predefined command line args
            Parser.Default.ParseArguments<Options>(args)
                .WithParsed<Options>(ParseSuccessHandler)
                .WithNotParsed<Options>(ParseErrorHandler);

            // "parsedArgs" static member contains parsed command line args
        }

        /// <summary> This function just exists to move this step out of main... </summary>
        static TorSharpSettings SetupTorSharpSettings()
        {
            return new TorSharpSettings
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
        }

        /// <summary>
        /// Setups up torsharp "tools".
        /// </summary>
        static async Task SetupTorSharpTools(TorSharpSettings settings)
        {
            using (HttpClient hc = new HttpClient())
            {
                var fetcher = new TorSharpToolFetcher(settings, hc);
                var updates = await fetcher.CheckForUpdatesAsync();

                Log.Logger.Debug($"Current Privoxy: {updates.Privoxy.LocalVersion?.ToString() ?? "(none)"}");
                Log.Logger.Debug($"Latest Privoxy: {updates.Privoxy.LatestDownload.Version}");
                Log.Logger.Debug($"Current Tor: {updates.Tor.LocalVersion?.ToString() ?? "(none)"}");
                Log.Logger.Debug($"Latest Tor: {updates.Tor.LatestDownload.Version}");

                if (updates.HasUpdate)
                {
                    await fetcher.FetchAsync(updates);
                }
            }
        }

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

            if (options.handlerType != 0)
            {
                Log.Logger.Debug("Handler Type: " + options.handlerType.ToString());
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
        static void BuildBsonDocument(List<string> parsedText, string crawledURL){
            
            List<BsonDocument> dataDocuments = new List<BsonDocument>();
            
            // Dictionary containing keywords desired, and a list of all contexts in which they were used
            Dictionary<string, Pair<int, List<string>>> contextCache = DataScraper.GetWordCountAndContext(parsedText, Constants.DefaultIgnoreWordsTXT);

            //make list from dict to sort
            var dictList = contextCache.ToList();

            // Sort takes a comparison operator
            // Comparison(x,y) -> less than 0 if x < y, 0 if equal, greater than 0 if x > y
            // for all keyValuePairs in dict, sort based on the frequency count
            // pair: word : list of 
            dictList.Sort((pair1,pair2) =>  pair1.Value.Item1 > pair2.Value.Item1 ? -1 : 1);

            var sentimentAnalysis = new BsonDocument();

            var numWords = dictList.Count > 100 ? 100 : dictList.Count;
            for (int i = 0; i < numWords; i++)
            {
                //Log.Logger.Debug("Getting Context words for " + dictList[i].Key);
                if (dictList[i].Key == "")
                {
                    continue;  // Skips the stupid empty string keyword problem we havn't fixed yet...
                }

                // word
                // Log.Logger.Debug("KEYWORD - " + dictList[i].Key + ":");
                // Log.Logger.Debug("IN RAW UNICODE" + Encoding.UTF8.GetBytes(dictList[i].Key)[0].ToString());
                // num occurances
                // Log.Logger.Debug(dictList[i].Value.Item1.ToString());
                // Log.Logger.Debug("keyword context:");

                // for (var j = 0; j < dictList[i].Value.Item2.Count; j++)
                // {
                //     Log.Logger.Debug(dictList[i].Value.Item2[j]);
                // }

                // Excludes words we don't care about
                var desiredWords = DataScraper.ExcludeWords(dictList[i].Value.Item2);

                //the context sentences
                //number of occurances of context words for a given keyword
                var contextWordCount = DataScraper.GetWordCount(desiredWords);

                // foreach(var kvpair in contextWordCount)
                // {
                //     if (kvpair.Value > 1)
                //         Log.Logger.Debug("Key: " + kvpair.Key.ToString() + "\n" + "Val: " + kvpair.Value.ToString());
                // }

                sentimentAnalysis.Add(new BsonElement(
                    dictList[i].Key,new BsonDocument
                    {
                        {"Count",dictList[i].Value.Item1},
                        {"ContextSentences", new BsonArray(dictList[i].Value.Item2)},
                        {"ContextWordFrequency", new BsonDocument(contextWordCount)}
                    }
                ));
            }

            // BSON doc
            var bson = new BsonDocument
            {
                {"WebsiteTitle", DataScraper.siteTitle},
                {"URL", crawledURL},
                // {"Raw", rawPageText},
                {"SentimentAnalysis", sentimentAnalysis}
            };

            if (bson != null)
            {
                DataScraper.dataDocuments.Add(bson);
            }

        }

    }
}
#endregion
