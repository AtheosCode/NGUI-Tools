using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/*ReadMe
    因为用文件路径的'.'来作为区分文件夹个普通文件的标识，所以文件夹名字和路径不能包括'.'
*/

public class ReplaceAtlas : EditorWindow {

    public delegate void OnSelectionCallback(Object obj);
    #region 旧图集
    public Object oldFile;
    public bool oldFoldout = true;
    public Vector2 oldScrollPos;
    private Dictionary<UIAtlas, bool> oldAtlasDic = new Dictionary<UIAtlas, bool>();
    private List<UIAtlas> oldAtlasList = new List<UIAtlas>();
    #endregion 旧图集
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
    private List<string> resultList = new List<string>();
    public Vector2 resultScrollPos;
    #endregion result

    //public bool choose;

    [MenuItem("Atheos/ReplaceAtlas")]
    public static void Repalce() {
        EditorWindow.GetWindow<ReplaceAtlas>(false, "Replace Atlas", true).Show();
        // NGUI 自带的图集选择器
        //GUILayout.BeginHorizontal();
        //{
        //    ComponentSelector.Draw<UIAtlas>("Atlas", NGUISettings.atlas, delegate(Object obj) {
        //        if (NGUISettings.atlas != obj) {
        //            NGUISettings.atlas = obj as UIAtlas;
        //            Repaint();
        //        }
        //    }, true, GUILayout.MinWidth(80f));

        //    EditorGUI.BeginDisabledGroup(NGUISettings.atlas == null);
        //    if (GUILayout.Button("New", GUILayout.Width(40f)))
        //        NGUISettings.atlas = null;
        //    EditorGUI.EndDisabledGroup();
        //}
        //GUILayout.EndHorizontal();
        //Object[] selections = Selection.GetFiltered(typeof(Object), SelectionMode.Assets);
    }

