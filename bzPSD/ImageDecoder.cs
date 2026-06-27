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
using System.Drawing;
using System.Drawing.PSD;
using System.Threading.Tasks;

namespace bzPSD
{
    public class ImageDecoder
    {
        public static PsdBitmap DecodeImage(PsdFile psdFile)
        {
            var bitmap = new PsdBitmap(psdFile.Columns, psdFile.Rows);

            if (psdFile.ColorMode == ColorMode.Multichannel)
            {
                Color[] inkColors = GetMultichannelInkColors(psdFile.DisplayInfo, psdFile.Channels, offset: 0);
                int ch = psdFile.Channels, d = psdFile.Depth;

                Parallel.For(0, psdFile.Rows, y =>
                {
                    int rowIndex = y * psdFile.Columns;
                    for (int x = 0; x < psdFile.Columns; x++)
                    {
                        int pos = rowIndex + x;
                        bitmap.SetPixel(x, y, CompositeInkChannels(psdFile.ImageData, ch, d, inkColors, pos));
                    }
                });
                return bitmap;
            }

            Parallel.For(0, psdFile.Rows, y =>
            {
                int rowIndex = y * psdFile.Columns;
                for (int x = 0; x < psdFile.Columns; x++)
                {
                    int pos = rowIndex + x;
                    bitmap.SetPixel(x, y, GetColor(psdFile, pos));
                }
            });

            return bitmap;
        }

        public static PsdBitmap DecodeImage(Layer layer)
        {
            if (layer.Rect.Width == 0 || layer.Rect.Height == 0) return null;

            var bitmap = new PsdBitmap(layer.Rect.Width, layer.Rect.Height);

            if (layer.PsdFile.ColorMode == ColorMode.Multichannel)
            {
                int channelCount = layer.PsdFile.Channels;
                // For non-Multichannel files DisplayInfo has a composite entry at [0]; for
                // Multichannel it does not, so channel i maps directly to DisplayInfo.Channels[i].
                Color[] inkColors = GetMultichannelInkColors(layer.PsdFile.DisplayInfo, channelCount, offset: 0);
                int d = layer.PsdFile.Depth;

                Parallel.For(0, layer.Rect.Height, y =>
                {
                    int rowIndex = y * layer.Rect.Width;
                    // Build the per-channel view once per row, not per pixel.
                    byte[][] chData = new byte[channelCount][];
                    for (int i = 0; i < channelCount; i++)
                        chData[i] = layer.SortedChannels.ContainsKey((short)i) ? layer.SortedChannels[(short)i].ImageData : null;

                    for (int x = 0; x < layer.Rect.Width; x++)
                    {
                        int pos = rowIndex + x;
                        bitmap.SetPixel(x, y, CompositeInkChannels(chData, channelCount, d, inkColors, pos));
                    }
                });
                return bitmap;
            }

            Parallel.For(0, layer.Rect.Height, y =>
            {
                int rowIndex = y * layer.Rect.Width;

                for (int x = 0; x < layer.Rect.Width; x++)
                {
                    int pos = rowIndex + x;

                    Color pixelColor = GetColor(layer, pos);

                    if (layer.SortedChannels.ContainsKey(-2))
                    {
                        int maskAlpha = GetColor(layer.MaskData, x, y);
                        int newAlpha = pixelColor.A * maskAlpha / 255;
                        pixelColor = Color.FromArgb(newAlpha, pixelColor);
                    }

                    bitmap.SetPixel(x, y, pixelColor);
                }
            });

            return bitmap;
        }

        /// <summary>
        /// Composites all visible layers bottom-to-top using each layer's blend mode and opacity.
        /// </summary>
        public static PsdBitmap CompositeLayers(PsdFile psdFile)
        {
            // PsdBitmap initialises all pixels to transparent black (all bytes zero).
            var result = new PsdBitmap(psdFile.Columns, psdFile.Rows);

            foreach (var layer in psdFile.Layers)
            {
                if (!layer.Visible) continue;
                using (var layerBitmap = DecodeImage(layer))
                {
                    if (layerBitmap == null) continue;
                    CompositeLayer(result, layerBitmap, layer);
                }
            }

            return result;
        }

