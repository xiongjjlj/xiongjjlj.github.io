class_name FfaSetup
extends RefCounted
## SettingsManager.PrepareMatchLength and Level.ShuffleSpawnPoints, FFA/Kills.
enum Length { SHORT, MEDIUM, LONG }
var match_length := Length.MEDIUM
var roster: Array[String] = ["Avocado", "Watermelon"]
var sources: Array[int] = [-1, -2] # Keyboard one/two, otherwise Godot joypad ID.
var bot_difficulty := 1
const BOT_SOURCE = -100
static func is_bot(source:int)->bool:return source<=BOT_SOURCE

var original_spawns := false
var spawn_rng := OriginalUnityRandom.new(0xB00F1)

static func score_goal(length: int, count: int) -> int:
	return [[10, 15], [14, 16], [16, 24]][clampi(length, 0, 2)][0 if count < 3 else 1]

func spawn_indices(count: int, tie_breaker := false, finalists: Array = []) -> Array[int]:
	var order: Array[int] = [0, 1, 2, 3, 4, 5]
	var offset := spawn_rng.range_int(0, 2)
	if count > 2 and not tie_breaker:
		for index in count:
			var next := spawn_rng.range_int(index, count)
			var old := order[index]
			order[index] = order[next]
			order[next] = old
	spawn_rng.range_int(1, 3) # GameManager consumes this even outside team mode.
	var active := 0
	var result: Array[int] = []
	for index in count:
		if tie_breaker and not finalists.has(index):
			result.append(-1)
			continue
		result.append(order[offset + active * 2] if count == 2 or (tie_breaker and finalists.size() == 2) else order[active])
		active += 1
	return result
