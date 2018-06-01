using System;
using System.Collections.Generic;
using UnityEngine;

public enum UIType {
    //关闭不需要刷新上一级界面
    NoUpdate = -200,
    //常驻界面
    Main = 4000,
    //关闭自动刷新上一级界面
    Update = 1000,
}
public class NGUIManager : MonoBehaviour {
    private static NGUIManager instance;

    public static NGUIManager Instance {
        get {
            return instance;
        }

        set {
            instance = value;
        }
    }

    public bool CheckIsWindowOpen(WinID activeID) {
        if (activeID >= WinID.Unsupported)
            return false;

        int id = (int)activeID;
        if (id < 0)
            return false;

        return IsOpen(activeID);
    }

    //public bool CheckIsOnlyMainNormalWindows(LinkedList<WinID> MainNormalIDs) {
    //    var winVal = openWins.Last;
    //    while (winVal != null) {
    //        UIWin uiWindow = winVal.Value;
    //        WinID winID = uiWindow.ID;
    //        if (MainNormalIDs.Contains(winID) == false)
    //            return false;
    //        winVal = winVal.Previous;
    //    }
    //}

    public void Awake() {
        Instance = this;
    }
    /// <summary>
    /// Panal最大depth值
    /// </summary>
    public int NowMaxDepth = 0;
    public Action<Transform> winAction;
    /// <summary>
    /// 每个受manager管理的panel的depth间隔(某些界面有多个panel)
    /// </summary>
    private readonly int depthInterval = 5;
    /// <summary>
    /// 给UI界面上的模型的Z值做预留空间
    /// </summary>
    private readonly int zInterval = 500;
    private LinkedList<UIWin> openWins = new LinkedList<UIWin>();
    private bool refreshing = false;
    private UIWin[] UIInstances = new UIWin[(int)WinID.Max];

    //关闭除Main层之外的界面
    public void CloseAll() {
        try {
            for (int i = openWins.Count - 1; i >= 0; i--) {
                LinkedListNode<UIWin> last = openWins.Last;
                if (last == null)
                    continue;
                UIWin ui = last.Value;
                if (ui.type != UIType.Main) {
                    doClose(ui);
                }
            }
        } catch (System.Exception ex) {
            Debug.LogException(ex);
        }
    }

    public void CloseWindow(WinID winID, object message = null) {
        try {
            if (!UIInstances[(int)winID])
                return;
            UIWin ui = getUI(winID);
            DoCloseWindow(ui, message);
        } catch (System.Exception ex) {
            Debug.LogException(ex);
        }
    }

    public void DoCloseWindow(UIWin win, object message = null, bool doRefresh = true) {
        try {
            if (win && IsOpen(win.ID)) {
                if (win.closeAnim) {
                    win.PlayCloseAnim(() => {
                        closeWin(win, message, doRefresh);
                    });
                } else {
                    closeWin(win, message, doRefresh);
                }
            }
        } catch (System.Exception ex) {
            Debug.LogException(ex);
        }
    }

    public void InitManage() {

    }
    public bool IsOpen(WinID id) {
        UIWin win = UIInstances[(int)id];
        if (win == null) {
            return false;
        } else {
            return openWins.Contains(win);
        }
    }

