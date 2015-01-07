Toxy [![Build Status](https://jenkins.impy.me/job/Toxy%20x86/badge/icon)](https://jenkins.impy.me/job/Toxy%20x86/)
====

Metro-style Tox client for Windows. [Tox](https://github.com/irungentoo/ProjectTox-Core "ProjectTox GitHub repo") is a free (as in freedom) Skype replacement.

At this point, this client isn't feature-complete and is not ready for an official release.
Some features may be missing or are partially broken. Updates will arrive in time.

Feel free to contribute.

### Features

* Standard features like:
  - Nickname customization
  - Status customization
  - Friendlist
  - One to one conversations
  - Friendrequests
* Avatars
* Group chats
* Voice chats
* Video chats
* Group voice chats
* Inline images
* Proxy support (SOCKS 5)
* Read receipts
* File transfers
* Typing detection
* DNS discovery (tox1 and tox3)
* Theme customization

### Binaries
Pre-compiled versions of Toxy can be found [here](https://jenkins.impy.me/ "Toxy Binaries"). Those include all of the dependencies.

### Screenshots

![Main Window (calling)](http://impy.me/i/6f44aa.png)
![Main Window with settings)](http://impy.me/i/4e2de8.png)

Compiling Toxy
===
You need:
* [SharpTox](https://github.com/Impyy/SharpTox "SharpTox GitHub repo") and its dependencies.
* [SharpTox.Vpx](https://github.com/Impyy/SharpTox.Vpx "SharpTox.Vpx GitHub repo")
* [SharpTox.Av.Filter](https://github.com/Impyy/SharpTox.Av.Filter "SharpTox.Av.Filter GitHub repo") and its dependencies.
* [SQLite](https://www.sqlite.org/download.html). (Toxy uses [sqlite-net](https://github.com/praeclarum/sqlite-net) to access SQLite's functions).
* [AForge.NET](https://github.com/andrewkirillov/AForge.NET). (AForge, AForge.Video, AForge.Video.DirectShow)

Once you have obtained those, place sqlite3.dll, libtox.dll, filter_audio.dll, SharpTox.dll, SharpTox.Vpx.dll, SharpTox.Av.Filter.dll, AForge.dll, AForge.Video.dll and AForge.Video.DirectShow.dll in the libs folder.

All other dependencies can be found in the packages.config file and should be downloaded by Visual Studio automatically.
This requires [NuGet](http://docs.nuget.org/docs/start-here/installing-nuget) to be installed.

===
### Special Thanks

* punker76
