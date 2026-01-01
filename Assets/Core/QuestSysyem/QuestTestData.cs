// 测试用任务数据生成器 (TestQuestData.cs)
#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CreateAssetMenu(fileName = "QuestTestData", menuName = "Test/QuestTestData")]
public class QuestTestData : ScriptableObject
{
    public List<QuestData> testQuests = new List<QuestData>
    {
        new QuestData
        {
            questID = 101,
            title = "失落的典籍",
            status = QuestStatus.NotStarted
        },
        new QuestData
        {
            questID = 102,
            title = "药剂师的请求",
            status = QuestStatus.NotStarted
        },
        new QuestData
        {
            questID = 201,
            title = "禁忌的知识",
            status = QuestStatus.NotStarted
        }
    };
}

// 在Editor文件夹下创建
[CustomEditor(typeof(QuestTestData))]
public class QuestTestDataEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        
        if (GUILayout.Button("注入测试数据"))
        {
            var data = (QuestTestData)target;
            QuestManager.Instance.InitializeQuests(data.testQuests);
            Debug.Log("已注入测试任务数据");
        }
    }
}
#endif