[![Travis build Status](https://travis-ci.org/imazen/imageflow-dotnet.svg?branch=master)](https://travis-ci.org/imazen/imageflow-dotnet)
[![Build status](https://ci.appveyor.com/api/projects/status/vqfofqe3bwqwdu4a?svg=true)](https://ci.appveyor.com/project/imazen/imageflow-dotnet)


Imageflow.NET is a .NET API for Imageflow, the image handling library for web servers. Imageflow focuses on security, quality, and performance - in that order.


```
PM> Install-Package Imageflow.Net
PM> Install-Package Imageflow.NativeRuntime.win-x86 
PM> Install-Package Imageflow.NativeRuntime.win-x86_64
PM> Install-Package Imageflow.NativeRuntime.win-x86_64-haswell
PM> Install-Package Imageflow.NativeRuntime.osx_10_11-x86_64
PM> Install-Package Imageflow.NativeRuntime.ubuntu_16_04-x86_64 
PM> Install-Package Imageflow.NativeRuntime.ubuntu_18_04-x86_64
PM> Install-Package Imageflow.NativeRuntime.ubuntu_18_04-x86_64-haswell
```

Note: You must install the [appropriate NativeRuntime(s)](https://www.nuget.org/packages?q=Imageflow+AND+NativeRuntime) in the project you are deploying - they have to copy imageflow.dll to the output folder. 

[NativeRuntimes](https://www.nuget.org/packages?q=Imageflow+AND+NativeRuntime) that are suffixed with -haswell (2013, AVX2 support) require a CPU of that generation or later. 


## [Examples are in the tests](https://github.com/imazen/imageflow-dotnet/tree/master/tests/Imageflow.Test)


* [Project source and issue site](https://github.com/imazen/imageflow-dotnet)