        private static void CompositeLayer(PsdBitmap dst, PsdBitmap src, Layer layer)
        {
            int ox = layer.Rect.X, oy = layer.Rect.Y;

            for (int y = 0; y < layer.Rect.Height; y++)
            {
                int dy = oy + y;
                if (dy < 0 || dy >= dst.Height) continue;

                for (int x = 0; x < layer.Rect.Width; x++)
                {
                    int dx = ox + x;
                    if (dx < 0 || dx >= dst.Width) continue;

                    Color s = src.GetPixel(x, y);
                    Color d = dst.GetPixel(dx, dy);
                    dst.SetPixel(dx, dy, AlphaComposite(Blend(s, d, layer.BlendModeKey), s.A, layer.Opacity, d));
                }
            }
        }

        private static Color Blend(Color s, Color d, string mode)
        {
            switch (mode.TrimEnd())
            {
                case "mul":  return Rgb(s, s.R * d.R / 255, s.G * d.G / 255, s.B * d.B / 255);
                case "scrn": return Rgb(s, 255 - (255 - s.R) * (255 - d.R) / 255, 255 - (255 - s.G) * (255 - d.G) / 255, 255 - (255 - s.B) * (255 - d.B) / 255);
                case "over": return Rgb(s, Overlay(s.R, d.R), Overlay(s.G, d.G), Overlay(s.B, d.B));
                case "hLit": return Rgb(s, Overlay(d.R, s.R), Overlay(d.G, s.G), Overlay(d.B, s.B));
                case "sLit": return Rgb(s, SoftLight(s.R, d.R), SoftLight(s.G, d.G), SoftLight(s.B, d.B));
                case "dark": return Rgb(s, Math.Min(s.R, d.R), Math.Min(s.G, d.G), Math.Min(s.B, d.B));
                case "lite": return Rgb(s, Math.Max(s.R, d.R), Math.Max(s.G, d.G), Math.Max(s.B, d.B));
                case "diff": return Rgb(s, Math.Abs(s.R - d.R), Math.Abs(s.G - d.G), Math.Abs(s.B - d.B));
                case "smud": return Rgb(s, s.R + d.R - 2 * s.R * d.R / 255, s.G + d.G - 2 * s.G * d.G / 255, s.B + d.B - 2 * s.B * d.B / 255);
                case "div":  return Rgb(s, ColorDodge(s.R, d.R), ColorDodge(s.G, d.G), ColorDodge(s.B, d.B));
                case "idiv": return Rgb(s, ColorBurn(s.R, d.R), ColorBurn(s.G, d.G), ColorBurn(s.B, d.B));
                default:     return s; // norm and unrecognised modes
            }
        }

        private static Color Rgb(Color template, int r, int g, int b)
            => Color.FromArgb(template.A, Clamp(r), Clamp(g), Clamp(b));

        private static int Clamp(int v) => v < 0 ? 0 : v > 255 ? 255 : v;

        // Overlay: condition on backdrop (d); Hard Light swaps s/d before calling this.
        private static int Overlay(int s, int d)
        {
            double sv = s / 255.0, dv = d / 255.0;
            return Clamp((int)((dv <= 0.5 ? 2.0 * sv * dv : 1.0 - 2.0 * (1.0 - sv) * (1.0 - dv)) * 255));
        }

        private static int SoftLight(int s, int d)
        {
            double sv = s / 255.0, dv = d / 255.0;
            double r = sv < 0.5
                ? dv - (1.0 - 2.0 * sv) * dv * (1.0 - dv)
                : dv + (2.0 * sv - 1.0) * (dv <= 0.25 ? ((16.0 * dv - 12.0) * dv + 4.0) * dv : Math.Sqrt(dv) - dv);
            return Clamp((int)(r * 255));
        }

        private static int ColorDodge(int s, int d)
            => d == 255 ? 255 : Clamp(s * 255 / (255 - d));

        private static int ColorBurn(int s, int d)
            => d == 0 ? 0 : Clamp(255 - (255 - s) * 255 / d);

        private static Color AlphaComposite(Color blended, int srcA, byte opacity, Color dst)
        {
            int effA = srcA * opacity / 255;
            int dstA = dst.A;
            int outA = effA + dstA * (255 - effA) / 255;
            if (outA == 0) return Color.Transparent;
            int dstWeight = dstA * (255 - effA) / 255;
            return Color.FromArgb(
                Clamp(outA),
                Clamp((blended.R * effA + dst.R * dstWeight) / outA),
                Clamp((blended.G * effA + dst.G * dstWeight) / outA),
                Clamp((blended.B * effA + dst.B * dstWeight) / outA));
        }

