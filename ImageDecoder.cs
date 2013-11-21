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
using System.Drawing.Imaging;
using System.Threading.Tasks;

namespace System.Drawing.PSD
{
	public class ImageDecoder
	{
		public static Bitmap DecodeImage(PsdFile psdFile)
		{
			Bitmap bitmap = new Bitmap(psdFile.Columns, psdFile.Rows, PixelFormat.Format32bppArgb);

			//Parallel load each row
			Parallel.For(0, psdFile.Rows, y =>
										  {
											  Int32 rowIndex = y * psdFile.Columns;

											  for (Int32 x = 0; x < psdFile.Columns; x++)
											  {
												  Int32 pos = rowIndex + x;

												  Color pixelColor = GetColor(psdFile, pos);

												  lock (bitmap)
												  {
													  bitmap.SetPixel(x, y, pixelColor);
												  }
											  }
										  });

			return bitmap;
		}

        public static Bitmap DecodeImage(Layer layer)
        {
            if (layer.Rect.Width == 0 || layer.Rect.Height == 0) return null;

            Bitmap bitmap = new Bitmap(layer.Rect.Width, layer.Rect.Height, PixelFormat.Format32bppArgb);

            Parallel.For(0, layer.Rect.Height, y =>
            {
                Int32 rowIndex = y * layer.Rect.Width;

                for (Int32 x = 0; x < layer.Rect.Width; x++)
                {
                    Int32 pos = rowIndex + x;

                    Color pixelColor = GetColor(layer, pos);

                    if (layer.SortedChannels.ContainsKey(-2))
                    {
                        Int32 maskAlpha = GetColor(layer.MaskData, x, y);
                        Int32 oldAlpha = pixelColor.A;

                        Int32 newAlpha = (oldAlpha * maskAlpha) / 255;
                        pixelColor = Color.FromArgb(newAlpha, pixelColor);
                    }

                    lock (bitmap)
                    {
                        bitmap.SetPixel(x, y, pixelColor);
                    }
                }
            });

            return bitmap;
        }

        public static Bitmap DecodeImage(Layer.Mask mask)
        {
            Layer layer = mask.Layer;

            if (mask.Rect.Width == 0 || mask.Rect.Height == 0) return null;

            Bitmap bitmap = new Bitmap(mask.Rect.Width, mask.Rect.Height, PixelFormat.Format32bppArgb);

            Parallel.For(0, layer.Rect.Height, y =>
            {
                Int32 rowIndex = y * layer.Rect.Width;

                for (Int32 x = 0; x < layer.Rect.Width; x++)
                {
                    Int32 pos = rowIndex + x;

                    Color pixelColor = Color.FromArgb(mask.ImageData[pos], mask.ImageData[pos], mask.ImageData[pos]);

                    lock (bitmap)
                    {
                        bitmap.SetPixel(x, y, pixelColor);
                    }
                }
            });
            return bitmap;
        }

		private static Color GetColor(PsdFile psdFile, Int32 pos)
		{
			Color c = Color.White;

			switch (psdFile.ColorMode)
			{
				case PsdFile.ColorModes.RGB:
					c = Color.FromArgb(psdFile.ImageData[0][pos], psdFile.ImageData[1][pos], psdFile.ImageData[2][pos]);
					break;
				case PsdFile.ColorModes.CMYK:
					c = CMYKToRGB(psdFile.ImageData[0][pos], psdFile.ImageData[1][pos], psdFile.ImageData[2][pos], psdFile.ImageData[3][pos]);
					break;
				case PsdFile.ColorModes.Multichannel:
					c = CMYKToRGB(psdFile.ImageData[0][pos], psdFile.ImageData[1][pos], psdFile.ImageData[2][pos], 0);
					break;
				case PsdFile.ColorModes.Grayscale:
				case PsdFile.ColorModes.Duotone:
					c = Color.FromArgb(psdFile.ImageData[0][pos], psdFile.ImageData[0][pos], psdFile.ImageData[0][pos]);
					break;
				case PsdFile.ColorModes.Indexed:
					{
						Int32 index = psdFile.ImageData[0][pos];
						c = Color.FromArgb(psdFile.ColorModeData[index], psdFile.ColorModeData[index + 256], psdFile.ColorModeData[index + 2 * 256]);
					}
					break;
				case PsdFile.ColorModes.Lab:
						c = LabToRGB(psdFile.ImageData[0][pos], psdFile.ImageData[1][pos], psdFile.ImageData[2][pos]);
					break;
			}

			return c;
		}

