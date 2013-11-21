using System.Diagnostics;
using System.Globalization;

namespace System.Drawing.PSD
{
	public partial class Layer
	{
		public class BlendingRanges
		{
			public Layer Layer { get; private set; }
			public Byte[] Data { get; set; }

			public BlendingRanges(Layer layer)
			{
				Data = new Byte[0];
				Layer = layer;
				Layer.BlendingRangesData = this;
			}

			public BlendingRanges(BinaryReverseReader reader, Layer layer)
			{
				Data = new Byte[0];
				Debug.WriteLine("BlendingRanges started at " + reader.BaseStream.Position.ToString(CultureInfo.InvariantCulture));

				Layer = layer;
				Int32 dataLength = reader.ReadInt32();
				if (dataLength <= 0) return;

				Data = reader.ReadBytes(dataLength);
			}

			public void Save(BinaryReverseWriter writer)
			{
				Debug.WriteLine("BlendingRanges Save started at " + writer.BaseStream.Position.ToString(CultureInfo.InvariantCulture));

				writer.Write((UInt32)Data.Length);
				writer.Write(Data);
			}
		}
	}
}
