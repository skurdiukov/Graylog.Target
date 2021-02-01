using System;
using System.Collections.Generic;

namespace Graylog.Target
{
	/// <summary>
	/// Расширения для работы с датами.
	/// </summary>
	internal static class Extensions
	{
		/// <summary>
		/// Начало эпохи Unix.
		/// </summary>
		internal static readonly DateTime UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

		/// <summary>
		/// Форматирует дату в виде Unix TimeStamp.
		/// </summary>
		/// <param name="date">Дата и время.</param>
		/// <returns>Отформатированная дата.</returns>
		public static double ToUnixTimestamp(this DateTime date)
		{
			return (date.ToUniversalTime() - UnixEpoch).TotalMilliseconds / 1000;
		}

		/// <summary>
		/// Yield single object as enumerable.
		/// </summary>
		/// <typeparam name="T">Object type.</typeparam>
		/// <param name="obj">Object value.</param>
		/// <returns>IEmumerable with single object.</returns>
		public static IEnumerable<T> Yield<T>(T obj)
		{
			yield return obj;
		}
	}
}