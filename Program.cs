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
	class Program
	{

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


			try
			{
				serverURL = new Uri(options.server);
				appname = options.appname;
				appid = options.appid;
				virtualProxy = !string.IsNullOrEmpty(options.virtualProxy) ? options.virtualProxy : "";
				openSheets = options.fetchobjects;
				if (options.selectionfield != null)
				{
					mySelection = new QlikSelection();
					mySelection.fieldname = options.selectionfield;
					mySelection.fieldvalues = options.selectionvalues.Split(',');
				}
				//TODO need to validate the params ideally

				////connect to the server (using windows credentials
				QlikConnection.Timeout = Int32.MaxValue;
				var d = DateTime.Now;

				ILocation remoteQlikSenseLocation = Qlik.Engine.Location.FromUri(serverURL);

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

				////Start to cache the apps
				IAppIdentifier appIdentifier = null;

				if (appid != null)
				{
					//Open up and cache one app, based on app ID
					appIdentifier = remoteQlikSenseLocation.AppWithId(appid);
					LoadCache(remoteQlikSenseLocation, appIdentifier, openSheets, mySelection);

				}
				else
				{
					if (appname != null)
					{
						//Open up and cache one app
						appIdentifier = remoteQlikSenseLocation.AppWithNameOrDefault(appname);
						LoadCache(remoteQlikSenseLocation, appIdentifier, openSheets, mySelection);
					}
					else
					{
						//Get all apps, open them up and cache them
						remoteQlikSenseLocation.GetAppIdentifiers().ToList().ForEach(id => LoadCache(remoteQlikSenseLocation, id, openSheets, null));
					}
				}


				////Wrap it up
				var dt = DateTime.Now - d;
				Print("Cache initialization complete. Total time: {0}", dt.ToString());
				remoteQlikSenseLocation.Dispose();
				return;
			}
			catch (UriFormatException)
			{
				Print("Invalid server paramater format. Format must be http[s]://host.domain.tld.");
			}
			catch (WebSocketException webEx)
			{
				Print("Unable to connect to establish WebSocket connection with: " + options.server);
				Print("Error: " + webEx.Message);

			}
		}

		static void LoadCache(ILocation location, IAppIdentifier id, bool opensheets, QlikSelection Selections)
		{
			//open up the app
			Print("{0}: Opening app", id.AppName);
			IApp app = location.App(id);
			Print("{0}: App open", id.AppName);

			//see if we are going to open the sheets too
			if (opensheets)
			{
				//see of we are going to make some selections too
				if (Selections != null)
				{
					for (int i = 0; i < Selections.fieldvalues.Length; i++)
					{
						//clear any existing selections
						Print("{0}: Clearing Selections", id.AppName);
						app.ClearAll(true);
						//apply the new selections
						Print("{0}: Applying Selection: {1} = {2}", id.AppName, Selections.fieldname, Selections.fieldvalues[i]);
						app.GetField(Selections.fieldname).Select(Selections.fieldvalues[i]);
						//cache the results
						cacheObjects(app, location, id);
					}

				}
				else
				{
					//clear any selections
					Print("{0}: Clearing Selections", id.AppName);
					app.ClearAll(true);
					//cache the results
					cacheObjects(app, location, id);
				}
			}

			Print("{0}: App cache completed", id.AppName);
			app.Dispose();
		}

		static void cacheObjects(IApp app, ILocation location, IAppIdentifier id)
		{
			//get a list of the sheets in the app
			Print("{0}: Getting sheets", id.AppName);
			var sheets = app.GetSheets().ToArray();
			//get a list of the objects in the app
			Print("{0}: Number of sheets - {1}, getting children", id.AppName, sheets.Count());
			IGenericObject[] allObjects = sheets.Concat(sheets.SelectMany(sheet => GetAllChildren(app, sheet))).ToArray();
			//draw the layout of all objects so the server calculates the data for them
			Print("{0}: Number of objects - {1}, caching all objects", id.AppName, allObjects.Count());
			var allLayoutTasks = allObjects.Select(o => o.GetLayoutAsync()).ToArray();
			Task.WaitAll(allLayoutTasks);
			Print("{0}: Objects cached", id.AppName);
		}

		private static IEnumerable<IGenericObject> GetAllChildren(IApp app, IGenericObject obj)
		{
			IEnumerable<IGenericObject> children = obj.GetChildInfos().Select(o => app.GetObject<GenericObject>(o.Id)).ToArray();
			return children.Concat(children.SelectMany(child => GetAllChildren(app, child)));
		}

		private static void Print(string txt)
		{
			Console.WriteLine("{0} - {1}", DateTime.Now.ToString("hh:mm:ss"), txt);
		}

		private static void Print(string txt, params object[] os)
		{
			Print(String.Format(txt, os));
		}


	}

	class QlikSelection
	{
		public string fieldname { get; set; }
		public string[] fieldvalues { get; set; }
	}

}
