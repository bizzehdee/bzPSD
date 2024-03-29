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


namespace bzPSD
{
    public ref struct LengthWriter
    {
        private long _lengthPosition;
        private readonly long _startPosition;
        private readonly BinaryReverseWriter _reverseWriter;

        public LengthWriter(BinaryReverseWriter writer)
        {
            _reverseWriter = writer;

            // we will write the correct length later, so remember 
            // the position
            _lengthPosition = _reverseWriter.BaseStream.Position;
            _reverseWriter.Write(0xFEEDFEED);

            // remember the start  position for calculation Image 
            // resources length
            _startPosition = _reverseWriter.BaseStream.Position;

            _lengthPosition = long.MinValue;
        }

        public void Write()
        {
            if (_lengthPosition == long.MinValue) return;

            long endPosition = _reverseWriter.BaseStream.Position;

            _reverseWriter.BaseStream.Position = _lengthPosition;
            long length = endPosition - _startPosition;
            _reverseWriter.Write((uint)length);
            _reverseWriter.BaseStream.Position = endPosition;

            _lengthPosition = long.MinValue;
        }

        public void Dispose()
        {
            Write();
        }
    }
}