        public static PsdBitmap DecodeImage(Layer.Mask mask)
        {
            if (mask.Rect.Width == 0 || mask.Rect.Height == 0) return null;

            var bitmap = new PsdBitmap(mask.Rect.Width, mask.Rect.Height);

            Parallel.For(0, mask.Rect.Height, y =>
            {
                int rowIndex = y * mask.Rect.Width;

                for (int x = 0; x < mask.Rect.Width; x++)
                {
                    int pos = rowIndex + x;
                    byte v = pos < mask.ImageData.Length ? mask.ImageData[pos] : (byte)0;
                    bitmap.SetPixel(x, y, Color.FromArgb(v, v, v));
                }
            });

            return bitmap;
        }

        // For 16-bit channels ImageData holds two bytes per pixel (big-endian); take the high byte for display.
        private static byte Ch(byte[] data, int pixelPos, int depth)
            => depth == 16 ? data[pixelPos * 2] : data[pixelPos];

        private static Color GetColor(PsdFile psdFile, int pos)
        {
            int d = psdFile.Depth;
            switch (psdFile.ColorMode)
            {
                case ColorMode.RGB:
                {
                    byte a = psdFile.ImageData.Length > 3 ? Ch(psdFile.ImageData[3], pos, d) : (byte)255;
                    return Color.FromArgb(a, Ch(psdFile.ImageData[0], pos, d), Ch(psdFile.ImageData[1], pos, d), Ch(psdFile.ImageData[2], pos, d));
                }
                case ColorMode.CMYK:
                    return CMYKToRGB(Ch(psdFile.ImageData[0], pos, d), Ch(psdFile.ImageData[1], pos, d), Ch(psdFile.ImageData[2], pos, d), Ch(psdFile.ImageData[3], pos, d));
                case ColorMode.Multichannel:
                    // Reached only if called directly; DecodeImage(PsdFile) routes multichannel separately.
                    return CompositeInkChannels(psdFile.ImageData, psdFile.Channels, d,
                        GetMultichannelInkColors(psdFile.DisplayInfo, psdFile.Channels, 0), pos);
                case ColorMode.Grayscale:
                case ColorMode.Duotone:
                {
                    byte v = Ch(psdFile.ImageData[0], pos, d);
                    return Color.FromArgb(v, v, v);
                }
                case ColorMode.Indexed:
                {
                    int index = Ch(psdFile.ImageData[0], pos, d);
                    return Color.FromArgb(psdFile.ColorModeData[index], psdFile.ColorModeData[index + 256], psdFile.ColorModeData[index + 2 * 256]);
                }
                case ColorMode.Lab:
                    return LabToRGB(Ch(psdFile.ImageData[0], pos, d), Ch(psdFile.ImageData[1], pos, d), Ch(psdFile.ImageData[2], pos, d));
                default:
                    return Color.White;
            }
        }

        private static Color GetColor(Layer layer, int pos)
        {
            int d = layer.PsdFile.Depth;
            Color c = Color.White;

            switch (layer.PsdFile.ColorMode)
            {
                case ColorMode.RGB:
                    c = Color.FromArgb(Ch(layer.SortedChannels[0].ImageData, pos, d), Ch(layer.SortedChannels[1].ImageData, pos, d), Ch(layer.SortedChannels[2].ImageData, pos, d));
                    break;
                case ColorMode.CMYK:
                    c = CMYKToRGB(Ch(layer.SortedChannels[0].ImageData, pos, d), Ch(layer.SortedChannels[1].ImageData, pos, d), Ch(layer.SortedChannels[2].ImageData, pos, d), Ch(layer.SortedChannels[3].ImageData, pos, d));
                    break;
                case ColorMode.Multichannel:
                    // Reached only if called directly; DecodeImage(Layer) routes multichannel separately.
                    {
                        int chCount = layer.PsdFile.Channels;
                        byte[][] chData = new byte[chCount][];
                        for (int i = 0; i < chCount; i++)
                            chData[i] = layer.SortedChannels.ContainsKey((short)i) ? layer.SortedChannels[(short)i].ImageData : null;
                        c = CompositeInkChannels(chData, chCount, d,
                            GetMultichannelInkColors(layer.PsdFile.DisplayInfo, chCount, 0), pos);
                        break;
                    }
                case ColorMode.Grayscale:
                case ColorMode.Duotone:
                {
                    byte v = Ch(layer.SortedChannels[0].ImageData, pos, d);
                    c = Color.FromArgb(v, v, v);
                    break;
                }
                case ColorMode.Indexed:
                {
                    int index = Ch(layer.SortedChannels[0].ImageData, pos, d);
                    c = Color.FromArgb(layer.PsdFile.ColorModeData[index], layer.PsdFile.ColorModeData[index + 256], layer.PsdFile.ColorModeData[index + 2 * 256]);
                    break;
                }
                case ColorMode.Lab:
                    c = LabToRGB(Ch(layer.SortedChannels[0].ImageData, pos, d), Ch(layer.SortedChannels[1].ImageData, pos, d), Ch(layer.SortedChannels[2].ImageData, pos, d));
                    break;
            }

            if (layer.SortedChannels.ContainsKey(-1))
                c = Color.FromArgb(Ch(layer.SortedChannels[-1].ImageData, pos, d), c);

            return c;
        }

