using System.Diagnostics;
using System.Globalization;
using System.IO;

namespace System.Drawing.PSD
{
	public partial class Layer
	{
		public class AdjusmentLayerInfo
		{
			/// <summary>
			/// The layer to which this info belongs
			/// </summary>
			internal Layer Layer { get; private set; }
			public String Key { get; set; }
			public Byte[] Data { get; set; }

			public AdjusmentLayerInfo(String key, Layer layer)
			{
				Key = key;
				Layer = layer;
				Layer.AdjustmentInfo.Add(this);
			}

			public AdjusmentLayerInfo(BinaryReverseReader reader, Layer layer)
			{
				Debug.WriteLine("AdjusmentLayerInfo started at " + reader.BaseStream.Position.ToString(CultureInfo.InvariantCulture));

				Layer = layer;

				String signature = new String(reader.ReadChars(4));
				if (signature != "8BIM")
				{
					throw new IOException("Could not read an image resource");
				}

				Key = new String(reader.ReadChars(4));

				UInt32 dataLength = reader.ReadUInt32();
				Data = reader.ReadBytes((Int32)dataLength);
			}

			public void Save(BinaryReverseWriter writer)
			{
				Debug.WriteLine("AdjusmentLayerInfo Save started at " + writer.BaseStream.Position.ToString(CultureInfo.InvariantCulture));

				const String signature = "8BIM";

				writer.Write(signature.ToCharArray());
				writer.Write(Key.ToCharArray());
				writer.Write((UInt32)Data.Length);
				writer.Write(Data);
			}

			public BinaryReverseReader DataReader
			{
				get
				{
					return new BinaryReverseReader(new MemoryStream(Data));
				}
			}
		}

	}
}
