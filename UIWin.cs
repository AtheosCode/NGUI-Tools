using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public enum WinID {
    node,
    Max,
    JL_UIMainEvent,
    JL_UIMain,
    UIHome,
    Unsupported,
}
public class UIWin : MonoBehaviour {


    public bool backdrop = false;
    public UIType type = UIType.NoUpdate;
    [HideInInspector]
    public WinID ID;
    //the back type of the win, it will used when open the win to judge which parent win show open and which should closed
    //only use for normal win
    public enum eCloseType {
        CloseNothing = 0, //close all the exist window
        ClosePrev1,
        ClosePrev2,
        ClosePrev3,
        CloseOthers = 99999, //close all the exist window. important: this value must be large
        Max = 100000,
    };
    public eCloseType closeType = eCloseType.CloseNothing; //this value can only be set from edit. you should change it by script. if you want to change the close type, use CloseType = X,the value is used only one time;
    private eCloseType tempCloseType = eCloseType.Max; //temp close type, use it one time. if the value is not CloseType.Max, UIManager will use this value as close type.
    public eCloseType CloseType {
        get {
            if (tempCloseType != eCloseType.Max) {
                eCloseType ret = tempCloseType;
                return ret;
            } else {
                return closeType;
            }
        }
        set {
            tempCloseType = value;
        }
    }


    public AnimationClip openAnim;
    public AnimationClip closeAnim;


    public void Close(object message) {
        if (!this) {
            Logger.LogError("close a win which is already destory!!!!!");
            return;
        }
        if (onClose != null) {
            onClose(message);
            onClose = null;
        }
        OnClose();
    }

    public virtual bool ReOpenable { get { return true; } } //wether open it even it is opened aleady. just for ui need reopend such as UIHome
    public bool IsOpen() {
        return NGUIManager.Instance.IsOpen(ID);
    }

    #region init
    private bool inited = false;
    private void Init() {
        if (inited) return;
        inited = true;
        OnInit();
    }
    protected virtual void OnInit() {

    }
    #endregion


    /// <summary>
    /// 打开窗口
    /// </summary>
    /// <param name="onClose">关闭时的回调</param>
    /// <param name="args">其它参数</param>
    /// <returns></returns>
    protected object[] args; //open params
    public object[] Args {
        get { return args; }
    }
    private Action<object> onClose;
    public bool Open(Action<object> onClose = null, params object[] args) {
        this.args = args;
        Init();
        fromOpen = true;
        // PlayOpenAnim();
        if (onClose != null) this.onClose = onClose;
        OnOpen(args);
        return true;
    }
    private bool fromOpen = false;
    protected bool FromOpen {
        get { return fromOpen; }
    }
    protected virtual void OnOpen(object[] args) {
        UIPanel uiPanel = gameObject.GetComponent<UIPanel>();
        if (uiPanel != null) {
            uiPanel.alpha = 0;
        } else {
            UIWidget uiwidget = gameObject.GetComponent<UIWidget>();
            if (uiwidget != null) {
                uiwidget.alpha = 0;
            }
        }
        gameObject.SetActive(true);
        transform.localScale = Vector3.zero;
        TweenAlpha.Begin(this.gameObject, 0.5f, 1);
        TweenScale.Begin(this.gameObject, 0.5f, Vector3.one);
    }

    public void Refresh() {
        fromOpen = false;
        OnOpen(args);
        onPlayOpenAnimEnd();
    }

    protected virtual void OnClose() {
        //TweenAlpha.Begin(this.gameObject, 0.5f, 0);
        //TweenScale.Begin(this.gameObject, 0.5f, Vector3.zero).SetOnFinished(delegate() {
        //    this.gameObject.SetActive(false);
        //});
        this.gameObject.SetActive(false);
    }

    public void PlayOpenAnim() {
        if (GetComponent<Animation>() && GetComponent<Animation>().isPlaying) return; //animation is playing
        //play items' animation
        PlayItemsAnim(true);
        if (openAnim) {
            //play close anim
            //Animation anim = Utils.SafeGetComponent<Animation>(gameObject);
            Animation anim = transform.GetComponent<Animation>();
            if (anim[openAnim.name] == null) {
                //Debug.LogError("Trying to play an animation : " + animationClip.name + " but it isn't in the animation list. I will add it, this time, though you should add it manually.");
                anim.AddClip(openAnim, openAnim.name);
            }
            anim.enabled = true;
            anim.Play(openAnim.name);
        }
    }

    //play items' open animation
    private float openAnimPlayTime = 0;
    public void PlayItemsAnim(bool afterOpenAnim = false) {
        if (IsPlayingAnim) return;
        IsPlayingAnim = true;
        try {
            //if (!HasItemsAnimComplete()) return;
            float winOpenDelay = (afterOpenAnim && openAnim) ? openAnim.length : 0;
            openAnimPlayTime = 0;
            openAnimPlayTime = openAnimPlayTime + winOpenDelay + 0.1f;
            Invoke("onPlayOpenAnimEnd", openAnimPlayTime);
        } catch (System.Exception ex) {
            Logger.LogException(ex);
            onPlayOpenAnimEnd();
        }
    }

    private bool playingAnim = false;
    public virtual bool IsPlayingAnim {
        get { return playingAnim; }
        set {
            //Main.instance.MsgWaitingCollider(Main.LockKey.ChangeUI, value);
            playingAnim = value;
        }
    }
    public Action OnPlayOpenAnimEnd;
    private void onPlayOpenAnimEnd() {
        IsPlayingAnim = false;
        if (OnPlayOpenAnimEnd != null) {
            OnPlayOpenAnimEnd();
        }
    }
    public void PlayCloseAnim(Action cb) {
        if (closeAnim) {
            //play close anim
            //Animation anim = Utils.SafeGetComponent<Animation>(gameObject);
            Animation anim = transform.GetComponent<Animation>();
            if (anim[closeAnim.name] == null) {
                //Debug.LogError("Trying to play an animation : " + animationClip.name + " but it isn't in the animation list. I will add it, this time, though you should add it manually.");
                anim.AddClip(closeAnim, closeAnim.name);
            }
            anim.Play(closeAnim.name);
            //Utils.Instance.DelayCall(closeAnim.length, cb);
        } else {
            if (cb != null) cb();
        }
    }


}
