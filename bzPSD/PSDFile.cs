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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing.PSD;
using System.Globalization;
using System.IO;
using System.Linq;

namespace bzPSD
{
    public class PsdFile
    {
        private List<Layer> _layers;
        private byte[] _globalLayerMaskData = Array.Empty<byte>(); // Masking data for the PSD
        private short _channels;
        private int _rows;
        private int _depth;
        private int _columns;

        public PsdFile()
        {
            _layers = new List<Layer>();
            Version = 1;
            _imageResources = new List<ImageResource>();
        }

        /// <summary>
        /// If ColorMode is ColorModes.Indexed, the following 768 bytes will contain 
        /// a 256-color palette. If the ColorMode is ColorModes.Duotone, the data 
        /// following presumably consists of screen parameters and other related information. 
        /// Unfortunately, it is intentionally not documented by Adobe, and non-Photoshop 
        /// readers are advised to treat duotone images as gray-scale images.
        /// </summary>
        public byte[] ColorModeData = Array.Empty<byte>();

        public short Version { get; private set; }


        /// <summary>
        /// The number of channels in the image, including any alpha channels.
        /// Supported range is 1 to 24.
        /// </summary>
        public short Channels
        {
            get { return _channels; }
            set
            {
                if (value < 1 || value > 24) throw new ArgumentException("Supported range is 1 to 24");
                _channels = value;
            }
        }

        /// <summary>
        /// The height of the image in pixels.
        /// </summary>
        public int Rows
        {
            get => _rows;
            set
            {
                if (value < 1 || value > 30000) throw new ArgumentException("Supported range is 1 to 30000.");
                _rows = value;
            }
        }

        /// <summary>
        /// The width of the image in pixels.
        /// </summary>
        public int Columns
        {
            get => _columns;
            set
            {
                if (value < 1 || value > 30000)
                    throw new ArgumentException("Supported range is 1 to 30000.");
                _columns = value;
            }
        }

        /// <summary>
        /// The number of bits per channel. Supported values are 1, 8, and 16.
        /// </summary>
        public int Depth
        {
            get => _depth;
            set
            {
                if (value == 1 || value == 8 || value == 16)
                    _depth = value;
                else
                    throw new ArgumentException("Supported values are 1, 8, and 16.");
            }
        }

        /// <summary>
        /// The color mode of the file.
        /// </summary>
        public ColorMode ColorMode { get; set; }

        public IEnumerable<Layer> Layers => _layers;

        public bool AbsoluteAlpha { get; set; }

        public byte[][] ImageData { get; set; }

        public ImageCompression ImageCompression { get; set; }

        public void AddLayer(Layer layer) => _layers.Add(layer);

        public void RemoveLayer(Layer layer) => _layers.Remove(layer);

        /// <summary>
        /// Creates a blank PsdFile with allocated (zero-filled) image data.
        /// </summary>
        public static PsdFile Create(int width, int height, ColorMode colorMode = ColorMode.RGB, int depth = 8)
        {
            if (width < 1 || width > 30000) throw new ArgumentOutOfRangeException(nameof(width));
            if (height < 1 || height > 30000) throw new ArgumentOutOfRangeException(nameof(height));
            if (depth != 1 && depth != 8 && depth != 16) throw new ArgumentException("depth must be 1, 8, or 16", nameof(depth));

            int channelCount = colorMode switch
            {
                ColorMode.Grayscale => 1,
                ColorMode.Duotone => 1,
                ColorMode.CMYK => 4,
                _ => 3
            };

            var psd = new PsdFile
            {
                Columns = width,
                Rows = height,
                ColorMode = colorMode,
                Depth = depth,
                Channels = (short)channelCount,
                ImageCompression = ImageCompression.Raw,
            };

            int bytesPerChannel = width * height * (depth == 16 ? 2 : 1);
            psd.ImageData = new byte[channelCount][];
            for (int i = 0; i < channelCount; i++)
                psd.ImageData[i] = new byte[bytesPerChannel];

            return psd;
        }

        private List<ImageResource> _imageResources;

        /// <summary>
        /// The Image resource blocks for the file
        /// </summary>
        public IEnumerable<ImageResource> ImageResources => _imageResources;

