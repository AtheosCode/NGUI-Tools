using UnityEngine;

public class Magic : MonoBehaviour {
    /// <summary>
    /// 动画
    /// </summary>
    private Animation[] animations;
    /// <summary>
    /// 渲染器
    /// </summary>
    private Renderer[] m_renderers;
    [SerializeField]
    private UIPanel m_uiPanel = null;
    /// <summary>
    /// 粒子系统
    /// </summary>
    private ParticleSystem[] particleSystems;
    /// <summary>
    /// 拖尾渲染器
    /// </summary>
    private TrailRenderer[] trackedReferences;

    public void LateUpdate() {
        if (m_uiPanel != null) {
            int targetRenderQueue = m_uiPanel.startingRenderQueue + m_uiPanel.drawCalls.Count * 2 + 1;
            for (int i = 0; i < m_renderers.Length; i++) {
                Material[] tempMaterial = m_renderers[i].sharedMaterials;
                for (int j = 0; j < tempMaterial.Length; j++) {
                    tempMaterial[j].renderQueue = targetRenderQueue;
                }
            }
        }
    }

    /// <summary>
    /// 设置panel，用于动态改变渲染层级
    /// </summary>
    public void SetUIPanel(Transform parentPanel) {
        if (parentPanel != null) {
            UIPanel uiPanel = parentPanel.gameObject.GetComponent<UIPanel>();
            if (uiPanel != null) {
                //uiPanel.addUISort(this);
                this.m_uiPanel = uiPanel;
            }
        }
    }

    private void Awake() {
        animations = GetComponentsInChildren<Animation>(true);
        particleSystems = GetComponentsInChildren<ParticleSystem>(true);
        m_renderers = GetComponentsInChildren<Renderer>(true);
        trackedReferences = GetComponentsInChildren<TrailRenderer>(true);
    }

    private void ChangeTrailRenderer(bool enable) {
        if (trackedReferences != null) {
            for (int i = 0; i < trackedReferences.Length; i++) {
                trackedReferences[i].enabled = enable;
            }
        }
    }

    private void OnDisable() {
        ChangeTrailRenderer(false);
        ResetParticleSystem();
        PlayAnimations();
    }

    private void OnEnable() {
        ChangeTrailRenderer(true);
    }

    private void PlayAnimations() {
        if (animations == null) return;
        for (int i = 0; i < animations.Length; i++) {
            if (animations[i] != null) {
                animations[i].Stop();
                animations[i].Play();
            }
        }
    }

    private void ResetParticleSystem() {
        if (particleSystems == null) return;
        for (int i = 0; i < particleSystems.Length; i++) {
            if (particleSystems[i] != null) {
                particleSystems[i].Clear(true);
            }
        }
    }

    private void Start() {
        if (this.m_uiPanel == null) {
            this.m_uiPanel = this.transform.parent.GetComponentInParent<UIPanel>();
        }
    }

    private void StopAnimations() {
        if (animations == null) return;
        for (int i = 0; i < animations.Length; i++) {
            if (animations[i] != null) {
                animations[i].Stop();
            }
        }
    }
}