		private static Color GetColor(Layer layer, Int32 pos)
		{
			Color c = Color.White;

			switch (layer.PsdFile.ColorMode)
			{
				case PsdFile.ColorModes.RGB:
					c = Color.FromArgb(layer.SortedChannels[0].ImageData[pos], layer.SortedChannels[1].ImageData[pos], layer.SortedChannels[2].ImageData[pos]);
					break;
				case PsdFile.ColorModes.CMYK:
					c = CMYKToRGB(layer.SortedChannels[0].ImageData[pos], layer.SortedChannels[1].ImageData[pos], layer.SortedChannels[2].ImageData[pos], layer.SortedChannels[3].ImageData[pos]);
					break;
				case PsdFile.ColorModes.Multichannel:
					c = CMYKToRGB(layer.SortedChannels[0].ImageData[pos], layer.SortedChannels[1].ImageData[pos], layer.SortedChannels[2].ImageData[pos], 0);
					break;
				case PsdFile.ColorModes.Grayscale:
				case PsdFile.ColorModes.Duotone:
					c = Color.FromArgb(layer.SortedChannels[0].ImageData[pos], layer.SortedChannels[0].ImageData[pos], layer.SortedChannels[0].ImageData[pos]);
					break;
				case PsdFile.ColorModes.Indexed:
					{
						Int32 index = layer.SortedChannels[0].ImageData[pos];
						c = Color.FromArgb(layer.PsdFile.ColorModeData[index], layer.PsdFile.ColorModeData[index + 256], layer.PsdFile.ColorModeData[index + 2 * 256]);
					}
					break;
				case PsdFile.ColorModes.Lab:
					{
						c = LabToRGB(layer.SortedChannels[0].ImageData[pos], layer.SortedChannels[1].ImageData[pos], layer.SortedChannels[2].ImageData[pos]);
					}
					break;
			}

			if (layer.SortedChannels.ContainsKey(-1)) c = Color.FromArgb(layer.SortedChannels[-1].ImageData[pos], c);

			return c;
		}

		private static Int32 GetColor(Layer.Mask mask, Int32 x, Int32 y)
		{
			Int32 c = 255;

			if (mask.PositionIsRelative)
			{
				x -= mask.Rect.X;
				y -= mask.Rect.Y;
			}
			else
			{
				x = (x + mask.Layer.Rect.X) - mask.Rect.X;
				y = (y + mask.Layer.Rect.Y) - mask.Rect.Y;
			}

			if (y >= 0 && y < mask.Rect.Height &&
				x >= 0 && x < mask.Rect.Width)
			{
				Int32 pos = y * mask.Rect.Width + x;
				c = pos < mask.ImageData.Length ? mask.ImageData[pos] : 255;
			}

			return c;
		}



		private static Color LabToRGB(Byte lb, Byte ab, Byte bb)
		{
			Double exL = lb;
			Double exA = ab;
			Double exB = bb;

			const Double lCoef = 256.0 / 100.0;
			const Double aCoef = 256.0 / 256.0;
			const Double bCoef = 256.0 / 256.0;

			Int32 l = (Int32)(exL / lCoef);
			Int32 a = (Int32)(exA / aCoef - 128.0);
			Int32 b = (Int32)(exB / bCoef - 128.0);

			// For the conversion we first convert values to XYZ and then to RGB
			// Standards used Observer = 2, Illuminant = D65

			const Double refX = 95.047;
			const Double refY = 100.000;
			const Double refZ = 108.883;

			Double varY = (l + 16.0) / 116.0;
			Double varX = a / 500.0 + varY;
			Double varZ = varY - b / 200.0;

			varY = Math.Pow(varY, 3) > 0.008856 ? Math.Pow(varY, 3) : (varY - 16/116)/7.787;
			varX = Math.Pow(varX, 3) > 0.008856 ? Math.Pow(varX, 3) : (varX - 16/116)/7.787;
			varZ = Math.Pow(varZ, 3) > 0.008856 ? Math.Pow(varZ, 3) : (varZ - 16/116)/7.787;

			Double x = refX * varX;
			Double y = refY * varY;
			Double z = refZ * varZ;

			return XYZToRGB(x, y, z);
		}

