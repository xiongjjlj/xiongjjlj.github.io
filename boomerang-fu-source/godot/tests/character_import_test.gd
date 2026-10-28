extends SceneTree

func _initialize() -> void:
	call_deferred("run")

func run() -> void:
	var report: Array = []
	var success := true
	for character_name in ["Avocado", "Watermelon"]:
		var actor := OriginalCharacterPreview.new()
		actor.name = character_name
		root.add_child(actor)
		var base: String = "res://assets/characters/" + character_name
		actor.configure(base + ".glb", base + "-idle.json")
		var before: Array = []
		for skeleton in actor.skeletons:
			for bone in skeleton.get_bone_count(): before.append(skeleton.get_bone_pose(bone))
		actor.apply_pose(0.317)
		var changed := 0
		var index := 0
		for skeleton in actor.skeletons:
			for bone in skeleton.get_bone_count():
				if before[index] != skeleton.get_bone_pose(bone): changed += 1
				index += 1
		var missing := actor.missing_targets.duplicate()
		var checked_clips: Array[String] = ["idle"]
		for clip in ["walk", "death", "windup-right", "windup-left", "melee-right", "melee-left", "disarm", "stunned", "fall"]:
			actor.set_clip(base + "-" + clip + ".json")
			actor.apply_pose(minf(0.317, actor.clip.duration * 0.5))
			missing.append_array(actor.missing_targets)
			checked_clips.append(clip)
		var passed := missing.is_empty() and changed > 0 and not actor.skeletons.is_empty()
		success = success and passed
		report.append({"character": character_name, "passed": passed, "bones": before.size(),
			"changed_bones": changed, "unresolved_targets": missing, "skeletons": actor.skeletons.size(), "checked_clips": checked_clips})
		actor.free()
	var result := {"passed": success, "characters": report, "gameplay_parity_verified": false,
		"visual_parity_verified": false, "note": "Checks Godot import, binding coverage, and pose changes; not Unity frame equivalence."}
	var file := FileAccess.open("res://../evidence/godot-character-import-check.json", FileAccess.WRITE)
	file.store_string(JSON.stringify(result, "\t") + "\n")
	file.close()
	print(JSON.stringify(result))
	quit(0 if success else 1)
