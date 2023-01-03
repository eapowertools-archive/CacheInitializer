# Status
[![Project Status: Unsupported – The project has reached a stable, usable state but the author(s) have ceased all work on it. A new maintainer may be desired.](https://www.repostatus.org/badges/latest/unsupported.svg)](https://www.repostatus.org/#unsupported)


# Qlik Sense Cache Initializer

Refer to [this article](https://adminplaybook.qlik-poc.com/docs/tooling/cache_warming.html#cacheinitializer-) for more comprehensive usage and background to the use case for this tool.

### Summary
This tool will "warm" the cache of a Qlik Sense server so that when using large apps, the users will experience shorter load times for their 'first' app opens and queries.  You can use it to load all apps, a single app, or you can use it to open the app and cycle through all the objects so that it will pre-calculate expressions to increase user performance. The cache initialzer also supports the ability to pass in selections.

### Download/Release
The project is now built in .NET Core, which means it can be run on any OS. That said, the download available currently under the releases section [here](https://github.com/eapowertools/CacheInitializer/releases), is a win64 executable (with all runtimes and dlls self contained). Since the executable contains all runtimes and dlls, you should be able to download and execute without any additional installation prerequisites. You can rebuild the project yourself if you'd like to run it on a Linux distribution or OS X by downloading the source files..

#### Credits
First, thanks to Joe Bickley for building this tool. Thanks to Øystein Kolsrud for helping with the Qlik Sense .net SDK steps, contributions by Roland Vecera and Goran Sander

#### Usage
```
cacheinitiazer.exe -s https://server.domain.com [-a appname] [-i appid] [-o] [-f fieldname] [-v "value 1,value 2"] [-p virtualproxyprefix]
```

#### Available Parameters:

```
  -s, --server     Required. URL to the server.
  -a, --appname    App to load (using app name)
  -i, --appid      App to load (using app ID)
  -p, --proxy      Virtual Proxy to use
  -o, --objects    (Default: False) cycle through all sheets and objects
  -f, --field      field to make selections in e.g Region
  -v, --values     values to select e.g  "France","Germany","Spain"
  --help           Display this help screen.
```

##### Notes
Also for those interested in similar capabilties but running in node.js check out Goran Sanders project (and many other goodies) here: https://github.com/ptarmiganlabs/butler-cw
