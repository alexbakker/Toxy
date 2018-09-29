Toxy
====

Tox client for Windows that loosely follows the official mockup and aims to be [TCS](https://tox.gitbooks.io/tox-client-standard/content/) compliant. [Tox](https://github.com/irungentoo/ProjectTox-Core "Toxcore GitHub repo") is a free (as in freedom) Skype replacement.

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

![Main Window](https://alexbakker.me/u/lzfwrz0a.png)
![Main Window with settings)](https://alexbakker.me/u/iwe5bi81.png)

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

* [Tox library](https://github.com/irungentoo/toxcore), more info on how to compile/obtain this can be found [here](https://github.com/alexbakker/SharpTox#things-youll-need).

Once you have obtained Tox, place libtox.dll in the libs folder.
