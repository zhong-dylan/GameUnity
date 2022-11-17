/*
 *  (c) 2015 HEADLOCK INC.
 */
using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Reflection;

namespace Framework
{
	public class CsvImporter : AssetPostprocessor
	{
		static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
		{
			for (int i = 0; i < importedAssets.Length; i++)
			{
				if (importedAssets[i].ToLower().EndsWith(".csv"))
				{
					if (System.IO.File.Exists(importedAssets[i]) == false) continue;

					string export_path = "Assets/Crossword/AddressableResources/Config";
					string export_class_path = "Assets/Crossword/Scripts/Data";
					CsvToScriptableObject.ExportScriptableCSV(importedAssets[i], export_path, export_class_path, null, true, true, false, true);
				}
			}
		}
	}

	public class CsvToScriptableObject
	{
		/// <summary>
		///  从 CSV 生成 Scriptableobject
		/// </summary>
		/// <param name="csv_path">目标CSV的路径</param>
		/// <param name="export_path">Scriptableobject 目标目录的路径</param>
		/// <param name="export_class_path">自动生成Scriptableobject类时的源输出目标目录</param>
		/// <param name="is_auto_update_cs">如果csv有变化，item有变化，是否要重新输出Scriptableobject的类</param>
		/// <param name="add_singleton_code">添加单例访问码</param>
		/// <param name="add_debug_code">添加调试代码<</param>
		/// <param name="is_force_update_cs" 强制代码更新<</param>
		/// <returns>数据更新了吗</returns>
		public static bool ExportScriptableCSV(string csv_path, string export_path = null, string export_class_path = null, string export_class_name = null, bool is_auto_update_cs = false, bool add_singleton_code = false, bool add_debug_code = false, bool is_force_update_cs = false)
		{
			TextAsset asset = AssetDatabase.LoadAssetAtPath<TextAsset>(csv_path);
			if (asset == null)
				return false;

			// コンテナ名生成
			string container_name = Path.GetFileNameWithoutExtension(csv_path);
			bool is_debug = false;
			if (add_debug_code)
				is_debug = container_name.Contains("Debug");

			// クラス名
			if (export_class_name == null)
				export_class_name = GetContainerClassName(csv_path, is_debug);

			// 出力パスの設定がなければCSVと同じフォルダ以下のobjectディレクトリ
			if (export_path == null)
			{
				export_path = Path.GetDirectoryName(csv_path) + "/object/";
			}
			if (export_path[export_path.Length - 1] != '/')
				export_path += "/";
			export_path += container_name + ".asset";

			// クラスの出力先がなければ出力先を同じディレクトリ
			if (export_class_path == null)
			{
				export_class_path = Path.GetDirectoryName(export_path) + "/";
			}
			if (export_class_path[export_class_path.Length - 1] != '/')
				export_class_path += "/";

			// 対象ディレクトリがなければ作る
			if (Directory.Exists(Path.GetDirectoryName(export_path)) == false)
			{
				Directory.CreateDirectory(Path.GetDirectoryName(export_path));
			}

			CsvReader reader = new CsvReader(asset.text);
			if (reader.isAllEnd)
				return false;

			// 定義部分抽出
			List<string> names = new List<string>();
			List<string> types = new List<string>();
			ReadNameAndType(ref reader, ref names, ref types);

			if (_CreateClassCS(names, types, export_class_name, export_class_path, csv_path, is_auto_update_cs, add_singleton_code, add_debug_code, is_force_update_cs))
			{
				CSVReimportWindow window = EditorWindow.GetWindow(typeof(CSVReimportWindow)) as CSVReimportWindow;
				CSVReimportWindow.ExportPath export = new CSVReimportWindow.ExportPath();
				export.csv_path = csv_path;
				export.export_path = Path.GetDirectoryName(export_path);
				export.class_name = export_class_name;
				window.Add(export);
				window.Focus();
				return false;
			}

			Type container_type = GetType(export_class_name);
			if (container_type == null)
				return false;

			Type item_type = GetType(export_class_name + "+Item");
			if (item_type == null)
				return false;

			// オブジェクト作成
			bool is_new_object = false;
			var obj = AssetDatabase.LoadAssetAtPath(export_path, container_type) as ScriptableObject;
			if (obj == null)
			{
				obj = ScriptableObject.CreateInstance(container_type);
				is_new_object = true;
			}
			if (obj == null)
				return false;
//================= <Ter変更:コンテナ出力エラーが発生した場合、エラーウィンドウを表示するように>
			try
			{
				if (!ExportContainer(container_type, obj, ref reader, names, types))
					return false;
			}
			catch(Exception e)
			{
				EditorUtility.DisplayDialog("Error", $"csvから変換時にエラーが発生しました。\n{container_name}.csvで出力エラーが発生しています。\n\nErrorMessage:{e.Message}", "OK");
				Debug.LogError($"{container_name}.csvで出力エラーが発生しています。ErrorMessage:{e.Message}");
				return false;
			}
//================= <Ter変更>

			if (add_debug_code)
				container_type.InvokeMember("isDebug", BindingFlags.SetField | BindingFlags.NonPublic | BindingFlags.Instance, null, obj, new object[] { is_debug });

			if (is_new_object)
			{
				AssetDatabase.CreateAsset(obj, export_path);
			}
			else
			{
				EditorUtility.SetDirty(obj);
			}
			return true;
		}

