using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;

public class AssetBundlesEditorManager : EditorWindow
{
    private Vector2 scrollVector2 = Vector2.zero;
    
    [MenuItem("Tools/AssetBundle管理", false, -10)]
    static void ShowToolsWindow()
    {
        AssetBundlesEditorManager window = EditorWindow.GetWindow<AssetBundlesEditorManager>(true, "AssetBundle管理", true);
        window.titleContent = new GUIContent("AssetBundle管理");
        window.minSize = new Vector2(600, 600);
        window.Show();
    }
    
    void DrawStyleItem(string path)
    {
        GUILayout.BeginHorizontal("box");
        GUILayout.Space(10);

        EditorGUILayout.SelectableLabel(Path.GetFileNameWithoutExtension(path), GUILayout.Height(20));
        if (GUILayout.Button("iOS-Android", GUILayout.Width(100), GUILayout.Height(20)))
        {
            AssetBundlesSystem.CalSelectionAssetsOnlyOne(path);
            AssetBundlesSystem.BuildAssetsBundlesNotClear(BuildTarget.iOS);
            AssetBundlesSystem.BuildAssetsBundlesNotClear(BuildTarget.Android);
            // AssetBundlesSystem.BuildAssetsBundlesNotClear(BuildTarget.StandaloneWindows64);
            // AssetBundlesSystem.BuildAssetsBundlesNotClear(BuildTarget.StandaloneOSX);
        }
        if (GUILayout.Button("iOS", GUILayout.Width(60), GUILayout.Height(20)))
        {
            AssetBundlesSystem.CalSelectionAssetsOnlyOne(path);
            AssetBundlesSystem.BuildAssetsBundlesNotClear(BuildTarget.iOS);
        }
        if (GUILayout.Button("Android", GUILayout.Width(60), GUILayout.Height(20)))
        {
            AssetBundlesSystem.CalSelectionAssetsOnlyOne(path);
            AssetBundlesSystem.BuildAssetsBundlesNotClear(BuildTarget.Android);
        }
        if (GUILayout.Button("Win64", GUILayout.Width(60), GUILayout.Height(20)))
        {
            AssetBundlesSystem.CalSelectionAssetsOnlyOne(path);
            AssetBundlesSystem.BuildAssetsBundlesNotClear(BuildTarget.StandaloneWindows64);
        }
        if (GUILayout.Button("OSX", GUILayout.Width(60), GUILayout.Height(20)))
        {
            AssetBundlesSystem.CalSelectionAssetsOnlyOne(path);
            AssetBundlesSystem.BuildAssetsBundlesNotClear(BuildTarget.StandaloneOSX);
        }
        GUILayout.EndHorizontal();
    }

