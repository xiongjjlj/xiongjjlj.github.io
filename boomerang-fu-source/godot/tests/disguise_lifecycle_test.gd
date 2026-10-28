extends SceneTree
const C=preload("res://core/original_timekeeper.gd")
var differences:Array=[]
var frames:=0
func compare(actual:Variant,expected:Variant,path:String)->void:
	if expected is float or expected is int:
		if actual!=C.f32(expected) and differences.size()<12:differences.append({"path":path,"actual":actual,"expected":expected})
	elif expected is Array:
		for i in expected.size():compare(actual[i],expected[i],path+"/"+str(i))
	elif actual!=expected and differences.size()<12:differences.append({"path":path,"actual":actual,"expected":expected})
func _initialize()->void:
	var fixture:Dictionary=JSON.parse_string(FileAccess.get_file_as_string("res://../evidence/disguise-native-lifecycle.json"))
	for test in fixture.cases:
		var actor:=OriginalActorMotion.new()
		var inventory:=OriginalPlayerWeapons.new()
		inventory.initialize(actor,OriginalCollisionWorld.new())
		var state:=OriginalDisguise.new()
		state.initialize(inventory)
		state.replay_random=true
		state.available.assign(test.available)
		state.level_present=test.level_present
		for obstacle in test.obstacles:state.obstacles.append({"type":int(obstacle.type),"active":obstacle.active,"position":Vector3(obstacle.position[0],obstacle.position[1],obstacle.position[2])})
		for row in test.rows:
			var input:Dictionary=row.input
			actor.velocity=Vector2(input.velocity[0],input.velocity[1])
			actor.time_scaler=C.f32(input.scale)
			actor.is_stunned=input.stunned
			actor.is_showing_off=input.showing
			actor.is_dashing=input.dashing
			actor.is_lunging=input.lunging
			actor.is_frozen=input.frozen
			actor.can_walk=input.walk
			actor.is_spawning_in=input.spawning
			actor.state=OriginalActorMotion.State.DEAD if input.dead else OriginalActorMotion.State.ALIVE
			inventory.powers.active=2048 if input.power else 0
			state.hidden=input.hidden
			state.random_draws.assign(row.random)
			if input.action==1:state.break_disguise()
			state.frame_step(input.delta,input.playing)
			var actual:Dictionary={"disguised":state.disguised,"timer":state.timer,"current":state.time_current,"total":state.time_total,"selected":state.selected,"visible":not state.disguised,"scales":[],"offsets":[]}
			for i in state.types.size():
				var scale:Vector3=state.scales.get(i,Vector3.ONE)
				var offset:Vector3=state.offsets.get(i,Vector3.ZERO)
				actual.scales.append([scale.x,scale.y,scale.z])
				actual.offsets.append([offset.x,offset.y,offset.z])
			for key in row.state:compare(actual[key],row.state[key],str(test.variant)+"/"+str(frames)+"/"+key)
			compare(state.random_draws.size(),0,"random draw consumption")
			frames+=1
			state.events.clear()
	for test in fixture.nearest:
		var state:=OriginalDisguise.new()
		state.obstacles.assign([{"type":32,"active":true,"position":Vector3(-4,0,test.z)},{"type":8,"active":test.active,"position":Vector3(4,0,test.z)}])
		compare(state.nearest(Vector3.ZERO,test.radius),test.result,"nearest")
	var report:Dictionary={"passed":differences.is_empty(),"cases":fixture.cases.size(),"frames":frames,"nearest":fixture.nearest.size(),"differences":differences,"scope":fixture.scope,"full_game_parity_verified":false}
	FileAccess.open("res://../evidence/godot-native-disguise-check.json",FileAccess.WRITE).store_string(JSON.stringify(report,"\t")+"\n")
	print("BF_DISGUISE_NATIVE "+JSON.stringify(report))
	quit(0 if report.passed else 1)
