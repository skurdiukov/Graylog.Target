#region Usings

using System;

using Newtonsoft.Json;

using NLog;
using NLog.Config;
using NLog.Targets;

#endregion

namespace Graylog.Target
{
	/// <summary>
	/// NLog target implementation.
	/// </summary>
	[Target("Graylog")]
	public class NLogTarget : TargetWithLayout
	{
		/// <summary>
		/// Create new instance of <see cref="NLogTarget"/> class.
		/// </summary>
		public NLogTarget()
		{
			Transport = new UdpTransport(new UdpTransportClient());
			Converter = new GelfConverter();
		}

		/// <summary>
		/// Create new instance of <see cref="NLogTarget"/> class.
		/// </summary>
		/// <param name="transport">Udp transport.</param>
		/// <param name="converter">Json converter.</param>
		public NLogTarget(ITransport transport, IConverter converter)
		{
			Transport = transport;
			Converter = converter;
		}

		/// <summary>
		/// Host ip address.
		/// </summary>
		[RequiredParameter]
		public string HostIp { get; set; }

		/// <summary>
		/// Host port.
		/// </summary>
		[RequiredParameter]
		public int HostPort { get; set; }

		/// <summary>
		/// Facility name.
		/// </summary>
		public string Facility { get; set; }

		/// <summary>
		/// Message converter.
		/// </summary>
		public IConverter Converter { get; }

		/// <summary>
		/// Message transport.
		/// </summary>
		public ITransport Transport { get; }

		/// <summary>
		/// Writes log event info into log.
		/// </summary>
		/// <param name="logEvent">Log event to be written out.</param>
		public void WriteLogEventInfo(LogEventInfo logEvent)
		{
			Write(logEvent);
		}

		/// <summary>
		/// Writes log event to the log target. Must be overridden in inheriting classes.
		/// </summary>
		/// <param name="logEvent">Log event to be written out.</param>
		protected override void Write(LogEventInfo logEvent)
		{
			var jsonObject = Converter.GetGelfJson(logEvent, Facility);
			if (jsonObject == null) return;
			Transport.Send(HostIp, HostPort, jsonObject.ToString(Formatting.None, null));
		}
	}
}