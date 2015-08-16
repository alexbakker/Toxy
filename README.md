Toxy
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

### Screenshots

![Main Window](https://impy.me/i/0a1538.png)
![Main Window with settings)](https://impy.me/u/c1946d.png)

Compiling Toxy
===

#### Submodules
```
git submodule update --init --recursive
```
#### NuGet dependencies
* AForge.Video
* NAudio
* Squirrel

These should be downloaded by Visual Studio automatically. This requires [NuGet](http://docs.nuget.org/docs/start-here/installing-nuget) to be installed.

An up-to-date list of NuGet dependencies can be found in Toxy/packages.config
#### Separate dependencies

* [Tox library](https://github.com/irungentoo/toxcore), more info on how to compile/obtain this can be found [here](https://github.com/Impyy/SharpTox#things-youll-need).

Once you have obtained Tox, place libtox.dll in the libs folder.
