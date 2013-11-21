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