        public ResolutionInfo Resolution
        {
            get => (ResolutionInfo)_imageResources.Find(x => x.ID == (int)ResourceIDs.ResolutionInfo);

            private set
            {
                ImageResource oldValue = _imageResources.Find(x => x.ID == (int)ResourceIDs.ResolutionInfo);
                if (oldValue != null)
                {
                    _imageResources.Remove(oldValue);
                }

                _imageResources.Add(value);
            }
        }

        /// <summary>
        /// Per-channel display information (resource 1007), or null if not present.
        /// Channels are in Photoshop panel order: composite, colour channels, then extra channels.
        /// For Multichannel files there is no composite entry, so index maps 1:1 to channel data.
        /// </summary>
        public DisplayInfo DisplayInfo =>
            (DisplayInfo)_imageResources.Find(x => x.ID == (int)ResourceIDs.DisplayInfo);

        /// <summary>
        /// ASCII names of extra (alpha / spot) channels (resource 1006), or null if not present.
        /// </summary>
        public AlphaChannels AlphaChannelNames =>
            (AlphaChannels)_imageResources.Find(x => x.ID == (int)ResourceIDs.AlphaChannelNames);

        /// <summary>
        /// Unicode names of extra (alpha / spot) channels (resource 1045), or null if not present.
        /// Prefer this over <see cref="AlphaChannelNames"/> when both are present.
        /// </summary>
        public UnicodeAlphaNames UnicodeAlphaNames =>
            (UnicodeAlphaNames)_imageResources.Find(x => x.ID == (int)ResourceIDs.UnicodeAlphaNames);

        public PsdFile Load(string filename)
        {
            using (FileStream stream = new FileStream(filename, FileMode.Open, FileAccess.Read))
            {
                return Load(stream);
            }
        }

        public PsdFile Load(byte[] data)
        {
            using var stream = new MemoryStream(data);
            return Load(stream);
        }

        public void Save(string filename)
        {
            using var stream = new FileStream(filename, FileMode.Create);
            Save(stream);
        }

        public void Save(Stream stream)
        {
            var writer = new BinaryReverseWriter(stream);

            // Header
            writer.Write("8BPS".ToCharArray());
            writer.Write(Version);
            writer.Write(new byte[6]);
            writer.Write(_channels);
            writer.Write(_rows);
            writer.Write(_columns);
            writer.Write((short)_depth);
            writer.Write((short)ColorMode);

            // Color Mode Data
            writer.Write((uint)ColorModeData.Length);
            writer.Write(ColorModeData);

            // Image Resources
            using (new LengthWriter(writer))
            {
                foreach (var res in _imageResources)
                    res.Save(writer);
            }

            // Layers and Mask
            using (new LengthWriter(writer))
            {
                SaveLayers(writer);
                SaveGlobalLayerMask(writer);
            }

            // Image Data
            writer.Write((short)ImageCompression);

            int bytesPerRow = _depth == 16 ? _columns * 2 : _columns;

            if (ImageCompression == ImageCompression.Rle)
            {
                long countsOffset = stream.Position;
                int countEntries = _channels * _rows;
                var rowCounts = new int[countEntries];

                for (int i = 0; i < countEntries; i++)
                    writer.Write((short)0);

                int idx = 0;
                for (int ch = 0; ch < _channels; ch++)
                    for (int row = 0; row < _rows; row++)
                        rowCounts[idx++] = RleHelper.EncodedRow(stream, ImageData[ch], row * bytesPerRow, bytesPerRow);

                long endOffset = stream.Position;
                stream.Position = countsOffset;
                for (int i = 0; i < countEntries; i++)
                    writer.Write((short)rowCounts[i]);
                stream.Position = endOffset;
            }
            else
            {
                for (int ch = 0; ch < _channels; ch++)
                    writer.Write(ImageData[ch]);
            }
        }

        private void SaveLayers(BinaryReverseWriter writer)
        {
            if (_layers.Count == 0)
            {
                writer.Write((uint)0);
                return;
            }

            using (new LengthWriter(writer))
            {
                writer.Write(AbsoluteAlpha ? (short)-_layers.Count : (short)_layers.Count);

                foreach (var layer in _layers)
                    layer.Save(writer);

                foreach (var layer in _layers)
                {
                    foreach (var ch in layer.Channels.Where(c => c.ID != -2))
                        ch.SavePixelData(writer);
                    layer.MaskData.SavePixelData(writer);
                }

                if (writer.BaseStream.Position % 2 == 1)
                    writer.Write((byte)0);
            }
        }

