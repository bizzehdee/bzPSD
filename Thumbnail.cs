using System.IO;

namespace System.Drawing.PSD
{
	/// <summary>
	/// Summary description for Thumbnail.
	/// </summary>
	public class Thumbnail : ImageResource
	{
		public Bitmap Image { get; set; }

		public Thumbnail(ImageResource imageResource)
			: base(imageResource)
		{
			using (BinaryReverseReader reverseReader = DataReader)
			{
				Int32 format = reverseReader.ReadInt32();
				Int32 width = reverseReader.ReadInt32();
				Int32 height = reverseReader.ReadInt32();
				/*Int32 widthBytes = */reverseReader.ReadInt32();
				/*Int32 size = */reverseReader.ReadInt32();
				/*Int32 compressedSize = */reverseReader.ReadInt32();
				/*Int16 bitPerPixel = */reverseReader.ReadInt16();
				/*Int16 planes = */reverseReader.ReadInt16();

				if (format == 1)
				{

					Byte[] imgData = reverseReader.ReadBytes((Int32)(reverseReader.BaseStream.Length - reverseReader.BaseStream.Position));

					using (MemoryStream strm = new MemoryStream(imgData))
					{
						Image = (Bitmap)(Drawing.Image.FromStream(strm).Clone());
					}

					if (ID == 1033)
					{
						//// BGR
						//for(int y=0;y<m_thumbnailImage.Height;y++)
						//  for (int x = 0; x < m_thumbnailImage.Width; x++)
						//  {
						//    Color c=m_thumbnailImage.GetPixel(x,y);
						//    Color c2=Color.FromArgb(c.B, c.G, c.R);
						//    m_thumbnailImage.SetPixel(x, y, c);
						//  }
					}

				}
				else
				{
					Image = new Bitmap(width, height, Imaging.PixelFormat.Format24bppRgb);
				}
			}
		}
	}
}
