/*
 *  (c) 2015 HEADLOCK INC.
 */
using System.Text;
using System;

namespace Framework
{
	/// <summary>
	/// Csv文字列から順にStringBuilderへ要素を取り出す為の構造体
	/// </summary>
	public struct CsvReader
	{
		/// <summary>
		/// Read()にStringBuilderを指定しなかった場合に使用されるStringBuilder
		///  ※これが使用される状況はスレッドセーフではない
		/// </summary>
		public static readonly StringBuilder builder = new StringBuilder();

		/// <summary>
		/// コンストラクタ
		/// </summary>
		/// <param name="_text">csv文字列</param>
		public CsvReader(string csv_text)
		{
			text = csv_text;
			len = text.Length;
			pos = 0;
			line = 1;
		}

		/// <summary>
		/// 位置をリセット
		/// </summary>
		public void Reset()
		{
			pos = 0;
			line = 1;
		}
		
//================= <Ter変更:SetPosition追加>
		public void SetPosition( int _pos , int _line )
		{ 
			pos = _pos;
			line = _line;
		}
//================= <Ter変更:SetPosition>

		/// <summary>
		/// 行の終わりまで解析したかどうか
		/// </summary>
		public bool isLineEnd
		{
			get
			{
				if (pos >= len)
					return true;

				char c = text[pos];
				return c == '\r' || c == '\n';
			}
		}

		/// <summary>
		/// テキストの終端まで解析したかどうか
		/// </summary>
		public bool isAllEnd
		{
			get
			{
				return pos >= len;
			}
		}

		/// <summary>
		/// 現在の行数取得
		/// </summary>
		public int lineNumber
		{
			get
			{
				return line;
			}
		}

		/// <summary>
		/// 現在解析中のcsv文字列を取得
		/// </summary>
		public string csvText
		{
			get
			{
				return text;
			}
		}

		/// <summary>
		/// 現在の解析位置を取得
		/// </summary>
		public int position
		{
			get
			{
				return pos;
			}
		}

		/// <summary>
		/// １データをStringBuilderに格納する。
		/// 　※内部のstaticなStringBuilderが使用されるのでスレッドセーフではない。
		/// </summary>
		public bool Read()
		{
			return Read(builder);
		}

		/// <summary>
		/// １要素を解析しStringBuilderに格納する
		/// </summary>
		/// <param name="_builder">格納先のStringBuilder。nullを指定すると内部のStringBuilderを使用する</param>
		/// <returns>trueなら１要素解析を行った。falseの場合は行の終端またはcsvの終端など。</returns>
		public bool Read(StringBuilder _builder)
		{
			if (_builder == null)
				_builder = builder;

			_builder.Length = 0;

			// すでに終端なら終了
			if (pos >= len)
				return false;

			char c = text[pos];

			// すでに改行コード上なら終了
			if (c == '\r' || c == '\n') // CR or LF ?
				return false;

			// " から始まれば、ダブルクォーテーションで囲まれた文字列
			if (c == '\"')
			{
				while(true)
				{
					++pos;
					if (pos >= len)
						return true;
					c = text[pos];

					if (c == '\"')  // " or "" ?
					{
						++pos;
						if (pos >= len)
							return true;
						c = text[pos];

						if (c == ',')
						{
							++pos;
							return true;
						}

						if (c == '\r' || c == '\n') // CR or LF ?
							return true;
					}
					else if (c == '\r')
					{
						// CR+LFならさらに進める
						if (pos + 1 < len && text[pos + 1] == '\n')
							++pos;

						c = '\n';
						++line;
					}
					else if (c == '\n')
					{
						++line;
					}

					_builder.Append(c);
				}
			}

			// ダブルクォーテーションで囲まれていない文字列
			while (true)
			{
				if (c == ',')
				{
					++pos;
					return true;
				}

				if (c == '\r' || c == '\n') // CR or LF ?
					return true;

				_builder.Append(c);

				++pos;
				if (pos >= len)
					return true;
				c = text[pos];
			}
		}

