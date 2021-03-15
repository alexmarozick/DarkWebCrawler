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

using AbotX2.Poco;

// Logger
using Serilog;

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
        
        public static string MONGO_URI = ""; // Specify a MongoURI if you plan on uploading data to your own backend

        public static string MONGO_DATABASE_NAME = ""; // If you are filling out the mongo field above, specify database name

        public static string MONGO_COLLECTION_NAME = "";

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
                    MaxPagesToCrawl = 30,                               // Max total urls this crawler should crawl
                    MaxCrawlDepth = 1,                                  // Depth for crawler to traverse urls
                    IsJavascriptRenderingEnabled = false,                // Should crawler render JS?
                    JavascriptRenderingWaitTimeInMilliseconds = 2000,   // How long to wait for js to process 
                    MaxConcurrentSiteCrawls = 1,                        // Only crawl a single site at a time
                    MaxRetryCount = 3                                   // Retries to connect and crawl site 'x' times
                };

                if (parsedArgs.InputFile == null) // THIS IS "-s" or "--single"
                {
                    var handler = new HttpClientHandler
                    {
                        Proxy = new WebProxy(new Uri("http://localhost:" + settings.PrivoxySettings.Port))
                    };

                    // Crawl
                    await DataScraper.Crawl(crawlConfig, handler, parsedArgs.handlerType, parsedArgs.StartingUri);

                    BuildBsonDocument(DataScraper.allParsedText, parsedArgs.StartingUri);

                    //reset vals for next crawl
                    DataScraper.allParsedText = new List<string>();
                    DataScraper.siteTitle = "";
                    DataScraper.dataDocuments = new List<BsonDocument>();

                    if (MONGO_URI == "")
                    {
                        Log.Logger.Information("Database information is no longer accessible or available." +
                                               "You will need to provide your own Mongo details in \"Crawler.cs\".");
                    }
                }
                else // THIS IS "-m" or "--multi"
                {

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

                        BuildBsonDocument(DataScraper.allParsedText,sitesToCrawl[i]);

                        if (MONGO_URI == "")
                        {
                            Log.Logger.Information("Database information is no longer accessible or available." +
                                                   "You will need to provide your own Mongo details in \"Crawler.cs\".");
                        }

                        if (MONGO_URI != "" && MONGO_COLLECTION_NAME != "" && MONGO_DATABASE_NAME != "")
                        {
                            var client = new MongoClient(MONGO_URI);
                            var database = client.GetDatabase(MONGO_DATABASE_NAME);
                            var collection = database.GetCollection<BsonDocument>(MONGO_COLLECTION_NAME);

                            collection.InsertMany(DataScraper.dataDocuments);
                        }

                        //reset vals for next crawl
                        DataScraper.allParsedText = new List<string>();
                        DataScraper.siteTitle = "";
                        DataScraper.dataDocuments = new List<BsonDocument>();
                    }
                }
                // * ==========================================================================

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

                // Excludes words we don't care about
                var desiredWords = DataScraper.ExcludeWords(dictList[i].Value.Item2);

                //the context sentences
                //number of occurances of context words for a given keyword
                var contextWordCount = DataScraper.GetWordCount(desiredWords);

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
