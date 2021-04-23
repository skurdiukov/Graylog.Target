#region Usings

using System;
using System.IO;
using System.Threading;
using LumenWorks.Framework.IO.Csv;

using NLog;

#endregion

namespace ConsoleRunner
{
	internal class Program
	{
		private static readonly Random Random = new Random();
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		public static void Main()
		{
			while (true)
			{
				var comic = GetNextComic();

				var eventInfo = new LogEventInfo
				{
					TimeStamp = DateTime.Now,
					Message = comic.Title,
					Level = LogLevel.Info,
				};
				eventInfo.Properties.Add("Publisher", comic.Publisher);
				eventInfo.Properties.Add("ReleaseDate", comic.ReleaseDate);

				Logger.Log(eventInfo);

				try
				{
					throw new Exception("Test exception", new Exception("Inner exception"));
				}
				catch (Exception e)
				{
					Logger.Warn(e, "Test message");
				}

				Thread.Sleep(1000);
			}
		}

		private static Comic GetNextComic()
		{
			var nextComicIndex = Random.Next(1, 400);
			using var csv = new CsvReader(new StreamReader("comics.csv"), false);
			
			csv.MoveTo(nextComicIndex);
			
			return new Comic
			{
				Title = csv[2],
				Publisher = csv[1],
				ReleaseDate = csv[0]
			};
		}

		internal class Comic
		{
			public string Title { get; set; }

			public string ReleaseDate { get; set; }

			public string Publisher { get; set; }
		}
	}
}