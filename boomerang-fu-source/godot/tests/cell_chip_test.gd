extends SceneTree
var differences:Array=[]
var count:=0
func equal(a:Variant,b:Variant)->bool:
	if a is Array:
		for i in a.size():
			if not equal(a[i],b[i]):return false
		return true
	return a==OriginalTimekeeper.f32(b) if a is float else a==b
func _initialize()->void:
	var fixture:Dictionary=JSON.parse_string(FileAccess.get_file_as_string("res://../evidence/cell-native-grid.json"))
	var frames:=0
	for item in fixture.chips:
		var state:=OriginalActorElements.new()
		state.actor=OriginalActorMotion.new()
		state.actor.is_frozen=true
		state.actor.slide_factor=item.slide
		for i in item.frames.size():
			var frame:Dictionary=item.frames[i]
			if frame.input.attempt:state.chip(Vector2(frame.input.random[0],frame.input.random[1]))
			state.timers(frame.input.delta)
			var actual:Dictionary={"frozen":state.actor.is_frozen,"freeze_timer":state.freeze_timer,"chip_timer":state.chip_timer,"velocity":[state.actor.velocity.x,state.actor.velocity.y]}
			for key in actual:
				if not equal(actual[key],frame.state[key]):
					count+=1
					if differences.size()<20:differences.append({"delta":item.delta,"slide":item.slide,"frame":i,"field":key,"actual":actual[key],"expected":frame.state[key]})
			state.events.clear()
			frames+=1
	var report:Dictionary={"passed":count==0,"cases":fixture.chips.size(),"frames":frames,"mismatch_count":count,"differences":differences,"scope":"Unchanged native ChipOutOfFrozen and timer blocks; same sampled random directions injected, RNG engine equivalence not asserted","full_game_parity_verified":false}
	FileAccess.open("res://../evidence/godot-native-cell-chip-check.json",FileAccess.WRITE).store_string(JSON.stringify(report,"\t")+"\n")
	print("BF_CELL_CHIP_TEST "+JSON.stringify(report))
	quit(0 if report.passed else 1)
