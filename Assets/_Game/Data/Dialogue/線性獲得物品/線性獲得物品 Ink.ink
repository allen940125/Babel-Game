VAR giftCount = 0  // 全局变量，记录对话次数

=== npc_give_item ===
# speaker: NPC

{ 
    // 根据对话次数显示不同内容
    - giftCount == 0:
        "你好，這是給你的第一樣東西 —— 古老的鑰匙。"
        ~ giftCount += 1
        -> give_item_1
    - giftCount == 1:
        "你又來啦？這次給你一瓶藥水，小心使用。"
        ~ giftCount += 1
        -> give_item_2
    - giftCount == 2:
        "這是最後一樣了，拿去吧，一張神秘的地圖。"
        ~ giftCount += 1
        -> give_item_3
    - else:
        "我已經沒東西可以給你了，下次早點來吧。"
        -> no_more_items
}

// 不同奖励的分支（可在此处触发游戏内的实际奖励逻辑）
=== give_item_1 ===
// TODO: 触发游戏内给钥匙的逻辑
-> END

=== give_item_2 ===
// TODO: 触发游戏内给药水的逻辑
-> END

=== give_item_3 ===
// TODO: 触发游戏内给地图的逻辑
-> END

=== no_more_items ===
-> END