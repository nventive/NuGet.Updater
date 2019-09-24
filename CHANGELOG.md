# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
- Added aliases for .Net Core application
- Added support for multiple target versions
- Added support for solution folders alongside sln files
- Added better error handling
- Added support for the "strict" parameter in the .Net Core app

### Changed
- Replaced package owner with package author
- Reworked internal logic of the updater: package list is now fetched from the files before being looked up online
- Updated sources handling to add support for any number of public or private package sources (it was previously limited to NuGet.org and any number of private package sources
- Set the current directory as the solution root if none is specified in the .Net Core application

### Deprecated

### Removed
- Removed superfluous arguments
- Removed code used to update project.json files

### Fixed

### Security