    public void OnGUI() {
        //ChooseComponent(ref oldFile, ref oldAtlasList, ref oldAtlasDic, ref oldScrollPos, ref oldFoldout, "旧的图集目录");
        ChooseComponent(ref newFile, ref newAtlasList, ref newAtlasDic, ref newScrollPos, ref newFoldout, "新的图集目录");
        ChooseObject<GameObject>(ref uiFile, ref uiAtlasList, ref uiAtlasDic, ref uiScrollPos, ref uiFoldout, "需要被替换的UI文件夹");
        if (uiFile != null) {
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("确认替换", GUILayout.Width(80))) {
                resultList.Clear();
                foreach (var item in uiAtlasDic) {
                    if (item.Key != null) {
                        UISprite[] sprite = item.Key.GetComponentsInChildren<UISprite>(true);
                        for (int j = 0; j < sprite.Length; j++) {
                            foreach (var atlasItem in newAtlasDic) {
                                if (item.Value == true) {
                                    BetterList<string> spriteNameList = atlasItem.Key.GetListOfSprites();
                                    foreach (var sp in spriteNameList) {
                                        if (sprite[j].spriteName == sp) {
                                            if (sprite[j].atlas != atlasItem.Key) {
                                                sprite[j].atlas = atlasItem.Key;
                                                string treeName = sprite[j].name;
                                                Transform tempTransform = sprite[j].transform;
                                                while (tempTransform.parent != null) {
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
            if (GUILayout.Button("清理结果", GUILayout.Width(80))) {
                resultList.Clear();
            }
            GUILayout.EndHorizontal();

            #region 结果显示
            GUILayout.BeginVertical();
            bool isScroll = false;
            if (resultList.Count > 5) {
                isScroll = true;
            }
            if (isScroll) {
                resultScrollPos = EditorGUILayout.BeginScrollView(resultScrollPos, GUILayout.Height(150));
            } else {
                EditorGUILayout.Space();
            }
            for (int i = 0; i < resultList.Count; i++) {

                GUILayout.Label(resultList[i]);

            }
            if (isScroll) {
                EditorGUILayout.EndScrollView();
            } else {
                EditorGUILayout.Space();
            }
            GUILayout.EndVertical();
            #endregion
        }
    }

    private void ChooseComponent<T>(ref Object file, ref List<T> atlasList, ref Dictionary<T, bool> atlasDic, ref Vector2 scrollPos, ref bool foldout, string tips = "目录文件") where T : Component {
        #region 根据选中的文件夹 找到目录下所有图集
        Object[] selections = Selection.GetFiltered(typeof(Object), SelectionMode.Assets);
        GUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(tips, GUILayout.MaxWidth(200));
        file = EditorGUILayout.ObjectField(file, typeof(Object), false, GUILayout.MinWidth(80f));
        GUILayout.EndHorizontal();
        EditorGUILayout.Space();
        if (file != null) {
            string path = AssetDatabase.GetAssetPath(file);
            atlasList.Clear();
            if (path != null && !path.Contains(".")) {
                string[] filePaths = Directory.GetFiles(path, "*.prefab", SearchOption.AllDirectories);
                for (int i = 0; i < filePaths.Length; i++) {
                    string truePath = filePaths[i].Replace('\\', '/');
                    GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(truePath);

                    if (prefab != null) {
                        T[] uiAtlas = prefab.GetComponentsInChildren<T>(true);
                        if (uiAtlas != null) {
                            for (int j = 0; j < uiAtlas.Length; j++) {
                                if (!atlasList.Contains(uiAtlas[j])) {
                                    atlasList.Add(uiAtlas[j]);
                                }
                            }
                        }
                    }
                }
            } else {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab != null) {
                    T[] uiAtlas = prefab.GetComponentsInChildren<T>(true);
                    if (uiAtlas != null) {
                        for (int j = 0; j < uiAtlas.Length; j++) {
                            if (!atlasList.Contains(uiAtlas[j])) {
                                atlasList.Add(uiAtlas[j]);
                            }
                        }
                    }
                }
            }
            #region 生成图集字典
            #region 清理残余内容
            List<T> tempList = new List<T>();
            foreach (var item in atlasDic) {
                tempList.Add(item.Key);
            }
            for (int i = 0; i < tempList.Count; i++) {
                if (!atlasList.Contains(tempList[i])) {
                    atlasDic.Remove(tempList[i]);
                }
            }
            #endregion 清理残余内容
            for (int i = 0; i < atlasList.Count; i++) {
                if (!atlasDic.ContainsKey(atlasList[i])) {
                    atlasDic.Add(atlasList[i], true);
                }
            }
            #endregion 生成图集字典
            foldout = EditorGUILayout.Foldout(foldout, "文件显示");
            if (foldout) {
                bool isScroll = false;
                if (atlasList.Count > 5) {
                    isScroll = true;
                }
                if (isScroll) {
                    scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(150));
                } else {
                    EditorGUILayout.Space();
                }
                GUILayout.Label("当前目录下所有图集：");
                for (int i = 0; i < atlasList.Count; i++) {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(i + "、  路径：" + AssetDatabase.GetAssetPath(atlasList[i]));
                    GUILayout.FlexibleSpace();
                    GUILayout.Label("名称：" + atlasList[i].name);
                    GUILayout.FlexibleSpace();
                    atlasDic[atlasList[i]] = EditorGUILayout.ToggleLeft("选择", atlasDic[atlasList[i]], GUILayout.MinWidth(80));
                    GUILayout.EndHorizontal();
                }
                if (isScroll) {
                    EditorGUILayout.EndScrollView();
                } else {
                    EditorGUILayout.Space();
                }
            }
        }
        #endregion 根据选中的文件夹 找到目录下所有图集
    }

    private void ChooseObject<T>(ref Object file, ref List<T> atlasList, ref Dictionary<T, bool> atlasDic, ref Vector2 scrollPos, ref bool foldout, string tips = "目录文件") where T : Object {
        #region 根据选中的文件夹 找到目录下所有图集
        GUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(tips, GUILayout.MaxWidth(200));
        file = EditorGUILayout.ObjectField(file, typeof(Object), false, GUILayout.MinWidth(80f));
        GUILayout.EndHorizontal();
        EditorGUILayout.Space();
        if (file != null) {
            string path = AssetDatabase.GetAssetPath(file);
            atlasList.Clear();
            if (path != null && !path.Contains(".")) {
                //利用C#提供的API 遍历当前目录下所有文件
                string[] filePaths = Directory.GetFiles(path, "*.prefab", SearchOption.AllDirectories);
                for (int i = 0; i < filePaths.Length; i++) {
                    string realPath = filePaths[i].Replace('\\', '/');
                    //T prefab = AssetDatabase.LoadAssetAtPath<T>(realPath, typeof(T));
                    T prefab = AssetDatabase.LoadAssetAtPath<T>(realPath);

                    if (prefab != null) {
                        if (!atlasList.Contains(prefab)) {
                            atlasList.Add(prefab);
                        }
                    }
                }
            } else {
                T prefab = AssetDatabase.LoadAssetAtPath<T>(path);
                if (prefab != null) {
                    if (!atlasList.Contains(prefab)) {
                        atlasList.Add(prefab);
                    }
                }
            }

            #region 生成图集字典
            #region 清理残余内容
            List<T> tempList = new List<T>();
            foreach (var item in atlasDic) {
                tempList.Add(item.Key);
            }
            for (int i = 0; i < tempList.Count; i++) {
                if (!atlasList.Contains(tempList[i])) {
                    atlasDic.Remove(tempList[i]);
                }
            }
            #endregion 清理残余内容
            for (int i = 0; i < atlasList.Count; i++) {
                if (!atlasDic.ContainsKey(atlasList[i])) {
                    atlasDic.Add(atlasList[i], true);
                }
            }
            #endregion 生成图集字典
            foldout = EditorGUILayout.Foldout(foldout, "文件显示");
            if (foldout) {
                bool isScroll = false;
                if (atlasList.Count > 5) {
                    isScroll = true;
                }
                if (isScroll) {
                    scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(150));
                } else {
                    EditorGUILayout.Space();
                }
                GUILayout.Label("当前目录下所有图集：");
                for (int i = 0; i < atlasList.Count; i++) {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(i + "、  路径：" + AssetDatabase.GetAssetPath(atlasList[i]));
                    GUILayout.FlexibleSpace();
                    if (atlasList[i] is GameObject) {
                        GameObject temp = atlasList[i] as GameObject;
                        GUILayout.Label("名称：" + temp.gameObject.name);
                    } else if (atlasList[i] is Component) {
                        Component temp = atlasList[i] as Component;
                        GUILayout.Label("名称：" + temp.name);
                    } else {
                        GUILayout.Label("名称：" + atlasList[i].name);
                    }
                    GUILayout.FlexibleSpace();
                    atlasDic[atlasList[i]] = EditorGUILayout.ToggleLeft("选择", atlasDic[atlasList[i]], GUILayout.MinWidth(80));
                    GUILayout.EndHorizontal();
                }
                if (isScroll) {
                    EditorGUILayout.EndScrollView();
                } else {
                    EditorGUILayout.Space();
                }
            }
        }
        #endregion 根据选中的文件夹 找到目录下所有图集
    }

    //private class objectData {
    //    public string name;
    //    public string realPath;
    //    public string treePath;
    //    public string typeNam;
    //}
}