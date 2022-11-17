#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;
using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using OfficeOpenXml;        
using OfficeOpenXml.Drawing;        
using OfficeOpenXml.Drawing.Chart;        
using OfficeOpenXml.Style;
using System.Linq;
using SimpleJSON;

namespace tools
{
    public class ExcelConfigure : EditorWindow
    {
        private string _excelFormworkFile = @"Assets/Editor/Formwork/excelFormwork.xlsx";
        private string _excelPath = @"Assets/Editor/Excel/";
        private string _jsonPath = @"Assets/Editor/Resources/TextAssets/Form/";
        private string _voPath = @"Assets/Scripts/VO/Auto/";
        private string _jsonUtilPath = @"Assets/Scripts/JsonUtil/AutoJsonUtil/";

        private int _excelTempMax = 0;
        private string _excelTempNamePref = "temp_";
        private List<string> _excelList = new List<string>();
        private Dictionary<string, string> _infoTypeDic = new Dictionary<string, string>();
        private List<Dictionary<string, string>> _excelInfoList = new List<Dictionary<string, string>>();

        private Vector2 scrollVector2 = Vector2.zero;
        private string search = "";

        #region 编缉器入口
        [MenuItem("Tools/导表工具", false, -20)]
        public static void Open()
        {
            ExcelConfigure window = EditorWindow.GetWindow<ExcelConfigure>();
            window.titleContent = new GUIContent("导表工具");
            window.minSize = new Vector2(600, 600);
            window.Show();
        }
        #endregion

        void OnEnable()
        {
            CalExcels();
        }
        void OnDisable()
        {
            
        }

        #region OnGUI
        void OnGUI()
        {
            DrawSearchGUI();

            Repaint();
        }
        #endregion

        void CalExcels()
        {
            _excelTempMax = 0;
            _excelList.Clear();

            string[] fileList = Directory.GetFiles(_excelPath);
            for (var i = 0; i < fileList.Length; i++)
            {
                var file = fileList[i];
                var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(file);

                //不是路径
                if (!Directory.Exists(file) && Path.GetExtension(file) == ".xlsx")
                {
                    _excelList.Add(file);
                    if (fileNameWithoutExtension.IndexOf(_excelTempNamePref) == 0)
                    {
                        var tempNum = int.Parse(fileNameWithoutExtension.Substring(_excelTempNamePref.Length));
                        if (_excelTempMax < tempNum)
                        {
                            _excelTempMax = tempNum;
                        }
                    }
                    
                }
                else if (Path.GetFileName(file) != ".DS_Store" && Path.GetExtension(file) != ".meta")
                {
                    Debug.LogError($"{_excelPath}: {Path.GetFileName(file)} 命名错误！");
                }
            }
        }

