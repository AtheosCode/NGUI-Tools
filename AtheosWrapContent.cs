using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// This script makes it possible for a scroll view to wrap its content, creating endless scroll views.
/// Usage: simply attach this script underneath your scroll view where you would normally place a UIGrid:
///
/// + Scroll View
/// |- AtheosWrappedContent
/// |-- Item 1
/// |-- Item 2
/// |-- Item 3
/// </summary>

[AddComponentMenu("AtheosWrapContent")]
public class AtheosWrapContent : MonoBehaviour {
    //public enum Direct {
    //    Top_Bottom,//Left_Right
    //    Bottom_Top,//Right_Left
    //    Center,
    //}

    public Dictionary<Transform, int> m_childDic = new Dictionary<Transform, int>();
    [Header("bool")]
    /// <summary>
    /// Whether the content will be automatically culled. Enabling this will improve performance in scroll views that contain a lot of items.
    /// </summary>
    [Tooltip("看不到的item是否隐藏")]
    public bool m_cullContent = true;
    /// <summary>
    /// 数据长度
    /// </summary>
    [Tooltip("item 数据长度")]
    public int m_dataCount = 0;
    /// <summary>
    /// 数据起始位置
    /// </summary>
    [Tooltip("item 数据起始位置")]
    public int m_dataInitial = 0;
    /// <summary>
    /// false:meaning left to right && top to bottom  true:meaning right to left && bottom to top
    /// </summary>
    [Tooltip("false:meaning left to right && top to bottom  true:meaning right to left && bottom to top")]
    public bool m_direct = false;
    /// <summary>
    /// 滚动方向 true:horizontal  false:vertical
    /// </summary>
    [Tooltip("滚动方向 true:horizontal  false:vertical")]
    public bool m_horizontal = false;
    /// <summary>
    /// 是否自动初始化
    /// </summary>
    [Tooltip("是否自动初始化")]
    public bool m_isAuto = true;
    /// <summary>
    /// 是否无限滚动
    /// </summary>
    [Tooltip("是否无限滚动")]
    public bool m_isEndless = false;
    [Header("int")]
    /// <summary>
    /// item 间隔大小
    /// </summary>
    [Tooltip("item 间隔大小")]
    public int m_itemSize = 100;
    [Header("Component")]
    /// <summary>
    /// UIPanel
    /// </summary>
    public UIPanel m_panel;
    /// <summary>
    /// UIScrollView
    /// </summary>
    public UIScrollView m_uiScrollView;
    /// <summary>
    /// 数据绑定 data binding
    /// </summary>
    public OnInitializeItem onInitializeItem;
    /// <summary>
    /// 最大可显示item数量
    /// </summary>
    public int m_canShowItem;
    /// <summary>
    /// 数据绑定委托方法
    /// </summary>
    /// <param name="go">绑定的Item</param>
    /// <param name="wrapIndex">Item的index</param>
    /// <param name="realIndex">真实数据的index</param>
    public delegate void OnInitializeItem(GameObject go, int wrapIndex, int realIndex);