		/// <summary>
		///   CSVからクラスを生成
		/// </summary>
		/// <param name="csv_path">対象のCSVへのパス</param>
		/// <param name="export_class_path">Scriptableobjectのクラスを自動生成する際のソースの出力先ディレクトリ</param>
		/// <param name="add_singleton_code">シングルトンでのアクセスコードを追加</param>
		/// <param name="add_debug">デバッグ用コードを追加<</param>
		/// <param name="is_debug">デバッグ用データ</param>
		/// <returns>クラスが生成されたか</returns>
		public static bool CreateClassCS(string csv_path, string export_class_path, bool add_singleton_code, bool add_debug, bool is_debug, bool is_force_update_cs)
		{
			TextAsset asset = AssetDatabase.LoadAssetAtPath<TextAsset>(csv_path);
			if (asset == null)
				return false;

			if (export_class_path == null)
			{
				export_class_path = Path.GetDirectoryName(csv_path) + "/object/";
			}
			if (export_class_path[export_class_path.Length - 1] != '/')
				export_class_path += "/";

			if (Directory.Exists(Path.GetDirectoryName(export_class_path)) == false)
			{
				Directory.CreateDirectory(Path.GetDirectoryName(export_class_path));
			}

			CsvReader reader = new CsvReader(asset.text);
			if (reader.isAllEnd)
				return false;

			// 定義部分抽出
			List<string> names = new List<string>();
			List<string> types = new List<string>();
			ReadNameAndType(ref reader, ref names, ref types);
			return _CreateClassCS(names, types, GetContainerClassName(csv_path, is_debug), export_class_path, csv_path, true, add_singleton_code, add_debug, is_force_update_cs);
		}

		/// <summary>
		/// Typeを取得する
		/// 参考 : https://stackoverflow.com/questions/25404237/how-to-get-enum-type-by-specifying-its-name-in-string
		/// </summary>
		static Type _GetType(string type_name)
		{
			switch (type_name)
			{
				case "bool":	return typeof(bool);
				case "long":	return typeof(long);
				case "ulong":	return typeof(ulong);
				case "int":		return typeof(int);
				case "uint":	return typeof(uint);
				case "float":	return typeof(float);
				case "string":	return typeof(string);
				case "short":	return typeof(short);
				case "ushort":	return typeof(ushort);
			}
			foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
			{
				var type = assembly.GetType(type_name);
				if (type == null)
					continue;

				return type;
			}
			return null;
		}

		/// <summary>
		/// Typeを取得する
		/// </summary>
		static Type GetType(string type_name)
		{
			Type type = _GetType(type_name);
			if (type == null)
			{
				type = _GetType(type_name.Replace(".", "+"));
			}
			return type;
		}

		/// <summary>
		///   ContainerClassのメンバ等に変化があり更新を行う必要があるか？
		/// </summary>
		public static bool IsClassUpdateContainer(string csv_path, string export_class_name)
		{
			TextAsset asset = AssetDatabase.LoadAssetAtPath<TextAsset>(csv_path);
			if (asset == null)
				return false;

			CsvReader reader = new CsvReader(asset.text);
			if (reader.isAllEnd)
				return false;

			// 定義部分抽出
			List<string> names = new List<string>();
			List<string> types = new List<string>();
			ReadNameAndType(ref reader, ref names, ref types);
			Type container_type = GetType(export_class_name);
			if (container_type == null)
				return true;

			return IsClassUpdateContainer(container_type, names, types);
		}

		// internal --------------------------------------------------------------------------------------------------------
		const string key_name = "key";
		const string container_key_name = "container_key";