		private static Color XYZToRGB(Double x, Double y, Double z)
		{
			// Standards used Observer = 2, Illuminant = D65
			// ref_X = 95.047, ref_Y = 100.000, ref_Z = 108.883
			Double varX = x / 100.0;
			Double varY = y / 100.0;
			Double varZ = z / 100.0;

			Double varR = varX * 3.2406 + varY * (-1.5372) + varZ * (-0.4986);
			Double varG = varX * (-0.9689) + varY * 1.8758 + varZ * 0.0415;
			Double varB = varX * 0.0557 + varY * (-0.2040) + varZ * 1.0570;

			varR = varR > 0.0031308 ? 1.055*(Math.Pow(varR, 1/2.4)) - 0.055 : 12.92*varR;
			varG = varG > 0.0031308 ? 1.055*(Math.Pow(varG, 1/2.4)) - 0.055 : 12.92*varG;
			varB = varB > 0.0031308 ? 1.055*(Math.Pow(varB, 1/2.4)) - 0.055 : 12.92*varB;

			Int32 nRed = (Int32)(varR * 256.0);
			Int32 nGreen = (Int32)(varG * 256.0);
			Int32 nBlue = (Int32)(varB * 256.0);

			nRed = nRed > 0 ? nRed : 0;
			nRed = nRed < 255 ? nRed : 255;

			nGreen = nGreen > 0 ? nGreen : 0;
			nGreen = nGreen < 255 ? nGreen : 255;

			nBlue = nBlue > 0 ? nBlue : 0;
			nBlue = nBlue < 255 ? nBlue : 255;

			return Color.FromArgb(nRed, nGreen, nBlue);
		}

		///////////////////////////////////////////////////////////////////////////////
		//
		// The algorithms for these routines were taken from:
		//     http://www.neuro.sfc.keio.ac.jp/~aly/polygon/info/color-space-faq.html
		//
		// RGB --> CMYK                              CMYK --> RGB
		// ---------------------------------------   --------------------------------------------
		// Black   = minimum(1-Red,1-Green,1-Blue)   Red   = 1-minimum(1,Cyan*(1-Black)+Black)
		// Cyan    = (1-Red-Black)/(1-Black)         Green = 1-minimum(1,Magenta*(1-Black)+Black)
		// Magenta = (1-Green-Black)/(1-Black)       Blue  = 1-minimum(1,Yellow*(1-Black)+Black)
		// Yellow  = (1-Blue-Black)/(1-Black)
		//

		private static Color CMYKToRGB(Byte c, Byte m, Byte y, Byte k)
		{
			Double dMaxColours = Math.Pow(2, 8);

			Double exC = c;
			Double exM = m;
			Double exY = y;
			Double exK = k;

			Double C = (1.0 - exC / dMaxColours);
			Double M = (1.0 - exM / dMaxColours);
			Double Y = (1.0 - exY / dMaxColours);
			Double K = (1.0 - exK / dMaxColours);

			Int32 nRed = (Int32)((1.0 - (C * (1 - K) + K)) * 255);
			Int32 nGreen = (Int32)((1.0 - (M * (1 - K) + K)) * 255);
			Int32 nBlue = (Int32)((1.0 - (Y * (1 - K) + K)) * 255);

			nRed = nRed > 0 ? nRed : 0;
			nRed = nRed < 255 ? nRed : 255;

			nGreen = nGreen > 0 ? nGreen : 0;
			nGreen = nGreen < 255 ? nGreen : 255;

			nBlue = nBlue > 0 ? nBlue : 0;
			nBlue = nBlue < 255 ? nBlue : 255;

			return Color.FromArgb(nRed, nGreen, nBlue);
		}
	}
}