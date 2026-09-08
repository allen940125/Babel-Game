using System.Collections;
using System.Text;
using Febucci.TextAnimatorForUnity;
using UnityEngine;

namespace Febucci.Examples
{
    public class GlitchTextEffect : MonoBehaviour
    {
        [SerializeField] TextAnimatorComponentBase textAnimator;

        [Header("Glitch Settings")]
        [Tooltip("多久換一次亂碼字元（秒）")]
        [SerializeField] float cycleInterval = 0.05f;
        [Tooltip("用來替換的隨機字元池")]
        [SerializeField] string charPool = "!@#$%^&*ABCXYZ日月火水木金土0123456789";

        string baseText;
        int[] glitchIndices; // 要持續亂跳的字元位置(依可見字元index)
        Coroutine activeCoroutine;
        readonly StringBuilder sb = new();

        // text: 原始文字, indices: 想要故障的可見字元位置(從0開始)
        public void SetGlitchingText(string text, int[] indices)
        {
            Debug.Log($"[Glitch] SetGlitchingText 被呼叫，text={text}"); // 加這行
            baseText = text;
            glitchIndices = indices;

            if (activeCoroutine != null) StopCoroutine(activeCoroutine);
            activeCoroutine = StartCoroutine(GlitchLoop());
        }



        IEnumerator GlitchLoop()
        {
            while (true)
            {
                string displayText = BuildGlitchedText(baseText, glitchIndices);
                textAnimator.SetText(displayText);
                yield return new WaitForSeconds(cycleInterval);
            }
        }

        string BuildGlitchedText(string text, int[] indices)
        {
            sb.Clear();
            int visibleIndex = 0;
            bool inTag = false;

            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];

                if (c == '<' && !inTag) { inTag = true; sb.Append(c); continue; }
                if (c == '>' && inTag) { inTag = false; sb.Append(c); continue; }
                if (inTag) { sb.Append(c); continue; }

                if (!char.IsWhiteSpace(c))
                {
                    bool shouldGlitch = System.Array.IndexOf(indices, visibleIndex) >= 0;
                    sb.Append(shouldGlitch ? charPool[Random.Range(0, charPool.Length)] : c);
                    visibleIndex++;
                }
                else
                {
                    sb.Append(c);
                }
            }

            return sb.ToString();
        }

        void OnDisable()
        {
            if (activeCoroutine != null) StopCoroutine(activeCoroutine);
        }
    }
}