		/// <summary>
		///  　コンテナ一つを読み込む
		/// </summary>
		static bool ExportContainer(Type container_type, object container_obj, ref CsvReader reader, List<string> names, List<string> types, string now_container_key = null)
		{
			// container_keyモードか
			bool is_container_key = now_container_key != null;

			Type item_type = GetType(container_type.FullName + "+Item");
			if (item_type == null)
				return false;

			Type item_list_type = Type.GetType("System.Collections.Generic.List`1[[" + container_type.FullName + "+Item, Assembly-CSharp]]");
			if (item_list_type == null)
			{
				// Editor側も検索
				item_list_type = Type.GetType("System.Collections.Generic.List`1[[" + container_type.FullName + "+Item, Assembly-CSharp-Editor]]");
			}
			if (item_list_type == null)
				return false;

			// キーが存在するか？
			FieldInfo key_info = item_type.GetField(key_name);
			List<int> key_list = new List<int>();
			bool is_intkey = true;
			if (key_info != null)
			{
				is_intkey = key_info.FieldType == typeof(int) || key_info.FieldType.IsEnum;
			}
			if (is_container_key && key_info != null)
			{
				if (is_intkey)
				{
					Array item_array = (Array)container_type.InvokeMember("items", BindingFlags.GetField, null, container_obj, null);
					if (item_array != null)
					{
						for (int i = 0; i < item_array.Length; i++)
						{
							object _item = item_array.GetValue(i);
							var key = item_type.InvokeMember(key_name, BindingFlags.GetField, null, _item, null);
							int hash = (int)key_info.FieldType.InvokeMember("GetHashCode", BindingFlags.InvokeMethod, null, key, null);
							key_list.Add(hash);
						}
					}
				} else
				{
					Array item_array = (Array)container_type.InvokeMember("hashs", BindingFlags.GetField, null, container_obj, null);
					if (item_array != null)
					{
						for (int i = 0; i < item_array.Length; i++)
						{
							key_list.Add((int)item_array.GetValue(i));
						}
					}
				}
			}

			var datas = Activator.CreateInstance(item_list_type);
			if (is_container_key)
			{
				Array item_array = (Array)container_type.InvokeMember("items", BindingFlags.GetField, null, container_obj, null);
				if (item_array != null)
				{
					for (int i = 0; i < item_array.Length; i++)
					{
						item_list_type.InvokeMember("Add", BindingFlags.InvokeMethod, null, datas, new object[] { item_array.GetValue(i) });
					}
				}
			}


			// コンテナキーが存在するか？
			if (names.IndexOf(container_key_name) >= 0 && !is_container_key)
			{
//================= <Ter変更:極力InvokeMemberを使わないように対応>
				List<object> temp_array = new List<object>();
//================= <Ter変更:極力InvokeMemberを使わないように対応>
				while (!reader.isAllEnd)
				{
//================= <Ter変更:SetPositionに変更>
					int line = reader.lineNumber;
					int pos = reader.position;
//================= <Ter変更:SetPositionに変更>

					string param = "";
					reader.Pop(out param);
					if (!string.IsNullOrEmpty(param))
					{
						if (param[0] == '#')
						{
							// 次の行へ
							reader.NextLine();
//================= <Ter変更:SetPositionに変更>
							line = reader.lineNumber;
							pos = reader.position;
//================= <Ter変更:SetPositionに変更>s
							continue;
						}
					}

					string now_key = null;
					string now_key_value = null;
					var data = Activator.CreateInstance(item_type);
					for (int i = 0, n = names.Count; i < n; i++)
					{
						if (names[i] == container_key_name)
						{
							now_key = param;
							if (IsEnumType(types[i]))
							{
								var value = (int)Enum.Parse(GetType(types[i]), param);
								param = value.ToString();
							}
							now_key_value = param;
							ReadParam(item_type, ref data, key_name, param);
							break;
						}
						reader.Pop(out param);
					}
//================= <Ter変更:SetPositionに変更>
					//reader.Reset();
					//while (reader.lineNumber != line)
					//	reader.NextLine();
					reader.SetPosition( pos , line );
//================= <Ter変更:SetPositionに変更>

					Type item_container_type = GetType(container_type.FullName + "+ContainerItem");
					if (item_container_type == null)
						return false;

					object item_container = null;
					{
//================= <Ter変更:極力InvokeMemberを使わないように対応>
						//Array item_array = (Array)item_list_type.InvokeMember("ToArray", BindingFlags.InvokeMethod, null, datas, null);
						for (int i = 0; i < temp_array.Count; i++)
						{
							object _item = temp_array[i];
							object key = null;
							if( key_info != null && key_list.Count > i )
								key = key_list[i];
							else
								key = item_type.InvokeMember(key_name, BindingFlags.GetField, null, _item, null);

//================= <Ter変更:極力InvokeMemberを使わないように対応>
							if (now_key_value == key.ToString())
							{
								item_container = item_type.InvokeMember("container", BindingFlags.GetField, null, _item, null);
								break;
							}
						}
					}

					bool is_container_new = item_container == null;
					if (is_container_new)
						item_container = Activator.CreateInstance(item_container_type);

					if (ExportContainer(item_container_type, item_container, ref reader, names, types, now_key))
					{
						if (is_container_new)
						{
							if (key_info != null)
							{
								item_type.InvokeMember("container", BindingFlags.SetField, null, data, new object[] { item_container });
								var key = item_type.InvokeMember(key_name, BindingFlags.GetField, null, data, null);
								int hash = (int)key_info.FieldType.InvokeMember("GetHashCode", BindingFlags.InvokeMethod, null, key, null);
								int num = key_list.Count;
								int index = key_list.Count;
								for (int i = 0; i < num; i++)
								{
									if (key_list[i] > hash)
									{
										index = i;
										break;
									}
								}
								key_list.Insert(index, hash);
//================= <Ter変更:極力InvokeMemberを使わないように対応>
								
								//item_list_type.InvokeMember("Insert", BindingFlags.InvokeMethod, null, datas, new object[] { index, data });
								temp_array.Insert( index, data );
							} else
							{
								//item_list_type.InvokeMember("Add", BindingFlags.InvokeMethod, null, datas, new object[] { data });
								temp_array.Add( data );
							}
						}
					}
				}
				
				for( int i = 0; i < temp_array.Count; ++i )
					item_list_type.InvokeMember("Add", BindingFlags.InvokeMethod, null, datas, new object[] { temp_array[i] });	

//================= <Ter変更:極力InvokeMemberを使わないように対応>
			}
			else
			{
				while (!reader.isAllEnd)
				{
					var data = Activator.CreateInstance(item_type);
					if (ReadItem(item_type, ref data, ref reader, names, types))
					{
						if (key_info != null)
						{
							var key = item_type.InvokeMember(key_name, BindingFlags.GetField, null, data, null);
							int hash = (int)key_info.FieldType.InvokeMember("GetHashCode", BindingFlags.InvokeMethod, null, key, null);
							int num = key_list.Count;
							int index = key_list.Count;
							for (int i = 0; i < num; i++)
							{
								if (key_list[i] > hash)
								{
									index = i;
									break;
								}
							}
							key_list.Insert(index, hash);
							item_list_type.InvokeMember("Insert", BindingFlags.InvokeMethod, null, datas, new object[] { index, data });
						}
						else
						{
							item_list_type.InvokeMember("Add", BindingFlags.InvokeMethod, null, datas, new object[] { data });
						}
					}

					// 次の行へ
					reader.NextLine();

					// container_keyの指定があった場合内容調べてcontainer_keyの部分に値が入っていたらここで抜ける
					if (is_container_key)
					{
						bool is_container_end = false;
						string param = "";
						int line = reader.lineNumber;
						int pos = reader.position;
						for (int i = 0, n = names.Count; i < n; i++)
						{
							reader.Pop(out param);
							if (names[i] == container_key_name)
							{
								if (!string.IsNullOrEmpty(param))
								{
									is_container_end = now_container_key != param;
								}
								break;
							}
						}
//================= <Ter変更:SetPositionに変更>
						//reader.Reset();
						//while (reader.lineNumber != line)
						//	reader.NextLine();
						reader.SetPosition( pos , line );
//================= <Ter変更:SetPositionに変更>

						if (is_container_end)
							break;
					}
				}
			}
			if (key_info != null && !is_intkey)
			{
				container_type.InvokeMember("hashs", BindingFlags.SetField, null, container_obj, new object[] { key_list.ToArray() });
			}
			var array = item_list_type.InvokeMember("ToArray", BindingFlags.InvokeMethod, null, datas, new object[] { });
			container_type.InvokeMember("items", BindingFlags.SetField, null, container_obj, new object[] { array });
			return true;
		}

