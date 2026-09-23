=== Test1001 ===
# synopsis: 主角在路邊遇到 Sans,兩人隨口聊了幾句。
# speaker: Sans
# portrait: left sans_happy
嘿,兄弟。今天天氣不錯啊。

# portrait: left sans_smirk
不過我猜你應該不是來跟我聊天氣的吧?

* [老實回答]
    -> honest_answer
* [開個玩笑]
    -> joke_answer
* [直接離開]
    -> leave_immediately

= honest_answer
# portrait: left sans_happy
哈,老實人啊。我喜歡。
-> continue_chat

= joke_answer
# portrait: left sans_smirk
哈哈哈,不錯嘛,算你有點幽默感。
-> continue_chat

= leave_immediately
# portrait: left none
……好吧,那就下次見了。
-> DONE

= continue_chat
# speaker: Sans
# portrait: left sans_happy
話說回來,你有看到我哥嗎?他最近老是在做奇怪的實驗。

* [沒看到]
    -> no_idea
* [有看到,他在實驗室]
    -> saw_him

= no_idea
沒關係,他應該又跑去哪裡鬼混了。
-> ending

= saw_him
# portrait: left sans_smirk
喔?實驗室啊……那我最好過去看看他有沒有把房子炸掉。
-> ending

= ending
# portrait: left none
好了,不打擾你了,掰掰。
-> DONE