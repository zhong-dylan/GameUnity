// #if UNITY_EDITOR
//
// using UnityEditor;
// using UnityEngine;
// using System;
// using System.IO;
// using System.Text;
// using System.Collections.Generic;
// using OfficeOpenXml;        
// using OfficeOpenXml.Drawing;        
// using OfficeOpenXml.Drawing.Chart;        
// using OfficeOpenXml.Style;
// using System.Linq;
// using SimpleJSON;
//
// namespace tools
// {
//     public class StageConfigure : EditorWindow
//     {
//         private string _excelFormworkFile = @"Assets/Editor/Formwork/stageFormwork.xlsx";
//         private string _excelPath = @"Assets/Editor/StageExcel/";
//         private string _jsonPath = @"Assets/Resources/TextAssets/Stages/";
//
//         private int _excelStageMax = 0;
//         private string _excelStageNamePref = "stage_";
//         private Dictionary<int, string> _excelStageList;
//         private Dictionary<string, int> _excelStageTilesList;
//         private Dictionary<int, string> _jsonStageList;
//
//         private static int _stageLayerMax = 50;
//         //private static int _stageWith = 9;
//         private static int _stageHight = 11;
//         private Vector2[] _stageSize;
//
//         private Vector2 scrollVector2 = Vector2.zero;
//         private string search = "";
//         private bool check = false;
//
//         #region 编缉器入口
//         [MenuItem("Tools/关卡编辑", false, 200)]
//         public static void Open()
//         {
//             StageConfigure window = EditorWindow.GetWindow<StageConfigure>();
//             window.titleContent = new GUIContent("关卡编辑");
//             window.minSize = new Vector2(800, 600);
//             window.Show();
//         }
//         #endregion
//
//         void OnEnable()
//         {
//             _excelStageList = new Dictionary<int, string>();
//             _excelStageTilesList = new Dictionary<string, int>();
//             _jsonStageList = new Dictionary<int, string>();
//
//             _stageSize = new Vector2[1];
//             _stageSize[0] = new Vector2(7, 10);
//
//             CalExcels();
//             CalJsons();
//         }
//         void OnDisable()
//         {
//             
//         }
//
//         #region OnGUI
//         void OnGUI()
//         {
//             DrawSearchGUI();
//
//             Repaint();
//         }
//         #endregion
//
//         void CalExcels()
//         {
//             _excelStageMax = 0;
//             _excelStageList.Clear();
//
//             string[] fileList = Directory.GetFiles(_excelPath);
//             for (var i = 0; i < fileList.Length; i++)
//             {
//                 var file = fileList[i];
//                 var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(file);
//
//                 //不是路径
//                 if (!Directory.Exists(file) && Path.GetExtension(file) == ".xlsx" && fileNameWithoutExtension.IndexOf(_excelStageNamePref) == 0)
//                 {
//                     var stageNum = int.Parse(fileNameWithoutExtension.Substring(_excelStageNamePref.Length));
//                     _excelStageList.Add(stageNum, file);
//                     if (_excelStageMax < stageNum)
//                     {
//                         _excelStageMax = stageNum;
//                     }
//                 }
//                 else if (Path.GetFileName(file) != ".DS_Store" && Path.GetExtension(file) != ".meta")
//                 {
//                     Debug.LogError($"{_excelPath}: {Path.GetFileName(file)} 命名错误！");
//                 }
//             }
//
//             _excelStageList = _excelStageList.OrderByDescending(o => o.Key).ToDictionary(o => o.Key, p => p.Value);
//         }
//
//         void CalJsons()
//         {
//             _jsonStageList.Clear();
//
//             string[] fileList = Directory.GetFiles(_jsonPath);
//             for (var i = 0; i < fileList.Length; i++)
//             {
//                 var file = fileList[i];
//                 var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(file);
//
//                 //不是路径
//                 if (!Directory.Exists(file) && Path.GetExtension(file) == ".xlsx" && fileNameWithoutExtension.IndexOf(_excelStageNamePref) == 0)
//                 {
//                     var stageNum = int.Parse(fileNameWithoutExtension.Substring(_excelStageNamePref.Length));
//                     _jsonStageList.Add(stageNum, file);
//                 }
//                 else if (Path.GetFileName(file) != ".DS_Store" && Path.GetExtension(file) != ".meta")
//                 {
//                     //Debug.LogError($"{_excelPath}: {Path.GetFileName(file)} 命名错误！");
//                 }
//             }
//         }
//
//         void DrawStyleItem(string path)
//         {
//             GUILayout.BeginHorizontal("box");
//             GUILayout.Space(10);
//
//             EditorGUILayout.SelectableLabel(Path.GetFileNameWithoutExtension(path), GUILayout.Height(20));
//             if (_excelStageTilesList.ContainsKey(path))
//             {
//                 EditorGUILayout.SelectableLabel((_excelStageTilesList[path] % 3 != 0 ? "(✖)" : "") + $"总块数：{_excelStageTilesList[path]}(余{_excelStageTilesList[path]%3})", GUILayout.Height(20));
//             }
//             if (GUILayout.Button("打开", GUILayout.Width(60), GUILayout.Height(20)))
//             {
//                 System.Diagnostics.Process.Start(path);
//
//                 GUIUtility.keyboardControl = 0; // 去输入框的蓝色高亮
//             }
//             if (GUILayout.Button("导json", GUILayout.Width(60), GUILayout.Height(20)))
//             {
//                 ReadExcel(path);
//                 CalJsons();
//
//                 GUIUtility.keyboardControl = 0; // 去输入框的蓝色高亮
//             }
//             if (GUILayout.Button("计算块", GUILayout.Width(60), GUILayout.Height(20)))
//             {
//                 CalExcelTile(path);
//
//                 GUIUtility.keyboardControl = 0; // 去输入框的蓝色高亮
//             }
//             GUILayout.EndHorizontal();
//         }
//
//         void DrawSearchGUI()
//         {
//             // 按钮事件
//             {
//                 GUILayout.BeginHorizontal();
//
//                 GUILayout.Space(10);
//                 // 创建一个普通规格的点击按钮
//                 if (GUILayout.Button("新建一张关卡表(底为7x10)", GUILayout.Width(200), GUILayout.Height(30)))
//                 {
//                     // 实现
//                     CreateOneExcel();
//
//                     GUIUtility.keyboardControl = 0; // 去输入框的蓝色高亮
//                 }
//
//
//                 GUILayout.Space(340);
//
//                 if (GUILayout.Button("刷新", GUILayout.Width(100), GUILayout.Height(30)))
//                 {
//                     CalExcels();
//                     CalJsons();
//
//                     GUIUtility.keyboardControl = 0; // 去输入框的蓝色高亮
//                 }
//
//                 GUILayout.Space(20);
//
//                 if (GUILayout.Button("计算块", GUILayout.Width(100), GUILayout.Height(30)))
//                 {
//                     CalExcelTiles();
//
//                     GUIUtility.keyboardControl = 0; // 去输入框的蓝色高亮
//                 }
//
//                 GUILayout.EndHorizontal();
//             }
//
//             { 
//                 GUILayout.BeginHorizontal("HelpBox");
//                 GUILayout.Space(10);
//
//                 GUILayout.Label("搜索:");
//                 search = EditorGUILayout.TextField("", search, "SearchTextField", GUILayout.MaxWidth(10000));
//                 GUILayout.Label("", "SearchCancelButtonEmpty");
//
//                 GUILayout.Space(50);
//                 GUILayout.Label("筛选计算块后数量不对的Excel:");
//                 check = EditorGUILayout.Toggle(check);
//                 GUILayout.Space(10);
//
//                 GUILayout.EndHorizontal();
//
//                 scrollVector2 = GUILayout.BeginScrollView(scrollVector2);
//                 foreach (var it in _excelStageList)
//                 {
//                     if (it.Value.Contains(search.ToLower()))
//                     {
//                         if (check && _excelStageTilesList.ContainsKey(it.Value) && _excelStageTilesList[it.Value] % 3 != 0)
//                         {
//                             DrawStyleItem(it.Value);
//                         }
//                         else if (!check)
//                         {
//                             DrawStyleItem(it.Value);
//                         }
//                     }
//                 }
//                 GUILayout.EndScrollView();
//             }
//
//             // 按钮事件
//             {
//                 GUILayout.BeginHorizontal();
//
//                 GUILayout.Space(10);
//                 // 创建一个普通规格的点击按钮
//                 if (GUILayout.Button("全部导出Json", GUILayout.Width(200), GUILayout.Height(30)))
//                 {
//                     ReadExcels();
//                     CalJsons();
//
//                     GUIUtility.keyboardControl = 0; // 去输入框的蓝色高亮
//                 }
//
//                 GUILayout.EndHorizontal();
//             }
//         }
//
//         void OnInspectorUpdate()
//         {
//
//         }
//
//         public void CreateOneExcel()
//         {
//             var stageNum = _excelStageMax + 1;
//             string path = _excelPath + _excelStageNamePref + stageNum + ".xlsx";
//             FileInfo file = new FileInfo(_excelFormworkFile);
//             if (file.Exists)
//             {
//                 file.CopyTo(path, true);
//             }
//
//             Debug.Log($"Excel[ {_excelStageNamePref + stageNum + ".xlsx"} ] Create Success!");
//
//             _excelStageMax++;
//             _excelStageList.Add(_excelStageMax, path);
//             _excelStageList = _excelStageList.OrderByDescending(o => o.Key).ToDictionary(o => o.Key, p => p.Value);
//         }
//
//         void CalExcelTiles()
//         {
//             string[] fileList = Directory.GetFiles(_excelPath);
//             for (var i = 0; i < fileList.Length; i++)
//             {
//                 var file = fileList[i];
//                 var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(file);
//
//                 //不是路径
//                 if (!Directory.Exists(file) && Path.GetExtension(file) == ".xlsx" && fileNameWithoutExtension.IndexOf(_excelStageNamePref) == 0)
//                 {
//                     CalExcelTile(file);
//                 }
//             }
//         }
//
//         void CalExcelTile(string excel)
//         {
//             //_excelStageTilesList
//             int tileCount = 0;
//             using (ExcelPackage package = new ExcelPackage(new FileStream(excel, FileMode.Open)))
//             {
//                 var sheet = package.Workbook.Worksheets[1];
//                 for (int i = 0; i < _stageLayerMax; i++)
//                 {
//                     for (int row = 1; row <= _stageHight; row++)
//                     {
//                         int trueRow = _stageHight * i + row;
//                         if (row == 1)
//                         {
//                             continue;
//                         }
//                         else
//                         {
//                             //7x10
//                             for (int col = 2; col < (int)_stageSize[0].x + 2; col++)
//                             {
//                                 var tile = sheet.GetValue(trueRow, col);
//                                 if (tile != null)
//                                 {
//                                     var tileStr = tile.ToString();
//                                     if (tileStr.StartsWith("1"))
//                                     {
//                                         tileCount++;
//                                     }
//                                     else if (tileStr.StartsWith("2"))
//                                     {
//                                         var splits = tileStr.Split(',');
//                                         if (splits.Length == 3 || splits.Length == 4)
//                                         {
//                                             tileCount++;
//                                             for (int n = 1; n < int.Parse(splits[splits.Length-1]); n++)
//                                             {
//                                                 Vector3 tempVec3 = Vector3.zero;
//                                                 switch (splits[splits.Length - 2])
//                                                 {
//                                                     case "↑":
//                                                         break;
//                                                     case "←":
//                                                         break;
//                                                     case "↓":
//                                                         break;
//                                                     case "→":
//                                                         break;
//                                                     default:
//                                                         continue;
//                                                 }
//                                                 tileCount++;
//                                             }
//                                         }
//                                         else
//                                         {
//                                             Debug.LogError($"Excel[ { Path.GetFileName(excel)} ] 有重叠块，但重叠块格式不对!");
//                                         }
//                                     }
//                                 }
//                             }
//
//                             _excelStageTilesList[excel] = tileCount;
//                         }
//                     }
//                 }
//             }
//         }
//
//         public void ReadExcels()
//         {
//             try
//             {
//                 string[] jsonList = Directory.GetFiles(_jsonPath);
//                 for (var i = 0; i < jsonList.Length; i++)
//                 {
//                     var file = jsonList[i];
//                     if (!Directory.Exists(file))
//                     {
//                         File.Delete(file);
//                     }
//                 }
//             }
//             catch (Exception e)
//             {
//                 Debug.LogError("删除文件夹文件错误：" + e.Message);
//             }
//
//             string[] fileList = Directory.GetFiles(_excelPath);
//             for (var i = 0; i < fileList.Length; i++)
//             {
//                 var file = fileList[i];
//                 var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(file);
//
//                 //不是路径
//                 if (!Directory.Exists(file) && Path.GetExtension(file) == ".xlsx" && fileNameWithoutExtension.IndexOf(_excelStageNamePref) == 0)
//                 {
//                     ReadExcel(file);
//                 }
//             }
//         }
//
//         public void ReadExcel(string excel)
//         {
//             StageVO vo = new StageVO();
//             //vo.vo_stageName = "";
//             //vo.vo_stageTileTypes = new List<int>();
//             vo.vo_allPanel = new Vector3(0, 0, 0);
//             vo.vo_onePanels = new List<StageVO.OnePanel>();
//
//             using (ExcelPackage package = new ExcelPackage(new FileStream(excel, FileMode.Open)))
//             {
//                 var sheet = package.Workbook.Worksheets[1];
//                 // name
//                 //if (sheet.GetValue(1, 1) != null && sheet.GetValue(1, 1).ToString().Length > 0)
//                 //{
//                 //    vo.vo_stageName = sheet.GetValue(1, 1).ToString();
//                 //}
//
//                 // types
//                 //if (sheet.GetValue(1, 2) != null && sheet.GetValue(1, 2).ToString().Length > 0)
//                 //{
//                 //    var types = sheet.GetValue(1, 2).ToString();
//                 //    var splits = types.Split(',');
//                 //    if (splits.Length <= 1)
//                 //    {
//                 //        Debug.LogError($"Excel[ { Path.GetFileName(excel)} ] [块种类型] 配置不正确，按1种块继续!");
//                 //        vo.vo_stageTileTypes.Add(1);
//                 //    }
//                 //    else
//                 //    {
//                 //        for (int i = 0; i < splits.Length; i++)
//                 //        {
//                 //            vo.vo_stageTileTypes.Add(int.Parse(splits[i]));
//                 //        }
//                 //    }
//                 //}
//
//                 for (int i = 0; i < _stageLayerMax; i++)
//                 {
//                     var tilesList = new List<StageVO.OneTile>();
//                     for (int row = 1; row <= _stageHight; row++)
//                     {
//                         int trueRow = _stageHight * i + row;
//                         if (row == 1)
//                         {
//                             continue;
//                         }
//                         else
//                         {
//                             //7x10
//                             for (int col = 2; col < (int)_stageSize[0].x + 2; col++)
//                             {
//                                 var tile = sheet.GetValue(trueRow, col);
//                                 if (tile != null)
//                                 {
//                                     var tileStr = tile.ToString();
//                                     if (tileStr.StartsWith("1"))
//                                     {
//                                         var splits = tileStr.Split(',');
//                                         if (splits.Length == 1)
//                                         {
//                                             Vector3 vec3 = new Vector3(((float)col - 2.0f) * 100.0f, (_stageSize[0].y - (float)row + 1.0f) * 100.0f, 0);
//                                             StageVO.OneTile oneTile;
//                                             oneTile.pos = vec3;
//                                             oneTile.animType = 0;
//                                             tilesList.Add(oneTile);
//                                         }
//                                         else if (splits.Length == 2)
//                                         {
//                                             Vector3 vec3 = new Vector3(((float)col - 2.0f) * 100.0f, (_stageSize[0].y - (float)row + 1.0f) * 100.0f, 0);
//                                             switch (splits[1])
//                                             {
//                                                 case "↑":
//                                                     vec3 = new Vector3(vec3.x, vec3.y + 50.0f, 0);
//                                                     break;
//                                                 case "←":
//                                                     vec3 = new Vector3(vec3.x - 50.0f, vec3.y, 0);
//                                                     break;
//                                                 case "↓":
//                                                     vec3 = new Vector3(vec3.x, vec3.y - 50.0f, 0);
//                                                     break;
//                                                 case "→":
//                                                     vec3 = new Vector3(vec3.x + 50.0f, vec3.y, 0);
//                                                     break;
//                                                 case "↖":
//                                                     vec3 = new Vector3(vec3.x - 50.0f, vec3.y + 50.0f, 0);
//                                                     break;
//                                                 case "↗":
//                                                     vec3 = new Vector3(vec3.x + 50.0f, vec3.y + 50.0f, 0);
//                                                     break;
//                                                 case "↙":
//                                                     vec3 = new Vector3(vec3.x - 50.0f, vec3.y - 50.0f, 0);
//                                                     break;
//                                                 case "↘":
//                                                     vec3 = new Vector3(vec3.x + 50.0f, vec3.y - 50.0f, 0);
//                                                     break;
//                                                 default:
//                                                     Debug.LogError($"Excel[ { Path.GetFileName(excel)} ] [行{trueRow},列{col}] 有普通块，方向参数配置错误! 按无位移生成。");
//                                                     break;
//                                             }
//                                             StageVO.OneTile oneTile;
//                                             oneTile.pos = vec3;
//                                             oneTile.animType = 0;
//                                             tilesList.Add(oneTile);
//                                         }
//                                         else
//                                         {
//                                             Debug.LogError($"Excel[ { Path.GetFileName(excel)} ] [行{trueRow},列{col}] 有普通块，但普通块格式不对!");
//                                         }
//                                     }
//                                     else if (tileStr.StartsWith("2"))
//                                     {
//                                         var splits = tileStr.Split(',');
//                                         if (splits.Length == 3 || splits.Length == 4)
//                                         {
//                                             Vector3 vec3 = new Vector3(((float)col - 2.0f) * 100.0f, (_stageSize[0].y - (float)row + 1.0f) * 100.0f, 0);
//                                             if (splits.Length == 4)
//                                             {
//                                                 switch (splits[1])
//                                                 {
//                                                     case "↑":
//                                                         vec3 = new Vector3(vec3.x, vec3.y + 50.0f, 0);
//                                                         break;
//                                                     case "←":
//                                                         vec3 = new Vector3(vec3.x - 50.0f, vec3.y, 0);
//                                                         break;
//                                                     case "↓":
//                                                         vec3 = new Vector3(vec3.x, vec3.y - 50.0f, 0);
//                                                         break;
//                                                     case "→":
//                                                         vec3 = new Vector3(vec3.x + 50.0f, vec3.y, 0);
//                                                         break;
//                                                     case "↖":
//                                                         vec3 = new Vector3(vec3.x - 50.0f, vec3.y + 50.0f, 0);
//                                                         break;
//                                                     case "↗":
//                                                         vec3 = new Vector3(vec3.x + 50.0f, vec3.y + 50.0f, 0);
//                                                         break;
//                                                     case "↙":
//                                                         vec3 = new Vector3(vec3.x - 50.0f, vec3.y - 50.0f, 0);
//                                                         break;
//                                                     case "↘":
//                                                         vec3 = new Vector3(vec3.x + 50.0f, vec3.y - 50.0f, 0);
//                                                         break;
//                                                     default:
//                                                         Debug.LogError($"Excel[ { Path.GetFileName(excel)} ] [行{trueRow},列{col}] 有重叠块，方向参数配置错误! 按无位移生成。");
//                                                         break;
//                                                 }
//                                             }
//                                             StageVO.OneTile oneTile;
//                                             oneTile.pos = vec3;
//                                             oneTile.animType = 0 == (int.Parse(splits[splits.Length - 1]) - 1) ? 2 : 1;
//                                             tilesList.Add(oneTile);
//
//                                             for (int n = 1; n < int.Parse(splits[splits.Length-1]); n++)
//                                             {
//                                                 Vector3 tempVec3 = Vector3.zero;
//                                                 switch (splits[splits.Length-2])
//                                                 {
//                                                     case "↑":
//                                                         tempVec3 = new Vector3(vec3.x, vec3.y + 10.0f * n, 0);
//                                                         break;
//                                                     case "←":
//                                                         tempVec3 = new Vector3(vec3.x - 10.0f * n, vec3.y, 0);
//                                                         break;
//                                                     case "↓":
//                                                         tempVec3 = new Vector3(vec3.x, vec3.y - 10.0f * n, 0);
//                                                         break;
//                                                     case "→":
//                                                         tempVec3 = new Vector3(vec3.x + 10.0f * n, vec3.y, 0);
//                                                         break;
//                                                     default:
//                                                         continue;
//                                                 }
//
//                                                 int oneNum = i + n;
//                                                 StageVO.OnePanel tempOne = vo.vo_onePanels.Count > oneNum ? vo.vo_onePanels[oneNum] : new StageVO.OnePanel();
//                                                 if (vo.vo_onePanels.Count <= oneNum)
//                                                 {
//                                                     tempOne.vo_panel = new Vector3(0, 0, -100 * oneNum);
//                                                     tempOne.vo_tiles = new List<StageVO.OneTile>();
//                                                     for (int ii = vo.vo_onePanels.Count; ii < oneNum; ii++)
//                                                     {
//                                                         StageVO.OnePanel ttOne = new StageVO.OnePanel();
//                                                         ttOne.vo_panel = new Vector3(0, 0, -100 * oneNum);
//                                                         ttOne.vo_tiles = new List<StageVO.OneTile>();
//                                                         vo.vo_onePanels.Add(ttOne);
//                                                     }
//                                                     vo.vo_onePanels.Add(tempOne);
//                                                 }
//                                                 StageVO.OneTile oneTile2;
//                                                 oneTile2.pos = tempVec3;
//                                                 oneTile2.animType = n == (int.Parse(splits[splits.Length - 1]) - 1) ? 2 : 1;
//                                                 tempOne.vo_tiles.Add(oneTile2);
//                                             }
//                                         }
//                                         else
//                                         {
//                                             Debug.LogError($"Excel[ { Path.GetFileName(excel)} ] [行{trueRow},列{col}] 有重叠块，但重叠块格式不对!");
//                                         }
//                                     }
//                                 }
//                             }
//                         }
//                     }
//
//                     if (tilesList.Count > 0)
//                     {
//                         StageVO.OnePanel one = vo.vo_onePanels.Count > i ? vo.vo_onePanels[i] : new StageVO.OnePanel();
//                         if (vo.vo_onePanels.Count <= i)
//                         {
//                             one.vo_panel = new Vector3(0, 0, -100 * i);
//                             one.vo_tiles = new List<StageVO.OneTile>();
//                             for (int ii = vo.vo_onePanels.Count; ii < i; ii++)
//                             {
//                                 StageVO.OnePanel ttOne = new StageVO.OnePanel();
//                                 ttOne.vo_panel = new Vector3(0, 0, -100 * i);
//                                 ttOne.vo_tiles = new List<StageVO.OneTile>();
//                                 vo.vo_onePanels.Add(ttOne);
//                             }
//                             vo.vo_onePanels.Add(one);
//                         }
//                         one.vo_tiles.AddRange(tilesList);
//                     }
//                 }
//             }
//             
//
//             if (vo.vo_onePanels.Count > 0)
//             {
//                 CreatJson(vo, Path.GetFileNameWithoutExtension(excel));
//             }
//             else
//             {
//                 Debug.LogError($"Excel[ {Path.GetFileName(excel)} ] no data!");
//             }
//         }
//
//         private JSONArray Vector3ToArray(Vector3 vec3, int animType = 0)
//         {
//             JSONArray array = new JSONArray();
//             array.Add(vec3.x.ToString());
//             array.Add(vec3.y.ToString());
//             array.Add(vec3.z.ToString());
//             if (animType > 0)
//             {
//                 array.Add(animType.ToString());
//             }
//             return array;
//         }
//
//         private void CreatJson(StageVO vo, string fileName)
//         {
//             JSONClass obj = new JSONClass();
//
//             // panel
//             obj.Add("panel", Vector3ToArray(vo.vo_allPanel));
//
//             // tiles
//             JSONArray array = new JSONArray();
//             for (int i = 0; i < vo.vo_onePanels.Count; i++)
//             {
//                 var one = vo.vo_onePanels[i];
//                 JSONClass data = new JSONClass();
//                 data.Add("panel", Vector3ToArray(one.vo_panel));
//
//                 JSONArray tileArray = new JSONArray();
//                 for (int j = 0; j < one.vo_tiles.Count; j++)
//                 {
//                     tileArray.Add(Vector3ToArray(one.vo_tiles[j].pos, one.vo_tiles[j].animType));
//                 }
//                 data.Add("tile", tileArray);
//
//                 array.Add(data);
//             }
//             obj.Add("layer", array);
//
//             var jsonPath = _jsonPath + fileName + ".json";
//             FileInfo fileInfo = new FileInfo(jsonPath);
//             if (fileInfo.Exists) { 
//                 fileInfo.Delete();
//             }
//
//             StreamWriter writer = fileInfo.CreateText(); //创建文件
//             writer.Write(obj.ToString());
//             writer.Flush();
//             writer.Dispose();
//             writer.Close();
//
//             Debug.Log($"Json[ {Path.GetFileName(jsonPath)} ] Create Success!");
//         }
//     }
// }
// #endif