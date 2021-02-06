#region Using Statements
// .NET
using System;
using System.IO;
using System.Net;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net.Http;
using System.Linq;
// Abot2
using Abot2.Core;      // Core components <change this comment later this is a bad description>
using Abot2.Crawler;   // Namespace where Crawler objects are defined
using Abot2.Poco;      //

// AbotX2
using AbotX2.Core;
using AbotX2.Crawler;  //
using AbotX2.Poco;     //

// Logger
using Serilog;         // Serilog provides diagnostic logging to files

//BSON for mongo
using MongoDB.Bson;
//JSON Doc Formats

//htmlAgilityParser
using HtmlAgilityPack;

//JSON 
using System.Text.Json;
using System.Text.Json.Serialization;

#endregion


namespace ScrapeAndCrawl
{

    public static class Constants
    {
        public static string PlaceNamesTXT = "./data_lists/place_names.txt";
        public static string UKUSPlaceNamesTXT = "./data_lists/uk_us_cities.txt";
    }

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
        public static async Task Crawl(CrawlConfigurationX configX, HttpClientHandler handler, string uriToCrawl = "http://google.com")
        {
            // 'using' sets up scope for crawlerX object to be used
            // disposes of object at end of scope. (i.e. close-curly-brace)
            // I saw this used in the github example. Maybe its good practice??

            ImplementationContainer impContainer = new ImplementationContainer();
            impContainer.PageRequester = new ProxyPageRequester(handler, configX, new WebContentExtractor(), null);

            ImplementationOverride impOverride = new ImplementationOverride(configX, impContainer); 

            using (var crawlerX = new CrawlerX(configX, impOverride))
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
            // Log.Logger.Information(rawPageText);
            // TODO:
            // * pase page content into data we want...

            // ? add private parsing methods to class and use them here

            // * Convert to BSON Doc and add to dataDocuments
            var parsedText = ParseRawHTML(rawPageText);

            // if word in location list add to counter dict
            var dict = ParserWordCheck(parsedText, Constants.PlaceNamesTXT);

            // foreach (var pen15 in dict)
            // {
            //     Log.Logger.Debug(pen15.Key + " " + pen15.Value.ToString());
            // }

            //TODO: build json
            // ? foreach(var i in parsedText)
            // ? {
            // ?     Log.Logger.Debug(i);
            // ? }

            // WordLocationDoc wld = new WordLocationDoc()
            // {
            //     //TODO: Get the actual title, maybe from the header? 
            //     WebsiteTitle = "PLACEHOLDER",
            //     URL = e.CrawledPage.Uri.ToString(),
            //     Locations = dict
            // };

            // string stringjson = JsonSerializer.Serialize(wld);

            // var bson = new BsonDocument.Parse(wld);

            var bson = new BsonDocument
            {
                {"WebsiteTitle", "test page"},
                {"URL", e.CrawledPage.Uri.ToString()},
                {"Raw", rawPageText},
                {"Locations", new BsonDocument {dict}},
            };

            Log.Logger.Debug(bson.ToJson());
            
            dataDocuments.Add(bson);
        }

        private static List<string> ParseRawHTML(string rawHTML)
        {
            List<string> parsed = new List<string>();

            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(rawHTML);
            var unwantedNodes = htmlDoc.DocumentNode.SelectNodes("//form");

            if (unwantedNodes != null)
            {
                foreach (var n in unwantedNodes)
                {
                    n.RemoveAllChildren();
                }
            }

            // unwantedNodes.Insert(htmlDoc.DocumentNode.)
            var node = htmlDoc.DocumentNode.SelectSingleNode("//body");
            foreach (var nNode in node.Descendants())
            {
                if (nNode.NodeType == HtmlNodeType.Text)
                {
                    if (unwantedNodes == null)
                    {
                        String nodeText = nNode.InnerText;
                        if (nodeText.Any( x => char.IsLetter(x)))
                        {
                            parsed.Add(nNode.InnerText);
                        }
                    }
                    else
                    {
                        if (!unwantedNodes.Contains(nNode))
                        {
                            String nodeText = nNode.InnerText;
                            if (nodeText.Any( x => char.IsLetter(x)))
                            {
                                parsed.Add(nNode.InnerText);
                            }   
                        }
                    }         
                }
            }
            return parsed;
        }

        /// <summary>
        /// This is an O(N^2) algorithm for counting number of occurances of certain keywords
        /// </summary>
        /// <returns> nothing currently </returns>

