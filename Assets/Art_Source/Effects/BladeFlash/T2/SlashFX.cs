using UnityEngine;
using DG.Tweening;

public class SlashFX : MonoBehaviour
{
    [SerializeField] Renderer mainSlash, thinSlash, hitFlash;
    [SerializeField] float randomRollRange = 25f;

    static readonly int Reveal = Shader.PropertyToID("_Reveal");
    static readonly int Erase = Shader.PropertyToID("_Erase");
    static readonly int Alpha = Shader.PropertyToID("_Alpha");

    Material mMain, mThin, mFlash;
    Sequence seq;

    void Awake()
    {
        mMain = mainSlash.material;   // 產生實例,避免互相影響
        mThin = thinSlash.material;
        mFlash = hitFlash.material;
    }

    public void Play()
    {
        seq?.Kill();
        gameObject.SetActive(true);

        // 隨機角度,避免每次長一樣
        transform.localRotation *= Quaternion.Euler(0, 0, Random.Range(-randomRollRange, randomRollRange));

        Reset(mMain); Reset(mThin);
        mFlash.SetFloat(Alpha, 1);
        hitFlash.transform.localScale = Vector3.zero;

        seq = DOTween.Sequence();

        // 主刀痕:快速掃出,然後尾巴收掉
        seq.Insert(0f, Tween(mMain, Reveal, 0.08f, Ease.OutCubic));
        seq.Insert(0.05f, Tween(mMain, Erase, 0.20f, Ease.InCubic));

        // 細線:更快、更亮、更早消失
        seq.Insert(0f, Tween(mThin, Reveal, 0.06f, Ease.OutQuad));
        seq.Insert(0.03f, Tween(mThin, Erase, 0.15f, Ease.InQuad));

        // 主刀痕輕微拉伸 (Punch 感)
        seq.Insert(0f, mainSlash.transform
            .DOScale(new Vector3(1.15f, 1f, 1f), 0.25f).From(Vector3.one).SetEase(Ease.OutQuad));

        // 命中閃光:放大 → 縮小,同時旋轉
        seq.Insert(0.05f, hitFlash.transform.DOScale(1.2f, 0.06f).SetEase(Ease.OutBack));
        seq.Insert(0.11f, hitFlash.transform.DOScale(0f, 0.10f).SetEase(Ease.InQuad));
        seq.Insert(0.05f, hitFlash.transform.DOLocalRotate(new Vector3(0, 0, 90), 0.16f));

        seq.OnComplete(() => gameObject.SetActive(false)); // 有物件池就改成回收
    }

    void Reset(Material m) { m.SetFloat(Reveal, 0); m.SetFloat(Erase, 0); m.SetFloat(Alpha, 1); }

    Tween Tween(Material m, int id, float dur, Ease ease) =>
        DOVirtual.Float(0f, 1f, dur, v => m.SetFloat(id, v)).SetEase(ease);

    void OnDestroy() { seq?.Kill(); Destroy(mMain); Destroy(mThin); Destroy(mFlash); }
}