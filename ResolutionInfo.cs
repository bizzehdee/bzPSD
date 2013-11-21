using System.IO;

namespace System.Drawing.PSD
{
	/// <summary>
	/// Summary description for ResolutionInfo.
	/// </summary>
	public class ResolutionInfo : ImageResource
	{
		/// <summary>
		/// Fixed-point number: pixels per inch
		/// </summary>
		public Int16 HRes
		{
			get; set;
		}

		/// <summary>
		/// Fixed-point number: pixels per inch
		/// </summary>
		public Int16 VRes
		{
			get; set;
		}

		/// <summary>
		/// 1=pixels per inch, 2=pixels per centimeter
		/// </summary>
		public enum ResUnit
		{
			PxPerInch = 1,
			PxPerCent = 2
		}

		public ResUnit HResUnit
		{
			get; set;
		}

		public ResUnit VResUnit
		{
			get; set;
		}

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

		public Unit WidthUnit
		{
			get; set;
		}

		public Unit HeightUnit
		{
			get; set;
		}

		public ResolutionInfo()
		{
			ID = (Int16)ResourceIDs.ResolutionInfo;
		}

		public ResolutionInfo(ImageResource imgRes)
			: base(imgRes)
		{
			BinaryReverseReader reverseReader = imgRes.DataReader;

			HRes = reverseReader.ReadInt16();
			HResUnit = (ResUnit)reverseReader.ReadInt32();
			WidthUnit = (Unit)reverseReader.ReadInt16();

			VRes = reverseReader.ReadInt16();
			VResUnit = (ResUnit)reverseReader.ReadInt32();
			HeightUnit = (Unit)reverseReader.ReadInt16();

			reverseReader.Close();
		}

		protected override void StoreData()
		{
			MemoryStream memoryStream = new MemoryStream();
			BinaryReverseWriter reverseWriter = new BinaryReverseWriter(memoryStream);

			reverseWriter.Write(HRes);
			reverseWriter.Write((Int32)HResUnit);
			reverseWriter.Write((Int16)WidthUnit);

			reverseWriter.Write(VRes);
			reverseWriter.Write((Int32)VResUnit);
			reverseWriter.Write((Int16)HeightUnit);

			reverseWriter.Close();
			memoryStream.Close();

			Data = memoryStream.ToArray();
		}

	}
}
