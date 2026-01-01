using System.Collections.Generic;
using UnityEngine;

public class QuestManager : Singleton<QuestManager>
{
    [Header("任務資料列表")]
    public List<QuestData> testQuests = new List<QuestData>();

    // 初始化任務（可以重新塞一批新的）
    public void InitializeQuests(List<QuestData> presetQuests)
    {
        testQuests = new List<QuestData>();

        foreach (var quest in presetQuests)
        {
            testQuests.Add(new QuestData
            {
                questID = quest.questID,
                title = quest.title,
                status = QuestStatus.NotStarted
            });
        }
    }

    // 取得任務狀態
    public QuestStatus GetQuestStatus(int questID)
    {
        var quest = testQuests.Find(q => q.questID == questID);
        if (quest != null)
        {
            return quest.status;
        }
        return QuestStatus.NotStarted;
    }

    // 開始任務
    public void StartQuest(int questID)
    {
        var quest = testQuests.Find(q => q.questID == questID);
        if (quest != null)
        {
            quest.status = QuestStatus.InProgress;
            Debug.Log($"任務 [{questID}] 已開始");
        }
        else
        {
            Debug.LogError($"找不到任務 ID: {questID}");
        }
    }

    // 完成任務
    public void CompleteQuest(int questID)
    {
        var quest = testQuests.Find(q => q.questID == questID);
        if (quest != null)
        {
            quest.status = QuestStatus.Completed;
            Debug.Log($"任務 [{questID}] 已完成");
        }
    }

    // 失敗任務
    public void FailedQuest(int questID)
    {
        var quest = testQuests.Find(q => q.questID == questID);
        if (quest != null)
        {
            quest.status = QuestStatus.Failed;
            Debug.Log($"任務 [{questID}] 失敗");
        }
    }
}