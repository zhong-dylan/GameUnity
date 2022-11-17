using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
/// <summary>
/// 根据当前预制体生成一个配置文件...
/// 需要恢复的时候读取配置文件重现关联资源...
/// </summary>
public class AutoAnimationRef : MonoBehaviour
{
    public class AnimationNames
    {
        public string[] names;
    }

    public static string[] PrefabPaths = { "Assets/Resources/CatRes/Prefabs/CatModels/golden.prefab", "Assets/Resources/CatRes/Prefabs/CatModels/golden_ui.prefab" };

    [MenuItem("Tools/Animation/备份动画数据")]
    public static void DumpGoldenAnimations()
    {
        foreach(string prefabPath in PrefabPaths)
        {
            _DumpGoldenAnimations(prefabPath);
        }
    }
   public static void _DumpGoldenAnimations(string prefabPath)
    {
        GameObject golden = (GameObject)AssetDatabase.LoadAssetAtPath(prefabPath, typeof(GameObject));
        Animation anim = golden.GetComponent<Animation>();
        List<string> animNamesList = new List<string>();
        foreach(AnimationState state in anim)
        {
            if(state.clip != null)
            {
                animNamesList.Add(AssetDatabase.GetAssetPath(state.clip));
            }
        }
        System.IO.File.WriteAllText($"{prefabPath}.anim.config", JsonUtility.ToJson(new AnimationNames() { names = animNamesList.ToArray() },true));
        AssetDatabase.Refresh();
    }

    [MenuItem("Tools/Animation/重新关联动画数据")]
    public static void ConfigGoldenAnimations()
    {
        foreach (string prefabPath in PrefabPaths)
        {
            _ConfigGoldenAnimations(prefabPath);
        }
    }
    public static void _ConfigGoldenAnimations(string prefabPath)
    {
        AnimationNames animNames = JsonUtility.FromJson<AnimationNames>(System.IO.File.ReadAllText($"{prefabPath}.anim.config"));
        GameObject golden = (GameObject)AssetDatabase.LoadAssetAtPath(prefabPath, typeof(GameObject));
        Animation anim = golden.GetComponent<Animation>();
        if(anim)
            DestroyImmediate(anim,true);
        anim = golden.AddComponent<Animation>();
        foreach (string animPath in animNames.names)
        {
            AnimationClip clip = (AnimationClip)AssetDatabase.LoadAssetAtPath(animPath, typeof(AnimationClip));
           if(clip == null)
            {
                Debug.LogError($"anim not exist {animPath}");
            }
            anim.AddClip(clip, clip.name);
        }
        AssetDatabase.SaveAssets();
        ZLog.LogPink($"{prefabPath} 配置完成");
        AssetDatabase.Refresh();
    }
}
