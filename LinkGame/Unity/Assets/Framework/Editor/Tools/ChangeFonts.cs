using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using System.IO;
using System;

/// <summary>
/// Text字体修改工具
/// </summary>
public class FontChangeTool : EditorWindow
{
    private string path = "Assets/Resources/Prefabs";
    List<GameObject> prefabList = new List<GameObject>();   
    string[] assetsPaths = new string[0];
    List<bool> flagList ;
    Vector2 scallPos;
     
    static FontChangeTool window; 
    bool isSpecifyFontReplace;//是否指定字体更换
    Font oldFont;
    Font toChange;
    static Font toChangeFont;
    bool isFontStyleReplace;//是否需要替换字体风格
    FontStyle toFontStyle;
    static FontStyle toChangeFontStyle;
 
 
    private void OnGUI()
    {
        RefreshUI();
    }
 
    [MenuItem("Tools/FontChangeTool", false, 20)]
    public static void InitFont()
    {
        window = (FontChangeTool)GetWindow(typeof(FontChangeTool));
        window.titleContent.text = "字体设置";
        window.position = new Rect(PlayerSettings.defaultScreenWidth / 2, PlayerSettings.defaultScreenHeight / 2, 600, 600);
        window.Show();
    }
    /// <summary>
    /// 刷新面板显示
    /// </summary>
    private void RefreshUI()
    {
        //GUILayout.BeginVertical();
        GUILayout.BeginHorizontal();
        {
            GUILayout.Label("文件夹路径", GUILayout.Width(50f));
            path = GUILayout.TextField(path);
            if (GUILayout.Button("浏览", GUILayout.Width(50f)))
            {
                var path2 = EditorUtility.OpenFolderPanel("窗口标题", Application.dataPath,"");
                if (path2.Equals("")) return;

                path = path2;
                GetAllPrefabPahtByDirectory(path, out assetsPaths);
                flagList = new List<bool>();
                for(int i = 0; i < assetsPaths.Length; i++)
                {
                    flagList.Add(false);
                }
            }
        }
        GUILayout.EndHorizontal();
        GUILayout.Space(20);
        if(path=="" || assetsPaths.Length <= 0)
        {
            GUILayout.Label("请先选择文件夹");
            return;
        }
        GUILayout.BeginHorizontal();
        {
            GUILayout.Label("选择需要转换字体的预置文件");
            if (GUILayout.Button("全选", GUILayout.Width(50f)))
            {
                for(int i = 0; i < flagList.Count; i++)
                    flagList[i] = !flagList[i];
            }
        }
        GUILayout.EndHorizontal();
        scallPos = EditorGUILayout.BeginScrollView(scallPos, GUILayout.Height(400));
        {
            for (int i = 0; i < assetsPaths.Length; i++)
            {
                string lable = Path.GetFileName(assetsPaths[i]);
                flagList[i] = EditorGUILayout.ToggleLeft(lable, flagList[i]);
            }
        }
        EditorGUILayout.EndScrollView();
        if(!flagList.Exists(s=>s))
        {
            return;
        }
 
        isSpecifyFontReplace = EditorGUILayout.Toggle("指定字体更换：", isSpecifyFontReplace);
        if (isSpecifyFontReplace)
            oldFont = (Font)EditorGUILayout.ObjectField("需要改动的字体：", oldFont, typeof(Font), true, GUILayout.MinWidth(100f));
        toChange = (Font)EditorGUILayout.ObjectField("目标字体：", toChange, typeof(Font), true, GUILayout.MinWidth(100f));
        toChangeFont = toChange;
        isFontStyleReplace = EditorGUILayout.Toggle("指定字体风格更换：",isFontStyleReplace);
        if (isFontStyleReplace)
        {
            toFontStyle = (FontStyle)EditorGUILayout.EnumPopup("字体风格：", toFontStyle, GUILayout.MinWidth(100f));
            toChangeFontStyle = toFontStyle;
        }
 
        if (toChangeFont != null)
        {
            if (GUILayout.Button("更换"))
            {
                ChangeFont();
            }
        }
        //GUILayout.EndVertical();
    } 
 
    public void Change(GameObject go)
    {
        //寻找Hierarchy面板下所有的Text .
        var tArray = go.GetComponentsInChildren<Text>(true);
        
        foreach (Text t in tArray)
        { 
            //这个很重要，如果没有这个代码，unity是不会察觉到编辑器有改动的，自然设置完后直接切换场景改变是不被保存的  
            //如果不加这个代码  在做完更改后 自己随便手动修改下场景里物体的状态 再保存就好了 
            Undo.RecordObject(t, t.gameObject.name);
            if (isSpecifyFontReplace)//指定字体更换，只更换某一种字体为目标字体
            {
                if (t.font == oldFont)
                {
                    t.font = toChangeFont;
                }
            }
            else//更换所有字体为目标字体
            {
                t.font = toChangeFont; 
            }
            if(isFontStyleReplace)
                t.fontStyle = toChangeFontStyle;
            //相当于让他刷新下 不然unity显示界面还不知道自己的东西被换掉了  还会呆呆的显示之前的东西
            EditorUtility.SetDirty(t);
        }
 
        //在这里储存预制体修改，用try catch是因为有可能有部分prefab会报错，但是并不影响操作
        try
        {
            PrefabUtility.SavePrefabAsset(go);
            Debug.Log("ChangeFont:" + go.name + ">>>>>Succed");
        }
        catch (Exception e)
        {
            Debug.LogWarning("ChangeFont:" + go.name + e.Message);
        }
    }
 
    public void ChangeFont()
    {
        List<string> assets = new List<string>();
        for(int i = 0; i < flagList.Count; i++)
        {
            if (flagList[i] && assetsPaths.Length>i)
                assets.Add(assetsPaths[i]);
        }
        GetAllPrefabGOByPath(assets, out prefabList);
        for(int i = 0; i < prefabList.Count; i++)
        {
            Change(prefabList[i]);
        }
    }
    
    public static void GetAllPrefabPahtByDirectory(string path,  out string[] assetsPaths)
    {
        assetsPaths = Directory.GetFiles(path, "*.prefab", SearchOption.AllDirectories);
    }
    public static void GetAllPrefabGOByPath( List<string>assetsPaths, out List<GameObject> prefabList)
    {
        prefabList = new List<GameObject>();
        GameObject _prefab;
        string path = "";
        foreach (var _path in assetsPaths)
        {
             path = _path.Replace("\\", "/").Replace(Application.dataPath,"Assets");
            _prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);//, typeof(GameObject)) as GameObject;
            prefabList.Add(_prefab);
        } 
    }
}