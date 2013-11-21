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
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Globalization;
using System.IO;

namespace System.Drawing.PSD
{
	public partial class Layer
	{
		internal PsdFile PsdFile { get; private set; }

		/// <summary>
		/// The rectangle containing the contents of the layer.
		/// </summary>
        public Rectangle Rect { get; private set; }
		public List<Channel> Channels { get; private set; }
		public SortedList<Int16, Channel> SortedChannels { get; private set; }
		/// <summary>
		/// The blend mode key for the layer
		/// </summary>
		/// <remarks>
		/// <list type="table">
		/// <term>norm</term><description>normal</description>
		/// <term>dark</term><description>darken</description>
		/// <term>lite</term><description>lighten</description>
		/// <term>hue </term><description>hue</description>
		/// <term>sat </term><description>saturation</description>
		/// <term>colr</term><description>color</description>
		/// <term>lum </term><description>luminosity</description>
		/// <term>mul </term><description>multiply</description>
		/// <term>scrn</term><description>screen</description>
		/// <term>diss</term><description>dissolve</description>
		/// <term>over</term><description>overlay</description>
		/// <term>hLit</term><description>hard light</description>
		/// <term>sLit</term><description>soft light</description>
		/// <term>diff</term><description>difference</description>
		/// <term>smud</term><description>exlusion</description>
		/// <term>div </term><description>color dodge</description>
		/// <term>idiv</term><description>color burn</description>
		/// </list>
		/// </remarks>
		private string _blendModeKeyStr = "norm";
		public string BlendModeKey
		{
			get { return _blendModeKeyStr; }
			private set
			{
				if (value.Length != 4) throw new ArgumentException("Key length must be 4");
				_blendModeKeyStr = value;
			}
		}

		/// <summary>
		/// 0 = transparent ... 255 = opaque
		/// </summary>
		public byte Opacity { get; private set; }

		/// <summary>
		/// false = base, true = nonbase
		/// </summary>
        public bool Clipping { get; private set; }

		private static readonly int ProtectTransBit = BitVector32.CreateMask();
		private static readonly int VisibleBit = BitVector32.CreateMask(ProtectTransBit);

		BitVector32 _flags;

		/// <summary>
		/// If true, the layer is visible.
		/// </summary>
		public bool Visible
		{
			get { return !_flags[VisibleBit]; }
			private set { _flags[VisibleBit] = !value; }
		}

		/// <summary>
		/// Protect the transparency
		/// </summary>
		public bool ProtectTrans
		{
			get { return _flags[ProtectTransBit]; }
			private set { _flags[ProtectTransBit] = value; }
		}

		/// <summary>
		/// The descriptive layer name
		/// </summary>
		public string Name { get; private set; }

		public BlendingRanges BlendingRangesData { get; set; }
        public Mask MaskData { get; private set; }
        public List<AdjusmentLayerInfo> AdjustmentInfo { get; private set; }

		public Layer(PsdFile psdFile)
		{
			AdjustmentInfo = new List<AdjusmentLayerInfo>();
			SortedChannels = new SortedList<Int16, Channel>();
			Channels = new List<Channel>();
			Rect = Rectangle.Empty;
			PsdFile = psdFile;
		}

