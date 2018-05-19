using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text.RegularExpressions;

namespace AlbumArtExtraction
{
	/// <summary>
	/// mp3形式(ID3v2.3) のファイルからアルバムアートを抽出する機能を表します
	/// </summary>
	public class ID3v23AlbumArtExtractor : IAlbumArtExtractor
	{

		#region Constractor 

		public ID3v23AlbumArtExtractor() { }

		#endregion

		#region Parsing picture for ID3Tag.

		/// <summary>
		/// ID3v2.2 タグから Image を取り出します
		/// </summary>
		private Image _ReadPictureInFrameHeaders(Stream file)
		{
			var count = 1;

			while (true)
			{
				// Frame Name
				var frameName = Helper.ReadAsAsciiString(file, 4);

				// 有効な Frame Name であるかどうかを示す
				var validName = Regex.IsMatch(frameName, "^[A-Z0-9]+$");

				// 無効な Frame Name であれば、ループ終了
				if (!validName) break;

				Debug.WriteLine($"frameName = {frameName}");
				var frameSize = Helper.ReadAsUInt(file, 4);

				// フラグ読み飛ばし
				Helper.Skip(file, 2);

				// APIC Frame の判定
				if (frameName == "APIC")
				{
					var removeCount = 0;

					Helper.Skip(file, 1);
					removeCount += 1;

					while (Helper.ReadAsByte(file) != 0x00U) removeCount++;

					Helper.Skip(file, 1);
					removeCount += 1;

					while (Helper.ReadAsByte(file) != 0x00U) removeCount++;

					var imageSource = Helper.ReadAsByteList(file, (int)frameSize - removeCount);

					// byte データを画像として変換する
					using (var memory = new MemoryStream())
					{
						memory.Write(imageSource.ToArray(), 0, imageSource.Count);
						using (var image = Image.FromStream(memory))
							return new Bitmap(image);
					}
				}

				// PIC Frame でないため、フレーム自体を読み飛ばす
				Helper.Skip(file, (int)frameSize);

				if (count > 74)
					throw new InvalidDataException("フレーム数が正常な範囲を超えました");

				count++;
			}

			return null;
		}

		#endregion

		#region Interface - IAlbumArtExtractor 

		/// <summary>
		/// 対象のファイルが形式と一致しているかを判別します
		/// </summary>
		public bool CheckType(string filePath)
		{
			using (var file = new FileStream(filePath, FileMode.Open, FileAccess.Read))
			{
				if (Helper.ReadAsAsciiString(file, 3) != "ID3" || Helper.ReadAsUShort(file) != 0x0300U)
					return false;

				// extended header が無いことを示す
				var hasNotExtendedHeader = (Helper.ReadAsByte(file) & 0x0040U) == 0;

				return hasNotExtendedHeader;
			}
		}

		/// <summary>
		/// アルバムアートを抽出します
		/// </summary>
		public Image Extract(string filePath)
		{
			using (var file = new FileStream(filePath, FileMode.Open, FileAccess.Read))
			{
				// ID3v2 Header 読み飛ばし
				Helper.Skip(file, 10);

				// Frame Headers
				return _ReadPictureInFrameHeaders(file);
			}
		}

		#endregion

	}
}