        private static int GetColor(Layer.Mask mask, int x, int y)
        {
            int c = 255;

            if (mask.PositionIsRelative)
            {
                x -= mask.Rect.X;
                y -= mask.Rect.Y;
            }
            else
            {
                x = x + mask.Layer.Rect.X - mask.Rect.X;
                y = y + mask.Layer.Rect.Y - mask.Rect.Y;
            }

            if (y >= 0 && y < mask.Rect.Height &&
                x >= 0 && x < mask.Rect.Width)
            {
                int pos = y * mask.Rect.Width + x;
                c = pos < mask.ImageData.Length ? mask.ImageData[pos] : 255;
            }

            return c;
        }

        /// <summary>
        /// Returns one display RGB colour per ink channel.
        /// For Multichannel files DisplayInfo has no composite entry, so <paramref name="offset"/> is 0.
        /// For RGB/CMYK files the composite is at index 0 and colour channels start at offset 1.
        /// Falls back to process-colour defaults (C/M/Y/K/black) when DisplayInfo is absent.
        /// </summary>
        private static Color[] GetMultichannelInkColors(DisplayInfo di, int channelCount, int offset)
        {
            var colors = new Color[channelCount];
            for (int i = 0; i < channelCount; i++)
            {
                int diIndex = i + offset;
                if (di != null && diIndex < di.Channels.Count)
                    colors[i] = InkColorToRGB(di.Channels[diIndex]);
                else
                    colors[i] = i switch { 0 => Color.Cyan, 1 => Color.Magenta, 2 => Color.Yellow, 3 => Color.Black, _ => Color.Black };
            }
            return colors;
        }

        /// <summary>
        /// Converts a <see cref="ChannelDisplayInfo"/> colour to a display RGB value.
        /// The result represents what the ink looks like at 100 % coverage on white paper.
        /// </summary>
        private static Color InkColorToRGB(ChannelDisplayInfo ch)
        {
            switch (ch.ColorSpace)
            {
                case ChannelColorSpace.RGB:
                    return Color.FromArgb(
                        ch.ColorComponents[0] * 255 / 65535,
                        ch.ColorComponents[1] * 255 / 65535,
                        ch.ColorComponents[2] * 255 / 65535);
                case ChannelColorSpace.CMYK:
                    // DisplayInfo CMYK components are 0-10000 (= 0-100 %).
                    // CMYKToRGB expects the PSD inverted convention (0 = full ink, 255 = no ink).
                    return CMYKToRGB(
                        (byte)(255 - ch.ColorComponents[0] * 255 / 10000),
                        (byte)(255 - ch.ColorComponents[1] * 255 / 10000),
                        (byte)(255 - ch.ColorComponents[2] * 255 / 10000),
                        (byte)(255 - ch.ColorComponents[3] * 255 / 10000));
                case ChannelColorSpace.Grayscale:
                    // 0 = white (no ink), 10000 = black (full ink).
                    byte v = (byte)(255 - ch.ColorComponents[0] * 255 / 10000);
                    return Color.FromArgb(v, v, v);
                default:
                    return Color.Black;
            }
        }

