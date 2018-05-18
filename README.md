# Graylog.Target

[![AppVeyor](https://img.shields.io/appveyor/ci/skurdiukov/graylog-target/master.svg)](https://ci.appveyor.com/project/skurdiukov/graylog-target) [![AppVeyor tests](https://img.shields.io/appveyor/tests/skurdiukov/graylog-target/master.svg)](https://ci.appveyor.com/project/skurdiukov/graylog-target) [![NuGet](https://img.shields.io/nuget/v/Graylog.Target.svg)](https://www.nuget.org/packages/Graylog.Target/)

Graylog.Target is an [NLog] target implementation to push log messages to [GrayLog2]. It implements the [Gelf] specification and communicates with GrayLog server via UDP.

## Solution

Solution is comprised of 3 projects: *Target* is the actual NLog target implementation, *UnitTest* contains the unit tests for the NLog target, and *ConsoleRunner* is a simple console project created in order to demonstrate the library usage.

## Usage

Use Nuget:

```shell
PM> Install-Package Graylog.Target
```

### Configuration

Here is a sample nlog configuration snippet:

```xml
<nlog xmlns="http://www.nlog-project.org/schemas/NLog.xsd"
      xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
  <extensions>
    <add assembly="Graylog.Target"/>
  </extensions>
  <targets>
    <target name="graylog"
            xsi:type="graylog"
            hostip="192.168.1.7 or hostname"
            hostport="12201"
            facility="console-runner" />
    </targets>
    <rules>
      <logger name="*" minlevel="Debug" writeTo="graylog" />
    </rules>
</nlog>
```

Options are the following:

* __name:__ arbitrary name given to the target
* __type:__ set this to "graylog"
* __hostip:__ IP address or hostname of the GrayLog2 server
* __hostport:__ Port number that GrayLog2 server is listening on
* __facility:__ The graylog2 facility to send log messages

### Code

NLog 4.5+ support [structured logging](https://github.com/nlog/nlog/wiki/How-to-use-structured-logging):

```csharp
_log.Info("Total time elapsed {Elapsed}ms", elapsed);
```

[NLog]: http://nlog-project.org/
[GrayLog2]: https://www.graylog.org/
[Gelf]: http://docs.graylog.org/en/stable/pages/gelf.html
