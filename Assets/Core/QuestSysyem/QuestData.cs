// 新增任務數據類別

using System.Collections.Generic;

// 任务数据类 (QuestCore.cs)
using System;

[Serializable]
public class QuestData
{
    public int questID;      // 任务唯一标识
    public string title;     // 任务名称
    public QuestStatus status; // 当前状态
}

public enum QuestStatus
{
    NotStarted,  // 未开始(默认)
    InProgress,  // 进行中
    Completed,   // 已完成
    Failed,     // 失敗
}