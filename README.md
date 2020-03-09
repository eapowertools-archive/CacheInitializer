# Qlik Sense Cache Initializer 

This is now the official home of Joe Bickley's CacheInitializer.

Summary:     This tool will "warm" the cache of a Qlik Sense server so that when using large apps the users get good performance right away.  You can use it to load all apps, a single app, and you can get it to just open the app to RAM or cycle through all the objects so that it will pre calculate expressions so users get rapid performance. You can also pass in selections too.

Credits:     Thanks to Øystein Kolsrud for helping with the Qlik Sense .net SDK steps, contributions by Roland Vecera and Goran Sander   
Uses the commandline.codeplex.com for processing parameters

Usage:       cacheinitiazer.exe -s https://server.domain.com [-a appname] [-i appid] [-o] [-f fieldname] [-v "value 1,value 2"] [-p virtualproxyprefix]

Notes:       This projects use the Qlik Sense .net SDK, you must use the right version of the SDK to match the server you are connecting too. To swap version   simply replace the .net SDK files in the BIN directory of this project, if you dont match them, it wont work.


Also for those interested in similar capabilties but running in node.js check out Goran Sanders project (and many other goodies) here: https://github.com/mountaindude/butler-cw