        /// <summary>
        /// Composites <paramref name="channelCount"/> ink channels onto white paper using
        /// multiplicative (subtractive) blending — equivalent to Photoshop's channel-preview mode.
        /// </summary>
        private static Color CompositeInkChannels(byte[][] channelData, int channelCount, int depth, Color[] inkColors, int pos)
        {
            double r = 1.0, g = 1.0, b = 1.0;
            for (int i = 0; i < channelCount; i++)
            {
                if (channelData[i] == null) continue;
                double coverage = Ch(channelData[i], pos, depth) / 255.0;
                r *= 1.0 - coverage * (1.0 - inkColors[i].R / 255.0);
                g *= 1.0 - coverage * (1.0 - inkColors[i].G / 255.0);
                b *= 1.0 - coverage * (1.0 - inkColors[i].B / 255.0);
            }
            return Color.FromArgb(255, Clamp((int)(r * 255)), Clamp((int)(g * 255)), Clamp((int)(b * 255)));
        }

        private static Color LabToRGB(byte lb, byte ab, byte bb)
        {
            double exL = lb;
            double exA = ab;
            double exB = bb;

            const double lCoef = 256.0 / 100.0;
            const double aCoef = 256.0 / 256.0;
            const double bCoef = 256.0 / 256.0;

            int l = (int)(exL / lCoef);
            int a = (int)(exA / aCoef - 128.0);
            int b = (int)(exB / bCoef - 128.0);

            // For the conversion we first convert values to XYZ and then to RGB
            // Standards used Observer = 2, Illuminant = D65

            const double refX = 95.047;
            const double refY = 100.000;
            const double refZ = 108.883;

            double varY = (l + 16.0) / 116.0;
            double varX = a / 500.0 + varY;
            double varZ = varY - b / 200.0;

            varY = Math.Pow(varY, 3) > 0.008856 ? Math.Pow(varY, 3) : (varY - 16.0 / 116.0) / 7.787;
            varX = Math.Pow(varX, 3) > 0.008856 ? Math.Pow(varX, 3) : (varX - 16.0 / 116.0) / 7.787;
            varZ = Math.Pow(varZ, 3) > 0.008856 ? Math.Pow(varZ, 3) : (varZ - 16.0 / 116.0) / 7.787;

            double x = refX * varX;
            double y = refY * varY;
            double z = refZ * varZ;

            return XYZToRGB(x, y, z);
        }

        private static Color XYZToRGB(double x, double y, double z)
        {
            // Standards used Observer = 2, Illuminant = D65
            // ref_X = 95.047, ref_Y = 100.000, ref_Z = 108.883
            double varX = x / 100.0;
            double varY = y / 100.0;
            double varZ = z / 100.0;

            double varR = varX * 3.2406 + varY * -1.5372 + varZ * -0.4986;
            double varG = varX * -0.9689 + varY * 1.8758 + varZ * 0.0415;
            double varB = varX * 0.0557 + varY * -0.2040 + varZ * 1.0570;

            varR = varR > 0.0031308 ? 1.055 * Math.Pow(varR, 1 / 2.4) - 0.055 : 12.92 * varR;
            varG = varG > 0.0031308 ? 1.055 * Math.Pow(varG, 1 / 2.4) - 0.055 : 12.92 * varG;
            varB = varB > 0.0031308 ? 1.055 * Math.Pow(varB, 1 / 2.4) - 0.055 : 12.92 * varB;

            int nRed = (int)(varR * 256.0);
            int nGreen = (int)(varG * 256.0);
            int nBlue = (int)(varB * 256.0);

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

        private static Color CMYKToRGB(byte c, byte m, byte y, byte k)
        {
            double dMaxColours = Math.Pow(2, 8);

            double exC = c;
            double exM = m;
            double exY = y;
            double exK = k;

            double C = 1.0 - exC / dMaxColours;
            double M = 1.0 - exM / dMaxColours;
            double Y = 1.0 - exY / dMaxColours;
            double K = 1.0 - exK / dMaxColours;

            int nRed = (int)((1.0 - (C * (1 - K) + K)) * 255);
            int nGreen = (int)((1.0 - (M * (1 - K) + K)) * 255);
            int nBlue = (int)((1.0 - (Y * (1 - K) + K)) * 255);

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