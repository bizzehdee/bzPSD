﻿#region Licence
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
using System.Collections.Generic;
using System.IO;

namespace System.Drawing.PSD
{
	public class AlphaChannels : ImageResource
	{
		public List<String> ChannelNames { get; set; }

		public AlphaChannels()
			: base((Int16)ResourceIDs.AlphaChannelNames)
		{
			ChannelNames = new List<String>();
		}

		public AlphaChannels(ImageResource imageResource)
			: base(imageResource)
		{
			ChannelNames = new List<String>();
			BinaryReverseReader reverseReader = imageResource.DataReader;
			// the names are pascal strings without padding!!!
			while ((reverseReader.BaseStream.Length - reverseReader.BaseStream.Position) > 0)
			{
				Byte stringLength = reverseReader.ReadByte();
				String s = new String(reverseReader.ReadChars(stringLength));

				if (s.Length > 0) ChannelNames.Add(s);
			}
			reverseReader.Close();
		}

		protected override void StoreData()
		{
			MemoryStream memoryStream = new MemoryStream();
			BinaryReverseWriter reverseWriter = new BinaryReverseWriter(memoryStream);

			foreach (String name in ChannelNames)
			{
				reverseWriter.Write((Byte)name.Length);
				reverseWriter.Write(name.ToCharArray());
			}

			reverseWriter.Close();
			memoryStream.Close();

			Data = memoryStream.ToArray();
		}
	}
}