    private void OnGUI()
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label("选择以下渠道，导出全部ab包资源：（删除全部再进行导出）");
        GUILayout.EndHorizontal();
        
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("iOS-Android-All", GUILayout.Width(200), GUILayout.Height(30f)))
        {
            AssetBundlesSystem.CalSelectionAssets();
            AssetBundlesSystem.BuildAssetsBundles(BuildTarget.iOS);
            AssetBundlesSystem.BuildAssetsBundles(BuildTarget.Android);
            // AssetBundlesSystem.BuildAssetsBundles(BuildTarget.StandaloneWindows64);
            // AssetBundlesSystem.BuildAssetsBundles(BuildTarget.StandaloneOSX);
        }
        GUILayout.Space(5);
        if (GUILayout.Button("iOS-All", GUILayout.Width(90), GUILayout.Height(30f)))
        {
            AssetBundlesSystem.CalSelectionAssets();
            AssetBundlesSystem.BuildAssetsBundles(BuildTarget.iOS);
        }
        GUILayout.Space(5);
        if (GUILayout.Button("Android-All", GUILayout.Width(90), GUILayout.Height(30f)))
        {
            AssetBundlesSystem.CalSelectionAssets();
            AssetBundlesSystem.BuildAssetsBundles(BuildTarget.Android);
        }
        GUILayout.Space(5);
        if (GUILayout.Button("Win64-All", GUILayout.Width(90), GUILayout.Height(30f)))
        {
            AssetBundlesSystem.CalSelectionAssets();
            AssetBundlesSystem.BuildAssetsBundles(BuildTarget.StandaloneWindows64);
        }
        GUILayout.Space(5);
        if (GUILayout.Button("OSX-All", GUILayout.Width(90), GUILayout.Height(30f)))
        {
            AssetBundlesSystem.CalSelectionAssets();
            AssetBundlesSystem.BuildAssetsBundles(BuildTarget.StandaloneOSX);
        }
        GUILayout.EndHorizontal();
        
        GUILayout.BeginHorizontal();
        GUILayout.Label("AB包列表：（本地测试可导单个ab包，正式时请使用上面导出全部ab包的方式！）");
        GUILayout.EndHorizontal();
        
        // scroll
        scrollVector2 = GUILayout.BeginScrollView(scrollVector2);
        foreach (var it in BuilderAssetBundles.abPaths)
        {
            DrawStyleItem(it.Key);
        }
        GUILayout.EndScrollView();
        
        GUILayout.BeginHorizontal();
        GUILayout.Label("选择以下渠道，清除对应全部ab包资源：");
        GUILayout.EndHorizontal();
        
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("全平台-Clear", GUILayout.Width(200), GUILayout.Height(30f)))
        {
            AssetBundlesSystem.ClearAssetBundles(BuildTarget.iOS);
            AssetBundlesSystem.ClearAssetBundles(BuildTarget.Android);
            AssetBundlesSystem.ClearAssetBundles(BuildTarget.StandaloneWindows64);
            AssetBundlesSystem.ClearAssetBundles(BuildTarget.StandaloneOSX);
        }
        GUILayout.Space(5);
        if (GUILayout.Button("iOS-Clear", GUILayout.Width(90), GUILayout.Height(30f)))
        {
            AssetBundlesSystem.ClearAssetBundles(BuildTarget.iOS);
        }
        GUILayout.Space(5);
        if (GUILayout.Button("Android-Clear", GUILayout.Width(90), GUILayout.Height(30f)))
        {
            AssetBundlesSystem.ClearAssetBundles(BuildTarget.Android);
        }
        GUILayout.Space(5);
        if (GUILayout.Button("Win64-Clear", GUILayout.Width(90), GUILayout.Height(30f)))
        {
            AssetBundlesSystem.ClearAssetBundles(BuildTarget.StandaloneWindows64);
        }
        GUILayout.Space(5);
        if (GUILayout.Button("OSX-Clear", GUILayout.Width(90), GUILayout.Height(30f)))
        {
            AssetBundlesSystem.ClearAssetBundles(BuildTarget.StandaloneOSX);
        }
        GUILayout.EndHorizontal();
        
        GUILayout.BeginHorizontal();
        GUILayout.Label("将对应渠道全部AB包放入 AssetBundles_Server/，按宏将需要入包体的AB包放入 Assets/StreamingAssets/，");
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        GUILayout.Label("进行加密处理，并在 AssetBundles_Server/ 下生成热更需要的文件列表：（运行都会先清空目录）");
        GUILayout.EndHorizontal();
        
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("iOS-Move", GUILayout.Width(100), GUILayout.Height(30f)))
        {
            AssetBundlesSystem.MoveAssetBundlesAndEncypt(BuildTarget.iOS);
        }
        GUILayout.Space(5);
        if (GUILayout.Button("Android-Move", GUILayout.Width(100), GUILayout.Height(30f)))
        {
            AssetBundlesSystem.MoveAssetBundlesAndEncypt(BuildTarget.Android);
        }
        GUILayout.Space(5);
        if (GUILayout.Button("Win64-Move", GUILayout.Width(100), GUILayout.Height(30f)))
        {
            AssetBundlesSystem.MoveAssetBundlesAndEncypt(BuildTarget.StandaloneWindows64);
        }
        GUILayout.Space(5);
        if (GUILayout.Button("OSX-Move", GUILayout.Width(100), GUILayout.Height(30f)))
        {
            AssetBundlesSystem.MoveAssetBundlesAndEncypt(BuildTarget.StandaloneOSX);
        }
        GUILayout.EndHorizontal();
    }
}