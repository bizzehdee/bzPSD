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
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;

namespace System.Drawing.PSD
{
	public class PsdFile
	{
		public enum ColorModes
		{
			Bitmap = 0, Grayscale = 1, Indexed = 2, RGB = 3, CMYK = 4, Multichannel = 7, Duotone = 8, Lab = 9
		};

		#region "Properties and Variables"

		/// <summary>
		/// If ColorMode is ColorModes.Indexed, the following 768 bytes will contain 
		/// a 256-color palette. If the ColorMode is ColorModes.Duotone, the data 
		/// following presumably consists of screen parameters and other related information. 
		/// Unfortunately, it is intentionally not documented by Adobe, and non-Photoshop 
		/// readers are advised to treat duotone images as gray-scale images.
		/// </summary>
		public Byte[] ColorModeData = new Byte[0];

		//Masking data for the PSD
		private Byte[] _globalLayerMaskData = new Byte[0];

		public Int16 Version { get; private set; }

		private Int16 _channels;
		/// <summary>
		/// The number of channels in the image, including any alpha channels.
		/// Supported range is 1 to 24.
		/// </summary>
		public Int16 Channels
		{
			get { return _channels; }
            private set
			{
				if (value < 1 || value > 24) throw new ArgumentException("Supported range is 1 to 24");
				_channels = value;
			}
		}


		private Int32 _rows;
		/// <summary>
		/// The height of the image in pixels.
		/// </summary>
		public Int32 Rows
		{
			get { return _rows; }
            private set
			{
				if (value < 0 || value > 30000) throw new ArgumentException("Supported range is 1 to 30000.");
				_rows = value;
			}
		}


		private Int32 _columns;
		/// <summary>
		/// The width of the image in pixels. 
		/// </summary>
		public Int32 Columns
		{
			get { return _columns; }
            private set
			{
				if (value < 0 || value > 30000)
					throw new ArgumentException("Supported range is 1 to 30000.");
				_columns = value;
			}
		}


		private Int32 _depth;
		/// <summary>
		/// The number of bits per channel. Supported values are 1, 8, and 16.
		/// </summary>
		public Int32 Depth
		{
			get { return _depth; }
            private set
			{
				if (value == 1 || value == 8 || value == 16)
				{
					_depth = value;
				}
				else
				{
					throw new ArgumentException("Supported values are 1, 8, and 16.");
				}
			}
		}

		/// <summary>
		/// The color mode of the file.
		/// </summary>
		public ColorModes ColorMode { get; private set; }

        private List<Layer> _layers;
		public IEnumerable<Layer> Layers { get { return _layers; } }
		public Boolean AbsoluteAlpha { get; private set; }
        public Byte[][] ImageData { get; private set; }
        public ImageCompression ImageCompression { get; private set; }

        private List<ImageResource> _imageResources;
		/// <summary>
		/// The Image resource blocks for the file
		/// </summary>
		/// 
        public IEnumerable<ImageResource> ImageResources { get { return _imageResources; } }

		public ResolutionInfo Resolution
		{
			get
			{
                return (ResolutionInfo)_imageResources.Find(x => x.ID == (Int32)ResourceIDs.ResolutionInfo);
			}

            private set
			{
                ImageResource oldValue = _imageResources.Find(x => x.ID == (Int32)ResourceIDs.ResolutionInfo);
                if (oldValue != null) _imageResources.Remove(oldValue);

                _imageResources.Add(value);
			}
		}
		#endregion //End Properties

		public PsdFile()
		{
            _layers = new List<Layer>();
			Version = 1;
            _imageResources = new List<ImageResource>();
		}

