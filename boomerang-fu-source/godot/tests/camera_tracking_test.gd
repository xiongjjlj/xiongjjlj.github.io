extends SceneTree
const C = preload("res://core/original_timekeeper.gd")
var differences: Array = []
func vector(value: Array) -> Vector3: return Vector3(value[0],value[1],value[2])
func compare(frame: int, field: String, actual: Variant, expected: Variant) -> void:
	if expected is float: expected = C.f32(expected)
	if expected is Array: expected = vector(expected)
	if actual != expected and differences.size()<15: differences.append({"frame":frame,"field":field,"actual":actual,"expected":expected})
func _initialize() -> void:
	var fixture: Dictionary = JSON.parse_string(FileAccess.get_file_as_string("res://../evidence/camera-native-tracking.json"))
	var rig := OriginalCameraRig.new()
	rig.initialize(16.0/9.0)
	for frame in fixture.trace.size():
		var state: Dictionary = fixture.trace[frame]
		rig.frame_delta = C.f32(state.delta)
		for action in state.actions:
			if action.kind == "add": rig.add_target(action.id,vector(action.position),vector(action.size),action.influence,action.duration)
		for item in state.inputs: rig.set_target_position(item.id,vector(item.position))
		for action in state.actions:
			if action.kind == "remove": rig.remove_target(action.id,vector(action.position),action.duration)
		rig.advance_tracking(state.delta,not state.freeze and not state.paused)
		rig.step(state.delta,state.freeze,state.paused)
		compare(frame,"count",rig.tracked.size(),state.targets.size())
		for target in state.targets:
			if not rig.tracked.has(target.id): continue
			for key in ["position","last","size","influence","transitioning"]: compare(frame,target.id+"/"+key,rig.tracked[target.id][key],target[key])
		compare(frame,"focus",rig.focus,state.focus)
		compare(frame,"zoom",rig.current_zoom,state.zoom)
		compare(frame,"position",rig.position,state.position)
	var report := {"passed":differences.is_empty(),"frames":fixture.trace.size(),"differences":differences,"original_player_execution":true,"scope":"Original Camera3D add/remove coroutines with explicit scheduler; supplied transforms; no full native frame replay","full_game_parity_verified":false}
	FileAccess.open("res://../evidence/godot-native-camera-tracking-check.json",FileAccess.WRITE).store_string(JSON.stringify(report,"\t")+"\n")
	print(JSON.stringify(report))
	quit(0 if differences.is_empty() else 1)
