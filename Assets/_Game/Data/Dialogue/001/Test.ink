-> start
VAR has_key = false
VAR gem_count = 0

=== start ===
你站在古老遺跡的入口，天色漸暗。

+ 進入遺跡 -> explore_ruins
+ 回村莊 -> return_village

=== explore_ruins ===
你踏進遺跡，石板地發出沉重的回響聲。

你發現了一扇門，上頭刻著：「帶著光明者得入」。

+ 嘗試打開門
    你推了推門，它紋絲不動。看來你需要某種鑰匙。
    -> look_around
+ 在附近找找 -> look_around

=== look_around ===
你仔細搜索四周……

~ temp roll = RANDOM(1, 3)

{roll == 1:
    你找到了一塊發光的寶石。
    ~ gem_count += 1
-> return_to_door
}

{roll == 2:
    你什麼也沒發現，只有塵土和蜘蛛網。
-> return_to_door
}

{roll == 3:
    你發現了一把古舊的鑰匙。
    ~ has_key = true
-> return_to_door
}


-> return_to_door

=== return_to_door ===
你回到門前。

{has_key:
    鑰匙發出微光，門緩緩打開了。
    -> treasure_room
- else:
    你仍無法打開門。也許該再搜尋看看？
    + 再找一次 -> look_around
    + 離開遺跡 -> return_village
}

=== treasure_room ===
門內是個閃爍著金光的寶庫。你的眼睛亮了起來！

~ gem_count += 3

你現在總共擁有 {gem_count} 顆寶石。

+ 拿走寶石離開 -> end
+ 繼續探索遺跡深處 -> deeper_ruins

=== deeper_ruins ===
你邁向更黑暗的通道，耳邊傳來低語……

-> END

=== return_village ===
你決定暫時離開遺跡，回到村莊休整。

-> END

=== end ===
你帶著寶藏回到村莊，成了傳說的一部分。

-> END
