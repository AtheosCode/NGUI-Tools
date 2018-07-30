using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class ReplaceAtlas : EditorWindow
{
    #region 新图集
    public Object newFile;
    public bool newFoldout = true;
    public Vector2 newScrollPos;
    private Dictionary<UIAtlas, bool> newAtlasDic = new Dictionary<UIAtlas, bool>();
    private List<UIAtlas> newAtlasList = new List<UIAtlas>();
    #endregion 新图集
    #region UI
    public Object uiFile;
    public bool uiFoldout = true;
    public Vector2 uiScrollPos;
    private Dictionary<GameObject, bool> uiAtlasDic = new Dictionary<GameObject, bool>();
    private List<GameObject> uiAtlasList = new List<GameObject>();
    #endregion UI
    #region result
    private Vector2 resultScrollPos;
    private List<string> resultList = new List<string>();
    #endregion result

    //public bool choose;
    #region 查找依赖
    private Dictionary<string, int> depentDic = new Dictionary<string, int>();
    private Vector2 dependScrollPos;
    private List<GameObject> dependList = new List<GameObject>();
    private Object dependFile;
    private string checkPath = "GameList";
    #endregion
    [MenuItem("Atheos/ReplaceAtlas")]
    public static void Repalce()
    {
        EditorWindow.GetWindow<ReplaceAtlas>(false, "Replace Atlas", true).Show();
    }
    public void OnGUI()
    {
        ChooseComponent(ref newFile, ref newAtlasList, ref newAtlasDic, ref newScrollPos, ref newFoldout, "新的图集目录");
        ChooseObject<GameObject>(ref uiFile, ref uiAtlasList, "需要被替换的UI文件夹");
        ShowTypeList(ref uiFoldout, ref uiScrollPos, ref uiAtlasList, ref uiAtlasDic);
        ChangeAtlasBySpriteName(uiAtlasDic, newAtlasDic, ref resultList);

        ChooseObject<GameObject>(ref dependFile, ref dependList, "需要查找引用的文件夹");
        if (dependFile)
        {
            checkPath = GUILayout.TextField(checkPath);
            ShowDependList(checkPath, dependList, ref depentDic);
        }
    }
    private void ChooseComponent<T>(ref Object file, ref List<T> atlasList, ref Dictionary<T, bool> atlasDic, ref Vector2 scrollPos, ref bool foldout, string tips = "目录文件") where T : Component
    {
        #region 根据选中的文件夹 找到目录下所有图集
        Object[] selections = Selection.GetFiltered(typeof(Object), SelectionMode.Assets);
        GUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(tips, GUILayout.MaxWidth(200));
        file = EditorGUILayout.ObjectField(file, typeof(Object), false, GUILayout.MinWidth(80f));
        GUILayout.EndHorizontal();
        EditorGUILayout.Space();
        if (file != null)
        {
            string path = AssetDatabase.GetAssetPath(file);
            atlasList.Clear();
            if (Directory.Exists(path))//文件夹
            {
                string[] filePaths = Directory.GetFiles(path, "*.prefab", SearchOption.AllDirectories);
                for (int i = 0; i < filePaths.Length; i++)
                {
                    string truePath = filePaths[i].Replace('\\', '/');
                    GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(truePath);
                    if (prefab != null)
                    {
                        T[] uiAtlas = prefab.GetComponentsInChildren<T>(true);
                        if (uiAtlas != null)
                        {
                            for (int j = 0; j < uiAtlas.Length; j++)
                            {
                                if (!atlasList.Contains(uiAtlas[j]))
                                {
                                    atlasList.Add(uiAtlas[j]);
                                }
                            }
                        }
                    }
                }
            }
            else if (File.Exists(path))//文件
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab != null)
                {
                    T[] uiAtlas = prefab.GetComponentsInChildren<T>(true);
                    if (uiAtlas != null)
                    {
                        for (int j = 0; j < uiAtlas.Length; j++)
                        {
                            if (!atlasList.Contains(uiAtlas[j]))
                            {
                                atlasList.Add(uiAtlas[j]);
                            }
                        }
                    }
                }
            }
            else
            {
                Debug.Log("无效的文件路径：" + path);
                return;
            }
            #region 生成图集字典
            #region 清理残余内容
            List<T> tempList = new List<T>();
            foreach (var item in atlasDic)
            {
                tempList.Add(item.Key);
            }
            for (int i = 0; i < tempList.Count; i++)
            {
                if (!atlasList.Contains(tempList[i]))
                {
                    atlasDic.Remove(tempList[i]);
                }
            }
            #endregion 清理残余内容
            for (int i = 0; i < atlasList.Count; i++)
            {
                if (!atlasDic.ContainsKey(atlasList[i]))
                {
                    atlasDic.Add(atlasList[i], true);
                }
            }
            #endregion 生成图集字典

            foldout = EditorGUILayout.Foldout(foldout, "文件显示");
            if (foldout)
            {
                bool isScroll = false;
                if (atlasList.Count > 5)
                {
                    isScroll = true;
                }
                if (isScroll)
                {
                    scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(150));
                }
                else
                {
                    EditorGUILayout.Space();
                }
                GUILayout.Label("当前目录下所有图集：");
                for (int i = 0; i < atlasList.Count; i++)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(i + "、  路径：" + AssetDatabase.GetAssetPath(atlasList[i]));
                    GUILayout.FlexibleSpace();
                    GUILayout.Label("名称：" + atlasList[i].name);
                    GUILayout.FlexibleSpace();
                    atlasDic[atlasList[i]] = EditorGUILayout.ToggleLeft("选择", atlasDic[atlasList[i]], GUILayout.MinWidth(80));
                    GUILayout.EndHorizontal();
                }
                if (isScroll)
                {
                    EditorGUILayout.EndScrollView();
                }
                else
                {
                    EditorGUILayout.Space();
                }
            }
        }
        #endregion 根据选中的文件夹 找到目录下所有图集
    }
    private void ChooseObject<T>(ref Object file, ref List<T> objectList, string tips = "目录文件") where T : Object
    {
        #region 根据选中的文件夹 找到目录下所有图集
        GUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(tips, GUILayout.MaxWidth(200));
        file = EditorGUILayout.ObjectField(file, typeof(Object), false, GUILayout.MinWidth(80f));
        GUILayout.EndHorizontal();
        EditorGUILayout.Space();
        objectList.Clear();
        if (file != null)
        {
            string path = AssetDatabase.GetAssetPath(file);
            if (Directory.Exists(path))//文件夹
            {
                //利用C#提供的API 遍历当前目录下所有文件
                string[] filePaths = Directory.GetFiles(path, "*.prefab", SearchOption.AllDirectories);
                for (int i = 0; i < filePaths.Length; i++)
                {
                    string realPath = filePaths[i].Replace('\\', '/');
                    //T prefab = AssetDatabase.LoadAssetAtPath<T>(realPath, typeof(T));
                    T prefab = AssetDatabase.LoadAssetAtPath<T>(realPath);

                    if (prefab != null)
                    {
                        if (!objectList.Contains(prefab))
                        {
                            objectList.Add(prefab);
                        }
                    }
                }
            }
            else if (File.Exists(path))//文件
            {
                T prefab = AssetDatabase.LoadAssetAtPath<T>(path);
                if (prefab != null)
                {
                    if (!objectList.Contains(prefab))
                    {
                        objectList.Add(prefab);
                    }
                }
            }else{
                GUILayout.Label("错误的文件路径"+path);
            }
        }
        #endregion 根据选中的文件夹 找到目录下所有图集
    }/// <summary>
     /// 展示选中的文件或文件夹下所有prefab上面挂载的类型T
     /// </summary>
     /// <typeparam name="T"></typeparam>
     /// <param name="foldout"></param>
     /// <param name="scrollPos"></param>
     /// <param name="atlasList"></param>
     /// <param name="atlasDic"></param>
    private void ShowTypeList<T>(ref bool foldout, ref Vector2 scrollPos, ref List<T> atlasList, ref Dictionary<T, bool> atlasDic) where T : Object
    {
        if (atlasList.Count > 0)
        {
            #region 生成图集字典
            #region 清理残余内容
            List<T> tempList = new List<T>();
            foreach (var item in atlasDic)
            {
                tempList.Add(item.Key);
            }
            for (int i = 0; i < tempList.Count; i++)
            {
                if (!atlasList.Contains(tempList[i]))
                {
                    atlasDic.Remove(tempList[i]);
                }
            }
            #endregion 清理残余内容 
            for (int i = 0; i < atlasList.Count; i++)
            {
                if (!atlasDic.ContainsKey(atlasList[i]))
                {
                    atlasDic.Add(atlasList[i], true);
                }
            }
            #endregion 生成图集字典 
            foldout = EditorGUILayout.Foldout(foldout, "文件显示");
            if (foldout)
            {
                bool isScroll = false;
                if (atlasList.Count > 5)
                {
                    isScroll = true;
                }
                if (isScroll)
                {
                    scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(150));
                }
                else
                {
                    EditorGUILayout.Space();
                }
                GUILayout.Label("当前目录下所有" + typeof(T).Name + "：");
                for (int i = 0; i < atlasList.Count; i++)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(i + "、  路径：" + AssetDatabase.GetAssetPath(atlasList[i]));
                    GUILayout.FlexibleSpace();
                    if (atlasList[i] is GameObject)
                    {
                        GameObject temp = atlasList[i] as GameObject;
                        GUILayout.Label("名称：" + temp.gameObject.name);
                    }
                    else if (atlasList[i] is Component)
                    {
                        Component temp = atlasList[i] as Component;
                        GUILayout.Label("名称：" + temp.name);
                    }
                    else
                    {
                        GUILayout.Label("名称：" + atlasList[i].name);
                    }
                    GUILayout.FlexibleSpace();
                    atlasDic[atlasList[i]] = EditorGUILayout.ToggleLeft("选择", atlasDic[atlasList[i]], GUILayout.MinWidth(80));
                    GUILayout.EndHorizontal();
                }
                if (isScroll)
                {
                    EditorGUILayout.EndScrollView();
                }
                else
                {
                    EditorGUILayout.Space();
                }
            }
        }
    }
    private Dictionary<string, int> GetDependList(List<GameObject> fileList)
    {
        Dictionary<string, int> countDic = new Dictionary<string, int>();
        foreach (var item in fileList)
        {
            var truePath = AssetDatabase.GetAssetPath(item);
            var dependPaths = AssetDatabase.GetDependencies(new string[] { truePath });
            foreach (var path in dependPaths)
            {
                if (countDic.ContainsKey(path))
                {
                    countDic[path] += 1;
                }
                else
                {
                    countDic.Add(path, 1);
                }
            }
        }
        return countDic;
    }
    /// <summary>
    /// 查找引用
    /// </summary>
    /// <param name="objectList"></param>
    /// <param name="resultDic"></param>
    private void ShowDependList(string filePath, List<GameObject> objectList, ref Dictionary<string, int> resultDic)
    {
        if (GUILayout.Button("查找引用", GUILayout.Width(80)))
        {
            resultDic = GetDependList(objectList);
        }
        dependScrollPos = EditorGUILayout.BeginScrollView(dependScrollPos, GUILayout.Height(500));
        GUIStyle style = new GUIStyle();
        style.normal.textColor = Color.red;
        foreach (var item in resultDic)
        {
            if (item.Key.Contains(filePath))
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("名称：" + item.Key, style);
                GUILayout.FlexibleSpace();
                GUILayout.Label("引用计数：" + item.Value, style);
                GUILayout.EndHorizontal();
            }
            else
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("名称：" + item.Key);
                GUILayout.FlexibleSpace();
                GUILayout.Label("引用计数：" + item.Value);
                GUILayout.EndHorizontal();
            }
        }
        EditorGUILayout.EndScrollView();
    }
    /// <summary>
    /// 根据新的图集，替换老的prefab中的引用
    /// </summary>
    /// <param name="objectDic"></param>
    /// <param name="atlasDic"></param>
    /// <param name="resultList"></param>
    private void ChangeAtlasBySpriteName(Dictionary<GameObject, bool> objectDic, Dictionary<UIAtlas, bool> atlasDic, ref List<string> resultList)
    {
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("确认替换", GUILayout.Width(80)))
        {
            resultList.Clear();
            foreach (var item in objectDic)
            {
                if (item.Key != null)
                {
                    UISprite[] sprite = item.Key.GetComponentsInChildren<UISprite>(true);
                    for (int j = 0; j < sprite.Length; j++)
                    {
                        foreach (var atlasItem in atlasDic)
                        {
                            if (item.Value == true)
                            {
                                BetterList<string> spriteNameList = atlasItem.Key.GetListOfSprites();
                                foreach (var sp in spriteNameList)
                                {
                                    if (sprite[j].spriteName == sp)
                                    {
                                        if (sprite[j].atlas != atlasItem.Key)
                                        {
                                            sprite[j].atlas = atlasItem.Key;
                                            string treeName = sprite[j].name;
                                            Transform tempTransform = sprite[j].transform;
                                            while (tempTransform.parent != null)
                                            {
                                                treeName = tempTransform.parent.name + "/" + treeName;
                                                tempTransform = tempTransform.parent;
                                            }
                                            resultList.Add("替换成功的物体：" + AssetDatabase.GetAssetPath(sprite[j]) + "  节点路径" + treeName + "      " + "替换成功的图片名字：" + sprite[j].spriteName);
                                        }
                                    }
                                }
                                EditorUtility.SetDirty(item.Key);
                            }
                        }
                    }
                }
            }
        }
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("清理结果", GUILayout.Width(80)))
        {
            resultList.Clear();
        }
        GUILayout.EndHorizontal();
        #region 结果显示
        GUILayout.BeginVertical();
        bool isScroll = false;
        if (resultList.Count > 5)
        {
            isScroll = true;
        }
        if (isScroll)
        {
            resultScrollPos = EditorGUILayout.BeginScrollView(resultScrollPos, GUILayout.Height(150));
        }
        else
        {
            EditorGUILayout.Space();
        }
        for (int i = 0; i < resultList.Count; i++)
        {
            GUILayout.Label(resultList[i]);
        }
        if (isScroll)
        {
            EditorGUILayout.EndScrollView();
        }
        else
        {
            EditorGUILayout.Space();
        }
        GUILayout.EndVertical();
        #endregion 结果显示

    }
}