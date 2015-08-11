Toxy [![Build Status](https://jenkins.impy.me/job/Toxy%20x86/badge/icon)](https://jenkins.impy.me/job/Toxy%20x86/)
====

Tox client for Windows that tries to follow the official mockup. [Tox](https://github.com/irungentoo/ProjectTox-Core "ProjectTox GitHub repo") is a free (as in freedom) Skype replacement.

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
* Proxy support (SOCKS5 & HTTP)
* Read receipts
* File transfers
* Typing detection
* DNS discovery (tox1 and tox3)

### Binaries
Pre-compiled versions of Toxy can be found [here](https://jenkins.impy.me/ "Toxy Binaries"). Those include all of the dependencies.

### Screenshots

![Main Window](https://impy.me/i/0a1538.png)
![Main Window with settings)](https://impy.me/u/c1946d.png)

Compiling Toxy
===
You need:
* [SharpTox](https://github.com/Impyy/SharpTox "SharpTox GitHub repo") and its dependencies.

Once you have obtained those, place libtox.dll and SharpTox.dll in the libs folder.

All other dependencies can be found in the packages.config file and should be downloaded by Visual Studio automatically.
This requires [NuGet](http://docs.nuget.org/docs/start-here/installing-nuget) to be installed.
