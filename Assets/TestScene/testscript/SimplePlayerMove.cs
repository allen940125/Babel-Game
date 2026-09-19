using UnityEngine;
using UnityEngine.InputSystem;

public class SimplePlayerMove : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;

    void Update()
    {
        if (Keyboard.current == null) return;

        float h = 0f;
        float v = 0f;

        if (Keyboard.current.aKey.isPressed) h -= 1f;
        if (Keyboard.current.dKey.isPressed) h += 1f;
        if (Keyboard.current.wKey.isPressed) v -= 1f;
        if (Keyboard.current.sKey.isPressed) v += 1f;

        Vector3 move = new Vector3(v, 0, h).normalized;
        transform.position += move * moveSpeed * Time.deltaTime;
    }
}