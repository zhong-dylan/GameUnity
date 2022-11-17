using System.Collections.Generic;
using System.Linq;
using System.Xml;
using UnityEditor;
using UnityEngine;

/// <summary>
/// シンボルを設定するウィンドウを管理するクラス
/// </summary>
public class SymbolWindow : EditorWindow
{
    //===================================================================================================
    // クラス
    //===================================================================================================

    /// <summary>
    /// シンボルのデータを管理するクラス
    /// </summary>
    private class SymbolData
    {
        public string   Name        { get; private set; }   // 宏命名
        public string   Comment     { get; private set; }   // 宏说明
        public bool     IsEnable    { get; set;         }   // 宏是否添加入游戏
        public string   Type        { get; private set; }   // 宏类型

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public SymbolData( XmlNode node )
        {
            Name    = node.Attributes[ "name"    ].Value;
            Comment = node.Attributes[ "comment" ].Value;
            Type = node.Attributes[ "type" ].Value;
        }
    }
    
    //===================================================================================================
    // 定数
    //===================================================================================================

    private const string ITEM_NAME      = "Tools/Symbols";              // コマンド名
    private const string WINDOW_TITLE   = "Symbols";                    // ウィンドウのタイトル
    private const string XML_PATH       = "Assets/Editor/Tools/symbols.xml";  // 読み込む .xml のファイルパス
    
    //===================================================================================================
    // 変数
    //===================================================================================================

    private static Vector2      mScrollPos = Vector2.zero;     // スクロール座標
    private static SymbolData[] mSymbolList;    // シンボルのリスト
    
    //===================================================================================================
    // 静的関数
    //===================================================================================================

    /// <summary>
    /// ウィンドウを開きます
    /// </summary>
    [MenuItem(ITEM_NAME, false, -19)]
    private static void Open()
    {
        var window = GetWindow<SymbolWindow>( true, WINDOW_TITLE );
        window.minSize = new Vector2(600, 600);
        window.Init();
    }
    
    //===================================================================================================
    // 関数
    //===================================================================================================

    /// <summary>
    /// 初期化する時に呼び出します
    /// </summary>
    private void Init()
    {
        var document = new XmlDocument();
        document.Load( XML_PATH );

        var root        = document.GetElementsByTagName( "root" )[ 0 ];
        var symbolList  = new List<XmlNode>();
            
        foreach ( XmlNode n in root.ChildNodes )
        {
            if ( n.Name == "symbol" )
            {
                symbolList.Add( n );
            }
        }

        mSymbolList = symbolList
            .Select( c => new SymbolData( c ) )
            .ToArray();

        var defineSymbols = PlayerSettings
            .GetScriptingDefineSymbolsForGroup( EditorUserBuildSettings.selectedBuildTargetGroup )
            .Split( ';' );

        foreach ( var n in mSymbolList )
        {
            n.IsEnable = defineSymbols.Any( c => c == n.Name );
        }
    }

    private void CreateLine(string _type)
    {
	    // mScrollPos = GUILayout.BeginScrollView(mScrollPos);
	    foreach ( var n in mSymbolList )
	    {
		    if (n.Type != _type) continue;
	        
		    // GUILayout.BeginHorizontal( GUILayout.ExpandWidth( true ) );
		    GUILayout.BeginHorizontal();
		    n.IsEnable = EditorGUILayout.Toggle( n.IsEnable, GUILayout.Width( 16 ) );
		    if ( GUILayout.Button( "Copy" ) )
		    {
			    EditorGUIUtility.systemCopyBuffer = n.Name;
		    }
		    EditorGUILayout.LabelField( n.Name, GUILayout.ExpandWidth( true ), GUILayout.MinWidth( 0 ) );
		    EditorGUILayout.LabelField( n.Comment, GUILayout.ExpandWidth( true ), GUILayout.MinWidth( 0 ) );
		    GUILayout.EndHorizontal();
	    }
	    // GUILayout.EndScrollView();
    }
        
    /// <summary>
    /// GUI を表示する時に呼び出されます
    /// </summary>
    private void OnGUI()
    {
	    GUILayout.BeginHorizontal();
	    GUILayout.Label("1、服务器选择（必须单选）");
	    GUILayout.EndHorizontal();
	    CreateLine("server");

	    GUILayout.Space(5);
	    GUILayout.BeginHorizontal();
        GUILayout.Label("2、测试设置（正式包应全关）");
        GUILayout.EndHorizontal();
        CreateLine("test");
        
        GUILayout.Space(5);
        GUILayout.BeginHorizontal();
        GUILayout.Label("3、游戏时间单位");
        GUILayout.EndHorizontal();
        CreateLine("time");
        
        GUILayout.Space(5);
        GUILayout.BeginHorizontal();
        GUILayout.Label("4、打ab包设置");
        GUILayout.EndHorizontal();
        CreateLine("ab");

        GUILayout.Space(5);
        GUILayout.BeginHorizontal();
        GUILayout.Label("5、测试包必开，正式包必关");
        GUILayout.EndHorizontal();
        CreateLine("offical");
        
        GUILayout.Space(20);
        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        if ( GUILayout.Button( "保存", GUILayout.Width(200), GUILayout.Height(30) ) )
        {
            var defineSymbols = mSymbolList
                .Where( c => c.IsEnable )
                .Select( c => c.Name )
                .ToArray();

			SymbolsSet (defineSymbols);
            Close();
            return;
        }
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();
    }

	public static void SymbolsSet(string[] defineSymbols){
		// ターゲットを設定
		BuildTargetGroup[] targets = {
			BuildTargetGroup.Android,
			BuildTargetGroup.iOS,
			BuildTargetGroup.Standalone,
			BuildTargetGroup.WebGL,
			EditorUserBuildSettings.selectedBuildTargetGroup
		};

		foreach (BuildTargetGroup target in targets) {
			PlayerSettings.SetScriptingDefineSymbolsForGroup(
				target, 
				string.Join( ";", defineSymbols )
			);
		}
	}

	public static void SymbolsSub(string defineSymbols){
		// ターゲットを設定
		BuildTargetGroup[] targets = {
			BuildTargetGroup.Android,
			BuildTargetGroup.iOS,
			BuildTargetGroup.Standalone,
			BuildTargetGroup.WebGL,
			EditorUserBuildSettings.selectedBuildTargetGroup
		};

		string symbol = defineSymbols;
		//char[] trim = {}
		foreach (BuildTargetGroup target in targets) {
			string tmp = PlayerSettings.GetScriptingDefineSymbolsForGroup (target);
			tmp = tmp.Replace (symbol, "");
			tmp = tmp.TrimStart (';');


			PlayerSettings.SetScriptingDefineSymbolsForGroup(
				target, 
				tmp
			);
		}
	}

	public static void SymbolsAdd(string defineSymbols){
		// ターゲットを設定
		BuildTargetGroup[] targets = {
			BuildTargetGroup.Android,
			BuildTargetGroup.iOS,
			BuildTargetGroup.Standalone,
			BuildTargetGroup.WebGL,
			EditorUserBuildSettings.selectedBuildTargetGroup
		};

		string symbol = defineSymbols;
		foreach (BuildTargetGroup target in targets) {
			string tmp = PlayerSettings.GetScriptingDefineSymbolsForGroup (target);
			if (tmp.Contains (symbol)==false) {
				tmp += ";"+symbol;
			}

			PlayerSettings.SetScriptingDefineSymbolsForGroup(
				target, 
				tmp
			);
		}
	}
}