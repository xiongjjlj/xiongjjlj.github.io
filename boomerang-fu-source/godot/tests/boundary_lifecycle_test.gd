extends SceneTree
const C=preload("res://core/original_timekeeper.gd")
var differences:Array=[]
var maximum_error:Dictionary={}
var comparisons:=0
var frames:=0
var inputs:Dictionary
func compare(actual:Variant,expected:Variant,path:String,tolerance:=0.0)->void:
	comparisons+=1
	if expected is float or expected is int:
		var error:=absf(float(actual)-C.f32(expected))
		var category:String=path.get_slice("/",0)
		maximum_error[category]=maxf(maximum_error.get(category,0.0),error)
		if error>tolerance and differences.size()<24:differences.append({"path":path,"actual":actual,"expected":expected,"error":error})
	elif expected is Array:
		for i in expected.size():compare(actual[i],expected[i],path+"/"+str(i),tolerance)
	elif actual!=expected and differences.size()<24:differences.append({"path":path,"actual":actual,"expected":expected})
func boundary()->OriginalLevelBoundary:
	var b:=OriginalLevelBoundary.new()
	b.initialize(inputs)
	return b
func _initialize()->void:
	inputs=JSON.parse_string(FileAccess.get_file_as_string("res://assets/original/boundary-inputs.json"))
	var fixture:Dictionary=JSON.parse_string(FileAccess.get_file_as_string("res://../evidence/boundary-native-lifecycle.json"))
	for test in fixture.shrinks:
		var b:=boundary()
		b.original_size=Vector2(test.size[0],test.size[1])
		b.size=b.original_size
		b.center=Vector2(test.center[0],test.center[1])
		b.angle=C.f32(test.angle)
		b.start(Vector2(test.target[0],test.target[1]),true)
		for row in test.frames:
			var playing:bool=not(row.frame>=test.pause and row.frame<test.pause+30 and test.pause>=0)
			b.frame_step(test.dt,playing)
			var prefix:String="shrink/"+test.name+"/"+str(row.frame)
			for key in ["timer","duration","shrinking","marker"]:compare(b.get(key),row[key],prefix+"/"+key)
			compare([b.size.x,b.size.y],row.size,"geometry/"+prefix+"/size",0.00002)
			compare([b.center.x,b.center.y],row.center,"geometry/"+prefix+"/center",0.00002)
			frames+=1
	for test in fixture.outside:
		var b:=boundary()
		b.shrinking=test.shrinking
		var actor:=OriginalActorMotion.new()
		actor.time_scaler=C.f32(test.scaler)
		var state:=OriginalActorBounds.new()
		state.boundary=b
		state.actor=actor
		var death_frame:=-1
		for row in test.frames:
			var i:int=row.frame
			var position:=Vector2(0 if i>=test.reenter and i<test.reenter+30 and test.reenter>=0 else 22,0)
			if i==test.fall:actor.state=OriginalActorMotion.State.FALLING
			if i==test.fall+30 and test.fall>=0:actor.state=OriginalActorMotion.State.ALIVE
			state.frame_step(test.dt,position)
			if state.death_requested and actor.state!=OriginalActorMotion.State.DEAD:
				actor.state=OriginalActorMotion.State.DEAD
				death_frame=i
			var actual:Dictionary={"timer":state.timer,"duration":b.out_duration,"outside":state.outside,"state":actor.state,"death_frame":death_frame,"danger":state.danger,"arrow":state.arrow,"effect":state.effect,"vibration":state.vibration,"metadata":state.metadata,"warnings":state.warnings}
			for key in actual:compare(actual[key],row[key],("pulse/" if key in ["danger","arrow","vibration"] else "outside/")+test.name+"/"+str(i)+"/"+key,0.00001 if key in ["danger","arrow","vibration"] else 0.0)
			frames+=1
	for test in fixture.queries:
		var b:=boundary()
		b.angle=C.f32(test.angle)
		var point:=Vector2(test.point[0],test.point[1])
		compare(b.is_in_bounds(point),test.inside,"query/inside/"+str(test))
		compare(b.is_near(point),test.near,"query/near/"+str(test))
		var clamped:=b.clamp_position(point,Vector2(5,5))
		compare([clamped.x,clamped.y],test.clamped,"clamp/"+str(test.angle)+"/"+str(point),0.00002)
	for test in fixture.killers:
		var actors:Array[OriginalActorMotion]=[OriginalActorMotion.new(),OriginalActorMotion.new()]
		var weapons:Array[OriginalPlayerWeapons]=[]
		var world:=OriginalCollisionWorld.new()
		for a in actors:
			var w:=OriginalPlayerWeapons.new()
			w.initialize(a,world)
			weapons.append(w)
		var combat:=OriginalCombatWorld.new()
		combat.initialize(actors,weapons,world)
		combat.boundary_owner=1
		combat.killer_ids[0]=[0,1,-1][int(test.kind)]
		combat.killer_timers[0]=C.f32(test.duration)
		actors[0].time_scaler=C.f32(test.scaler)
		for row in test.frames:
			combat.frame_step(C.f32(1.0/60.0))
			compare(combat.killer_ids[0],row.killer,"killer/id")
			compare(combat.killer_timers.get(0,0.0),row.timer,"killer/timer")
			var saved:int=combat.killer_ids[0]
			actors[0].state=OriginalActorMotion.State.ALIVE
			combat.kill(0,"outofbounds")
			compare(combat.killer_ids[0],row.death_owner,"killer/boundary_owner")
			combat.killer_ids[0]=saved
			combat.events.clear()
			frames+=1
	for test in fixture.games:
		var b:=boundary()
		b.round_timer=179.0
		b.tutorial=test.variant==3
		for row in test.frames:
			var i:int=row.frame
			var playing:bool=not(test.variant==2 and i<80)
			var survivors:int=1 if test.variant==4 or (test.variant==5 and i>100) else 2
			if test.variant==1:b.shrinking=i<90
			b.game_frame(C.f32(1.0/60.0),playing,survivors)
			for pair in [["round_timer","timer"],["sudden_death","sudden"],["shrinking","shrinking"],["marker","marker"],["owner","owner"]]:compare(b.get(pair[0]),row[pair[1]],"game/"+str(test.variant)+"/"+str(i)+"/"+pair[1])
			frames+=1
	var report:Dictionary={"passed":differences.is_empty(),"shrink_cases":fixture.shrinks.size(),"outside_cases":fixture.outside.size(),"killer_cases":fixture.killers.size(),"game_cases":fixture.games.size(),"frames":frames,"queries":fixture.queries.size(),"comparisons":comparisons,"maximum_error":maximum_error,"differences":differences,"full_game_parity_verified":false,"scope":"Unchanged native source block fixtures. Shrink position and Quaternion clamp tolerances 2e-5; visual pulse tolerance 1e-5. Timers, state decisions, death frames and killer IDs exact. Random sudden-death target distribution/RNG not compared."}
	FileAccess.open("res://../evidence/godot-native-boundary-check.json",FileAccess.WRITE).store_string(JSON.stringify(report,"\t")+"\n")
	print("BF_BOUNDARY_NATIVE "+JSON.stringify(report))
	quit(0 if report.passed else 1)
