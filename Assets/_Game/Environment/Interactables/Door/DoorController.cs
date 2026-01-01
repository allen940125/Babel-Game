using System;
using UnityEngine;

public class DoorController : MonoBehaviour
{
    // 1. 將類型改為 string，這樣你在 Inspector 就可以輸入 Tag 名稱 (例如 "Player")
    [SerializeField] private string _targetTag = "Player";

    Animator _animator;
    
    private void Awake()
    {
        _animator = GetComponent<Animator>();
    }

    // 當物體進入觸發範圍 (開門)
    private void OnTriggerEnter(Collider other)
    {
        // 2. 使用 CompareTag 方法比對，這比用 == 更高效且安全
        if (other.CompareTag(_targetTag))
        {
            OpenDoor();
        }
    }

    // 當物體離開觸發範圍 (關門)
    private void OnTriggerExit(Collider other)
    {
        // 離開的也要確認是不是指定的那個人/物體
        if (other.CompareTag(_targetTag))
        {
            CloseDoor();
        }
    }

    private void OpenDoor()
    {
        Debug.Log("門開了！執行開門動畫或邏輯");
        _animator.SetBool("isOpen" ,true);
        // 在這裡加入你的動畫程式碼，例如: animator.SetBool("IsOpen", true);
    }

    private void CloseDoor()
    {
        Debug.Log("門關了！");
        _animator.SetBool("isOpen" ,false);
        // 在這裡加入你的動畫程式碼，例如: animator.SetBool("IsOpen", false);
    }
}