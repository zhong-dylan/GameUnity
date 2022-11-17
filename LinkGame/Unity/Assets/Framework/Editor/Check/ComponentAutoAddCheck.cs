using System.Collections;
using System.Collections.Generic;
using Sound;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

public class AutoAddLanguageItem 
{
    [InitializeOnLoadMethod]
    private static void Init()
    {
        EditorApplication.hierarchyChanged += ComponentAutoAddCheck;
    }

    private static void ComponentAutoAddCheck()
    {
        GameObject go = Selection.activeGameObject;
        if(go != null)
        {
            // 添加按钮时，默认添加音声
            var button = go.GetComponent<Button>();
            if(button != null && go.GetComponent<Sound.Component.GOSoundControl>() == null)
            {
                go.AddComponent<Sound.Component.GOSoundControl>();
            }
            
            // 添加文本时，默认添加多语言
            var text = go.GetComponent<Text>();
            if (text != null 
               && (text.font == null || text.font.name == "Arial")
               && go.GetComponent<Language.Component.TextLanguageControl>() == null)
            {
                var font = Resources.Load<Font>("Fonts/STYuanti-SC-Bold");
                text.font = font;
                go.AddComponent<Language.Component.TextLanguageControl>();
            }
        }
    }
}