		/// <summary>
		///   アイテム一つを読み込み
		/// </summary>
		static bool ReadItem(Type item_type, ref object item_obj, ref CsvReader reader, List<string> names, List<string> types, string container_key = null)
		{
			string param = "";
			reader.Pop(out param);

			if (!string.IsNullOrEmpty(param))
			{
				if (param[0] == '#')
				{
					// 次の行へ
					return false;
				}
			}

			bool is_empty = true;
			for (int i = 0, n = names.Count; i < n; i++)
			{
				if (!string.IsNullOrEmpty(types[i]))
				{
					if(names[i] == key_name && IsEnumType(types[i]))
					{
						if(!string.IsNullOrEmpty(param))
						{
							var value = (int)Enum.Parse(GetType(types[i]), param);
							param = value.ToString();
						}
					}
					if (ReadParam(item_type, ref item_obj, names[i], param))
						is_empty = false;
				}
				reader.Pop(out param);
			}
			return !is_empty;
		}

		/// <summary>
		///   パラメータ解析
		/// </summary>
		static bool ReadParam(Type item_type, ref object item_obj, string name, string param)
		{
			FieldInfo info = item_type.GetField(name);
			if (info == null)
			{
				return false;
			}

			bool param_empty = string.IsNullOrEmpty(param);
			if (info.FieldType == typeof(string))
			{
				item_type.InvokeMember(name, BindingFlags.SetField, null, item_obj, new object[] { param });
			}
			else
			{
				if (!param_empty)
				{
					var converter = System.ComponentModel.TypeDescriptor.GetConverter(info.FieldType);
					item_type.InvokeMember(name, BindingFlags.SetField, null, item_obj, new object[] { converter.ConvertFrom(param) });
				}
			}
			return !param_empty;
		}

