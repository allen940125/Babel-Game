using UnityEngine;
using Febucci.TextAnimatorForUnity; 

public class IndependentTypewriterSpeed : MonoBehaviour
{
    [SerializeField] private float speedMultiplier = 1f;

    private TypewriterComponent typewriter;

    // 讓外部可以拿到 typewriter，方便訂閱事件
    public TypewriterComponent Typewriter => typewriter;

    private void Awake()
    {
        typewriter = GetComponent<TypewriterComponent>();
    }

    public void ShowText(string text)
    {
        if (typewriter == null) return;

        string wrapped = speedMultiplier == 1f
            ? text
            : $"<speed={speedMultiplier}>{text}<speed=1>";

        typewriter.ShowText(wrapped);
    }
}