        private void SaveGlobalLayerMask(BinaryReverseWriter writer)
        {
            writer.Write((uint)_globalLayerMaskData.Length);
            if (_globalLayerMaskData.Length > 0)
                writer.Write(_globalLayerMaskData);
        }

        public PsdFile Load(Stream stream)
        {
            //binary reverse reader reads data types in big-endian format.
            BinaryReverseReader reader = new BinaryReverseReader(stream);

            #region "Headers"
            //The headers area is used to check for a valid PSD file
            Debug.WriteLine("LoadHeader started at " + reader.BaseStream.Position.ToString(CultureInfo.InvariantCulture));

            string signature = new string(reader.ReadChars(4));

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
            ColorMode = (ColorMode)reader.ReadInt16();

            //by end of headers, the reader has read 26 bytes into the file.
            #endregion //End Headers

            #region "ColorModeData"
            Debug.WriteLine("LoadColorModeData started at " + reader.BaseStream.Position.ToString(CultureInfo.InvariantCulture));

            uint paletteLength = reader.ReadUInt32(); //readUint32() advances the reader 4 bytes.
            if (paletteLength > 0)
            {
                ColorModeData = reader.ReadBytes((int)paletteLength);
            }
            #endregion //End ColorModeData

            #region "Loading Image Resources"
            //This part takes extensive use of classes that I didn't write therefore
            //I can't document much on what they do.

            Debug.WriteLine("LoadingImageResources started at " + reader.BaseStream.Position.ToString(CultureInfo.InvariantCulture));

            _imageResources.Clear();

            uint imgResLength = reader.ReadUInt32();
            if (imgResLength <= 0) return this;

            long startPosition = reader.BaseStream.Position;

            while (reader.BaseStream.Position - startPosition < imgResLength)
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
                    case ResourceIDs.DisplayInfo:
                        imgRes = new DisplayInfo(imgRes);
                        break;
                    case ResourceIDs.UnicodeAlphaNames:
                        imgRes = new UnicodeAlphaNames(imgRes);
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
            uint layersAndMaskLength = reader.ReadUInt32();

            if (layersAndMaskLength <= 0) return this;

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

            ImageData = new byte[_channels][];

            //---------------------------------------------------------------

            if (ImageCompression == ImageCompression.Rle)
            {
                // The RLE-compressed data is proceeded by a 2-byte data count for each row in the data,
                // which we're going to just skip.
                reader.BaseStream.Position += _rows * _channels * 2;
            }

            //---------------------------------------------------------------

            int bytesPerRow = 0;

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

            for (int ch = 0; ch < _channels; ch++)
            {
                ImageData[ch] = new byte[_rows * bytesPerRow];

                switch (ImageCompression)
                {
                    case ImageCompression.Raw:
                        reader.Read(ImageData[ch], 0, ImageData[ch].Length);
                        break;
                    case ImageCompression.Rle:
                        {
                            for (int i = 0; i < _rows; i++)
                            {
                                int rowIndex = i * _columns;
                                RleHelper.DecodedRow(reader.BaseStream, ImageData[ch], rowIndex, bytesPerRow);
                            }
                        }
                        break;
                }
            }

            #endregion //End LoadingFinalImage

            return this;
        }

        /// <summary>
        /// Loads up the Layers of the supplied PSD file.
        /// </summary>      
        private void LoadLayers(BinaryReverseReader reader)
        {
            Debug.WriteLine("LoadLayers started at " + reader.BaseStream.Position.ToString(CultureInfo.InvariantCulture));

            uint layersInfoSectionLength = reader.ReadUInt32();

            if (layersInfoSectionLength <= 0)
                return;

            long startPosition = reader.BaseStream.Position;

            short numberOfLayers = reader.ReadInt16();

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

            for (int i = 0; i < numberOfLayers; i++)
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

            uint maskLength = reader.ReadUInt32();

            if (maskLength <= 0) return;

            _globalLayerMaskData = reader.ReadBytes((int)maskLength);
        }
    }
}