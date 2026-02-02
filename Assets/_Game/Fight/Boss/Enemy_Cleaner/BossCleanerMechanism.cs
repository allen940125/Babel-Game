using UnityEngine;

public class BossCleanerMechanism : BossSpecialMechanism
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag(targetTag))
        {
            // 進入 -> 關閉
            if(visualObject) visualObject.SetActive(false);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if(visualObject) visualObject.SetActive(true);
    }
}