        void DrawStyleItem(string path)
        {
            GUILayout.BeginHorizontal("box");
            GUILayout.Space(10);

            EditorGUILayout.SelectableLabel(Path.GetFileNameWithoutExtension(path), GUILayout.Height(20));
            if (GUILayout.Button("打开", GUILayout.Width(60), GUILayout.Height(20)))
            {
                System.Diagnostics.Process.Start(path);

                GUIUtility.keyboardControl = 0; // 去输入框的蓝色高亮
            }
            if (GUILayout.Button("导出", GUILayout.Width(60), GUILayout.Height(20)))
            {
                ReadExcel(path);

                GUIUtility.keyboardControl = 0; // 去输入框的蓝色高亮
                
                // 刷新Unity
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
            GUILayout.EndHorizontal();
        }

        void DrawSearchGUI()
        {
            // 按钮事件
            {
                GUILayout.BeginHorizontal();

                GUILayout.Space(10);
                // 创建一个普通规格的点击按钮
                if (GUILayout.Button("新建表", GUILayout.Width(200), GUILayout.Height(30)))
                {
                    // 实现
                    CreateOneExcel();

                    GUIUtility.keyboardControl = 0; // 去输入框的蓝色高亮
                }


                GUILayout.Space(150);

                if (GUILayout.Button("刷新", GUILayout.Width(100), GUILayout.Height(30)))
                {
                    CalExcels();

                    GUIUtility.keyboardControl = 0; // 去输入框的蓝色高亮
                }

                GUILayout.Space(20);

                if (GUILayout.Button("清空生成文件", GUILayout.Width(100), GUILayout.Height(30)))
                {
                    try
                    {
                        string[] jsonList = Directory.GetFiles(_jsonPath);
                        for (var i = 0; i < jsonList.Length; i++)
                        {
                            var file = jsonList[i];
                            if (!Directory.Exists(file))
                            {
                                File.Delete(file);
                            }
                        }
                        Debug.Log("Json已清空");

                        string[] voList = Directory.GetFiles(_voPath);
                        for (var i = 0; i < voList.Length; i++)
                        {
                            var file = voList[i];
                            if (!Directory.Exists(file))
                            {
                                File.Delete(file);
                            }
                        }
                        Debug.Log("vo已清空");

                        string[] jsonUtilList = Directory.GetFiles(_jsonUtilPath);
                        for (var i = 0; i < jsonUtilList.Length; i++)
                        {
                            var file = jsonUtilList[i];
                            if (!Directory.Exists(file))
                            {
                                File.Delete(file);
                            }
                        }
                        Debug.Log("jsonUtil已清空");
                    }
                    catch (Exception e)
                    {
                        Debug.LogError("删除文件文件错误：" + e.Message);
                    }

                    GUIUtility.keyboardControl = 0; // 去输入框的蓝色高亮
                }

                GUILayout.EndHorizontal();
            }

            { 
                GUILayout.BeginHorizontal("HelpBox");
                GUILayout.Space(10);

                GUILayout.Label("搜索:");
                search = EditorGUILayout.TextField("", search, "SearchTextField", GUILayout.MaxWidth(10000));
                GUILayout.Label("", "SearchCancelButtonEmpty");

                GUILayout.EndHorizontal();

                scrollVector2 = GUILayout.BeginScrollView(scrollVector2);
                foreach (var it in _excelList)
                {
                    if (it.Contains(search.ToLower()))
                    {
                        DrawStyleItem(it);
                    }
                }
                GUILayout.EndScrollView();
            }

            // 按钮事件
            {
                GUILayout.BeginHorizontal();

                GUILayout.Space(10);
                // 创建一个普通规格的点击按钮
                if (GUILayout.Button("全部导出", GUILayout.Width(200), GUILayout.Height(30)))
                {
                    ReadExcels();

                    GUIUtility.keyboardControl = 0; // 去输入框的蓝色高亮
                    
                    // 刷新Unity
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                }

                GUILayout.EndHorizontal();
            }
        }

        void OnInspectorUpdate()
        {

        }

        public void CreateOneExcel()
        {
            var tempNum = _excelTempMax + 1;
            string path = _excelPath + _excelTempNamePref + tempNum + ".xlsx";
            FileInfo file = new FileInfo(_excelFormworkFile);
            if (file.Exists)
            {
                file.CopyTo(path, true);
            }

            Debug.Log($"Excel[ {_excelTempNamePref + tempNum + ".xlsx"} ] Create Success!");

            _excelTempMax++;
            _excelList.Add(path);
        }
        
        public void ReadExcels()
        {
            try
            {
                string[] jsonList = Directory.GetFiles(_jsonPath);
                for (var i = 0; i < jsonList.Length; i++)
                {
                    var file = jsonList[i];
                    if (!Directory.Exists(file))
                    {
                        File.Delete(file);
                    }
                }

                string[] voList = Directory.GetFiles(_voPath);
                for (var i = 0; i < voList.Length; i++)
                {
                    var file = voList[i];
                    if (!Directory.Exists(file))
                    {
                        File.Delete(file);
                    }
                }

                string[] jsonUtilList = Directory.GetFiles(_jsonUtilPath);
                for (var i = 0; i < jsonUtilList.Length; i++)
                {
                    var file = jsonUtilList[i];
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

            string[] fileList = Directory.GetFiles(_excelPath);
            for (var i = 0; i < fileList.Length; i++)
            {
                var file = fileList[i];
                var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(file);

                //不是路径
                if (!Directory.Exists(file) && Path.GetExtension(file) == ".xlsx")
                {
                    ReadExcel(file);
                }
            }
        }

        public void ReadExcel(string excel)
        {
            _infoTypeDic.Clear();
            _excelInfoList.Clear();

            using (ExcelPackage package = new ExcelPackage(new FileStream(excel, FileMode.Open)))
            {
                var sheet = package.Workbook.Worksheets[1];

                var needCols = new List<int>();

                // 读取数据
                for (int row = 3, rowCount = sheet.Dimension.End.Row; row <= rowCount; row++)   // 第一列为#判断为废弃行数据
                {
                    var infoDic = new Dictionary<string, string>();
                    if (row == 3)
                    {
                        for (int col = 2, colCount = sheet.Dimension.End.Column; col <= colCount; col++)    // 第一行无视，第二行参数命名，第三行参数类型，第二行单元格为空或第三行单元格开头不为（string、number、bool、array）则判断为废弃列数据
                        {
                            var curCol_Row2 = sheet.GetValue(2, col);     // 当前列第二格单元格，检测是否命名
                            var curCol_Row3 = sheet.GetValue(3, col);     // 当前列第三格单元格，检测是否为（string、number、bool、array）

                            if (curCol_Row2 != null && curCol_Row3 != null)
                            {
                                var str_curCol_Row2 = curCol_Row2.ToString();
                                var str_curCol_Row3 = curCol_Row3.ToString();
                                if (str_curCol_Row2.Length > 0
                                    && (str_curCol_Row3.ToLower() == "string"
                                    || str_curCol_Row3.ToLower() == "number"
                                    || str_curCol_Row3.ToLower() == "bool"
                                    || str_curCol_Row3.ToLower().StartsWith("array")
                                    ))
                                {
                                    infoDic.Add(str_curCol_Row2, str_curCol_Row3);
                                    needCols.Add(col);
                                }
                            }
                        }
                        _infoTypeDic = infoDic;
                    }
                    else
                    {
                        var curRow_Col1 = sheet.GetValue(row, 1);     // 非第3行第一格单元格开始填#为废弃行
                        var curRow_Col2 = sheet.GetValue(row, 2);     // 非第3行第二格单元格（id列）为空则不考虑导出
                        if ((curRow_Col1 != null && curRow_Col1.ToString().StartsWith("#")) 
                            || curRow_Col2 == null 
                            || string.IsNullOrEmpty(curRow_Col2.ToString()))
                        {
                            continue;
                        }

                        for (int i = 0, Count = needCols.Count; i < Count; i++)
                        {
                            var col = needCols[i];
                            var str_curCol_Row2 = sheet.GetValue(2, col).ToString();
                            if (sheet.GetValue(row, col) != null)
                            {
                                infoDic.Add(str_curCol_Row2, sheet.GetValue(row, col).ToString());
                            }
                            else
                            {
                                infoDic.Add(str_curCol_Row2, "");
                            }
                        }
                        _excelInfoList.Add(infoDic);
                    }
                }
            }

            CreatJson(Path.GetFileNameWithoutExtension(excel));
        }

        private string GetTypeStr(string excelType)
        {
            if (excelType == "number")
            {
                return "int";
            }
            return excelType;
        }

        private void CreatJson(string fileName)
        {
            // create json
            var jsonPath = _jsonPath + fileName + ".json";
            FileInfo fileInfo = new FileInfo(jsonPath);
            if (fileInfo.Exists)
            {
                fileInfo.Delete();
            }
            JSONClass obj = new JSONClass();
            JSONArray array = new JSONArray();
            foreach (var rowInfo in _excelInfoList)
            {
                JSONClass data = new JSONClass();
                foreach (var info in rowInfo)
                {
                    var infoType = _infoTypeDic[info.Key].ToLower();
                    if (infoType == "string")
                    {
                        data.Add(info.Key, info.Value);
                    }
                    else if (infoType == "number") {
                        data.Add(info.Key, info.Value);
                    }
                    else if (infoType == "bool")
                    {
                        var boolValue = "false";
                        int value = 0;
                        if (info.Value.ToLower() == "true" || (int.TryParse(info.Value, out value) && value > 0))
                        {
                            boolValue = "true";
                        }
                        data.Add(info.Key, boolValue);
                    }
                    else if (infoType.StartsWith("array,array"))
                    {
                        JSONArray array_1 = new JSONArray();
                        if (info.Value.Length > 0)
                        {
                            var splitTypes = infoType.Split(',');
                            if (splitTypes.Length > 2)
                            {
                                var splits = info.Value.Split(',');
                                for (var i = 0; i < splits.Length; i++)
                                {
                                    JSONArray array_2 = new JSONArray();
                                    var splits2 = splits[i].Split('|');
                                    for (var j = 0; j < splits2.Length; j++)
                                    {
                                        if (splitTypes[2] == "bool")
                                        {
                                            var boolValue = "false";
                                            if (splits2[j].ToLower() == "true" || int.Parse(splits2[j]) > 0)
                                            {
                                                boolValue = "true";
                                            }
                                            array_2.Add(boolValue);
                                        }
                                        else
                                        {
                                            array_2.Add(splits2[j]);
                                        }
                                    }
                                    array_1.Add(array_2);
                                }
                            }
                            else
                            {
                                Debug.LogError($"Excel[{fileName}] Key[{info.Key}] 格式不对，需要: array,array,[类型]");
                            }
                        }
                        data.Add(info.Key, array_1);
                    }
                    else if (infoType.StartsWith("array"))
                    {
                        JSONArray array_1 = new JSONArray();
                        if (info.Value.Length > 0)
                        {
                            var splitTypes = infoType.Split(',');
                            if (splitTypes.Length == 2)
                            {
                                var splits = info.Value.Split(',');
                                for (var i = 0; i < splits.Length; i++)
                                {
                                    if (splitTypes[1] == "bool")
                                    {
                                        var boolValue = "false";
                                        if (splits[i].ToLower() == "true" || int.Parse(splits[i]) > 0)
                                        {
                                            boolValue = "true";
                                        }
                                        array_1.Add(boolValue);
                                    }
                                    else
                                    {
                                        array_1.Add(splits[i]);
                                    }
                                }
                            }
                            else if (splitTypes.Length > 2)
                            {
                                var splits = info.Value.Split(',');
                                for (var i = 0; i < splits.Length; i++)
                                {
                                    JSONArray array_2 = new JSONArray();
                                    var splits2 = splits[i].Split('|');
                                    for (var j = 0; j < splits2.Length; j++)
                                    {
                                        if (splitTypes[2] == "bool")
                                        {
                                            var boolValue = "false";
                                            if (splits2[j].ToLower() == "true" || int.Parse(splits2[j]) > 0)
                                            {
                                                boolValue = "true";
                                            }
                                            array_2.Add(boolValue);
                                        }
                                        else
                                        {
                                            array_2.Add(splits2[j]);
                                        }
                                    }
                                    array_1.Add(array_2);
                                }
                            }
                            else
                            {
                                Debug.LogError($"Excel[{fileName}] Key[{info.Key}] 格式不对，需要: array,[类型],[类型]");
                            }
                        }
                        data.Add(info.Key, array_1);
                    }
                }
                array.Add(data);
            }
            obj.Add("info", array);

            StreamWriter writer = fileInfo.CreateText(); //创建文件
            writer.Write(obj.ToString());
            writer.Flush();
            writer.Dispose();
            writer.Close();

            // A/B Test的表格不需要生成vo文件
            if (fileName != "stageExpand2" 
                && fileName != "dailyStageExpand2"
                && fileName != "dailyChapter2"
                )
            {
                // vo cs
                var voPath = _voPath + fileName + "VO.cs";
                FileInfo voFileInfo = new FileInfo(voPath);
                if (voFileInfo.Exists)
                {
                    voFileInfo.Delete();
                }
                StreamWriter voWriter = voFileInfo.CreateText(); //创建文件
                voWriter.WriteLine("using UnityEngine;");
                voWriter.WriteLine("using System.Collections;");
                voWriter.WriteLine("using System.Collections.Generic;");
                voWriter.WriteLine("");
                voWriter.WriteLine($"public class {fileName}VO");
                voWriter.WriteLine("{");
                foreach (var it in _infoTypeDic)
                {
                    if (it.Value == "string")
                    {
                        voWriter.WriteLine($"    public string {it.Key};");
                    }
                    else if (it.Value == "number")
                    {
                        voWriter.WriteLine($"    public int {it.Key};");
                    }
                    else if (it.Value == "bool")
                    {
                        voWriter.WriteLine($"    public bool {it.Key};");
                    }
                    else if (it.Value.StartsWith("array,array"))
                    {
                        var splitTypes = it.Value.Split(',');
                        if (splitTypes.Length > 3)
                        {
                            voWriter.WriteLine($"    public struct {it.Key}Struct");
                            voWriter.WriteLine("    {");
                            for (int i = 2; i < splitTypes.Length; i++)
                            {
                                voWriter.WriteLine($"        public {GetTypeStr(splitTypes[i])} value{i};");
                            }
                            voWriter.WriteLine("    }");
                            voWriter.WriteLine($"    public List<List<{it.Key}Struct>> {it.Key};");
                        }
                        else if (splitTypes.Length > 2)
                        {
                            voWriter.WriteLine($"    public List<List<{GetTypeStr(splitTypes[2])}>> {it.Key};");
                        }
                        else
                        {
                            Debug.LogError($"Excel[{fileName}] Key[{it.Value}] 格式不对，需要: array,array,[类型]");
                        }
                    }
                    else if (it.Value.StartsWith("array"))
                    {
                        var splitTypes = it.Value.Split(',');
                        if (splitTypes.Length == 2)
                        {
                            voWriter.WriteLine($"    public List<{GetTypeStr(splitTypes[1])}> {it.Key};");
                        }
                        else if (splitTypes.Length > 2)
                        {
                            if (it.Key.ToLower() == "reward")
                            {
                                voWriter.WriteLine($"    public List<Reward> {it.Key};");
                            }
                            else
                            {
                                voWriter.WriteLine($"    public struct {it.Key}Struct");
                                voWriter.WriteLine("    {");
                                for (int i = 1; i < splitTypes.Length; i++)
                                {
                                    voWriter.WriteLine($"        public {GetTypeStr(splitTypes[i])} value{i};");
                                }
                                voWriter.WriteLine("    }");
                                voWriter.WriteLine($"    public List<{it.Key}Struct> {it.Key};");
                            }
                        }
                        else
                        {
                            Debug.LogError($"Excel[{fileName}] Key[{it.Value}] 格式不对，需要: array,[类型],[类型]");
                        }
                    }
                }
                voWriter.WriteLine("}");
                voWriter.Flush();
                voWriter.Dispose();
                voWriter.Close();
            }
            
            // jsonUtil cs
            // A/B Test 不需要生成vo
            var voName = fileName;
            if (fileName == "stageExpand2")
            {
                voName = "stageExpand";
            }
            else if (fileName == "dailyStageExpand2")
            {
                voName = "dailyStageExpand";
            }
            else if (fileName == "dailyChapter2")
            {
                voName = "dailyChapter";
            }

            var jsonUtilPath = _jsonUtilPath + fileName + "JsonUtil.cs";
            FileInfo jsonUtilFileInfo = new FileInfo(jsonUtilPath);
            if (jsonUtilFileInfo.Exists)
            {
                jsonUtilFileInfo.Delete();
            }
            StreamWriter jsonUtilWriter = jsonUtilFileInfo.CreateText(); //创建文件
            jsonUtilWriter.WriteLine("using UnityEngine;");
            jsonUtilWriter.WriteLine("using System.Collections.Generic;");
            jsonUtilWriter.WriteLine("using SimpleJSON;");
            jsonUtilWriter.WriteLine("");
            jsonUtilWriter.WriteLine($"public class {fileName}JsonUtil");
            jsonUtilWriter.WriteLine("{");
            jsonUtilWriter.WriteLine($"    public static Dictionary<{GetTypeStr(_infoTypeDic.First().Value)}, {voName}VO> parse()");
            jsonUtilWriter.WriteLine("    {");
            jsonUtilWriter.WriteLine($"        var dic = new Dictionary<{GetTypeStr(_infoTypeDic.First().Value)}, {voName}VO>();");
            jsonUtilWriter.WriteLine("#if UNITY_EDITOR && !ENABLE_ASSETBUNDLES");
            jsonUtilWriter.WriteLine($"        var textObj = Resources.Load(\"TextAssets/Form/{fileName}\") as TextAsset;");
            jsonUtilWriter.WriteLine("#else");
            jsonUtilWriter.WriteLine($"        var textObj = CacheLoad.Instance().LoadResourceFromAssetBundle<TextAsset>(\"{fileName}\", \"form.bytes\");");
            jsonUtilWriter.WriteLine("#endif");
            jsonUtilWriter.WriteLine("        if (textObj)");
            jsonUtilWriter.WriteLine("        {");
            jsonUtilWriter.WriteLine("            var json = JSON.Parse(textObj.text);");
            jsonUtilWriter.WriteLine("            var infoArr = json[\"info\"];");
            jsonUtilWriter.WriteLine("            var infoArrCount = infoArr.Count;");
            jsonUtilWriter.WriteLine("            for (int r = 0; r < infoArrCount; r++)");
            jsonUtilWriter.WriteLine("            {");
            jsonUtilWriter.WriteLine("                var rowJ = infoArr[r];");
            jsonUtilWriter.WriteLine($"                var vo = new {voName}VO();");

            foreach (var it in _infoTypeDic)
            {
                if (it.Value == "string")
                {
                    jsonUtilWriter.WriteLine($"                vo.{it.Key} = rowJ[\"{it.Key}\"];");
                }
                else if (it.Value == "number")
                {
                    jsonUtilWriter.WriteLine($"                vo.{it.Key} = rowJ[\"{it.Key}\"].AsInt;");
                }
                else if (it.Value == "bool")
                {
                    jsonUtilWriter.WriteLine($"                vo.{it.Key} = rowJ[\"{it.Key}\"].AsBool;");
                }
                else if (it.Value.StartsWith("array,array"))
                {
                    var splitTypes = it.Value.Split(',');
                    if (splitTypes.Length > 3)
                    {
                        jsonUtilWriter.WriteLine($"                vo.{it.Key} = new List<List<{it.Key}Struct>>();");
                        jsonUtilWriter.WriteLine($"                var {it.Key}JsonArr = rowJ[\"{it.Key}\"];");
                        jsonUtilWriter.WriteLine($"                var {it.Key}JsonArrCount = {it.Key}JsonArr.Count;");
                        jsonUtilWriter.WriteLine($"                for (int i = 0; i < {it.Key}JsonArrCount; i++)");
                        jsonUtilWriter.WriteLine("                {");
                        jsonUtilWriter.WriteLine($"                    var tempList = new List<{GetTypeStr(splitTypes[2])})>();");
                        jsonUtilWriter.WriteLine($"                    var {it.Key}JsonArr2 = {it.Key}JsonArr[i];");
                        jsonUtilWriter.WriteLine($"                    var {it.Key}JsonArr2Count = {it.Key}JsonArr2.Count;");
                        jsonUtilWriter.WriteLine($"                    for (int j = 0; j < {it.Key}JsonArr2Count; j++)");
                        jsonUtilWriter.WriteLine("                    {");
                        jsonUtilWriter.WriteLine($"                            var {it.Key}Strc = new {voName}VO.{it.Key}Struct();");
                        for (int i = 1; i < splitTypes.Length; i++)
                        {
                            string asType = "";
                            if (splitTypes[i] == "number")
                            {
                                asType = ".AsInt";
                            }
                            else if (splitTypes[i] == "bool")
                            {
                                asType = ".AsBool";
                            }
                            jsonUtilWriter.WriteLine($"                            {it.Key}Strc.value{i} = {it.Key}JsonArr2[j][{i - 1}]{asType};");
                        }
                        jsonUtilWriter.WriteLine($"                        tempList.Add({it.Key}Strc);");
                        jsonUtilWriter.WriteLine("                    }");
                        jsonUtilWriter.WriteLine($"                    vo.types.Add(tempList);");
                        jsonUtilWriter.WriteLine("                }");
                    }
                    else if (splitTypes.Length > 2)
                    {
                        jsonUtilWriter.WriteLine($"                vo.{it.Key} = new List<List<{GetTypeStr(splitTypes[2])}>>();");
                        jsonUtilWriter.WriteLine($"                var {it.Key}JsonArr = rowJ[\"{it.Key}\"];");
                        jsonUtilWriter.WriteLine($"                var {it.Key}JsonArrCount = {it.Key}JsonArr.Count;");
                        jsonUtilWriter.WriteLine($"                for (int i = 0; i < {it.Key}JsonArrCount; i++)");
                        jsonUtilWriter.WriteLine("                {");
                        jsonUtilWriter.WriteLine($"                    var tempList = new List<{GetTypeStr(splitTypes[2])}>();");
                        jsonUtilWriter.WriteLine($"                    var {it.Key}JsonArr2 = {it.Key}JsonArr[i];");
                        jsonUtilWriter.WriteLine($"                    var {it.Key}JsonArr2Count = {it.Key}JsonArr2.Count;");
                        jsonUtilWriter.WriteLine($"                    for (int j = 0; j < {it.Key}JsonArr2Count; j++)");
                        jsonUtilWriter.WriteLine("                    {");
                        jsonUtilWriter.WriteLine($"                        tempList.Add({it.Key}JsonArr2[j]);");
                        jsonUtilWriter.WriteLine("                    }");
                        jsonUtilWriter.WriteLine($"                    vo.types.Add(tempList);");
                        jsonUtilWriter.WriteLine("                }");
                    }
                    else
                    {
                        Debug.LogError($"Excel[{fileName}] Key[{it.Value}] 格式不对，需要: array,array,[类型]");
                    }
                }
                else if (it.Value.StartsWith("array"))
                {
                    var splitTypes = it.Value.Split(',');
                    if (splitTypes.Length == 2)
                    {
                        jsonUtilWriter.WriteLine($"                vo.{it.Key} = new List<{GetTypeStr(splitTypes[1])}>();");
                        jsonUtilWriter.WriteLine($"                var {it.Key}JsonArr = rowJ[\"{it.Key}\"];");
                        jsonUtilWriter.WriteLine($"                var {it.Key}JsonArrCount = {it.Key}JsonArr.Count;");
                        jsonUtilWriter.WriteLine($"                for (int i = 0; i < {it.Key}JsonArrCount; i++)");
                        jsonUtilWriter.WriteLine("                {");
                        string asType = "";
                        if (splitTypes[1] == "number")
                        {
                            asType = ".AsInt";
                        }
                        else if (splitTypes[1] == "bool")
                        {
                            asType = ".AsBool";
                        }
                        jsonUtilWriter.WriteLine($"                    vo.{it.Key}.Add({it.Key}JsonArr[i]{asType});");
                        jsonUtilWriter.WriteLine("                }");
                    }
                    else if (splitTypes.Length > 2)
                    {
                        if (it.Key.ToLower() == "reward")
                        {
                            if (splitTypes.Length > 3)
                            {
                                Debug.LogError($"Excel[{fileName}] Key[{it.Value}] 表格中出现错误的reward类型，reward需要: array,number,number");
                            }
                            for (int i = 1; i < splitTypes.Length; i++)
                            {
                                if (splitTypes[i] != "number")
                                {
                                    Debug.LogError($"Excel[{fileName}] Key[{it.Value}] 表格中出现错误的reward类型，reward需要: array,number,number");
                                }
                            }
                            jsonUtilWriter.WriteLine($"                vo.{it.Key} = new List<Reward>();");
                            jsonUtilWriter.WriteLine($"                var {it.Key}JsonArr = rowJ[\"{it.Key}\"];");
                            jsonUtilWriter.WriteLine($"                var {it.Key}JsonArrCount = {it.Key}JsonArr.Count;");
                            jsonUtilWriter.WriteLine($"                for (int i = 0; i < {it.Key}JsonArrCount; i++)");
                            jsonUtilWriter.WriteLine("                {");
                            jsonUtilWriter.WriteLine($"                    var {it.Key}Strc = new Reward();");
                            jsonUtilWriter.WriteLine($"                    {it.Key}Strc.id = {it.Key}JsonArr[i][0].AsInt;");
                            jsonUtilWriter.WriteLine($"                    {it.Key}Strc.num = {it.Key}JsonArr[i][1].AsInt;");
                            jsonUtilWriter.WriteLine($"                    vo.{it.Key}.Add({it.Key}Strc);");
                            jsonUtilWriter.WriteLine("                }");
                        }
                        else
                        {
                            jsonUtilWriter.WriteLine($"                vo.{it.Key} = new List<{voName}VO.{it.Key}Struct>();");
                            jsonUtilWriter.WriteLine($"                var {it.Key}JsonArr = rowJ[\"{it.Key}\"];");
                            jsonUtilWriter.WriteLine($"                var {it.Key}JsonArrCount = {it.Key}JsonArr.Count;");
                            jsonUtilWriter.WriteLine($"                for (int i = 0; i < {it.Key}JsonArrCount; i++)");
                            jsonUtilWriter.WriteLine("                {");
                            jsonUtilWriter.WriteLine($"                    var {it.Key}Strc = new {voName}VO.{it.Key}Struct();");
                            for (int i = 1; i < splitTypes.Length; i++)
                            {
                                string asType = "";
                                if (splitTypes[i] == "number")
                                {
                                    asType = ".AsInt";
                                }
                                else if (splitTypes[i] == "bool")
                                {
                                    asType = ".AsBool";
                                }
                                jsonUtilWriter.WriteLine($"                    {it.Key}Strc.value{i} = {it.Key}JsonArr[i][{i - 1}]{asType};");
                            }
                            jsonUtilWriter.WriteLine($"                    vo.{it.Key}.Add({it.Key}Strc);");
                            jsonUtilWriter.WriteLine("                }");
                        }
                    }
                    else
                    {
                        Debug.LogError($"Excel[{fileName}] Key[{it.Value}] 格式不对，需要: array,[类型],[类型]");
                    }
                }
            }

            jsonUtilWriter.WriteLine("                dic.Add(vo.id, vo);");
            jsonUtilWriter.WriteLine("            }");
            jsonUtilWriter.WriteLine("        }");
            jsonUtilWriter.WriteLine("        return dic;");
            jsonUtilWriter.WriteLine("    }");
            jsonUtilWriter.WriteLine("}");
            jsonUtilWriter.Flush();
            jsonUtilWriter.Dispose();
            jsonUtilWriter.Close();

            Debug.Log($"[ {Path.GetFileName(jsonPath)} ] Create Success!");
        }
    }
}
#endif