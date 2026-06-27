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

namespace bzPSD
{
    // ─── OSType ID helpers ──────────────────────────────────────────────────────

    internal static class OsTypeId
    {
        // Format: uint32 length; if 0 → next 4 bytes are the 4-char key literal;
        // if non-zero → next <length> bytes are the key.
        internal static string Read(BinaryReverseReader r)
        {
            uint len = r.ReadUInt32();
            return new string(r.ReadChars(len == 0 ? 4 : (int)len));
        }

        internal static void Write(BinaryReverseWriter w, string id)
        {
            if (id.Length == 4)
            {
                w.Write((uint)0);
                w.Write(id.ToCharArray());
            }
            else
            {
                w.Write((uint)id.Length);
                w.Write(id.ToCharArray());
            }
        }
    }

    internal static class PsdUnicodeString
    {
        // Format: int32 char count; then count × uint16 UTF-16 BE chars.
        internal static string Read(BinaryReverseReader r)
        {
            int count = r.ReadInt32();
            var chars = new char[count];
            for (int i = 0; i < count; i++)
                chars[i] = (char)r.ReadUInt16();
            return new string(chars);
        }

        internal static void Write(BinaryReverseWriter w, string s)
        {
            w.Write(s.Length);
            foreach (char c in s)
                w.Write((ushort)c);
        }
    }

    // ─── Value base + factory ───────────────────────────────────────────────────

    internal abstract class PsdValue
    {
        public abstract string OsType { get; }
        internal abstract void WriteValue(BinaryReverseWriter w);

        internal static PsdValue ReadValue(string osType, BinaryReverseReader r)
        {
            switch (osType)
            {
                case "obj ": return PsdReference.Read(r);
                case "Objc":
                case "GlbO": return new PsdDescriptorValue(PsdDescriptor.Read(r), osType);
                case "VlLs": return PsdList.Read(r);
                case "doub": return new PsdDoubleValue(r.ReadDouble());
                case "UntF": return PsdUnitDouble.Read(r);
                case "UnFl": return PsdUnitDoubles.Read(r);
                case "TEXT": return PsdStringValue.Read(r);
                case "enum": return PsdEnumValue.Read(r);
                case "long": return new PsdIntValue(r.ReadInt32());
                case "comp": return new PsdLongValue(r.ReadInt64());
                case "bool": return new PsdBoolValue(r.ReadByte() != 0);
                case "type":
                case "GlbC": return PsdClassValue.Read(r, osType);
                case "alis":
                case "Pth ":
                case "tdta": return PsdBytesValue.Read(r, osType);
                default:
                    throw new NotSupportedException($"Unknown PSD OSType: '{osType}'");
            }
        }
    }

    // ─── Concrete value types ───────────────────────────────────────────────────

    internal sealed class PsdDescriptorValue : PsdValue
    {
        private readonly string _osType;
        public PsdDescriptor Value { get; }
        public override string OsType => _osType;
        public PsdDescriptorValue(PsdDescriptor value, string osType) { Value = value; _osType = osType; }
        internal override void WriteValue(BinaryReverseWriter w) => Value.Write(w);
    }

    internal sealed class PsdDoubleValue : PsdValue
    {
        public double Value { get; set; }
        public override string OsType => "doub";
        public PsdDoubleValue(double value) { Value = value; }
        internal override void WriteValue(BinaryReverseWriter w) => w.Write(Value);
    }

    internal sealed class PsdIntValue : PsdValue
    {
        public int Value { get; set; }
        public override string OsType => "long";
        public PsdIntValue(int value) { Value = value; }
        internal override void WriteValue(BinaryReverseWriter w) => w.Write(Value);
    }

    internal sealed class PsdLongValue : PsdValue
    {
        public long Value { get; set; }
        public override string OsType => "comp";
        public PsdLongValue(long value) { Value = value; }
        internal override void WriteValue(BinaryReverseWriter w) => w.Write(Value);
    }

    internal sealed class PsdBoolValue : PsdValue
    {
        public bool Value { get; set; }
        public override string OsType => "bool";
        public PsdBoolValue(bool value) { Value = value; }
        internal override void WriteValue(BinaryReverseWriter w) => w.Write((byte)(Value ? 1 : 0));
    }

