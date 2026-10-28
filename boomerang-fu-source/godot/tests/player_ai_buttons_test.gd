extends SceneTree
const B=preload("res://core/original_player_ai_buttons.gd")
const C=preload("res://core/original_timekeeper.gd")
var differences:Array=[]
var compared:=0
var frame_count:=0
var max_routines:=0
func compare(got:Dictionary,want:Dictionary,path:String)->void:
	for key:String in want:
		compared+=1
		var expected:Variant=want[key] if want[key] is bool else C.f32(want[key])
		if got[key]!=expected and differences.size()<60:differences.append({"path":path+"."+key,"got":got[key],"expected":expected})
func _initialize()->void:
	var fixture:Dictionary=JSON.parse_string(FileAccess.get_file_as_string("res://../evidence/player-ai-buttons-native-frames.json"))
	for scenario:Dictionary in fixture.cases:
		var buttons:=B.new();buttons.input_timer=C.f32(scenario.initial.timer);buttons.input_delay=C.f32(scenario.initial.delay)
		buttons.attack_pressed=scenario.initial.attack_pressed;buttons.dash_pressed=scenario.initial.dash_pressed
		buttons.attack=scenario.initial.attack;buttons.dash=scenario.initial.dash;buttons.released=scenario.initial.thrown;buttons.held=scenario.initial.held;buttons.blocked=scenario.initial.blocked
		for row:Dictionary in scenario.frames:
			buttons.begin_frame(C.f32(row.delta));buttons.advance_input_timer()
			for command:Dictionary in row.commands:
				match command.kind:
					"push":buttons.push_button(int(command.button))
					"hold":buttons.replace_hold(int(command.button),C.f32(command.duration))
					"cancel_hold":buttons.cancel(buttons.hold_handle)
					"release":buttons.release_button(int(command.button))
					"held":buttons.held=command.value
					"blocked":buttons.blocked=command.value
			var path:="case["+str(scenario.id)+"].frame["+str(row.frame)+"]"
			compare(buttons.snapshot(),row.ai,path+".ai")
			buttons.consume_release();compare(buttons.snapshot(),row.after_input,path+".after_input")
			buttons.coroutine_phase();compare(buttons.snapshot(),row.late,path+".late")
			max_routines=maxi(max_routines,buttons.routines.size());frame_count+=1
		buttons.clear()
		if not buttons.routines.is_empty() or not buttons.order.is_empty():differences.append({"reason":"routine cleanup failed","case":scenario.id})
	var report:={"passed":differences.is_empty(),"comparisons":compared,"frames":frame_count,"scenarios":fixture.cases.size(),"tolerance":0.0,"differences":differences,"maximum_pending_routines":max_routines,"full_player_ai_verified":false,"scope":"Original button bodies and real Unity coroutine phases, with scripted calls and the normal active-play PlayerInput release reset. Full bot/world scheduling remains separate."}
	FileAccess.open("res://../evidence/godot-player-ai-buttons-check.json",FileAccess.WRITE).store_string(JSON.stringify(report,"  ")+"\n")
	print("BF_PLAYER_AI_BUTTONS "+JSON.stringify(report));quit(0 if differences.is_empty() else 1)
