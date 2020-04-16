using System;
using System.Collections.Generic;
using CommandLine;
using CommandLine.Text;

namespace CacheInitializer
{
	// Define a class to receive parsed values
	class Options
	{
		[Option('s', "server", Required = true, HelpText = "URL to the server.")]
		public string Server { get; set; }

		[Option('a', "appname", Required = false, HelpText = "App to load (using app name)")]
		public string AppName { get; set; }

		[Option('i', "appid", Required = false, HelpText = "App to load (using app ID)")]
		public string AppID { get; set; }

		[Option('p', "proxy", Required = false, HelpText = "Virtual Proxy to use")]
		public string VirtualProxy { get; set; }

		[Option('o', "objects", Required = false, Default = false, HelpText = "cycle through all sheets and objects")]
		public bool FetchObjects { get; set; }

		[Option('f', "field", Required = false, HelpText = "field to make selections in e.g Region")]
		public string SelectionField { get; set; }

		[Option('v', "values", Required = false, HelpText = "values to select e.g  \"France\",\"Germany\",\"Spain\"")]
		public string SelectionValues { get; set; }

		[Option('d', "debug", Required = false, HelpText = "Run with logging set to debug.")]
		public bool Debug { get; set; }

		static void DisplayHelp<T>(ParserResult<T> result, IEnumerable<Error> errs)
		{
			var helpText = HelpText.AutoBuild(result, (current) => HelpText.DefaultParsingErrorsHandler(result, current));
			Console.WriteLine(helpText);
		}
	}
}