        public PsdFile Load(String filename)
		{
			using (FileStream stream = new FileStream(filename, FileMode.Open))
			{
				//binary reverse reader reads data types in big-endian format.
				BinaryReverseReader reader = new BinaryReverseReader(stream);

				#region "Headers"
				//The headers area is used to check for a valid PSD file
				Debug.WriteLine("LoadHeader started at " + reader.BaseStream.Position.ToString(CultureInfo.InvariantCulture));

				String signature = new String(reader.ReadChars(4));
				if (signature != "8BPS") throw new IOException("Bad or invalid file stream supplied");

				//get the version number, should be 1 always
				if ((Version = reader.ReadInt16()) != 1) throw new IOException("Invalid version number supplied");

				//get rid of the 6 bytes reserverd in PSD format
				reader.BaseStream.Position += 6;

				//get the rest of the information from the PSD file.
				//Everytime ReadInt16() is called, it reads 2 bytes.
				//Everytime ReadInt32() is called, it reads 4 bytes.
				_channels = reader.ReadInt16();
				_rows = reader.ReadInt32();
				_columns = reader.ReadInt32();
				_depth = reader.ReadInt16();
				ColorMode = (ColorModes)reader.ReadInt16();

				//by end of headers, the reader has read 26 bytes into the file.
				#endregion //End Headers

				#region "ColorModeData"
				Debug.WriteLine("LoadColorModeData started at " + reader.BaseStream.Position.ToString(CultureInfo.InvariantCulture));

				UInt32 paletteLength = reader.ReadUInt32(); //readUint32() advances the reader 4 bytes.
				if (paletteLength > 0)
				{
					ColorModeData = reader.ReadBytes((Int32)paletteLength);
				}
				#endregion //End ColorModeData


				#region "Loading Image Resources"
				//This part takes extensive use of classes that I didn't write therefore
				//I can't document much on what they do.

				Debug.WriteLine("LoadingImageResources started at " + reader.BaseStream.Position.ToString(CultureInfo.InvariantCulture));

                _imageResources.Clear();

				UInt32 imgResLength = reader.ReadUInt32();
				if (imgResLength <= 0) return null;

				Int64 startPosition = reader.BaseStream.Position;

				while ((reader.BaseStream.Position - startPosition) < imgResLength)
				{
					ImageResource imgRes = new ImageResource(reader);

					ResourceIDs resID = (ResourceIDs)imgRes.ID;
					switch (resID)
					{
						case ResourceIDs.ResolutionInfo:
							imgRes = new ResolutionInfo(imgRes);
							break;
						case ResourceIDs.Thumbnail1:
						case ResourceIDs.Thumbnail2:
							imgRes = new Thumbnail(imgRes);
							break;
						case ResourceIDs.AlphaChannelNames:
							imgRes = new AlphaChannels(imgRes);
							break;
					}

                    _imageResources.Add(imgRes);

				}
				// make sure we are not on a wrong offset, so set the stream position 
				// manually
				reader.BaseStream.Position = startPosition + imgResLength;

				#endregion //End LoadingImageResources


				#region "Layer and Mask Info"
				//We are gonna load up all the layers and masking of the PSD now.
				Debug.WriteLine("LoadLayerAndMaskInfo - Part1 started at " + reader.BaseStream.Position.ToString(CultureInfo.InvariantCulture));
				UInt32 layersAndMaskLength = reader.ReadUInt32();

				if (layersAndMaskLength <= 0) return null;

				//new start position
				startPosition = reader.BaseStream.Position;

				//Lets start by loading up all the layers
				LoadLayers(reader);
				//we are done the layers, load up the masks
				LoadGlobalLayerMask(reader);

				// make sure we are not on a wrong offset, so set the stream position 
				// manually
				reader.BaseStream.Position = startPosition + layersAndMaskLength;
				#endregion //End Layer and Mask info

				#region "Loading Final Image"

				//we have loaded up all the information from the PSD file
				//into variables we can use later on.

				//lets finish loading the raw data that defines the image 
				//in the picture.

				Debug.WriteLine("LoadImage started at " + reader.BaseStream.Position.ToString(CultureInfo.InvariantCulture));

				ImageCompression = (ImageCompression)reader.ReadInt16();

				ImageData = new Byte[_channels][];

				//---------------------------------------------------------------

				if (ImageCompression == ImageCompression.Rle)
				{
					// The RLE-compressed data is proceeded by a 2-byte data count for each row in the data,
					// which we're going to just skip.
					reader.BaseStream.Position += _rows * _channels * 2;
				}

				//---------------------------------------------------------------

				Int32 bytesPerRow = 0;

				switch (_depth)
				{
					case 1:
						bytesPerRow = _columns;//NOT Shure
						break;
					case 8:
						bytesPerRow = _columns;
						break;
					case 16:
						bytesPerRow = _columns * 2;
						break;
				}

				//---------------------------------------------------------------

				for (Int32 ch = 0; ch < _channels; ch++)
				{
					ImageData[ch] = new Byte[_rows * bytesPerRow];

					switch (ImageCompression)
					{
						case ImageCompression.Raw:
							reader.Read(ImageData[ch], 0, ImageData[ch].Length);
							break;
						case ImageCompression.Rle:
							{
								for (Int32 i = 0; i < _rows; i++)
								{
									Int32 rowIndex = i * _columns;
									RleHelper.DecodedRow(reader.BaseStream, ImageData[ch], rowIndex, bytesPerRow);
								}
							}
							break;
					}
				}

				#endregion //End LoadingFinalImage
			}

            return this;
		} //end Load()

