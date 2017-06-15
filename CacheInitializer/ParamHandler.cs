using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommandLine;
using CommandLine.Text;

namespace CacheInitializer
{
    // Define a class to receive parsed values
    class Options
    {
   
        [Option('s', "server", Required = true,
          HelpText = "URL to the server.")]
        public string server { get; set; }

        [Option('a', "appname", Required = false,
          HelpText = "App to load")]
        public string appname { get; set; }

        [Option('p', "proxy", Required = false,
          HelpText = "Virtual Proxy to use")]
        public string virtualProxy { get; set; }

        [Option('o', "objects", Required = false, DefaultValue = false,
         HelpText = "cycle through all sheets and objects")]
        public bool fetchobjects { get; set; }

        [Option('f', "field", Required = false,
         HelpText = "field to make selections in e.g Region")]
        public string selectionfield { get; set; }

        [Option('v', "values", Required = false,
         HelpText = "values to select e.g  \"France\",\"Germany\",\"Spain\"")]
        public string selectionvalues { get; set; }

        [ParserState]
        public IParserState LastParserState { get; set; }

        [HelpOption]
        public string GetUsage()
        {
            return HelpText.AutoBuild(this,
              (HelpText current) => HelpText.DefaultParsingErrorsHandler(this, current));
        }
    }
}
