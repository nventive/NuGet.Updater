## About

NuGet.Updater allows batch updates of NuGet packages found in solutions.

## Getting Started

The NuGet Updater can be installed as a standalone .Net Core tool using the following command:
`dotnet tool install -g nventive.NuGet.Updater.Tool`

Help can be found with :
`nugetupdater --help`

The NuGet updater library can also be installed as a NuGet package in a UWP or .Net Standard application.

## Sample commands

- Update all packages in the current folder (and its subfolders) to the latest stable version found on NuGet.org
```
nugetupdater --useNuGetorg
```
This can also be achieved using the following command
```
nugetupdater --feed=https://api.nuget.org/v3/index.json
```

- Update all packages in `MySolution.sln` to the latest stable version available on NuGet.org
```
nugetupdater --solution=MySolution.sln -n
```

- Update packages to either beta, stable or alpha (whichever's the highest)
```
nugetupdater -s=MySolution.sln -n --version=beta -v=alpha
```

- Update packages to the latest beta version available on a private feed
```
nugetupdater -s=MySolution.sln --feed=https://pkgs.dev.azure.com/account/_packaging/feed/nuget/v3/index.json|personalaccesstoken --version=beta
```

- Update packages from `nventive` from NuGet.org, except for `PackageA` and `PackageB`
```
nugetupdater -s=MySolution.sln -n --packageAuthor=nventive --ignore=PackageA -i=PackageB
```

- Update only `PackageA` and `PackageB` from NuGet.org and a private feed
```
nugetupdater -s=MySolution.sln -n -f=https://pkgs.dev.azure.com/account/_packaging/feed/nuget/v3/index.json|personalaccesstoken --update=PackageA -u=PackageB
```

- Update packages to latest stable, even if a higher version is already found in the solution
```
nugetupdater -s=MySolution.sln -n --allowDowngrade
```
