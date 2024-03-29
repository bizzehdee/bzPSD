﻿#region Licence
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
using System.Drawing.PSD;
using System.IO;

namespace bzPSD
{
    /// <summary>
    /// Summary description for ResolutionInfo.
    /// </summary>
    public sealed class ResolutionInfo : ImageResource
    {
        /// <summary>
        /// Fixed-point number: pixels per inch
        /// </summary>
        public short HRes { get; private set; }

        /// <summary>
        /// Fixed-point number: pixels per inch
        /// </summary>
        public short VRes { get; private set; }

        /// <summary>
        /// 1=pixels per inch, 2=pixels per centimeter
        /// </summary>
        public enum ResUnit
        {
            PxPerInch = 1,
            PxPerCent = 2
        }

        public ResUnit HResUnit { get; private set; }

        public ResUnit VResUnit { get; private set; }

        /// <summary>
        /// 1=in, 2=cm, 3=pt, 4=picas, 5=columns
        /// </summary>
        public enum Unit
        {
            In = 1,
            Cm = 2,
            Pt = 3,
            Picas = 4,
            Columns = 5
        }

        public Unit WidthUnit { get; private set; }

        public Unit HeightUnit { get; private set; }

        public ResolutionInfo()
        {
            ID = (short)ResourceIDs.ResolutionInfo;
        }

        public ResolutionInfo(ImageResource imgRes)
            : base(imgRes)
        {
            using (BinaryReverseReader reverseReader = imgRes.DataReader)
            {
                HRes = reverseReader.ReadInt16();
                HResUnit = (ResUnit)reverseReader.ReadInt32();
                WidthUnit = (Unit)reverseReader.ReadInt16();

                VRes = reverseReader.ReadInt16();
                VResUnit = (ResUnit)reverseReader.ReadInt32();
                HeightUnit = (Unit)reverseReader.ReadInt16();
            }
        }

        protected override void StoreData()
        {
            using (var memoryStream = new MemoryStream())
            using (var reverseWriter = new BinaryReverseWriter(memoryStream))
            {
                reverseWriter.Write(HRes);
                reverseWriter.Write((int)HResUnit);
                reverseWriter.Write((short)WidthUnit);

                reverseWriter.Write(VRes);
                reverseWriter.Write((int)VResUnit);
                reverseWriter.Write((short)HeightUnit);

                Data = memoryStream.ToArray();
            }
        }

        public override string ToString()
        {
            return string.Format("{0}{2}x{1}{3}", HRes, VRes, Enum.GetName(typeof(Unit), WidthUnit), Enum.GetName(typeof(Unit), HeightUnit));
        }
    }
}