    public void Init(int dataInitial, int dataCount) {
        if (CacheScrollView()) {
            m_dataInitial = dataInitial;
            if (m_dataInitial > dataCount - 1) {
                m_dataInitial = dataCount - 1;
            }
            m_dataCount = dataCount;
            InitChildData();
            RefreshChildData();
            m_uiScrollView.ResetPosition();
            gameObject.SetActive(true);
        } else {
            gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// 刷新Item数据
    /// </summary>
    /// <param name="isResetPosition">是否刷新位置</param>
    public void RefreshChildData(bool isResetPosition = true) {
        for (int i = 0, imax = transform.childCount; i < imax; ++i) {
            Transform temp = transform.GetChild(i);
            int realIndex;
            if (m_childDic.TryGetValue(temp, out realIndex)) {
                if (isResetPosition) {
                    temp.localPosition = m_horizontal ? new Vector3((realIndex - m_dataInitial) * m_itemSize, 0f, 0f) : new Vector3(0f, -(realIndex - m_dataInitial) * m_itemSize, 0f);
                }
                UpdateItem(temp, i, realIndex);
            }
        }
    }

    /// <summary>
    /// Cache the scroll view and return 'false' if the scroll view is not found.
    /// </summary>
    private bool CacheScrollView() {
        m_uiScrollView = NGUITools.FindInParents<UIScrollView>(gameObject);
        //Atheos 用unity自带的GetComponentInParent在编辑器模式下会出现获取不到组件的BUG
        //m_uiScrollView = transform.GetComponentInParent<UIScrollView>();
        if (m_uiScrollView == null) return false;
        m_panel = m_uiScrollView.GetComponent<UIPanel>();
        if (m_uiScrollView.movement == UIScrollView.Movement.Horizontal) {
            m_canShowItem = Mathf.CeilToInt(m_panel.GetViewSize().x / m_itemSize);
            m_horizontal = true;
        } else if (m_uiScrollView.movement == UIScrollView.Movement.Vertical) {
            m_canShowItem = Mathf.CeilToInt(m_panel.GetViewSize().y / m_itemSize);
            m_horizontal = false;
        } else return false;
        switch (m_uiScrollView.contentPivot) {
            //正常的排序方式
            case UIWidget.Pivot.TopLeft:
            case UIWidget.Pivot.Top:
            case UIWidget.Pivot.TopRight:
            case UIWidget.Pivot.Left:
            case UIWidget.Pivot.Center:
                m_direct = false;
                break;
            case UIWidget.Pivot.Right:
            case UIWidget.Pivot.BottomLeft:
            case UIWidget.Pivot.Bottom:
            case UIWidget.Pivot.BottomRight:
                m_direct = true;
                break;
            default:
                break;
        }
        return true;
    }

    /// <summary>
    /// 重置index值
    /// </summary>
    private void InitChildData() {
        if (transform.childCount == 0) {
            return;
        }
        if (m_dataCount < transform.childCount) {
            for (int i = 0; i < transform.childCount; i++) {
                Transform temp = transform.GetChild(i);
                int realIndex;
                if (m_direct) {
                    realIndex = transform.childCount - 1 - i;
                } else {
                    realIndex = i;
                }
                if (m_childDic.ContainsKey(temp)) {
                    m_childDic[temp] = realIndex;
                } else {
                    m_childDic.Add(temp, realIndex);
                }
                temp.gameObject.SetActive(m_dataCount > realIndex);
            }
        } else {
            for (int i = 0; i < transform.childCount; i++) {
                Transform temp = transform.GetChild(i);
                int realIndex;
                if (m_direct) {
                    realIndex = i + m_dataCount - transform.childCount;
                } else {
                    realIndex = i + m_dataInitial;
                    //if (realIndex >= m_dataCount) {
                    //    realIndex = realIndex - m_dataCount;
                    //}
                }

                if (m_childDic.ContainsKey(temp)) {
                    m_childDic[temp] = realIndex;
                } else {
                    m_childDic.Add(temp, realIndex);
                }
                temp.gameObject.SetActive(true);
            }
        }
    }

    /// <summary>
    /// Callback triggered by the UIPanel when its clipping region moves (for example when it's being scrolled).
    /// </summary>
    private void OnMove(UIPanel panel) { WrapContent(); }

    private void Start() {
        CacheScrollView();
        if (m_isAuto) {
            EditorExecute();
        }
        if (m_uiScrollView != null) {
            m_panel.onClipMove = OnMove;
        } else {
            m_panel = transform.GetComponentInParent<UIPanel>();
            m_panel.onClipMove = OnMove;
        }
    }

    /// <summary>
    /// Want to update the content of items as they are scrolled? Override this function.
    /// </summary>
    private void UpdateItem(Transform item, int index, int realIndex = 0) {
        if (onInitializeItem != null) {
            if (m_childDic.TryGetValue(item, out realIndex)) {
                onInitializeItem(item.gameObject, index, realIndex);
            } else {
                onInitializeItem(item.gameObject, index, (m_uiScrollView.movement == UIScrollView.Movement.Vertical) ? Mathf.RoundToInt(item.localPosition.y / m_itemSize) : Mathf.RoundToInt(item.localPosition.x / m_itemSize));
            }
        }
    }

    /// <summary>
    /// Wrap all content, repositioning all children as needed.
    /// </summary>
    private void WrapContent() {
        float extents = m_itemSize * m_childDic.Count * 0.5f;
        Vector3[] corners = m_panel.worldCorners;

        for (int i = 0; i < 4; ++i) {
            Vector3 v = corners[i];
            v = transform.InverseTransformPoint(v);
            corners[i] = v;
        }

        Vector3 center = Vector3.Lerp(corners[0], corners[2], 0.5f);
        bool allWithinRange = true;
        float ext2 = extents * 2f;

        if (m_horizontal) {
            float min = corners[0].x - m_itemSize;
            float max = corners[2].x + m_itemSize;

            for (int i = 0, imax = transform.childCount; i < imax; ++i) {
                Transform temp = transform.GetChild(i);
                float distance = temp.localPosition.x - center.x;

                if (distance < -extents) {
                    Vector3 pos = temp.localPosition;
                    pos.x += ext2;
                    distance = pos.x - center.x;
                    if (m_childDic.ContainsKey(temp)) {
                        int newIndex = m_childDic[temp] + m_childDic.Count;
                        if (m_isEndless) {
                            if (0 <= newIndex && newIndex < m_dataCount) {
                                m_childDic[temp] = newIndex;
                                temp.localPosition = pos;
                                UpdateItem(temp, i);
                            } else {
                                m_childDic[temp] = newIndex - m_dataCount;
                                temp.localPosition = pos;
                                UpdateItem(temp, i);
                            }
                        } else {
                            if (0 <= newIndex && newIndex < m_dataCount) {
                                m_childDic[temp] = newIndex;
                                temp.localPosition = pos;
                                UpdateItem(temp, i);
                            } else {
                                allWithinRange = false;
                            }
                        }
                        //if (m_isEndless || (0 <= newIndex && newIndex < m_dataCount)) {
                        //    m_childDic[temp] = newIndex;
                        //    temp.localPosition = pos;
                        //    UpdateItem(temp, i);
                        //} else {
                        //    allWithinRange = false;
                        //}
                    } else {
                        Debug.Log("没有初始化");
                    }
                } else if (distance > extents) {
                    Vector3 pos = temp.localPosition;
                    pos.x -= ext2;
                    distance = pos.x - center.x;
                    if (m_childDic.ContainsKey(temp)) {
                        int newIndex = m_childDic[temp] - m_childDic.Count;
                        if (m_isEndless) {
                            if (0 <= newIndex && newIndex < m_dataCount) {
                                m_childDic[temp] = newIndex;
                                temp.localPosition = pos;
                                UpdateItem(temp, i);
                            } else {
                                m_childDic[temp] = newIndex + m_dataCount;
                                temp.localPosition = pos;
                                UpdateItem(temp, i);
                            }
                        } else {
                            if (0 <= newIndex && newIndex < m_dataCount) {
                                m_childDic[temp] = newIndex;
                                temp.localPosition = pos;
                                UpdateItem(temp, i);
                            } else {
                                allWithinRange = false;
                            }
                        }
                        //if (m_isEndless || (0 <= newIndex && newIndex < m_dataCount)) {
                        //    m_childDic[temp] = newIndex;
                        //    temp.localPosition = pos;
                        //    UpdateItem(temp, i);
                        //} else {
                        //    allWithinRange = false;
                        //}
                    } else {
                        Debug.Log("没有初始化");
                    }
                }

                if (m_cullContent) {
                    distance += m_panel.clipOffset.x - transform.localPosition.x;
                    if (!UICamera.IsPressed(temp.gameObject))
                        NGUITools.SetActive(temp.gameObject, m_isEndless || (distance > min && distance < max && 0 <= m_childDic[temp] && m_childDic[temp] < m_dataCount), false);
                }
            }
        } else {
            float min = corners[0].y - m_itemSize;
            float max = corners[2].y + m_itemSize;

            for (int i = 0, imax = transform.childCount; i < imax; ++i) {
                Transform temp = transform.GetChild(i);
                float distance = temp.localPosition.y - center.y;

                if (distance < -extents) {
                    Vector3 pos = temp.localPosition;
                    pos.y += ext2;
                    distance = pos.y - center.y;
                    if (m_childDic.ContainsKey(temp)) {
                        int newIndex = m_childDic[temp] - m_childDic.Count;
                        if (m_isEndless) {
                            if (0 <= newIndex && newIndex < m_dataCount) {
                                m_childDic[temp] = newIndex;
                                temp.localPosition = pos;
                                UpdateItem(temp, i);
                            } else {
                                m_childDic[temp] = newIndex + m_dataCount;
                                temp.localPosition = pos;
                                UpdateItem(temp, i);
                            }
                        } else {
                            if (0 <= newIndex && newIndex < m_dataCount) {
                                m_childDic[temp] = newIndex;
                                temp.localPosition = pos;
                                UpdateItem(temp, i);
                            } else {
                                allWithinRange = false;
                            }
                        }
                        //if (m_isEndless || (0 <= newIndex && newIndex < m_dataCount)) {
                        //    m_childDic[temp] = newIndex;
                        //    temp.localPosition = pos;
                        //    UpdateItem(temp, i);
                        //} else {
                        //    allWithinRange = false;
                        //}
                    } else {
                        Debug.Log("没有初始化");
                    }
                } else if (distance > extents) {
                    Vector3 pos = temp.localPosition;
                    pos.y -= ext2;
                    distance = pos.y - center.y;
                    if (m_childDic.ContainsKey(temp)) {
                        int newIndex = m_childDic[temp] + m_childDic.Count;
                        if (m_isEndless) {
                            if (0 <= newIndex && newIndex < m_dataCount) {
                                m_childDic[temp] = newIndex;
                                temp.localPosition = pos;
                                UpdateItem(temp, i);
                            } else {
                                m_childDic[temp] = newIndex - m_dataCount;
                                temp.localPosition = pos;
                                UpdateItem(temp, i);
                            }
                        } else {
                            if (0 <= newIndex && newIndex < m_dataCount) {
                                m_childDic[temp] = newIndex;
                                temp.localPosition = pos;
                                UpdateItem(temp, i);
                            } else {
                                allWithinRange = false;
                            }
                        }
                        //if (m_isEndless || (0 <= newIndex && newIndex < m_dataCount)) {
                        //    m_childDic[temp] = newIndex;
                        //    temp.localPosition = pos;
                        //    UpdateItem(temp, i);
                        //} else {
                        //    allWithinRange = false;
                        //}
                    } else {
                        Debug.Log("没有初始化");
                    }
                }

                if (m_cullContent) {
                    distance += m_panel.clipOffset.y - transform.localPosition.y;
                    if (!UICamera.IsPressed(temp.gameObject))
                        NGUITools.SetActive(temp.gameObject, m_isEndless || (distance > min && distance < max && 0 <= m_childDic[temp] && m_childDic[temp] < m_dataCount), false);
                }
            }
        }
        m_uiScrollView.restrictWithinPanel = !allWithinRange;
    }
    #region Item排序 编辑器使用

    [ContextMenu("EditorExecute")]
    private void EditorExecute() {
        CacheScrollView();
        Init(m_dataInitial, m_dataCount);
    }
    #endregion Item排序 编辑器使用
}