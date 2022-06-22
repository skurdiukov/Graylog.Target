# Changelog

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/) and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.6.0]
### Changed
- Package `NLog` updated to version `5.0.1`
- Package `Newtonsoft.Json` updated to version `13.0.1`

## [1.5.0]
### Added
- Option for switching objects serialization as logging properties.
- Some benchmarks.
### Changed
- Fixed ConsoleRunner.

## [1.4.0]
### Changed
- Updated dependencies.
- Fixed bug with loop reference in property value.
- No longer support .NET 4.5.

## [1.3.0]
### Added
- `MappedDiagnosticsLogicalContext` can be included into message if `IncludeMdlcProperties` set.

## [1.2.1]
### Changed
- Fixed bug, when second convertion of same message failed with exception.

## [1.2.0]
### Added
- Refactored `UdpTransportClient`.

## [1.1.0] - 2018-05-15
### Changed
- Optimized `UdpTransport.Send` method.


[1.6.0]: https://github.com/skurdiukov/Graylog.Target/compare/releases/v1.5.1...releases/v1.6.0
[1.2.0]: https://github.com/skurdiukov/Graylog.Target/compare/releases/v1.2.0...releases/v1.2.1
[1.2.0]: https://github.com/skurdiukov/Graylog.Target/compare/releases/v1.1.0...releases/v1.2.0
[1.1.0]: https://github.com/skurdiukov/Graylog.Target/compare/19959397d274e1f4a9c7af6289fdfb3935a33572...releases/v1.1.0