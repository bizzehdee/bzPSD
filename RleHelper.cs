#region Licence
/*
Copyright (c) 2013, Darren Horrocks
All rights reserved.

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are met:
    * Redistributions of source code must retain the above copyright
      notice, this list of conditions and the following disclaimer.
    * Redistributions in binary form must reproduce the above copyright
      notice, this list of conditions and the following disclaimer in the
      documentation and/or other materials provided with the distribution.
    * Neither the name of the <organization> nor the
      names of its contributors may be used to endorse or promote products
      derived from this software without specific prior written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
DISCLAIMED. IN NO EVENT SHALL <COPYRIGHT HOLDER> BE LIABLE FOR ANY
DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
(INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
(INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */
#endregion
using System.IO;

namespace System.Drawing.PSD
{
	class RleHelper
	{
		private class RlePacketStateMachine
		{
			private bool _rlePacket;
			private readonly byte[] _packetValues = new byte[128];
			private int _packetLength;
			private readonly Stream _stream;

			internal void Flush()
			{
				byte header;
				if (_rlePacket)
				{
					header = (byte)(-(_packetLength - 1));
				}
				else
				{
					header = (byte)(_packetLength - 1);
				}

				_stream.WriteByte(header);

				int length = (_rlePacket ? 1 : _packetLength);

				_stream.Write(_packetValues, 0, length);

				_packetLength = 0;
			}

			internal void Push(byte color)
			{
				switch (_packetLength)
				{
					case 0:
						_rlePacket = false;
						_packetValues[0] = color;
						_packetLength = 1;
						break;
					case 1:
						_rlePacket = (color == _packetValues[0]);
						_packetValues[1] = color;
						_packetLength = 2;
						break;
					default:
						if (_packetLength == _packetValues.Length)
						{
							// Packet is full. Start a new one.
							Flush();
							Push(color);
						}
						else if (_packetLength >= 2 && _rlePacket && color != _packetValues[_packetLength - 1])
						{
							// We were filling in an RLE packet, and we got a non-repeated color.
							// Emit the current packet and start a new one.
							Flush();
							Push(color);
						}
						else if (_packetLength >= 2 && _rlePacket && color == _packetValues[_packetLength - 1])
						{
							// We are filling in an RLE packet, and we got another repeated color.
							// Add the new color to the current packet.
							++_packetLength;
							_packetValues[_packetLength - 1] = color;
						}
						else if (_packetLength >= 2 && !_rlePacket && color != _packetValues[_packetLength - 1])
						{
							// We are filling in a raw packet, and we got another random color.
							// Add the new color to the current packet.
							++_packetLength;
							_packetValues[_packetLength - 1] = color;
						}
						else if (_packetLength >= 2 && !_rlePacket && color == _packetValues[_packetLength - 1])
						{
							// We were filling in a raw packet, but we got a repeated color.
							// Emit the current packet without its last color, and start a
							// new RLE packet that starts with a length of 2.
							--_packetLength;
							Flush();
							Push(color);
							Push(color);
						}
						break;
				}
			}

			internal RlePacketStateMachine(Stream stream)
			{
				_stream = stream;
			}
		}

		public static int EncodedRow(Stream stream, byte[] imgData, int startIdx, int columns)
		{
			long startPosition = stream.Position;

			RlePacketStateMachine machine = new RlePacketStateMachine(stream);

			for (int x = 0; x < columns; ++x)
				machine.Push(imgData[x + startIdx]);

			machine.Flush();

			return (int)(stream.Position - startPosition);
		}

		public static void DecodedRow(Stream stream, byte[] imgData, int startIdx, int columns)
		{
			int count = 0;
			while (count < columns)
			{
				byte byteValue = (byte)stream.ReadByte();

				int len = byteValue;
				if (len < 128)
				{
					len++;
					while (len != 0 && (startIdx + count) < imgData.Length)
					{
						byteValue = (byte)stream.ReadByte();

						imgData[startIdx + count] = byteValue;
						count++;
						len--;
					}
				}
				else if (len > 128)
				{
					// Next -len+1 bytes in the dest are replicated from next source byte.
					// (Interpret len as a negative 8-bit int.)
					len ^= 0x0FF;
					len += 2;
					byteValue = (byte)stream.ReadByte();

					while (len != 0 && (startIdx + count) < imgData.Length)
					{
						imgData[startIdx + count] = byteValue;
						count++;
						len--;
					}
				}
				else if (128 == len)
				{
					// Do nothing
				}
			}

		}
	}
}