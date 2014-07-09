Toxy [![Build Status](http://jenkins.impy.me/job/Toxy%20WPF/badge/icon)](http://jenkins.impy.me/job/Toxy%20WPF/)
====

Metro-style Tox client for Windows. ([Tox](https://github.com/irungentoo/ProjectTox-Core "ProjectTox GitHub repo") is a free (as in freedom) Skype replacement.)

At this point, this client isn't feature-complete and is not ready for an official release.
Some features may be missing or are partially broken. Updates will arrive in time.

Feel free to contribute.

### Features

* Standard features like:
  - Nickname customization
  - Status customization
  - Friendlist
  - One to one conversations
  - Friendrequest listing
* Group chats
* Voice chats
* File transfers
* Typing detection
* DNS discovery (tox1 and tox3)
* Theme customization

### Screenshots

![Main Window](http://reverbs.pw/i/46f5a9.png)

Binaries
===
A pre-compiled version of Toxy can be found [here](http://jenkins.impy.me/job/Toxy%20WPF/lastSuccessfulBuild/artifact/toxy.zip "Toxy Binaries"). This includes all of the dependencies.

Things you'll need to compile
===

* The [SharpTox library](https://github.com/Impyy/SharpTox "SharpTox GitHub repo") and its dependencies. Once you have obtained those, place libtoxav.dll and SharpTox.dll in the libs folder.

All other dependencies can be found in the packages.config file and should be downloaded by Visual Studio automatically

===
### Special Thanks

* Punker