    internal sealed class PsdStringValue : PsdValue
    {
        public string Value { get; set; }
        public override string OsType => "TEXT";
        public PsdStringValue(string value) { Value = value; }
        internal static PsdStringValue Read(BinaryReverseReader r) => new PsdStringValue(PsdUnicodeString.Read(r));
        internal override void WriteValue(BinaryReverseWriter w) => PsdUnicodeString.Write(w, Value);
    }

    internal sealed class PsdUnitDouble : PsdValue
    {
        // Unit is a 4-char literal like "#Pxl", "#Pnt", "#Ang", "#Prc", "#Nne", etc.
        public string Unit { get; }
        public double Value { get; set; }
        public override string OsType => "UntF";
        public PsdUnitDouble(string unit, double value) { Unit = unit; Value = value; }
        internal static PsdUnitDouble Read(BinaryReverseReader r)
            => new PsdUnitDouble(new string(r.ReadChars(4)), r.ReadDouble());
        internal override void WriteValue(BinaryReverseWriter w)
        {
            w.Write(Unit.ToCharArray());
            w.Write(Value);
        }
    }

    internal sealed class PsdUnitDoubles : PsdValue
    {
        public string Unit { get; }
        public double[] Values { get; }
        public override string OsType => "UnFl";
        public PsdUnitDoubles(string unit, double[] values) { Unit = unit; Values = values; }
        internal static PsdUnitDoubles Read(BinaryReverseReader r)
        {
            string unit = new string(r.ReadChars(4));
            int count = r.ReadInt32();
            var values = new double[count];
            for (int i = 0; i < count; i++)
                values[i] = r.ReadDouble();
            return new PsdUnitDoubles(unit, values);
        }
        internal override void WriteValue(BinaryReverseWriter w)
        {
            w.Write(Unit.ToCharArray());
            w.Write(Values.Length);
            foreach (double v in Values)
                w.Write(v);
        }
    }

    internal sealed class PsdEnumValue : PsdValue
    {
        public string TypeId { get; }
        public string Value { get; }
        public override string OsType => "enum";
        public PsdEnumValue(string typeId, string value) { TypeId = typeId; Value = value; }
        internal static PsdEnumValue Read(BinaryReverseReader r)
            => new PsdEnumValue(OsTypeId.Read(r), OsTypeId.Read(r));
        internal override void WriteValue(BinaryReverseWriter w)
        {
            OsTypeId.Write(w, TypeId);
            OsTypeId.Write(w, Value);
        }
    }

    internal sealed class PsdClassValue : PsdValue
    {
        private readonly string _osType;
        public string Name { get; }
        public string ClassId { get; }
        public override string OsType => _osType;
        public PsdClassValue(string name, string classId, string osType) { Name = name; ClassId = classId; _osType = osType; }
        internal static PsdClassValue Read(BinaryReverseReader r, string osType)
            => new PsdClassValue(PsdUnicodeString.Read(r), OsTypeId.Read(r), osType);
        internal override void WriteValue(BinaryReverseWriter w)
        {
            PsdUnicodeString.Write(w, Name);
            OsTypeId.Write(w, ClassId);
        }
    }

    internal sealed class PsdBytesValue : PsdValue
    {
        private readonly string _osType;
        public byte[] Data { get; }
        public override string OsType => _osType;
        public PsdBytesValue(byte[] data, string osType) { Data = data; _osType = osType; }
        internal static PsdBytesValue Read(BinaryReverseReader r, string osType)
        {
            int len = r.ReadInt32();
            return new PsdBytesValue(r.ReadBytes(len), osType);
        }
        internal override void WriteValue(BinaryReverseWriter w)
        {
            w.Write(Data.Length);
            w.Write(Data);
        }
    }