		public Layer(BinaryReverseReader reverseReader, PsdFile psdFile)
		{
			AdjustmentInfo = new List<AdjusmentLayerInfo>();
			SortedChannels = new SortedList<Int16, Channel>();
			Channels = new List<Channel>();
			Debug.WriteLine("Layer started at " + reverseReader.BaseStream.Position.ToString(CultureInfo.InvariantCulture));

			PsdFile = psdFile;

			Rectangle localRectangle = new Rectangle
			                           {
				                           Y = reverseReader.ReadInt32(),
										   X = reverseReader.ReadInt32()
			                           };
			localRectangle.Height = reverseReader.ReadInt32() - localRectangle.Y;
			localRectangle.Width = reverseReader.ReadInt32() - localRectangle.X;

			Rect = localRectangle;

			Int32 numberOfChannels = reverseReader.ReadUInt16();
			Channels.Clear();
			for (Int32 channel = 0; channel < numberOfChannels; channel++)
			{
				Channel ch = new Channel(reverseReader, this);
				Channels.Add(ch);
				SortedChannels.Add(ch.ID, ch);
			}

			String signature = new String(reverseReader.ReadChars(4));

			if (signature != "8BIM") throw (new IOException("Layer Channelheader error"));

			_blendModeKeyStr = new String(reverseReader.ReadChars(4));
			Opacity = reverseReader.ReadByte();

			Clipping = reverseReader.ReadByte() > 0;

			Byte flags = reverseReader.ReadByte();
			_flags = new BitVector32(flags);

			reverseReader.ReadByte(); //padding

			Debug.WriteLine("Layer extraDataSize started at " + reverseReader.BaseStream.Position.ToString(CultureInfo.InvariantCulture));

			// this is the total size of the MaskData, the BlendingRangesData, the 
			// Name and the AdjustmenLayerInfo
			UInt32 extraDataSize = reverseReader.ReadUInt32();

			// remember the start position for calculation of the 
			// AdjustmenLayerInfo size
			Int64 extraDataStartPosition = reverseReader.BaseStream.Position;

			MaskData = new Mask(reverseReader, this);
			BlendingRangesData = new BlendingRanges(reverseReader, this);

			Int64 namePosition = reverseReader.BaseStream.Position;

			Name = reverseReader.ReadPascalString();

			Int32 paddingBytes = (Int32)((reverseReader.BaseStream.Position - namePosition) % 4);

			Debug.Print("Layer {0} padding bytes after name", paddingBytes);
			reverseReader.ReadBytes(paddingBytes);

			AdjustmentInfo.Clear();

			Int64 adjustmenLayerEndPos = extraDataStartPosition + extraDataSize;
			while (reverseReader.BaseStream.Position < adjustmenLayerEndPos)
			{
				try
				{
					AdjustmentInfo.Add(new AdjusmentLayerInfo(reverseReader, this));
				}
				catch
				{
					reverseReader.BaseStream.Position = adjustmenLayerEndPos;
				}
			}

			// make shure we are not on a wrong offset, so set the stream position 
			// manually
			reverseReader.BaseStream.Position = adjustmenLayerEndPos;
		}

		public void Save(BinaryReverseWriter reverseWriter)
		{
			Debug.WriteLine("Layer Save started at " + reverseWriter.BaseStream.Position.ToString(CultureInfo.InvariantCulture));

			reverseWriter.Write(Rect.Top);
			reverseWriter.Write(Rect.Left);
			reverseWriter.Write(Rect.Bottom);
			reverseWriter.Write(Rect.Right);

			reverseWriter.Write((Int16)Channels.Count);
			foreach (Channel ch in Channels) ch.Save(reverseWriter);

			const String signature = "8BIM";
			reverseWriter.Write(signature.ToCharArray());
			reverseWriter.Write(_blendModeKeyStr.ToCharArray());
			reverseWriter.Write(Opacity);
			reverseWriter.Write((Byte)(Clipping ? 1 : 0));
			reverseWriter.Write((Byte)_flags.Data);
			reverseWriter.Write((Byte)0);

			using (new LengthWriter(reverseWriter))
			{
				MaskData.Save(reverseWriter);
				BlendingRangesData.Save(reverseWriter);

				Int64 namePosition = reverseWriter.BaseStream.Position;

				reverseWriter.WritePascalString(Name);

				Int32 paddingBytes = (int)((reverseWriter.BaseStream.Position - namePosition) % 4);
				Debug.Print("Layer {0} write padding bytes after name", paddingBytes);

				for (Int32 i = 0; i < paddingBytes; i++) reverseWriter.Write((Byte)0);

				foreach (AdjusmentLayerInfo info in AdjustmentInfo) info.Save(reverseWriter);
			}
		}

        public override string ToString()
        {
            return String.Format("{0} {1} {2}", Name, Visible ? "Visible" : "Hidden", BlendModeKey);
        }

	}
}
