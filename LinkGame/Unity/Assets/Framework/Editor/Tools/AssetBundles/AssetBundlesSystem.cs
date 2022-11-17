using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class AssetBundlesSystem
{
    /// <summary>
    /// 清空并计算全部需要打ab包的资源文件夹列表
    /// </summary>
    public static void CalSelectionAssets()
    {
        BuilderAssetBundles.ClearAssetBundleBuilds();
        BuilderAssetBundles.SetAssetBundleBuilds();
    }
    
    /// <summary>
    /// 清空并计算单个需要打ab包的资源文件夹列表
    /// </summary>
    /// <param name="abPath">单个需要打ab包的资源文件夹名称</param>
    public static void CalSelectionAssetsOnlyOne(string abPath)
    {
        BuilderAssetBundles.ClearAssetBundleBuilds();
        BuilderAssetBundles.SearchFileAssetBundleBuild(BuilderAssetBundles.abPathPref + abPath);
    }

    /// <summary>
    /// 清空输出文件夹并打ab包
    /// </summary>
    /// <param name="target">渠道</param>
    public static void BuildAssetsBundles(BuildTarget target)
    {
        ClearAssetBundles(target);
        BuildPipeline.BuildAssetBundles(
            BuilderAssetBundles.GetOutAssetsDirecotion(target)
            , BuilderAssetBundles.GetAssetBundleBuilds()
            , BuildAssetBundleOptions.ChunkBasedCompression
            , target);
    }
    
    /// <summary>
    /// 打ab包（不清空输出文件夹）
    /// </summary>
    /// <param name="target">渠道</param>
    public static void BuildAssetsBundlesNotClear(BuildTarget target)
    {
        BuildPipeline.BuildAssetBundles(
            BuilderAssetBundles.GetOutAssetsDirecotion(target)
            , BuilderAssetBundles.GetAssetBundleBuilds()
            , BuildAssetBundleOptions.ChunkBasedCompression
            , target);
    }

    /// <summary>
    /// 清空输出文件夹
    /// </summary>
    /// <param name="target">渠道</param>
    public static void ClearAssetBundles(BuildTarget target)
    {
        try
        {
            // 清空生成目录
            string[] fileList = Directory.GetFiles(BuilderAssetBundles.GetOutAssetsDirecotion(target));
            for (var i = 0; i < fileList.Length; i++)
            {
                var file = fileList[i];
                if (!Directory.Exists(file))
                {
                    File.Delete(file);
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError("删除文件文件错误：" + e.Message);
        }
    }
    
    /// <summary>
    /// 清空并移动选择渠道的所有ab包至 Assets/StreamingAssets 下,并进行加密处理
    /// </summary>
    /// <param name="target">渠道</param>
    public static void MoveAssetBundlesAndEncypt(BuildTarget target)
    {
        string targetPath = Application.streamingAssetsPath;
        try
        {
            string[] fileList = Directory.GetFiles(targetPath);
            for (var i = 0; i < fileList.Length; i++)
            {
                var file = fileList[i];
                if (!Directory.Exists(file))
                {
                    File.Delete(file);
                }
            }
            
            // 清空服务器热更资源目录
            fileList = Directory.GetFiles(BuilderAssetBundles.GetOutServerAssetsDirecotion(target));
            for (var i = 0; i < fileList.Length; i++)
            {
                var file = fileList[i];
                if (!Directory.Exists(file))
                {
                    File.Delete(file);
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError("删除文件文件错误：" + e.Message);
        }

        if (!Directory.Exists(targetPath))
        {
            Directory.CreateDirectory(targetPath);
        }

        // 需上传服务器cdn的资源路径
        var serverPath = BuilderAssetBundles.GetOutServerAssetsDirecotion(target);
        
        // 整理是否需要丢入包体的资源
        var checkAbPaths = new Dictionary<string, bool>();
        foreach (var it in BuilderAssetBundles.abPaths)
        {
            var key = Path.GetFileNameWithoutExtension(it.Key).ToLower();
            if (checkAbPaths.ContainsKey(key))
            {
                ZLog.Error($"key[{it.Key}] 转为 {key} 有重复值，请检查修改！");
            }
            else
            {
                checkAbPaths.Add(key, it.Value);
            }
        }
        
        string[] fileList2 = Directory.GetFiles(BuilderAssetBundles.GetOutAssetsDirecotion(target));
        for (var i = 0; i < fileList2.Length; i++)
        {
            var file = fileList2[i];
            if (!Directory.Exists(file) && Path.GetFileName(file) != "AssetBundles" && !file.EndsWith(".manifest"))
            {
                var serverFile = Path.Combine(serverPath, Path.GetFileName(file));
                File.Copy(file, serverFile);
                
                // 加密
                Byte[] temp = File.ReadAllBytes(serverFile);
                AssetBundleGlobal.Decrypt(ref temp);
                File.WriteAllBytes(serverFile, temp);

                // 仅将需要进入包体的ab包加入 streamingAssetsPath
#if WHOLE_PACKAGE
                var targetFile = Path.Combine(targetPath, Path.GetFileName(file));
                File.Copy(serverFile, targetFile);
#else
                var keys = Path.GetFileName(file).Split('.');
                if (checkAbPaths.ContainsKey(keys[0]) && checkAbPaths[keys[0]] == true)
                {
                    var targetFile = Path.Combine(targetPath, Path.GetFileName(file));
                    File.Copy(serverFile, targetFile);
                }
#endif
            }
        }

        CreateFileList(serverPath, checkAbPaths, true); // cdn上的资源列表文件
        // CreateFileList(targetPath, checkAbPaths, false); // 包体内的资源列表文件（废弃，改为直接计算文件的md5）
    }

    /// <summary>
    /// Create FileList
    /// </summary>
    static void CreateFileList(string outPath, Dictionary<string, bool> checkAbPaths, bool backup)
    {
        string filePath = Path.Combine(outPath, "files");
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }

        StreamWriter streamWriter = new StreamWriter(filePath);

        string[] files = Directory.GetFiles(outPath);
        for (int i = 0; i < files.Length; i++)
        {
            string tmpfilePath = files[i];
            if (tmpfilePath.Equals(filePath) || Path.GetFileName(tmpfilePath) == "AssetBundles" || tmpfilePath.EndsWith(".manifest"))
                continue;
            
            Debug.Log(tmpfilePath);
            tmpfilePath.Replace("\\", "/");
            
            var keys = Path.GetFileName(tmpfilePath).Split('.');
            var isIn = checkAbPaths.ContainsKey(keys[0]) && checkAbPaths[keys[0]] == true ? 1 : 0;
            
            FileInfo file = new FileInfo(tmpfilePath);
            streamWriter.WriteLine(Path.GetFileName(tmpfilePath) + "," + AssetBundleGlobal.GetFileMD5(tmpfilePath, false) + "," + file.Length + "," + isIn);
        }
        streamWriter.Close();
        streamWriter.Dispose();
        
        // 复制一份未加密的备份
        if (backup)
        {
            File.Copy(filePath, Path.Combine(outPath, "files_backup.txt"));
        }
        
        // 加密
        Byte[] temp = File.ReadAllBytes(filePath);
        AssetBundleGlobal.Decrypt(ref temp);
        File.WriteAllBytes(filePath, temp);
    }

    
}