		/// <summary>
		/// Loads up the Layers of the supplied PSD file
		/// </summary>      
		private void LoadLayers(BinaryReverseReader reader)
		{
			Debug.WriteLine("LoadLayers started at " + reader.BaseStream.Position.ToString(CultureInfo.InvariantCulture));

			UInt32 layersInfoSectionLength = reader.ReadUInt32();

			if (layersInfoSectionLength <= 0)
				return;

			Int64 startPosition = reader.BaseStream.Position;

			Int16 numberOfLayers = reader.ReadInt16();

			// If <0, then number of layers is absolute value,
			// and the first alpha channel contains the transparency data for
			// the merged result.
			if (numberOfLayers < 0)
			{
				AbsoluteAlpha = true;
				numberOfLayers = Math.Abs(numberOfLayers);
			}

            _layers.Clear();

			if (numberOfLayers == 0) return;

			for (Int32 i = 0; i < numberOfLayers; i++)
			{
                _layers.Add(new Layer(reader, this));
			}

			foreach (Layer layer in Layers)
			{
				foreach (Layer.Channel channel in layer.Channels.Where(c => c.ID != -2))
				{
					channel.LoadPixelData(reader);
				}
				layer.MaskData.LoadPixelData(reader);
			}


			if (reader.BaseStream.Position % 2 == 1) reader.ReadByte();

			// make sure we are not on a wrong offset, so set the stream position 
			// manually
			reader.BaseStream.Position = startPosition + layersInfoSectionLength;
		}

		/// <summary>
		/// Load up the masking information of the supplied PSD
		/// </summary>        
		private void LoadGlobalLayerMask(BinaryReverseReader reader)
		{
			Debug.WriteLine("LoadGlobalLayerMask started at " + reader.BaseStream.Position.ToString(CultureInfo.InvariantCulture));

			UInt32 maskLength = reader.ReadUInt32();

			if (maskLength <= 0) return;

			_globalLayerMaskData = reader.ReadBytes((Int32)maskLength);
		}

	}

	public enum ImageCompression
	{
		/// <summary>
		/// Raw data
		/// </summary>
		Raw = 0,
		/// <summary>
		/// RLE compressed
		/// </summary>
		Rle = 1,
		/// <summary>
		/// ZIP without prediction.
		/// <remarks>
		/// This is currently not supported since it is ot documented.
		/// Loading will result in an image where all channels are set to zero.
		/// </remarks>
		/// </summary>
		Zip = 2,
		/// <summary>
		/// ZIP with prediction.
		/// <remarks>
		/// This is currently not supported since it is ot documented. 
		/// Loading will result in an image where all channels are set to zero.
		/// </remarks>
		/// </summary>
		ZipPrediction = 3
	}
}
