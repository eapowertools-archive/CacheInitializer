using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Threading.Tasks;
using Qlik.Engine;
using Qlik.Engine.Communication;
using Qlik.Sense.Client;
using CommandLine;

// Title:       Qlik Sense Cache Initializer 

// Summary:     This tool will "warm" the cache of a Qlik Sense server so that when using large apps the users get good performance right away.  
//              You can use it to load all apps, a single app, and you can get it to just open the app to RAM or cycle through all the objects 
//              so that it will pre calculate expressions so users get rapid performance. You can also pass in selections too.
// Credits:     Thanks to Øystein Kolsrud for helping with the Qlik Sense .net SDK steps, contributions by Roland Vecera and Goran Sander
//              Uses the commandline.codeplex.com for processing parameters


// Usage:       cacheinitiazer.exe -s https://server.domain.com [-a appname] [-i appid] [-o] [-f fieldname] [-v "value 1,value 2"] [-p virtualproxyprefix]
// Notes:       This projects use the Qlik Sense .net SDK, you must use the right version of the SDK to match the server you are connecting too. 
//              To swap version   simply replace the .net SDK files in the BIN directory of this project, if you dont match them, it wont work!


namespace CacheInitializer
{
	internal enum LogLevel
	{
		Info,
		Debug
	}

	internal class QlikSelection
	{
		public string fieldname { get; set; }
		public string[] fieldvalues { get; set; }
	}

	class Program
	{
		private static bool DEBUG_MODE = false;

		static void Main(string[] args)
		{
			//// process the parameters using the https://github.com/commandlineparser/commandline/wiki/Getting-Started
			Parser.Default.ParseArguments<Options>(args)
			.WithParsed(options => DoWork(options)) // options is an instance of Options type
			.WithNotParsed(errors =>
			{
				foreach (Error anError in errors)
				{
					if (anError.Tag == ErrorType.MissingRequiredOptionError)
					{
						//Console.WriteLine("Missing required argument '--" + ((MissingRequiredOptionError)anError).NameInfo.LongName + "'.");
					}
				}
			});

			return;
		}

		private static void DoWork(Options options)
		{
			Uri serverURL;
			string appname;
			string appid;
			bool openSheets;
			string virtualProxy;
			QlikSelection mySelection = null;

			ILocation remoteQlikSenseLocation = null;

			try
			{
				if (options.Debug)
				{
					DEBUG_MODE = true;
					Print(LogLevel.Debug, "Debug logging enabled.");

				}
				Print(LogLevel.Debug, "setting parameter values in main");
				serverURL = new Uri(options.Server);
				appname = options.AppName;
				appid = options.AppID;
				virtualProxy = !string.IsNullOrEmpty(options.VirtualProxy) ? options.VirtualProxy : "";
				openSheets = options.FetchObjects;
				if (options.SelectionField != null)
				{
					mySelection = new QlikSelection();
					mySelection.fieldname = options.SelectionField;
					mySelection.fieldvalues = options.SelectionValues.Split(',');
				}
				//TODO need to validate the params ideally

				Print(LogLevel.Debug, "setting remoteQlikSenseLocation"); ;

				////connect to the server (using windows credentials
				QlikConnection.Timeout = Int32.MaxValue;
				var d = DateTime.Now;

				remoteQlikSenseLocation = Qlik.Engine.Location.FromUri(serverURL);


				Print(LogLevel.Debug, "validating http(s) and virtual proxy"); ;
				if (virtualProxy.Length > 0)
				{
					remoteQlikSenseLocation.VirtualProxyPath = virtualProxy;
				}
				bool isHTTPs = false;
				if (serverURL.Scheme == Uri.UriSchemeHttps)
				{
					isHTTPs = true;
				}
				remoteQlikSenseLocation.AsNtlmUserViaProxy(isHTTPs, null, false);

				Print(LogLevel.Debug, "starting to cache applications");
				////Start to cache the apps
				IAppIdentifier appIdentifier = null;

				if (appid != null)
				{
					//Open up and cache one app, based on app ID
					appIdentifier = remoteQlikSenseLocation.AppWithId(appid);
					Print(LogLevel.Debug, "got app identifier by ID");
					LoadCache(remoteQlikSenseLocation, appIdentifier, openSheets, mySelection);
					Print(LogLevel.Debug, "finished caching by ID");

				}
				else
				{
					if (appname != null)
					{
						//Open up and cache one app
						appIdentifier = remoteQlikSenseLocation.AppWithNameOrDefault(appname);
						Print(LogLevel.Debug, "got app identifier by name");
						LoadCache(remoteQlikSenseLocation, appIdentifier, openSheets, mySelection);
						Print(LogLevel.Debug, "finished caching by name");
					}
					else
					{
						//Get all apps, open them up and cache them
						remoteQlikSenseLocation.GetAppIdentifiers().ToList().ForEach(id => LoadCache(remoteQlikSenseLocation, id, openSheets, null));
						Print(LogLevel.Debug, "finished caching all applications");
					}
				}


				////Wrap it up
				var dt = DateTime.Now - d;
				Print(LogLevel.Info, "Cache initialization complete. Total time: {0}", dt.ToString());
				remoteQlikSenseLocation.Dispose();
				Print(LogLevel.Debug, "done");

				return;
			}
			catch (UriFormatException)
			{
				Print(LogLevel.Info, "Invalid server paramater format. Format must be http[s]://host.domain.tld.");
				return;
			}
			catch (WebSocketException webEx)
			{
				if (remoteQlikSenseLocation != null)
				{
					Print(LogLevel.Info, "Disposing remoteQlikSenseLocation");
					remoteQlikSenseLocation.Dispose();
				}

				Print(LogLevel.Info, "Unable to connect to establish WebSocket connection with: " + options.Server);
				Print(LogLevel.Info, "Error: " + webEx.Message);

				return;
			}
			catch (TimeoutException timeoutEx)
			{
				Print(LogLevel.Info, "Timeout Exception - Unable to connect to: " + options.Server);
				Print(LogLevel.Info, "Error: " + timeoutEx.Message);

				return;
			}
			catch (Exception ex)
			{
				if (ex.Message.Trim() == "Websocket closed unexpectedly (EndpointUnavailable):")
				{
					Print(LogLevel.Info, "Error: licenses exhausted.");
					return;
				}
				else
				{
					Print(LogLevel.Info, "Unexpected error.");
					Print(LogLevel.Info, "Message: " + ex.Message);

					return;
				}
			}
		}