		/// <summary>
		///   コンテナ名取得
		/// </summary>
		static string GetContainerClassName(string csv_path, bool is_debug)
		{
			string container_name = Path.GetFileNameWithoutExtension(csv_path);
			if (is_debug)
				container_name = container_name.Replace("Debug", "");

			// クラス名
			return container_name + "Container";
		}

		/// <summary>
		///   CSVから変数名とタイプの定義を抽出
		/// </summary>
		static bool ReadNameAndType(ref CsvReader reader, ref List<string> names, ref List<string> types)
		{
			// パラメータ名記録（1行目はパラメータ名である必要がある
			while (!reader.isLineEnd)
			{
				string name = "";
				reader.Pop(out name);
				if (!string.IsNullOrEmpty(name))
				{
					if (name[0] == '#')
					{
						name = name.Remove(0, 1);
					}
				}
				names.Add(name);
			}
			reader.NextLine();

			if (reader.isAllEnd)
				return false;

			// パラメータのタイプ取得（2行目はタイプの定義である必要がある
			while (!reader.isLineEnd)
			{
				string type = "";
				reader.Pop(out type);
				if (!string.IsNullOrEmpty(type))
				{
					if (type[0] == '#')
					{
						type = type.Remove(0, 1);
					}
				}
				types.Add(type);
			}
			reader.NextLine();
			return true;
		}

		/// <summary>
		///   ContainerClassのメンバ等に変化があり更新を行う必要があるか？
		/// </summary>
		static bool IsClassUpdateContainer(Type container_type, List<string> names, List<string> types)
		{
			Type item_type = GetType(container_type.FullName + "+ContainerItem+Item");
			if (item_type == null)
			{
				item_type = GetType(container_type.FullName + "+Item");
			}
			if (item_type == null)
				return false;

			int field_count = 0;
			bool is_change = false;
			for (int i = 0, n = names.Count; i < n; i++)
			{
				if (string.IsNullOrEmpty(types[i]))
					continue;

				if (names[i] == container_key_name)
					continue;

				++field_count;
				FieldInfo info = item_type.GetField(names[i]);
				if (info == null)
				{
					is_change = true;
					break;
				}

				Type type = GetType(types[i]);
				if (type != info.FieldType)
				{
					if (names[i] == key_name && type != null)
					{
						if (type.IsEnum && info.FieldType == typeof(int))
							continue;
					}
					is_change = true;
					break;
				}
			}
			if (item_type.GetFields().Length != field_count)
				is_change = true;

			return is_change;
		}

		/// <summary>
		///   CSVからクラスを生成
		/// </summary>
		static bool _CreateClassCS(List<string> names, List<string> types, string container_name, string dir_path, string csv_path, bool is_auto_update_cs, bool add_singleton_code, bool add_debug, bool is_force_update_cs)
		{
			Type container_type = GetType(container_name);
			if (container_type != null && !is_force_update_cs)
			{
				if (!is_auto_update_cs)
				{
					return false;
				}
				else
				{
					if (!IsClassUpdateContainer(container_type, names, types))
						return false;
				}
			}

			// コンテナキーが存在するか？
			int container_key_index = names.IndexOf(container_key_name);
			if (container_key_index >= 0)
			{
				CreateContainerlClassCS(names, types, container_name, dir_path, csv_path, container_key_index, add_singleton_code, add_debug);
			}
			else
			{
				CreateNormalClassCS(names, types, container_name, dir_path, csv_path, add_singleton_code, add_debug);
			}

			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();
			return true;
		}

