using System.Collections.Generic;

namespace ScrapeAndCrawl{



    public class WordLocationDoc
    {
        /// <summary>
        /// This class is used to build the JSON / BSON object
        /// Create alternate versions of this class for other webiste types/research problems 
        /// This one will be used for location wordcounts
        ///</summary>
        public string WebsiteTitle;
        public string URL;
        public Dictionary<string,int> Locations;
    }


}