    internal sealed class PsdList : PsdValue
    {
        public List<PsdValue> Items { get; }
        public override string OsType => "VlLs";
        public PsdList(List<PsdValue> items) { Items = items; }
        internal static PsdList Read(BinaryReverseReader r)
        {
            int count = r.ReadInt32();
            var items = new List<PsdValue>(count);
            for (int i = 0; i < count; i++)
            {
                string itemType = new string(r.ReadChars(4));
                items.Add(PsdValue.ReadValue(itemType, r));
            }
            return new PsdList(items);
        }
        internal override void WriteValue(BinaryReverseWriter w)
        {
            w.Write(Items.Count);
            foreach (var item in Items)
            {
                w.Write(item.OsType.ToCharArray());
                item.WriteValue(w);
            }
        }
    }

    // ─── Reference type ─────────────────────────────────────────────────────────

    internal sealed class PsdReference : PsdValue
    {
        public List<PsdRefItem> Items { get; }
        public override string OsType => "obj ";
        public PsdReference(List<PsdRefItem> items) { Items = items; }
        internal static PsdReference Read(BinaryReverseReader r)
        {
            int count = r.ReadInt32();
            var items = new List<PsdRefItem>(count);
            for (int i = 0; i < count; i++)
            {
                string refType = new string(r.ReadChars(4));
                items.Add(PsdRefItem.Read(refType, r));
            }
            return new PsdReference(items);
        }
        internal override void WriteValue(BinaryReverseWriter w)
        {
            w.Write(Items.Count);
            foreach (var item in Items)
            {
                w.Write(item.RefType.ToCharArray());
                item.Write(w);
            }
        }
    }

    internal abstract class PsdRefItem
    {
        public abstract string RefType { get; }
        internal abstract void Write(BinaryReverseWriter w);

        internal static PsdRefItem Read(string refType, BinaryReverseReader r)
        {
            switch (refType)
            {
                case "prop": return PsdPropertyRef.Read(r);
                case "Clss": return PsdClassRef.Read(r);
                case "Enmr": return PsdEnumeratedRef.Read(r);
                case "rele": return PsdOffsetRef.Read(r);
                case "Idnt": return new PsdIdentifierRef(r.ReadInt32());
                case "indx": return PsdIndexRef.Read(r);
                case "name": return PsdNameRef.Read(r);
                default:
                    throw new NotSupportedException($"Unknown PSD reference type: '{refType}'");
            }
        }
    }

    internal sealed class PsdPropertyRef : PsdRefItem
    {
        public string Name { get; }
        public string ClassId { get; }
        public string KeyId { get; }
        public override string RefType => "prop";
        public PsdPropertyRef(string name, string classId, string keyId) { Name = name; ClassId = classId; KeyId = keyId; }
        internal static PsdPropertyRef Read(BinaryReverseReader r)
            => new PsdPropertyRef(PsdUnicodeString.Read(r), OsTypeId.Read(r), OsTypeId.Read(r));
        internal override void Write(BinaryReverseWriter w)
        {
            PsdUnicodeString.Write(w, Name);
            OsTypeId.Write(w, ClassId);
            OsTypeId.Write(w, KeyId);
        }
    }

    internal sealed class PsdClassRef : PsdRefItem
    {
        public string Name { get; }
        public string ClassId { get; }
        public override string RefType => "Clss";
        public PsdClassRef(string name, string classId) { Name = name; ClassId = classId; }
        internal static PsdClassRef Read(BinaryReverseReader r)
            => new PsdClassRef(PsdUnicodeString.Read(r), OsTypeId.Read(r));
        internal override void Write(BinaryReverseWriter w)
        {
            PsdUnicodeString.Write(w, Name);
            OsTypeId.Write(w, ClassId);
        }
    }

    internal sealed class PsdEnumeratedRef : PsdRefItem
    {
        public string Name { get; }
        public string ClassId { get; }
        public string TypeId { get; }
        public string Value { get; }
        public override string RefType => "Enmr";
        public PsdEnumeratedRef(string name, string classId, string typeId, string value)
        { Name = name; ClassId = classId; TypeId = typeId; Value = value; }
        internal static PsdEnumeratedRef Read(BinaryReverseReader r)
            => new PsdEnumeratedRef(PsdUnicodeString.Read(r), OsTypeId.Read(r), OsTypeId.Read(r), OsTypeId.Read(r));
        internal override void Write(BinaryReverseWriter w)
        {
            PsdUnicodeString.Write(w, Name);
            OsTypeId.Write(w, ClassId);
            OsTypeId.Write(w, TypeId);
            OsTypeId.Write(w, Value);
        }
    }