		/// <summary>
		///   CSVからクラスを生成（通常版
		/// </summary>
		static void CreateNormalClassCS(List<string> names, List<string> types, string container_name, string dir_path, string csv_path, bool add_singleton_code, bool add_debug)
		{
			// キーが存在するか？
			int key_index = names.IndexOf(key_name);
			bool is_int_key		= false;
			bool is_enum_key	= false;
			if (key_index >= 0)
			{
				is_int_key	= types[key_index] == "int";
				is_enum_key = IsEnumType(types[key_index]);
			}

			// ソースを作成
			using (StreamWriter sw = new StreamWriter(dir_path + "/" + container_name + ".cs", false, Encoding.UTF8))
			{
				sw.WriteLine("using UnityEngine;");
				sw.WriteLine("using System;");
				sw.WriteLine("");
				sw.WriteLine("/// <summary>");
				sw.WriteLine("/// " + container_name + ".csvから出力されたScriptableObjectテンプレートクラス");
				sw.WriteLine("/// TargetCSV : " + csv_path);
				sw.WriteLine("/// </summary>");

				// シングルトンコード
				var add_singleton_interface = "";
				if (add_singleton_code == true)
					add_singleton_interface = ", Framework.ISingletonAsset";

				if (key_index >= 0)
				{
					if (is_int_key || is_enum_key)
						sw.WriteLine("public class " + container_name + " : Framework.IntKeyContainerBase" + add_singleton_interface);
					else
						sw.WriteLine("public class " + container_name + " : Framework.NotValueKeyContainerBase<" + types[key_index] + ">" + add_singleton_interface);
				}
				else
				{
					sw.WriteLine("public class " + container_name + " : ScriptableObject" + add_singleton_interface);
				}
				sw.WriteLine("{");

				// シングルトンコード
				if (add_singleton_code == true)
					WriteSingletonCode(sw, container_name, add_debug, false);

				sw.WriteLine("	/// <summary>");
				sw.WriteLine("	/// " + container_name + "で使用するデータ一行分の定義");
				sw.WriteLine("	/// </summary>");
				sw.WriteLine("	[Serializable]");
				if (key_index >= 0)
				{
					sw.WriteLine("	public class Item : ItemBase");
				}
				else
				{
					sw.WriteLine("	public class Item");
				}
				sw.WriteLine("	{");
				for (int i = 0, n = types.Count; i < n; i++)
				{
					if (i == key_index)
						continue;

					if (string.IsNullOrEmpty(types[i]))
						continue;

					sw.WriteLine("		public " + types[i] + " " + names[i] + ";");
				}
				sw.WriteLine("	}");
				sw.WriteLine("	public Item[]	items;");
				if (key_index >= 0)
				{
					sw.WriteLine("");
					sw.WriteLine("	/// <summary>");
					sw.WriteLine("	/// データの取得");
					sw.WriteLine("	/// </summary>");
					sw.WriteLine("	public Item Get(" + types[key_index] + " key)");
					sw.WriteLine("	{");
					if (add_debug == true)
					{
						sw.WriteLine("	#if AMBER_DEBUG");
						sw.WriteLine("		if(debugContainer != null)");
						sw.WriteLine("		{");
						sw.WriteLine("			var debug = debugContainer.Get(key);");
						sw.WriteLine("			if(debug != null)");
						sw.WriteLine("				return debug;");
						sw.WriteLine("		}");
						sw.WriteLine("	#endif");
					}
					if (is_enum_key)
					{
						sw.WriteLine("		return GetItem(items, (int)key) as Item;"); // enum -> int にキャスト
					}
					else
					{
						sw.WriteLine("		return GetItem(items, key) as Item;");
					}
					sw.WriteLine("	}");
				}
				sw.WriteLine("}");

				sw.Flush();
				sw.Close();
			}
		}

		/// <summary>
		/// 文字列から enum の種類を取得する
		/// </summary>
		static bool IsEnumType(string enumName)
		{
			var type = GetType(enumName);
			if (type == null)
				return false;

			return type.IsEnum;
		}

