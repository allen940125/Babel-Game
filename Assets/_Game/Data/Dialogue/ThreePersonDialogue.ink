-> start
// ThreePersonDialogue.ink
// 三人對話場景範例，支援角色圖片、左右邊設定、登場與退場（左右邊各只能同時存在一人）

=== start ===
# scene: 傍晚的遺跡前

# character: name=Alice, sprite=AliceImage, side=left
# character: name=Bob, sprite=BobImage, side=right
# character: name=Carol, sprite=CarolImage, side=left

# enter: Alice left
# speaker: Alice
Alice: "今天風有點大，遺跡的氣氛也太詭異了吧……"

# enter: Bob right
# speaker: Bob
Bob: "哈，別這麼膽小，Alice。妳不是說想探險嗎？"

# exit: Alice
# enter: Carol left
# speaker: Carol
Carol: "你們怎麼兩個先來了？我剛在村子聽到一些傳言，說這裡最近有人失蹤。"

# speaker: Bob
Bob: "哎呀，別搞得這麼可怕，我們只是進來看看而已。"

# exit: Carol
# enter: Alice left
# speaker: Alice
Alice: "如果真的有危險，我們是不是該準備點什麼？比如火把、繩索之類的？"

# speaker: Bob
Bob: "我準備好了照明棒，還有一些乾糧。我可不想餓著肚子探索這種地方。"

# exit: Bob
# enter: Carol right
# speaker: Carol
Carol: "說到乾糧，我帶了村裡新烤的麵包，要不要先吃點再進去？"

# speaker: Alice
Alice: "不錯喔，先吃點東西壯壯膽也好。"

# exit: Carol
# enter: Bob right
# speaker: Bob
Bob: "你們真的一點都不緊張嗎？萬一遇到怪物怎麼辦？"

# speaker: Alice
Alice: "所以我們要小心行動，別亂跑。"

# speaker: Bob
Bob: "……好吧，那我們準備出發吧。"

+ 「進入遺跡」 -> explore_ruins
+ 「還是回村莊好了」 -> return_village

=== explore_ruins ===
# scene: 他們走進遺跡
# exit: Alice
# exit: Bob
# enter: Carol
# speaker: Carol
Carol: "哇……裡面比我想像中還要陰暗。妳們快點點火。"

+ 「繼續前進」 -> deeper_ruins

=== return_village ===
# scene: 回村莊了
# exit: Alice
# exit: Bob
# exit: Carol

-> DONE

=== deeper_ruins ===
# scene: 更深的遺跡
# enter: Alice
# speaker: Alice
Alice: "這裡的牆壁上有奇怪的圖案……你們看這個，是不是像一把鑰匙？"

# exit: Alice
# enter: Bob
# speaker: Bob
Bob: "我覺得我們可能觸發了什麼機關……聽，是不是有聲音？"

# exit: Bob
# enter: Carol
# speaker: Carol
Carol: "我們該不會要開啟什麼古老詛咒吧……還是回去比較好？"

+ 「留下來探索」 -> END
+ 「離開這裡」 -> return_village
