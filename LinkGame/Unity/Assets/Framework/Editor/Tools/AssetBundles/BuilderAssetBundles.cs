using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;

public class BuilderAssetBundles
{
    // 需打包成ab包的资源文件夹路径前缀
    public static string abPathPref = "Assets/Editor/Resources/";
    /// <summary>
    /// 路径名，是否进包体
    /// 需打包成ab包的资源文件夹路径名称与在move操作时是否需要进包体
    /// </summary>
    public static Dictionary<string, bool> abPaths = new Dictionary<string, bool>() {
        {"TextAssets/Form", true},
        {"TextAssets/Stages", true},
        {"TextAssets/FilterWord", true},
        {"Prefabs/RoomPrefabs", true},
        {"Sprites/Room/Room1", false},
        {"Sprites/Room/Room2", false},
        {"Sprites/Room/Room3", true},
        {"Sprites/Room/Room4", false},
        {"Sprites/Room/Room5", false},
        {"Sprites/Room/Room6", false},
        {"Sprites/Room/Room7", true},
        {"Sprites/Room/Room8", false},
        {"Sprites/Room/Room9", true},
        {"Sprites/Room/Room10", true},
        {"Sprites/Room/Room11", false},
        {"Sprites/Room/Room12", false},
        {"Sprites/Room/Room13", false},
        {"Sprites/Room/Room14", true},
        {"Sprites/Room/Room15", false},
    };
    static Dictionary<string, List<string>> assetsDic = new Dictionary<string, List<string>>();

    /// <summary>
    /// 打包输出地址
    /// </summary>
    /// <param name="target">渠道</param>
    /// <returns>打包输出地址</returns>
    public static string GetOutAssetsDirecotion(BuildTarget target) 
    {
        string assetBundleDirectory = "";
        switch (target)
        {
            case BuildTarget.iOS:
                assetBundleDirectory = "AssetBundles/iOS/AssetBundles";
                break;
            case BuildTarget.Android:
                assetBundleDirectory = "AssetBundles/Android/AssetBundles";
                break;
            case BuildTarget.StandaloneWindows64:
                assetBundleDirectory = "AssetBundles/Win64/AssetBundles";
                break;
            case BuildTarget.StandaloneOSX:
                assetBundleDirectory = "AssetBundles/OSX/AssetBundles";
                break;
            default:
                assetBundleDirectory = "AssetBundles/Other/AssetBundles";
                break;
        }
        
        if (!Directory.Exists(assetBundleDirectory))
        {
            Directory.CreateDirectory(assetBundleDirectory);
        }
        return assetBundleDirectory;
    }
    
    /// <summary>
    /// 服务器热更资源输出地址
    /// </summary>
    /// <param name="target">渠道</param>
    /// <returns>服务器热更资源输出地址</returns>
    public static string GetOutServerAssetsDirecotion(BuildTarget target) 
    {
        string assetBundleDirectory = "";
        switch (target)
        {
            case BuildTarget.iOS:
                assetBundleDirectory = "AssetBundles_Server/iOS/AssetBundles";
                break;
            case BuildTarget.Android:
                assetBundleDirectory = "AssetBundles_Server/Android/AssetBundles";
                break;
            case BuildTarget.StandaloneWindows64:
                assetBundleDirectory = "AssetBundles_Server/Win64/AssetBundles";
                break;
            case BuildTarget.StandaloneOSX:
                assetBundleDirectory = "AssetBundles_Server/OSX/AssetBundles";
                break;
            default:
                assetBundleDirectory = "AssetBundles_Server/Other/AssetBundles";
                break;
        }
        
        if (!Directory.Exists(assetBundleDirectory))
        {
            Directory.CreateDirectory(assetBundleDirectory);
        }
        return assetBundleDirectory;
    }

    /// <summary>
    /// 需要打ab包的资源文件夹列表
    /// </summary>
    /// <returns>列表</returns>
    public static AssetBundleBuild[] GetAssetBundleBuilds()
    {
        var listAssets = new List<AssetBundleBuild>();
        foreach (var it in assetsDic)
        {
            AssetBundleBuild assetBundleBuild = new AssetBundleBuild();
            assetBundleBuild.assetBundleName = it.Key;
            assetBundleBuild.assetBundleVariant = "bytes";
            assetBundleBuild.assetNames = it.Value.ToArray();
            listAssets.Add(assetBundleBuild);
        }
        return listAssets.ToArray();
    }
    
    /// <summary>
    /// 清空需要打ab包的资源文件夹列表
    /// </summary>
    public static void ClearAssetBundleBuilds()
    {
        assetsDic.Clear();
    }

    /// <summary>
    /// 计算全部需要打ab包的资源文件夹列表
    /// </summary>
    public static void SetAssetBundleBuilds()
    {
        foreach (var it in abPaths)
        {
            SearchFileAssetBundleBuild(abPathPref + it.Key);
        }
    }

    /// <summary>
    /// 计算单个需要打ab包的资源文件夹列表
    /// </summary>
    /// <param name="path">文件夹名称</param>
    public static void SearchFileAssetBundleBuild(string path)
    {
        DirectoryInfo directory = new DirectoryInfo(@path);
        FileSystemInfo[] fileSystemInfos = directory.GetFileSystemInfos();
        foreach (var item in fileSystemInfos)
        {
            var filePath = item.ToString();
            var fileName = Path.GetFileName(filePath);
            if (!fileName.Contains(".meta") && !fileName.Contains(".DS_Store"))
            {
                CheckFileOrDirectoryReturnBundleName(item,  Path.Combine(path, fileName));
            }
        }
    }

    /// <summary>
    /// 文件夹继续遍历，文件缓存进需要打ab包的资源文件夹列表
    /// </summary>
    /// <param name="fileSystemInfo">文件信息</param>
    /// <param name="path">详细路径</param>
    public static void CheckFileOrDirectoryReturnBundleName(FileSystemInfo fileSystemInfo, string path)
    {
        FileInfo fileInfo = fileSystemInfo as FileInfo;
        if (fileInfo != null)
        {
            string[] strs = path.Split('.');
            string[] dictors = strs[0].Split('/');
            string name = dictors[dictors.Length - 2];
            if (!assetsDic.ContainsKey(name))
            {
                var list = new List<string>();
                list.Add(path);
                assetsDic.Add(name, list);
            }
            else
            {
                assetsDic[name].Add(path);
            }
        }
        else
        {
            SearchFileAssetBundleBuild(path);
        }
    }

}