		static void LoadCache(ILocation location, IAppIdentifier id, bool opensheets, QlikSelection Selections)
		{
			IApp app = null;
			try
			{
				//open up the app
				Print(LogLevel.Info, "{0}: Opening app", id.AppName);
				app = location.App(id);
				Print(LogLevel.Info, "{0}: App open", id.AppName);

				//see if we are going to open the sheets too
				if (opensheets)
				{
					//see of we are going to make some selections too
					if (Selections != null)
					{
						for (int i = 0; i < Selections.fieldvalues.Length; i++)
						{
							//clear any existing selections
							Print(LogLevel.Info, "{0}: Clearing Selections", id.AppName);
							app.ClearAll(true);
							//apply the new selections
							Print(LogLevel.Info, "{0}: Applying Selection: {1} = {2}", id.AppName, Selections.fieldname, Selections.fieldvalues[i]);
							app.GetField(Selections.fieldname).Select(Selections.fieldvalues[i]);
							//cache the results
							cacheObjects(app, location, id);
						}

					}
					else
					{
						//clear any selections
						Print(LogLevel.Info, "{0}: Clearing Selections", id.AppName);
						app.ClearAll(true);
						//cache the results
						cacheObjects(app, location, id);
					}
				}

				Print(LogLevel.Info, "{0}: App cache completed", id.AppName);
				app.Dispose();
			}
			catch (Exception ex)
			{
				if (app != null)
				{
					app.Dispose();
				}
				throw ex;
			}
		}

		static void cacheObjects(IApp app, ILocation location, IAppIdentifier id)
		{
			//get a list of the sheets in the app
			Print(LogLevel.Info, "{0}: Getting sheets", id.AppName);
			var sheets = app.GetSheets().ToArray();
			//get a list of the objects in the app
			Print(LogLevel.Info, "{0}: Number of sheets - {1}, getting children", id.AppName, sheets.Count());
			IGenericObject[] allObjects = sheets.Concat(sheets.SelectMany(sheet => GetAllChildren(app, sheet))).ToArray();
			//draw the layout of all objects so the server calculates the data for them
			Print(LogLevel.Info, "{0}: Number of objects - {1}, caching all objects", id.AppName, allObjects.Count());
			var allLayoutTasks = allObjects.Select(o => o.GetLayoutAsync()).ToArray();
			Task.WaitAll(allLayoutTasks);
			Print(LogLevel.Info, "{0}: Objects cached", id.AppName);
		}

		private static IEnumerable<IGenericObject> GetAllChildren(IApp app, IGenericObject obj)
		{
			IEnumerable<IGenericObject> children = obj.GetChildInfos().Select(o => app.GetObject<GenericObject>(o.Id)).ToArray();
			return children.Concat(children.SelectMany(child => GetAllChildren(app, child)));
		}

		private static void Print(LogLevel level, string txt)
		{
			if (level == LogLevel.Info)
			{
				Console.WriteLine("{0} - {1}", DateTime.Now.ToString("hh:mm:ss"), txt);
			}
			else if (level == LogLevel.Debug && !DEBUG_MODE)
			{
				return;
			}
			else if (level == LogLevel.Debug && DEBUG_MODE)
			{
				Console.WriteLine("DEBUG\t{0} - {1}", DateTime.Now.ToString("hh:mm:ss"), txt);
			}
			else
			{
				throw new ArgumentException("Invalid LogLevel specified.");
			}
		}

		private static void Print(LogLevel level, string txt, params object[] os)
		{
			Print(level, String.Format(txt, os));
		}
	}
}
