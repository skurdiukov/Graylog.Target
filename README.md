# Graylog.Target ![http://teamcity.esphere.local/viewType.html?buildTypeId=mod_graylog_target_pack](http://teamcity.esphere.local/app/rest/builds/buildType:(id:mod_graylog_target_pack),branch:master/statusIcon.svg)
Graylog.Target is an [NLog] target implementation to push log messages to [GrayLog2]. It implements the [Gelf] specification and communicates with GrayLog server via UDP.

## Solution
Solution is comprised of 3 projects: *Target* is the actual NLog target implementation, *UnitTest* contains the unit tests for the NLog target, and *ConsoleRunner* is a simple console project created in order to demonstrate the library usage.
## Usage
Use Nuget:
```
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
	  <!-- Other targets (e.g. console) -->
    
	  <target name="graylog" 
			  xsi:type="graylog" 
			  hostip="192.168.1.7 or hostname" 
			  hostport="12201" 
			  facility="console-runner"
	  />
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
```c#
_log
    .WithProperty("PropertyName1", 1)
    .WithProperty("PropertyName2", 2)
    .Debug("Debug");
```
or in serilog format:
```c#
_log.Info("Total time elapsed {Elapsed}ms", elapsed);
```

[NLog]: http://nlog-project.org/
[GrayLog2]: https://www.graylog.org/
[Gelf]: http://docs.graylog.org/en/stable/pages/gelf.html