		/// <summary>
		///   CSVからクラスを生成（グループ版
		/// </summary>
		static void CreateContainerlClassCS(List<string> names, List<string> types, string container_name, string dir_path, string csv_path, int container_key_index, bool add_singleton_code, bool add_debug)
		{
			bool is_container_int_key	= types[container_key_index] == "int";
			bool is_container_enum_key	= IsEnumType(types[container_key_index]);

			// キーが存在するか？
			int key_index = names.IndexOf(key_name);
			bool is_int_key		= false;
			bool is_enum_key	= false;
			if (key_index >= 0)
			{
				is_int_key	= types[key_index] == "int";
				is_enum_key = IsEnumType(types[key_index]);
			}

			// ソースを作成
			using (StreamWriter sw = new StreamWriter(dir_path + "/" + container_name + ".cs", false, Encoding.UTF8))
			{
				// シングルトンコード
				var add_singleton_interface = "";
				if (add_singleton_code == true)
					add_singleton_interface = ", Framework.ISingletonAsset";

				sw.WriteLine("using UnityEngine;");
				sw.WriteLine("using System;");
				sw.WriteLine("");
				sw.WriteLine("/// <summary>");
				sw.WriteLine("/// " + container_name + ".csvから自動出力されたScriptableObjectクラス");
				sw.WriteLine("/// TargetCSV : " + csv_path);
				sw.WriteLine("/// </summary>");
				if (is_container_int_key || is_container_enum_key)
					sw.WriteLine("public class " + container_name + " : Framework.IntKeyContainerBase" + add_singleton_interface);
				else
					sw.WriteLine("public class " + container_name + " : Framework.NotValueKeyContainerBase<" + types[container_key_index] + ">" + add_singleton_interface);
				sw.WriteLine("{");

				// シングルトンコード
				if (add_singleton_code == true)
					WriteSingletonCode(sw, container_name, add_debug, true);

				sw.WriteLine("	/// <summary>");
				sw.WriteLine("	/// 内部要素");
				sw.WriteLine("	/// </summary>");
				sw.WriteLine("	[Serializable]");
				sw.WriteLine("	public class ContainerItem");
				sw.WriteLine("	{");
				sw.WriteLine("		[Serializable]");
				if (key_index >= 0)
				{
					if (is_int_key || is_enum_key)
						sw.WriteLine("		public class Item : Framework.IntKeyContainerBase.ItemBase");
					else
						sw.WriteLine("		public class Item : Framework.NotValueKeyContainerBase<" + types[key_index] + ">.ItemBase");
				}
				else
				{
					sw.WriteLine("		public class Item");
				}
				sw.WriteLine("		{");
				for (int i = 0, n = types.Count; i < n; i++)
				{
					if (i == container_key_index)
						continue;

					if (i == key_index)
						continue;

					if (string.IsNullOrEmpty(types[i]))
						continue;

					sw.WriteLine("			public " + types[i] + " " + names[i] + ";");
				}
				sw.WriteLine("		}");
				sw.WriteLine("		public Item[]	items;");
				if (add_debug)
				{
					sw.WriteLine("#if AMBER_DEBUG");
					sw.WriteLine("		public ContainerItem debug = null;");
					sw.WriteLine("#endif");
				}
				if (key_index >= 0)
				{
					sw.WriteLine("");
					if (!is_int_key && !is_enum_key)
					{

						sw.WriteLine("		/// <summary>");
						sw.WriteLine("		///   検索用のhash配列");
						sw.WriteLine("		/// </summary>");
						sw.WriteLine("		public int[]	hashs;");
						sw.WriteLine("");
					}
					sw.WriteLine("		/// <summary>");
					sw.WriteLine("		/// データの取得");
					sw.WriteLine("		/// </summary>");
					sw.WriteLine("		public Item Get(" + types[key_index] + " key)");
					sw.WriteLine("		{");
					if (add_debug)
					{
						sw.WriteLine("#if AMBER_DEBUG");
						sw.WriteLine("			if (debug != null)");
						if (is_int_key)
							sw.WriteLine("				return Framework.ContainerFinder.GetItem(debug.items, key) as Item;");
						else if (is_enum_key)
							sw.WriteLine("				return Framework.ContainerFinder.GetItem(debug.items, (int)key) as Item;");
						else
							sw.WriteLine("				return Framework.ContainerFinder.GetItem<" + types[key_index] + ">(debug.items, hashs, key) as Item;");
						sw.WriteLine("#endif");
					}
					if (is_int_key)
						sw.WriteLine("			return Framework.ContainerFinder.GetItem(items, key) as Item;");
					else if(is_enum_key)
						sw.WriteLine("			return Framework.ContainerFinder.GetItem(items, (int)key) as Item;");
					else
						sw.WriteLine("			return Framework.ContainerFinder.GetItem<" + types[key_index] + ">(items, hashs, key) as Item;");
					sw.WriteLine("		}");
				}
				sw.WriteLine("	}");
				sw.WriteLine("");
				sw.WriteLine("	/// <summary>");
				sw.WriteLine("	///  ひと固まりのデータ定義");
				sw.WriteLine("	/// </summary>");
				sw.WriteLine("	[Serializable]");
				sw.WriteLine("	public class Item : ItemBase");
				sw.WriteLine("	{");
				sw.WriteLine("		public ContainerItem container;");
				sw.WriteLine("	}");
				sw.WriteLine("	public Item[]	items;");
				sw.WriteLine("");
				sw.WriteLine("	/// <summary>");
				sw.WriteLine("	/// データの取得");
				sw.WriteLine("	/// </summary>");
				sw.WriteLine("	public Item Get(" + types[container_key_index] + " key)");
				sw.WriteLine("	{");
				if (add_debug == true)
				{
					sw.WriteLine("	#if AMBER_DEBUG");
					sw.WriteLine("		if(debugContainer != null)");
					sw.WriteLine("		{");
					sw.WriteLine("			var debug = debugContainer.Get(key);");
					sw.WriteLine("			if(debug != null)");
					sw.WriteLine("				return debug;");
					sw.WriteLine("		}");
					sw.WriteLine("	#endif");
				}
				if (is_container_enum_key)
				{
					sw.WriteLine("		return GetItem(items, (int)key) as Item;"); // enum -> int にキャスト
				}
				else
				{
					sw.WriteLine("		return GetItem(items, key) as Item;");
				}
				sw.WriteLine("	}");
				sw.WriteLine("}");

				sw.Flush();
				sw.Close();
			}
		}

		/// <summary>
		///  シングルトンコード追加
		/// </summary>
		static void WriteSingletonCode(StreamWriter sw, string container_name, bool add_debug, bool is_container_key)
		{
			sw.WriteLine("	/// <summary>");
			sw.WriteLine("	/// for ISingletonAsset");
			sw.WriteLine("	/// </summary>");
			sw.WriteLine("	public static " + container_name + " instance { get; private set; }");
			if (add_debug)
			{
				sw.WriteLine("	public void _SetInstance()");
				sw.WriteLine("	{");
				sw.WriteLine("		if(!_SetDebugContainer())");
				sw.WriteLine("			instance = this;");
				sw.WriteLine("	}");
			}
			else
			{
				sw.WriteLine("	public void _SetInstance() { instance = this; }");
			}
			sw.WriteLine("	public void _ClearInstance() { if (instance == this) instance = null; }");
			sw.WriteLine("");
			if (add_debug)
				WriteDebugCode(sw, container_name, is_container_key);
		}

