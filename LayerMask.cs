using System.Collections.Specialized;
using System.Diagnostics;
using System.Globalization;

namespace System.Drawing.PSD
{
	public partial class Layer
	{
		public class Mask
		{
			/// <summary>
			/// The layer to which this mask belongs
			/// </summary>
			public Layer Layer { get; private set; }

			/// <summary>
			/// The rectangle enclosing the mask.
			/// </summary>
			public Rectangle Rect { get; set; }

			public byte DefaultColor { get; set; }


			private static readonly int PositionIsRelativeBit = BitVector32.CreateMask();
			private static readonly int DisabledBit = BitVector32.CreateMask(PositionIsRelativeBit);
// ReSharper disable InconsistentNaming
			private static readonly int _invertOnBlendBit = BitVector32.CreateMask(DisabledBit);
// ReSharper restore InconsistentNaming

			private BitVector32 _flags;
			/// <summary>
			/// If true, the position of the mask is relative to the layer.
			/// </summary>
			public bool PositionIsRelative
			{
				get
				{
					return _flags[PositionIsRelativeBit];
				}
				set
				{
					_flags[PositionIsRelativeBit] = value;
				}
			}

			public bool Disabled
			{
				get { return _flags[DisabledBit]; }
				set { _flags[DisabledBit] = value; }
			}

			/// <summary>
			/// if true, invert the mask when blending.
			/// </summary>
			public bool InvertOnBlendBit
			{
				get { return _flags[_invertOnBlendBit]; }
				set { _flags[_invertOnBlendBit] = value; }
			}

			internal Mask(Layer layer)
			{
				Layer = layer;
				Layer.MaskData = this;
			}

			internal Mask(BinaryReverseReader reader, Layer layer)
			{
				Debug.WriteLine("Mask started at " + reader.BaseStream.Position.ToString(CultureInfo.InvariantCulture));

				Layer = layer;

				uint maskLength = reader.ReadUInt32();

				if (maskLength <= 0)
					return;

				long startPosition = reader.BaseStream.Position;

				Rectangle localRectangle = new Rectangle
				{
					Y = reader.ReadInt32(),
					X = reader.ReadInt32()
				};
				localRectangle.Height = reader.ReadInt32() - localRectangle.Y;
				localRectangle.Width = reader.ReadInt32() - localRectangle.X;

				Rect = localRectangle;

				DefaultColor = reader.ReadByte();

				byte flags = reader.ReadByte();
				_flags = new BitVector32(flags);

				if (maskLength == 36)
				{
#pragma warning disable 168
					BitVector32 realFlags = new BitVector32(reader.ReadByte());

					byte realUserMaskBackground = reader.ReadByte();

					Rectangle rect = new Rectangle
					{
						Y = reader.ReadInt32(),
						X = reader.ReadInt32(),
						Height = reader.ReadInt32() - Rect.Y,
						Width = reader.ReadInt32() - Rect.X
					};
#pragma warning restore 168
                }

				// there is other stuff following, but we will ignore this.
				reader.BaseStream.Position = startPosition + maskLength;
			}

			public void Save(BinaryReverseWriter writer)
			{
				Debug.WriteLine("Mask Save started at " + writer.BaseStream.Position.ToString(CultureInfo.InvariantCulture));

				if (Rect.IsEmpty)
				{
					writer.Write((uint)0);
					return;
				}

				using (new LengthWriter(writer))
				{
					writer.Write(Rect.Top);
					writer.Write(Rect.Left);
					writer.Write(Rect.Bottom);
					writer.Write(Rect.Right);

					writer.Write(DefaultColor);

					writer.Write((byte)_flags.Data);

					// padding 2 bytes so that size is 20
					writer.Write(0);
				}
			}

		    public byte[] ImageData { get; set; }

		    internal void LoadPixelData(BinaryReverseReader reader)
			{
				Debug.WriteLine("Mask.LoadPixelData started at " + reader.BaseStream.Position.ToString(CultureInfo.InvariantCulture));

				if (Rect.IsEmpty || Layer.SortedChannels.ContainsKey(-2) == false)
					return;

				Channel maskChannel = Layer.SortedChannels[-2];


				maskChannel.Data = reader.ReadBytes(maskChannel.Length);


				using (BinaryReverseReader readerImg = maskChannel.DataReader)
				{
					maskChannel.ImageCompression = (ImageCompression)readerImg.ReadInt16();

					int bytesPerRow = 0;

					switch (Layer.PsdFile.Depth)
					{
						case 1:
							bytesPerRow = Rect.Width;//NOT Shure
							break;
						case 8:
							bytesPerRow = Rect.Width;
							break;
						case 16:
							bytesPerRow = Rect.Width * 2;
							break;
					}

					maskChannel.ImageData = new byte[Rect.Height * bytesPerRow];
					// Fill Array
					for (int i = 0; i < maskChannel.ImageData.Length; i++)
					{
						maskChannel.ImageData[i] = 0xAB;
					}

					ImageData = (byte[])maskChannel.ImageData.Clone();

					switch (maskChannel.ImageCompression)
					{
						case ImageCompression.Raw:
							readerImg.Read(maskChannel.ImageData, 0, maskChannel.ImageData.Length);
							break;
						case ImageCompression.Rle:
							{
								int[] rowLenghtList = new int[Rect.Height];

								for (int i = 0; i < rowLenghtList.Length; i++)
									rowLenghtList[i] = readerImg.ReadInt16();

								for (int i = 0; i < Rect.Height; i++)
								{
									int rowIndex = i * Rect.Width;
									RleHelper.DecodedRow(readerImg.BaseStream, maskChannel.ImageData, rowIndex, bytesPerRow);
								}
							}
							break;
					}

					ImageData = (byte[])maskChannel.ImageData.Clone();

				}
			}

			internal void SavePixelData(BinaryReverseWriter writer)
			{
				//writer.Write(m_data);
			}
		}
	}
}
