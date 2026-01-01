using UnityEngine;
using System.Collections.Generic;

[DisallowMultipleComponent]
public class FadeObject : MonoBehaviour
{
    [Header("透明度設定")]
    [Tooltip("變透明的目標值 (0.0 = 完全隱藏)")]
    public float targetTransparency = 0.2f;
    [Tooltip("淡入/淡出速度（較大越快）")]
    public float fadeSpeed = 5f;
    [Tooltip("是否包含子物件的 renderer")]
    public bool affectsChildrenRenderers = true;

    // 每個 renderer 對應到它的材質陣列 (實體 instance)
    private Dictionary<Renderer, Material[]> rendererToMaterials = new Dictionary<Renderer, Material[]>();

    // 每個 renderer 對應到每個材質的原始顏色
    private Dictionary<Renderer, Color[]> rendererOriginalColors = new Dictionary<Renderer, Color[]>();

    // 每個 renderer 當前是否處於 Transparent 模式（避免重複設定 renderMode）
    private Dictionary<Renderer, bool> rendererIsCurrentlyTransparent = new Dictionary<Renderer, bool>();

    private Renderer[] cachedRenderers;

    // 狀態
    private bool isFading = false;      // true = target is transparent (fade out)
    private float progress = 0f;        // 0 => original, 1 => targetTransparency

    // Shader property ids
    private static readonly int ID_BaseColor = Shader.PropertyToID("_BaseColor");
    private static readonly int ID_Color = Shader.PropertyToID("_Color");
    private static readonly int ID_Tint = Shader.PropertyToID("_TintColor");

    void Awake()
    {
        CacheRenderersAndMaterials();
    }

    void OnValidate()
    {
        // 編輯器改參數時也嘗試重建快取（安全）
        CacheRenderersAndMaterials();
    }

    private void CacheRenderersAndMaterials()
    {
        if (affectsChildrenRenderers)
            cachedRenderers = GetComponentsInChildren<Renderer>(true);
        else
            cachedRenderers = GetComponents<Renderer>();

        rendererToMaterials.Clear();
        rendererOriginalColors.Clear();
        rendererIsCurrentlyTransparent.Clear();

        foreach (var r in cachedRenderers)
        {
            if (r == null) continue;

            Material[] mats;

            // ★ 在編輯器模式避免用 materials 會造成 leak
            if (Application.isPlaying)
            {
                mats = r.materials;      // 在 Play Mode 才可用
            }
            else
            {
                mats = r.sharedMaterials; // 在 Edit Mode 用 sharedMaterials
            }

            rendererToMaterials[r] = mats;

            // 其餘與你原本的一樣
            Color[] origCols = new Color[mats.Length];
            for (int i = 0; i < mats.Length; i++)
            {
                var mat = mats[i];
                if (mat == null) { origCols[i] = Color.white; continue; }

                Color c = Color.white;
                if (mat.HasProperty(ID_BaseColor)) c = mat.GetColor(ID_BaseColor);
                else if (mat.HasProperty(ID_Color)) c = mat.GetColor(ID_Color);
                else if (mat.HasProperty(ID_Tint)) c = mat.GetColor(ID_Tint);
                else if (mat.HasProperty("_Color")) c = mat.GetColor("_Color");

                origCols[i] = c;
            }
            rendererOriginalColors[r] = origCols;

            rendererIsCurrentlyTransparent[r] = false;
        }
    }


    /// <summary>
    /// 由外部呼叫：開始淡化 (被遮擋)
    /// </summary>
    public void StartFade()
    {
        isFading = true;
    }

    /// <summary>
    /// 由外部呼叫：停止淡化 (恢復不透明)
    /// </summary>
    public void StopFade()
    {
        isFading = false;
    }

