# DarkWebCrawler
#### Project source files for our web crawling project at the University of Oregon. Class CIS 433 Computer and Network Security

<br></br>

# abot-test
#### Currently holds a simple crawler program. Main simply calls a couple of methods that creates a simple "polite" crawler, and also processes and handles a single page request

</br>

### Program (located in Crawler.cs) looks like this
``` C#
using System;
using System.Threading.Tasks;
using Abot2.Core;
using Abot2.Crawler;
using Abot2.Poco;
using Serilog;


namespace ScrapeAndCrawl
{
    class Crawler
    {
        // Program Args are optional
        static async Task Main(string[] args)
        {
            // sets up logger with Serilog
            // awaits SimpleCrawler(args[0])
            // awaits SinglePageRequest(args[0])
        }

        static async Task SimpleCrawler(string uriToHandle)
        {
            // Sets up crawler config object
            // Sets up crawler object
            // awaits crawler to crawl
        }

        static async Task SinglePageRequest(string uriToHandle)
        {
            // Sets up single page request
            // awaits page request
        }
    }
}
```