		/// <summary>
		///  デバッグコード追加
		/// </summary>
		static void WriteDebugCode(StreamWriter sw, string container_name, bool is_container_key)
		{
			sw.WriteLine("#if AMBER_DEBUG");
			sw.WriteLine("	/// <summary>");
			sw.WriteLine("	/// for DebugCode");
			sw.WriteLine("	/// </summary>");
			sw.WriteLine("	public " + container_name + " debugContainer { get; private set; }");
			sw.WriteLine("	public bool _SetDebugContainer()");
			sw.WriteLine("	{");
			sw.WriteLine("		if(!isDebug)");
			sw.WriteLine("		{");
			sw.WriteLine("			if(" + container_name + ".instance != null)");
			sw.WriteLine("			{");
			sw.WriteLine("				if(" + container_name + ".instance.isDebug)");
			sw.WriteLine("					this.debugContainer = " + container_name + ".instance;");
			if (is_container_key)
			{
				sw.WriteLine("				" + container_name + ".instance.SetUpContainer();");
			}
			sw.WriteLine("			}");
			sw.WriteLine("			return false;");
			sw.WriteLine("		}");
			sw.WriteLine("");
			sw.WriteLine("		if(" + container_name + ".instance != null)");
			sw.WriteLine("		{");
			sw.WriteLine("			" + container_name + ".instance.debugContainer = this;");
			if (is_container_key)
			{
				sw.WriteLine("			" + container_name + ".instance.SetUpContainer();");
			}
			sw.WriteLine("			return true;");
			sw.WriteLine("		}");
			sw.WriteLine("		return false;");
			sw.WriteLine("	}");
			if(is_container_key)
			{
				sw.WriteLine("	public void SetUpContainer()");
				sw.WriteLine("	{");
				sw.WriteLine("		if (debugContainer == null)");
				sw.WriteLine("			return;");
				sw.WriteLine("");
				sw.WriteLine("		for (int i = 0; i < debugContainer.items.Length; ++i)");
				sw.WriteLine("		{");
				sw.WriteLine("			var data = GetItem(items, debugContainer.items[i].key) as Item;");
				sw.WriteLine("			if (data != null)");
				sw.WriteLine("				debugContainer.items[i].container.debug = data.container;");
				sw.WriteLine("		}");
				sw.WriteLine("	}");
			}
			sw.WriteLine("#else");
			sw.WriteLine("	public bool _SetDebugContainer()");
			sw.WriteLine("	{");
			sw.WriteLine("		return isDebug;");
			sw.WriteLine("	}");
			sw.WriteLine("#endif");
			sw.WriteLine("	[SerializeField]");
			sw.WriteLine("	bool isDebug = false;");
			sw.WriteLine("");
		}
	}

	/// <summary>
	///  クラスを生成した際にすぐに認識されないので認識されるまで待機してCSV再読み込みするためのウインドウ
	/// </summary>
	public class CSVReimportWindow : EditorWindow
	{
		/// <summary>
		///   出力設定
		/// </summary>
		[Serializable]
		public class ExportPath
		{
			public string csv_path;
			public string export_path;
			public string class_name;
		}

		/// <summary>
		///   クラスが生成されたクラス
		/// </summary>
		public void Add(ExportPath path)
		{
			if (paths.Contains(path))
				return;

			paths.Add(path);
		}

		// internal --------------------------------------------------------------------------------------------------------
		List<ExportPath> paths = new List<ExportPath>();
		int count = 0;
		int time = 0;

		// 毎フレーム更新
		void Update()
		{
			if (paths.Count > 0)
			{
				List<ExportPath> del_list = new List<ExportPath>();
				for (int i = 0, n = paths.Count; i < n; i++)
				{
					if (!CsvToScriptableObject.IsClassUpdateContainer(paths[i].csv_path, paths[i].class_name))
					{
						del_list.Add(paths[i]);
						CsvToScriptableObject.ExportScriptableCSV(paths[i].csv_path, paths[i].export_path, null, paths[i].class_name);
					}
				}

				for (int i = 0, n = del_list.Count; i < n; i++)
				{
					paths.Remove(del_list[i]);
				}

				time++;
				if (time > 20)
				{
					Repaint();
					time = 0;
				}
			}
			else
			{
				Close();
			}
		}

		/// <summary>
		///   GUI表示
		/// </summary>
		void OnGUI()
		{
			count++;
			string str = "isBusy";
			for (int i = 0; i < count % 3; i++)
				str += "・";
			EditorGUILayout.LabelField(str);
		}
	}
}