    /// <summary>
    /// 每幀呼叫（由 Controller）去進行逐幀更新。
    /// 回傳：是否仍需繼續更新（true = 還在過程中）
    /// </summary>
    public bool DoFadeUpdate(float deltaTime)
    {
        if (rendererToMaterials.Count == 0) return false;

        float target = isFading ? 1f : 0f; // progress 0..1
        if (Mathf.Approximately(progress, target))
        {
            // 確保最終值被套用一次
            ApplyAllRenderers(progress);
            return false;
        }

        // MoveTowards 比 Lerp 更穩定來控制速度
        float step = fadeSpeed * deltaTime;
        progress = Mathf.MoveTowards(progress, target, step);

        ApplyAllRenderers(progress);

        return !Mathf.Approximately(progress, target);
    }

    private void ApplyAllRenderers(float progressT)
    {
        // progressT: 0 => original alpha, 1 => targetTransparency
        float alpha = Mathf.Lerp(1f, targetTransparency, progressT);

        foreach (var kv in rendererToMaterials)
        {
            var rend = kv.Key;
            var mats = kv.Value;
            if (rend == null) continue;

            // 決定是否要把該 renderer 切換到 Transparent 模式（若任一材質需要 alpha < 1）
            bool needsTransparent = alpha < 1f;
            bool currentlyTransparent = rendererIsCurrentlyTransparent.ContainsKey(rend) && rendererIsCurrentlyTransparent[rend];

            if (needsTransparent && !currentlyTransparent)
            {
                // 對此 renderer 的每個材質都切換 mode
                for (int i = 0; i < mats.Length; i++)
                {
                    SetMaterialMode(mats[i], true);
                }
                rendererIsCurrentlyTransparent[rend] = true;
            }
            else if (!needsTransparent && currentlyTransparent)
            {
                for (int i = 0; i < mats.Length; i++)
                {
                    SetMaterialMode(mats[i], false);
                }
                rendererIsCurrentlyTransparent[rend] = false;
            }

            // 更新每個材質的顏色 alpha（逐材質）
            Color[] origCols = rendererOriginalColors[rend];
            for (int i = 0; i < mats.Length; i++)
            {
                var mat = mats[i];
                if (mat == null) continue;

                Color baseCol = (i < origCols.Length) ? origCols[i] : Color.white;
                Color newCol = new Color(baseCol.r, baseCol.g, baseCol.b, baseCol.a * alpha);

                // 優先支援 URP 的 _BaseColor，再 fallback 到常見名稱
                if (mat.HasProperty(ID_BaseColor)) mat.SetColor(ID_BaseColor, newCol);
                if (mat.HasProperty(ID_Color)) mat.SetColor(ID_Color, newCol);
                if (mat.HasProperty(ID_Tint)) mat.SetColor(ID_Tint, newCol);
                if (mat.HasProperty("_Color")) mat.SetColor("_Color", newCol);
            }
        }
    }

    // 將材質的 Surface Mode 切換（適用 URP Lit / Shader Graph 的 _Surface）
    private void SetMaterialMode(Material material, bool transparent)
    {
        if (material == null) return;

        // 一些 URP/HDRP shader 使用 _Surface (0 opaque, 1 transparent)
        const string SURFACE = "_Surface";
        const string RENDERTYPE = "_RenderType"; // note: sometimes use SetOverrideTag instead
        // Blend keys may differ per shader; the following works for many URP setups
        if (transparent)
        {
            if (material.HasProperty(SURFACE)) material.SetFloat(SURFACE, 1f);
            material.SetOverrideTag("RenderType", "Transparent");
            // Set common blending (URP may ignore some of these depending on shader)
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            material.DisableKeyword("_ALPHATEST_ON");
            material.EnableKeyword("_ALPHABLEND_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        }
        else
        {
            if (material.HasProperty(SURFACE)) material.SetFloat(SURFACE, 0f);
            material.SetOverrideTag("RenderType", "Opaque");
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
            material.SetInt("_ZWrite", 1);
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Geometry;
            material.DisableKeyword("_ALPHATEST_ON");
            material.DisableKeyword("_ALPHABLEND_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        }
    }
}
