using System.Collections.Generic;

namespace ScrapeAndCrawl.Extensions
{
    /// <summary>
    /// This class is used to build the JSON / BSON object
    /// Create alternate versions of this class for other webiste types/research problems 
    /// This one will be used for location wordcounts
    ///</summary>
    public class WordLocationDoc
    {
        public string WebsiteTitle;
        public string URL;
        public Dictionary<string,int> Locations;
    }

    /// <summary>
    /// Credit to StackOverflow user "Ritch Metlon" - https://stackoverflow.com/users/402547/ritch-melton
    /// Taken from this StackOverflow thread - https://stackoverflow.com/questions/7787994/is-there-a-version-of-the-class-tuple-whose-items-properties-are-not-readonly-an
    /// </summary>
    public class Pair<T1, T2>
    {
        public T1 Item1 { get; set; }
        public T2 Item2 { get; set; }
    }

    /// <summary> Helpful for not mispelling handler types. </summary>
    public enum PageHandlerType
    {
        NULL = 0,
        wordFreq = 1,
        sentAnal = 2
    }

    public static class Constants
    {
        public static string PlaceNamesTXT =         "./data_lists/place_names.txt";
        public static string UKUSPlaceNamesTXT =     "./data_lists/uk_us_cities.txt";
        public static string DefaultIgnoreWordsTXT = "./data_lists/default_ignore_words.txt";
        public static string WikiTestListTXT =       "./data_lists/wiki_test_list.txt";
        public static char[] CharsToTrim = new char[]
        {
            '\"',
            '\'',
            '(',
            ')',
            '#',
            '$',
            '%',
            '^',
            '&',
            '|',
            ';',
            ':',
            '`',
            '~'
        };
    }
}