		/// <summary>
		/// 次の行へ進める
		/// </summary>
		public bool NextLine()
		{
			if (pos >= len)
				return false;

			char c = text[pos];

			if (c != '\r' && c != '\n') // not CR or LF ?
			{
				while (Read() == true) {}
				c = text[pos];
			}

			if (c == '\r') // CR ?
			{
				++pos;
				if (pos >= len)
					return false;
				c = text[pos];

				if (c == '\n') // CR+LF ?
					++pos;

				++line;
				return true;
			}
			else if (c == '\n') // LF ?
			{
				++pos;
				++line;
				return true;
			}

			return false;
		}

		/// <summary>
		/// １要素を解析し、string変数に読み込む
		/// </summary>
		/// <param name="v">string変数</param>
		/// <param name="def">デフォルト値</param>
		/// <param name="_builder">StringBuilder</param>
		/// <returns>読み込んだかどうか</returns>
		public bool Pop(out string v, string def = "", StringBuilder _builder = null)
		{
			if (Read(_builder) == false || GetBuilder(_builder).Length == 0)
			{
				v = def;
				return false;
			}

			v = GetBuilder(_builder).ToString(); // GCAlloc
			return true;
		}

		/// <summary>
		/// １要素を解析し、int変数に読み込む
		/// </summary>
		/// <param name="v">int変数</param>
		/// <param name="def">デフォルト値</param>
		/// <param name="_builder">StringBuilder</param>
		/// <returns>読み込んだかどうか</returns>
		public bool Pop(out int v, int def = 0, StringBuilder _builder = null)
		{
			if (Read(_builder) == false || GetBuilder(_builder).Length == 0)
			{
				v = def;
				return false;
			}

			v = int.Parse(GetBuilder(_builder).ToString());
			return true;
		}

		/// <summary>
		/// １要素を解析し、long変数に読み込む
		/// </summary>
		/// <param name="v">int変数</param>
		/// <param name="def">デフォルト値</param>
		/// <param name="_builder">StringBuilder</param>
		/// <returns>読み込んだかどうか</returns>
		public bool Pop(out long v, long def = 0, StringBuilder _builder = null)
		{
			if (Read(_builder) == false || GetBuilder(_builder).Length == 0)
			{
				v = def;
				return false;
			}

			v = long.Parse(GetBuilder(_builder).ToString());
			return true;
		}

		/// <summary>
		/// １要素を解析し、float変数に読み込む
		/// </summary>
		/// <param name="v">float変数</param>
		/// <param name="def">デフォルト値</param>
		/// <param name="_builder">StringBuilder</param>
		/// <returns>読み込んだかどうか</returns>
		public bool Pop(out float v, float def = 0f, StringBuilder _builder = null)
		{
			if (Read(_builder) == false || GetBuilder(_builder).Length == 0)
			{
				v = def;
				return false;
			}

			v = float.Parse(GetBuilder(_builder).ToString()); // GCAlloc
			return true;
		}

		/*
		 * 2016/08/24
		 * この定義があると上の各種Popがうまく使えないので一旦コメントアウト
		 */
#if false
		/// <summary>
		/// １要素を解析し、enum変数に読み込む
		/// </summary>
		/// <param name="v">enum変数</param>
		/// <param name="def">デフォルト値</param>
		/// <param name="_builder">StringBuilder</param>
		/// <returns>読み込んだかどうか</returns>
		public bool Pop<T>(ref T v, T def = default(T), StringBuilder _builder = null)
		{
			if (Read(_builder) == false || GetBuilder(_builder).Length == 0)
			{
				v = def;
				return false;
			}

			var s = GetBuilder(_builder).ToString();
			var type = typeof(T);

			if (Enum.IsDefined(type, s) == false)
			{
				v = def;
				return false;
			}

			v = (T)Enum.Parse(type, s);
			return true;
		}
#endif

		// internal --------------------------------------------------------------------------------------------------------

		/// <summary>
		/// 解析対象のcsv文字列
		/// </summary>
		string text;

		/// <summary>
		/// textの長さ
		/// </summary>
		int len;

		/// <summary>
		/// 現在の解析位置
		/// </summary>
		int pos;

		/// <summary>
		/// 現在の行番号。1から始まる。
		/// </summary>
		int line;

		/// <summary>
		/// 渡したStringBuilderがnullなら内部のStringBuilderを、
		/// そうでなければそのまま返す
		/// </summary>
		StringBuilder GetBuilder(StringBuilder _builder)
		{
			return _builder != null ? _builder : builder;
		}
	}
}