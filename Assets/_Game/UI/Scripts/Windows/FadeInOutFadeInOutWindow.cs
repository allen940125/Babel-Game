using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    public class FadeInOutWindow : BasePanel
    {
        [Header("Fade Settings")]
        [SerializeField] private Image fadeImage;
        [SerializeField] private Color fadeColor = Color.black;
        [SerializeField] private float fadeSpeed = 4f;

        [Header("Alpha Settings")]
        [SerializeField] private float maxAlpha = 1f;
        [SerializeField] private float minAlpha = 0f;

        private bool isFading = false;
        private float targetAlpha;

        protected override void Awake()
        {
            base.Awake();
            if (fadeImage != null)
                fadeImage.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, minAlpha);
        }

        protected override void Start()
        {
            base.Start();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
        }

        private void Update()
        {
            if (!isFading) return;

            Color currentColor = fadeImage.color;
            currentColor.a = Mathf.MoveTowards(currentColor.a, targetAlpha, fadeSpeed * Time.deltaTime);
            fadeImage.color = currentColor;

            if (Mathf.Approximately(currentColor.a, targetAlpha))
                isFading = false;
        }

        /// <summary>
        /// 開始淡入 (從透明到指定alpha)
        /// </summary>
        public void FadeIn(float targetMaxAlpha, float speed)
        {
            maxAlpha = Mathf.Clamp01(targetMaxAlpha);
            fadeSpeed = speed;
            targetAlpha = maxAlpha;
            isFading = true;
        }

        public void FadeIn(float speed)
        {
            fadeSpeed = speed;
            targetAlpha = maxAlpha;
            isFading = true;
        }

        public void FadeIn()
        {
            targetAlpha = maxAlpha;
            isFading = true;
        }

        /// <summary>
        /// 開始淡出 (從不透明到透明)
        /// </summary>
        public void FadeOut(float targetMinAlpha, float speed)
        {
            minAlpha = Mathf.Clamp01(targetMinAlpha);
            fadeSpeed = speed;
            targetAlpha = minAlpha;
            isFading = true;
        }

        public void FadeOut(float speed)
        {
            fadeSpeed = speed;
            targetAlpha = minAlpha;
            isFading = true;
        }

        public void FadeOut()
        {
            targetAlpha = minAlpha;
            isFading = true;
        }

        /// <summary>
        /// 設定淡入淡出的底色
        /// </summary>
        public void SetFadeColor(Color color)
        {
            fadeColor = color;
            if (fadeImage != null)
            {
                Color current = fadeImage.color;
                fadeImage.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, current.a);
            }
        }

        /// <summary>
        /// 設定背景圖片 (會重置顏色成白色透明)
        /// </summary>
        public void SetFadeImage(Sprite sprite)
        {
            if (fadeImage == null) return;

            fadeImage.sprite = sprite;
            fadeColor = Color.white;
            fadeImage.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, 0f);
        }

        /// <summary>
        /// 清除背景圖片
        /// </summary>
        public void ClearFadeImage()
        {
            if (fadeImage == null) return;

            fadeImage.sprite = null;
            fadeColor = Color.black;
            fadeImage.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, 0f);
        }
    }
}
