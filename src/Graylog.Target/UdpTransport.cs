#region Usings

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;

#endregion

namespace Graylog.Target
{
	/// <summary>
	/// Udp transport.
	/// </summary>
	internal sealed class UdpTransport : ITransport
	{
		/// <summary>
		/// Chunk also contains 12 byte prefix, so 8192 - 12.
		/// </summary>
		internal const int MaxMessageSizeInChunk = MaxMessageSizeInUdp - PrefixSize;

		/// <summary>
		/// UDP datagrams are limited to a size of 8192 bytes.
		/// </summary>
		private const int MaxMessageSizeInUdp = 8192;

		/// <summary>
		/// Message prefix size 12 bytes.
		/// </summary>
		private const int PrefixSize = 12;

		/// <summary>
		/// Limitation from GrayLog2.
		/// </summary>
		private const int MaxNumberOfChunksAllowed = 128;

		/// <summary>
		/// Transport client.
		/// </summary>
		private readonly ITransportClient _transportClient;

		/// <summary>
		/// Create new instance of <see cref="UdpTransport"/> class.
		/// </summary>
		/// <param name="transportClient">Transport client.</param>
		public UdpTransport(ITransportClient transportClient)
		{
			_transportClient = transportClient;
		}

		/// <summary>
		/// Sends a UDP datagram to GrayLog2 server.
		/// </summary>
		/// <param name="serverIpAddress">IP address of the target GrayLog2 server.</param>
		/// <param name="serverPort">Port number of the target GrayLog2 instance.</param>
		/// <param name="message">Message (in JSON) to log.</param>
		public void Send(string serverIpAddress, int serverPort, string message)
		{
			var compressedMessage = CompressMessage(message);

			if (compressedMessage.Length > MaxMessageSizeInUdp)
			{
				_transportClient.Send(CreateChunks(compressedMessage), serverIpAddress, serverPort);
			}
			else
			{
				_transportClient.Send(compressedMessage, compressedMessage.Length, serverIpAddress, serverPort);
			}
		}

		/// <summary>
		/// Create chunks from source message.
		/// </summary>
		/// <param name="message">Source message.</param>
		/// <returns>Collection of chunks.</returns>
		internal static IEnumerable<byte[]> CreateChunks(byte[] message)
		{
			// Our compressed message is too big to fit in a single datagram. Need to chunk...
			// https://github.com/Graylog2/graylog2-docs/wiki/GELF "Chunked GELF"
			var numberOfChunksRequired = (message.Length - 1) / MaxMessageSizeInChunk + 1;
			if (numberOfChunksRequired > MaxNumberOfChunksAllowed)
				yield break;

			var messageId = GenerateMessageId(message);

			var sendBuffer = new byte[MaxMessageSizeInUdp];
			var header = ConstructChunkHeader(messageId, 0, numberOfChunksRequired);

			header.CopyTo(sendBuffer, 0);

			for (var i = 0; i < numberOfChunksRequired; i++)
			{
				var offset = i * MaxMessageSizeInChunk;
				var chunkSize = Math.Min(MaxMessageSizeInChunk, message.Length - offset);
				sendBuffer[10] = (byte)i;

				if (sendBuffer.Length != PrefixSize + chunkSize)
				{
					Array.Resize(ref sendBuffer, PrefixSize + chunkSize);
				}

				Array.Copy(message, offset, sendBuffer, PrefixSize, chunkSize);

				yield return sendBuffer;
			}
		}

		/// <summary>
		/// Chunk header structure is:
		/// - Chunked GELF ID: 0x1e 0x0f (identifying this message as a chunked GELF message)
		/// - Message ID: 8 bytes (Must be the same for every chunk of this message. Identifying the whole message itself and is used to reassemble the chunks later.)
		/// - Sequence Number: 1 byte (The sequence number of this chunk)
		/// - Total Number: 1 byte (How many chunks does this message consist of in total).
		/// </summary>
		/// <param name="messageId">Unique identifier of the whole message (not just this chunk).</param>
		/// <param name="chunkSequenceNumber">Sequence number of this chunk.</param>
		/// <param name="chunkCount">Total number of chunks whole message consists of.</param>
		/// <returns>Chunk header in bytes.</returns>
		private static byte[] ConstructChunkHeader(byte[] messageId, int chunkSequenceNumber, int chunkCount)
		{
			var b = new byte[12];

			b[0] = 0x1e;
			b[1] = 0x0f;
			messageId.CopyTo(b, 2);
			b[10] = (byte)chunkSequenceNumber;
			b[11] = (byte)chunkCount;

			return b;
		}

