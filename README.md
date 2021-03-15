# Spotlight, the Dark Web Crawler

[![Version](https://badge.fury.io/gh/tterb%2FHyde.svg)](https://badge.fury.io/gh/tterb%2FHyde)

<!-- <img alt="C#" src="https://img.shields.io/badge/c%23%20-%23239120.svg?&style=for-the-badge&logo=c-sharp&logoColor=white"/> -->

<!-- <br></br> -->

Spotlight is an open-source .NET console application developed with Microsoft’s .NET standard 5.0, and is written in C#. The program is meant to be run from any command line tool, and requires the use of specifically defined input commands. Spotlight is the culmination of multiple open-source libraries to simplify the process of crawling and scraping text content from the Dark Web. Amongst these libraries includes: Abot, TorSharp, HtmlAgilityPack, MongoDB, CommandLineParser, and Serilog.

## Requirements

* Windows 10
* Installed .NET 5.0+ SDK

## Developer Installation Instructions

Clone this repository to a secure location on your pc. Once cloned, use any command line tool or program to run the following commands...

```sh
    # Navigate to the ./webcrawler/ folder in the repository

    dotnet restore
    dotnet build
```

This will create a working executable of the current version of Spotlight. The exe file will be located in...

```sh
    # ./webcrawler/bin/Debug/net5.0/
```

Simply run the exe from that folder location to start the program. However, before running please read the following commands that the program uses.

```
Commands:

  -s, --single     Crawl a single URL. Specify the URL.

  -m, --multi      Crawl multiple URLs. Pass input file containing URLs.

  -h, --handler    (Group: Page Handler) Specify page handler type:     
                   * wordFrequency
                   * sentimentAnalysis

  --help           Display this help screen.

  --version        Display version information.

```

When running the program you must specify the type of crawl to perform, and then which OnCrawlCompletion handler to use. The "single" crawl type requires a single url as input, where the "multi" crawl type requires a txt file as input with a url on each line (file should end with empty line). An example execution looks as follows:

```sh
    # Navigate to the ./webcrawler/ folder in the repository
    
    ./bin/Debug/net5.0/webcrawler.exe -s "url to crawl" --handler sentimentAnalysis

    # Or

    ./bin/Debug/net5.0/webcrawler.exe -m ./path/to/txt/file --handler sentimentAnalysis
```