    internal sealed class PsdOffsetRef : PsdRefItem
    {
        public string Name { get; }
        public string ClassId { get; }
        public int Offset { get; }
        public override string RefType => "rele";
        public PsdOffsetRef(string name, string classId, int offset) { Name = name; ClassId = classId; Offset = offset; }
        internal static PsdOffsetRef Read(BinaryReverseReader r)
            => new PsdOffsetRef(PsdUnicodeString.Read(r), OsTypeId.Read(r), r.ReadInt32());
        internal override void Write(BinaryReverseWriter w)
        {
            PsdUnicodeString.Write(w, Name);
            OsTypeId.Write(w, ClassId);
            w.Write(Offset);
        }
    }

    internal sealed class PsdIdentifierRef : PsdRefItem
    {
        public int Value { get; }
        public override string RefType => "Idnt";
        public PsdIdentifierRef(int value) { Value = value; }
        internal override void Write(BinaryReverseWriter w) => w.Write(Value);
    }

    internal sealed class PsdIndexRef : PsdRefItem
    {
        public string Name { get; }
        public string ClassId { get; }
        public int Index { get; }
        public override string RefType => "indx";
        public PsdIndexRef(string name, string classId, int index) { Name = name; ClassId = classId; Index = index; }
        internal static PsdIndexRef Read(BinaryReverseReader r)
            => new PsdIndexRef(PsdUnicodeString.Read(r), OsTypeId.Read(r), r.ReadInt32());
        internal override void Write(BinaryReverseWriter w)
        {
            PsdUnicodeString.Write(w, Name);
            OsTypeId.Write(w, ClassId);
            w.Write(Index);
        }
    }

    internal sealed class PsdNameRef : PsdRefItem
    {
        public string Name { get; }
        public string ClassId { get; }
        public string Value { get; }
        public override string RefType => "name";
        public PsdNameRef(string name, string classId, string value) { Name = name; ClassId = classId; Value = value; }
        internal static PsdNameRef Read(BinaryReverseReader r)
            => new PsdNameRef(PsdUnicodeString.Read(r), OsTypeId.Read(r), PsdUnicodeString.Read(r));
        internal override void Write(BinaryReverseWriter w)
        {
            PsdUnicodeString.Write(w, Name);
            OsTypeId.Write(w, ClassId);
            PsdUnicodeString.Write(w, Value);
        }
    }

    // ─── Descriptor ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Parses and round-trips an Adobe OSType Descriptor (the binary structure used
    /// throughout PSD for structured metadata, including text layer data in TySh).
    /// Items are kept in file order. Key lookup is O(n) but descriptor item counts
    /// are small in practice.
    /// </summary>
    internal sealed class PsdDescriptor
    {
        public string Name { get; set; } = string.Empty;
        public string ClassId { get; set; } = string.Empty;

        private readonly List<(string key, PsdValue value)> _items = new List<(string, PsdValue)>();

        internal PsdValue this[string key]
        {
            get
            {
                foreach (var (k, v) in _items)
                    if (k == key) return v;
                return null;
            }
            set
            {
                for (int i = 0; i < _items.Count; i++)
                    if (_items[i].key == key) { _items[i] = (key, value); return; }
                _items.Add((key, value));
            }
        }

        internal static PsdDescriptor Read(BinaryReverseReader r)
        {
            var d = new PsdDescriptor
            {
                Name = PsdUnicodeString.Read(r),
                ClassId = OsTypeId.Read(r)
            };
            int count = r.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                string key = OsTypeId.Read(r);
                string osType = new string(r.ReadChars(4));
                d._items.Add((key, PsdValue.ReadValue(osType, r)));
            }
            return d;
        }

        internal void Write(BinaryReverseWriter w)
        {
            PsdUnicodeString.Write(w, Name);
            OsTypeId.Write(w, ClassId);
            w.Write(_items.Count);
            foreach (var (key, value) in _items)
            {
                OsTypeId.Write(w, key);
                w.Write(value.OsType.ToCharArray());
                value.WriteValue(w);
            }
        }
    }
}