    public UIWin OpenWindow(WinID winID, Action<object> onClose = null, params object[] args) {
        try {
            if (winID >= WinID.Unsupported) {
                //UITipManager.Instance().ShowTip(11102);
                return null;
            }

            UIWin win = getUI(winID);
            if (win) {
                if (win.ReOpenable || !IsOpen(winID)) {
                    if (win.type == UIType.Update) {
                        if (win.CloseType == UIWin.eCloseType.CloseOthers) {
                            CloseAll();
                        } else {
                            int count = (int)win.CloseType;
                            if (count > 0) {
                                for (int index = 0; index < count; ++index) {
                                    LinkedListNode<UIWin> last = openWins.Last;
                                    if (last == null || last.Value.type == UIType.Main)
                                        continue;
                                    last.Value.gameObject.SetActive(false);
                                    last.Value.Close(null);
                                }
                            }
                        }
                    }
                    if (openWins.Contains(win)) {
                        openWins.Remove(win);
                    }
                    if (win.type == UIType.Main) {
                        openWins.AddFirst(win);
                    } else {
                        openWins.AddLast(win);
                    }
                    int i = 0;
                    foreach (UIWin temp in openWins) {
                        if (temp && temp.type != UIType.Main) {
                            temp.transform.localPosition = new Vector3(temp.transform.localPosition.x, temp.transform.localPosition.y, i * -zInterval); //
                            UIPanel[] tempPanel = temp.GetComponentsInChildren<UIPanel>(true);
                            List<UIPanel> tempList = new List<UIPanel>();
                            for (int j = 0; j < tempPanel.Length; j++) {
                                tempList.Add(tempPanel[j]);
                            }
                            tempList.Sort((a, b) => a.depth.CompareTo(b.depth));
                            for (int k = 0; k < tempList.Count; k++) {
                                tempList[k].depth = i * depthInterval + k;
                                NowMaxDepth = tempList[k].depth + 1;
                            }

                            if (temp && temp.ID == WinID.JL_UIMainEvent) {
                                temp.GetComponent<UIPanel>().depth = -1;
                            }
                            i++;
                        }
                    }

                    win.Open(onClose, args);
                    if (winAction != null && win.type != UIType.Main) {
                        if (win.backdrop) {
                            winAction(win.transform);
                        } else {
                            winAction(null);
                        }
                    }

                    //foreach (UIWin temp in openWins)
                    //{
                    //    int Num = openWins.Count;
                    //    bool a = temp.transform.gameObject.activeSelf;
                    //    string Name = temp.transform.gameObject.name;
                    //    Debug.LogError("====Num:::" + Num + "====IsActive:::" + a + "====Name:::" + Name);
                    //}
                }
            }
            return win;
        } catch (System.Exception ex) {
            Debug.LogException(ex);
            return null;
        }
    }

    private void closeWin(UIWin win, object message, bool doRefresh) {
        doClose(win, message);
        if (win.type == UIType.Update) {
            if (doRefresh && !refreshing) {
                refreshing = true;
                if (openWins.Count > 0) {
                    UIWin lastWin = openWins.Last.Value;
                    if (lastWin && lastWin.type != UIType.Main) {
                        lastWin.Refresh();
                        lastWin.gameObject.SetActive(true);
                    }
                }
                refreshing = false;
            } else {
                if (openWins.Count > 0) {
                    UIWin lastWin = openWins.Last.Value;
                    if (lastWin && lastWin.type != UIType.Main) {
                        lastWin.gameObject.SetActive(true);
                    }
                }
            }
        }
    }

    private void DestroyUINode(WinID ID) {
        UIWin win = UIInstances[(int)ID];
        if (win) {
            if (openWins.Contains(win)) {
                openWins.Remove(win);
                win.Close(null);
                if (winAction != null) {
                    if (openWins.Last != null) {
                        UIWin lastWin = openWins.Last.Value;
                        if (lastWin.backdrop) {
                            winAction(lastWin.transform);
                        } else {
                            winAction(null);
                        }
                    } else {
                        winAction(null);
                    }
                }
            }
            NGUITools.Destroy(win.gameObject);
            UIInstances[(int)ID] = null;
        }
    }

    private void doClose(UIWin win, object message = null) {
        if (win) {
            openWins.Remove(win);
            win.gameObject.SetActive(false);
            win.Close(message);
            if (winAction != null) {
                if (openWins.Last != null) {
                    UIWin lastWin = openWins.Last.Value;
                    if (lastWin.backdrop) {
                        winAction(lastWin.transform);
                    } else {
                        winAction(null);
                    }
                } else {
                    winAction(null);
                }
            }
        }
    }

    private bool OnBack() {
        if (openWins.Last == null)
            return false;
        UIWin curWin = openWins.Last.Value;
        if (!curWin.backdrop)
            return false;
        DoCloseWindow(curWin);
        return true;
    }