        private static Dictionary<string,int> ParserWordCheck(List<string> parsedText, string keywordsFileLocation)
        {
            // Create a Hashset of keywords to check against where ...
            // * each key contains only the chars of the keyword
            // * each key is NOT null or empty
            HashSet<string> keywordsSet = new HashSet<string>(
                File.ReadLines(keywordsFileLocation)
                .Select(keyword => keyword.Trim().ToLower())
                .Where(keyword => !string.IsNullOrEmpty(keyword)),
                StringComparer.OrdinalIgnoreCase
            );

            // Tracks each found word
            HashSet<string> foundWords = new HashSet<string>();

            //  will track number of times the word is found
            Dictionary<string, int> wordInstanceCount = new Dictionary<string, int>();
            List<string> words = new List<string>();

            foreach(var str in parsedText)
            {
                foreach(var word in str.Split(' '))
                {
                    if (keywordsSet.Contains(word))
                    {
                        wordInstanceCount[word] = wordInstanceCount.ContainsKey(word) ? wordInstanceCount[word] + 1 : 1;
                    }
                }
            }
            return wordInstanceCount;
        }
        private static void old_ParserWordCheck(List<string> parsedText, string keywordsFileLocation)
        {
            // Create a Hashset of keywords to check against where ...
            // * each key contains only the chars of the keyword
            // * each key is NOT null or empty
            HashSet<string> keywordsSet = new HashSet<string>(
                File.ReadLines(keywordsFileLocation)
                .Select(keyword => keyword.Trim())
                .Where(keyword => !string.IsNullOrEmpty(keyword)),
                StringComparer.OrdinalIgnoreCase
            );

            int shortestWord = keywordsSet.Min(word => word.Length);
            int longestWord = keywordsSet.Max(word => word.Length);

            // Tracks each found word
            HashSet<string> foundWords = new HashSet<string>();
            // will track number of times the word is found
            Dictionary<string, int> wordInstanceCount = new Dictionary<string, int>();

            // Go through all parsed html text
            for (int i = 0; i < parsedText.Count; i++)
            {
                // Algorithm for checking number of occurances of a keyword (if it exists)
                for (int length = shortestWord; length <= longestWord; ++length) 
                {
                    for (int position = 0; position <= parsedText[i].Length - length; ++position)
                    {
                        string sub = parsedText[i].Substring(position, length);

                        if (keywordsSet.Contains(sub))
                        {
                            // add word to foundWords
                            foundWords.Add(sub);
                            // if word already tracked in instanceCount then increment
                            if (wordInstanceCount.ContainsKey(sub))
                                wordInstanceCount[sub]++;
                            // else add word and set number of times found
                            else
                                wordInstanceCount.Add(sub, 1);
                        } 
                    }
                }
            }
            
            var foundList = foundWords.ToList<string>();

            for (int i = 0; i < foundList.Count; i++)
            {
                Log.Logger.Debug("\n");
                Log.Logger.Debug("Found word: " + foundList[i]);
                if (wordInstanceCount.ContainsKey(foundList[i]))
                    Log.Logger.Debug("Num occurances: " + wordInstanceCount[foundList[i]]);
                Log.Logger.Debug("\n");
            }
        }
#endregion
    }

    //Extend the PageRequester class and override the method that creates the HttpWebRequest
    public class ProxyPageRequester : PageRequester
    {
        private readonly CrawlConfiguration _config; 
        private readonly IWebContentExtractor _contentExtractor;
        private readonly CookieContainer _cookieContainer = new CookieContainer();
        private HttpClientHandler _httpClientHandler;
        private HttpClient _httpClient;
        private  HttpClientHandler _torHandler; 

        public ProxyPageRequester(HttpClientHandler torHandler, CrawlConfiguration config, IWebContentExtractor contentExtractor = null, HttpClient httpClient = null) : base(config, contentExtractor, httpClient)
        {
            _config = config;
            _contentExtractor = contentExtractor;

            _torHandler = torHandler;
        }

        protected override HttpClientHandler BuildHttpClientHandler(Uri rootUri)
        {

            return _torHandler;

            // HttpClientHandler request = base.BuildHttpClientHandler(rootUri);
            // request.Proxy = new WebProxy(_config.ConfigurationExtensions["TorProxy"]);
            // return request;
        }
    }

}

