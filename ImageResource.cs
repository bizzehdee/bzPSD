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
	public enum ResourceIDs
	{
		Undefined = 0,
		MacPrintInfo = 1001,
		ResolutionInfo = 1005,
		AlphaChannelNames = 1006,
		DisplayInfo = 1007,
		Caption = 1008,
		BorderInfo = 1009,
		BgColor = 1010,
		PrintFlags = 1011,
		MultiChannelHalftoneInfo = 1012,
		ColorHalftoneInfo = 1013,
		DuotoneHalftoneInfo = 1014,
		MultiChannelTransferFunctions = 1015,
		ColorTransferFunctions = 1016,
		DuotoneTransferFunctions = 1017,
		DuotoneImageInfo = 1018,
		BlackWhiteRange = 1019,
		EPSOptions = 1021,
		QuickMaskInfo = 1022, //2 bytes containing Quick Mask channel ID, 1 byte boolean indicating whether the mask was initially empty.
		LayerStateInfo = 1024, //2 bytes containing the index of target layer. 0=bottom layer.
		WorkingPathUnsaved = 1025,
		LayersGroupInfo = 1026, //2 bytes per layer containing a group ID for the dragging groups. Layers in a group have the same group ID.
// ReSharper disable InconsistentNaming
		IPTC_NAA = 1028,
// ReSharper restore InconsistentNaming
		RawFormatImageMode = 1029,
		JPEGQuality = 1030,
		GridGuidesInfo = 1032,
		Thumbnail1 = 1033,
		CopyrightInfo = 1034,
		URL = 1035,
		Thumbnail2 = 1036,
		GlobalAngle = 1037,
		ColorSamplers = 1038,
		ICCProfile = 1039, //The raw bytes of an ICC format profile, see the ICC34.pdf and ICC34.h files from the Internation Color Consortium located in the documentation section
		Watermark = 1040,
		ICCUntagged = 1041, //1 byte that disables any assumed profile handling when opening the file. 1 = intentionally untagged.
		EffectsVisible = 1042, //1 byte global flag to show/hide all the effects layer. Only present when they are hidden.
		SpotHalftone = 1043, // 4 bytes for version, 4 bytes for length, and the variable length data.
		DocumentSpecific = 1044,
		UnicodeAlphaNames = 1045, // 4 bytes for length and the string as a unicode string
		IndexedColorTableCount = 1046, // 2 bytes for the number of colors in table that are actually defined
		TransparentIndex = 1047,
		GlobalAltitude = 1049,  // 4 byte entry for altitude
		Slices = 1050,
		WorkflowURL = 1051, //Unicode string, 4 bytes of length followed by unicode string
		JumpToXPEP = 1052, //2 bytes major version, 2 bytes minor version,
		//4 bytes count. Following is repeated for count: 4 bytes block size,
		//4 bytes key, if key = 'jtDd' then next is a Boolean for the dirty flag
		//otherwise it’s a 4 byte entry for the mod date
		AlphaIdentifiers = 1053, //4 bytes of length, followed by 4 bytes each for every alpha identifier.
		URLList = 1054, //4 byte count of URLs, followed by 4 byte long, 4 byte ID, and unicode string for each count.
		VersionInfo = 1057, //4 byte version, 1 byte HasRealMergedData, unicode string of writer name, unicode string of reader name, 4 bytes of file version.
		Unknown4 = 1058, //pretty long, 302 bytes in one file. Holds creation date, maybe Photoshop license number
		XMLInfo = 1060, //some kind of XML definition of file. The xpacket tag seems to hold binary data
		Unknown = 1061, //seems to be common!
		Unknown2 = 1062, //seems to be common!
		Unknown3 = 1064, //seems to be common!
		PathInfo = 2000, //2000-2999 actually I think?
		ClippingPathName = 2999,
		PrintFlagsInfo = 10000
	}

	/// <summary>
	/// Summary description for ImageResource.
	/// </summary>
	public class ImageResource
	{
		public Int16 ID { get; set; }
        public String Name { get; private set; }
        public Byte[] Data { get; set; }
        public String OSType { get; private set; }

		public ImageResource()
		{
			OSType = String.Empty;
			Name = String.Empty;
		}

		public ImageResource(short id)
		{
			OSType = String.Empty;
			Name = String.Empty;
			ID = id;
		}

		public ImageResource(ImageResource imgRes)
		{
			OSType = String.Empty;
			ID = imgRes.ID;
			Name = imgRes.Name;

			Data = new Byte[imgRes.Data.Length];
			imgRes.Data.CopyTo(Data, 0);
		}

		public ImageResource(BinaryReverseReader reverseReader)
		{
			Name = String.Empty;
			OSType = new String(reverseReader.ReadChars(4));
			if (OSType != "8BIM" && OSType != "MeSa")
			{
				throw new InvalidOperationException("Could not read an image resource");
			}

			ID = reverseReader.ReadInt16();
			Name = reverseReader.ReadPascalString();

			UInt32 settingLength = reverseReader.ReadUInt32();
			Data = reverseReader.ReadBytes((Int32)settingLength);

			if (reverseReader.BaseStream.Position % 2 == 1) reverseReader.ReadByte();
		}

		public void Save(BinaryReverseWriter reverseWriter)
		{
			StoreData();

			if (OSType == String.Empty) OSType = "8BIM";

			reverseWriter.Write(OSType.ToCharArray());
			reverseWriter.Write(ID);

			reverseWriter.WritePascalString(Name);

			reverseWriter.Write(Data.Length);
			reverseWriter.Write(Data);

			if (reverseWriter.BaseStream.Position % 2 == 1) reverseWriter.Write((Byte)0);
		}

		protected virtual void StoreData()
		{

		}

		public BinaryReverseReader DataReader
		{
			get
			{
				return new BinaryReverseReader(new MemoryStream(Data));
			}
		}

		public override string ToString()
		{
			return String.Format("{0} {1}", (ResourceIDs)ID, Name);
		}
	}
}