		/// <summary>
		/// Compresses the given message using GZip algorithm.
		/// </summary>
		/// <param name="message">Message to be compressed.</param>
		/// <returns>Compressed message in bytes.</returns>
		private static byte[] CompressMessage(string message)
		{
			var compressedMessageStream = new MemoryStream();
			using (var gzipStream = new GZipStream(compressedMessageStream, CompressionLevel.Optimal))
			{
				var messageBytes = Encoding.UTF8.GetBytes(message);
				gzipStream.Write(messageBytes, 0, messageBytes.Length);
				gzipStream.Flush();
			}

			return compressedMessageStream.ToArray();
		}

		/// <summary>
		/// Generates a unique identifier for the whole message.
		/// Message id is composed of
		/// - 3rd segment of the IP address - 8 bits
		/// - 4th segment of the IP address - 8 bits
		/// - DateTime.Now.Second - 6 bits
		/// - First 42 bits of MD5 hash of compressed message.
		/// </summary>
		/// <param name="compressedMessage">Compressed message.</param>
		/// <returns>Byte arraye with uniqueue id of message.</returns>
		private static byte[] GenerateMessageId(byte[] compressedMessage)
		{
			// create a bit array to store the entire message id (which is 8 bytes)
			var bitArray = new BitArray(64);

			// Read the server ip address
			var addresses = Dns.GetHostAddresses(Dns.GetHostName());
			var address = addresses.FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork);

			if (address == null)
				return null;

			// read bytes of the last 2 segments and insert bits into the bit array
			var addressBytes = address.GetAddressBytes();
			AddToBitArray(bitArray, 0, addressBytes[2], 0, 8);
			AddToBitArray(bitArray, 8, addressBytes[3], 0, 8);

			// read the current second and insert 6 bits into the bit array
			var second = DateTime.Now.Second;
			AddToBitArray(bitArray, 16, (byte)second, 0, 6);

			// generate the MD5 hash of the compressed message
			byte[] hashOfCompressedMessage;
			using (var md5 = MD5.Create())
			{
				hashOfCompressedMessage = md5.ComputeHash(compressedMessage);
			}

			// insert the first 42 bits into the bit array
			var startIndex = 22;
			for (var hashByteIndex = 0; hashByteIndex < 5; hashByteIndex++)
			{
				var hashByte = hashOfCompressedMessage[hashByteIndex];
				AddToBitArray(bitArray, startIndex, hashByte, 0, 8);
				startIndex += 8;
			}

			// copy all bits from bit array into a byte[]
			var result = new byte[8];
			bitArray.CopyTo(result, 0);

			return result;
		}

		/// <summary>
		/// Inserts bits from the given byte into the given BitArray instance.
		/// </summary>
		/// <param name="bitArray">BitArray instance to be populated with bits.</param>
		/// <param name="bitArrayIndex">Index pointer in BitArray to start inserting bits from.</param>
		/// <param name="byteData">Byte to extract bits from and insert into the given BitArray instance.</param>
		/// <param name="byteDataIndex">Index pointer in byteData to start extracting bits from.</param>
		/// <param name="length">Number of bits to extract from byteData.</param>
		private static void AddToBitArray(BitArray bitArray, int bitArrayIndex, byte byteData, int byteDataIndex, int length)
		{
			var localBitArray = new BitArray(new[] { byteData });

			for (var i = byteDataIndex + length - 1; i >= byteDataIndex; i--)
			{
				bitArray.Set(bitArrayIndex, localBitArray.Get(i));
				bitArrayIndex++;
			}
		}
	}
}