    private void OnEscape() {
        //#if !UNITY_EDITOR
        //        Application.Quit();
        //#else
        //        UnityEditor.EditorApplication.ExecuteMenuItem("Edit/Play");//editor状态下也能够跑同样流程
        //#endif
    }

    #region init

    public UIFont defFont;

    public void Init() {
        try {
            InitItem();
            InitEvent();
        } catch (System.Exception ex) {
            Debug.LogException(ex);
        }
    }

    //{
    //    get { return Utils.FindUIFontByName("DefFont"); }
    //}
    public void UnInit() {
        foreach (UIWin win in UIInstances) {
            if (win) {
                doClose(win, null);
                DestroyImmediate(win.gameObject);
            }
        }
        DestroyUINode(WinID.UIHome);
        Array.Clear(UIInstances, 0, UIInstances.Length);
    }

    private void InitEvent() {
        //注意：对于那些要在UI没起来之前就需要注册的事件都应该这里注册
    }

    private void InitItem() {
        for (int i = 0; i < transform.childCount; ++i) {
            Transform child = transform.GetChild(i);
            UIWin ui = child.GetComponent<UIWin>();
            if (ui != null) {
                try {
                    WinID ID = (WinID)Enum.Parse(typeof(WinID), ui.GetType().ToString());
                    UIInstances[(int)ID] = ui;
                    ui.ID = ID;
                    child.gameObject.SetActive(false);
                } catch (System.Exception ex) {
                    Logger.LogWarning("Unsupport UI" + child.name);
                }
            }
        }
    }

    private void OnDestroy() {
    }
    #endregion init
    #region manager UI

    //get special type of UI. if the ui is not created, return null
    public static UIWin GetUI(WinID ID) {
        return Instance.UIInstances[(int)ID];
    }

    //get special type of UI. if the ui is not created, return null
    public static T GetUI<T>() where T : UIWin {
        try {
            WinID ID = (WinID)Enum.Parse(typeof(WinID), typeof(T).ToString());
            return Instance.UIInstances[(int)ID] as T;
        } catch (System.Exception ex) {
            return default(T);
        }
    }

    //get special type of UI. if the ui is not created, created it.
    public static T SafeGetUI<T>() where T : UIWin {
        try {
            WinID ID = (WinID)Enum.Parse(typeof(WinID), typeof(T).ToString());
            return Instance.getUI(ID) as T;
        } catch (System.Exception ex) {
            return default(T);
        }
    }

    private UIWin getUI(WinID ID) {
        UIWin UI = null;
        try {
            if (ID == WinID.Max)
                return null;
            UI = UIInstances[(int)ID];
            if (UI == null) {
                //there is none can use, then add a new one
                string path = string.Format("UI/{0}", ID);
                UnityEngine.Object prefab = Resources.Load(path);
                if (prefab == null) {
                    Logger.LogWarning(string.Format("UI/{0} is not exist!", ID));
                    return null;
                } else {
                    GameObject go = (GameObject)Instantiate(prefab);
                    go.name = prefab.name;
                    UI = go.GetComponent<UIWin>();
                    if (!UI) {
                        Logger.LogError(string.Format("UI:{0} has not UIWin component!", ID));
                        return null;
                    }
                    Transform trans = go.transform;
                    trans.parent = transform;

                    //统一缩放面板 change by zhuliang
                    trans.localScale = Vector3.one;//new Vector3(1.5f,1.5f,1f);//
                    //UpdateLocalScaleByFather(trans,trans);

                    trans.localPosition = new Vector3(0, 0, (float)UI.type);
                    UIInstances[(int)ID] = UI;
                    UI.ID = ID;
                    //init anchor
                    //UIAnchor[] anchors = UI.GetComponentsInChildren<UIAnchor>();
                    //foreach (var anchor in anchors)
                    //{
                    //    anchor.runOnlyOnce = false;
                    //    anchor.uiCamera = UICamera.currentCamera;
                    //}
                    go.SetActive(false);
                }
            }
            return UI;
        } catch (System.Exception ex) {
            Debug.LogException(ex);
            return null;
        }
    }
    #endregion manager UI

}
