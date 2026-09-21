using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;

public class NpcInteractable : MonoBehaviour
{
    [Header("Dialogue Data")]
    [SerializeField] private string npcId;
    [SerializeField] private TextAsset inkJson;

    [Header("Optional")]
    [SerializeField] private GameObject interactPrompt;

    private bool playerInRange;

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"偵測到碰撞: {other.gameObject.name}, Tag: {other.tag}");
        if (!other.CompareTag("Player")) return;
        playerInRange = true;
        if (interactPrompt) interactPrompt.SetActive(true);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        playerInRange = false;
        if (interactPrompt) interactPrompt.SetActive(false);
    }

    private void Update()
    {
        if (!playerInRange) return;
        if (Dialoguecontroller.Instance.DialogueIsPlaying) return;
        if (Keyboard.current == null) return;

        if (Keyboard.current.eKey.wasPressedThisFrame)
        {
            Debug.Log("偵測到 E 鍵按下,呼叫 StartDialogue");
            Dialoguecontroller.Instance.StartDialogue(npcId, inkJson, UIType.DialogueWindowNew).Forget();
            if (interactPrompt) interactPrompt.SetActive(false);
        }
    }
}