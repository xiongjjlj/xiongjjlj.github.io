extends SceneTree
const H=preload("res://core/original_floor_hazard.gd")
var differences:Array=[]
var comparisons:=0
var maximum_error:=0.0
func compare(a:Variant,b:Variant,label:String)->void:
	if b is Array:
		if a.size()!=b.size():differences.append({"label":label,"actual":a,"expected":b});return
		for i in b.size():compare(a[i],b[i],label+"/"+str(i))
	else:
		comparisons+=1
		var bad:bool=a!=b
		if a is float:
			var error:=absf(a-OriginalTimekeeper.f32(b));maximum_error=maxf(maximum_error,error);bad=error>0.0
		if bad and differences.size()<30:differences.append({"label":label,"actual":a,"expected":b})
func _initialize()->void:
	var fixture:Dictionary=JSON.parse_string(FileAccess.get_file_as_string("res://../evidence/floor-hazards-native.json"))
	var samples:=0
	for case_index in fixture.cases.size():
		var c:Dictionary=fixture.cases[case_index];var h:=H.new()
		h.initialize(c.trap,Vector3(5,-3,0.4),Color(1,0.1,0.2,0),OriginalUnityRandom.new(c.seed),c.size)
		for sample:Dictionary in c.samples:
			samples+=1;h.events.clear()
			if sample.activate:h.start_falling(sample.playing)
			h.frame_step(c.dt,sample.collapsing)
			var actual:=h.snapshot();var label:=str(case_index)+"/"+str(sample.tick)
			for key:String in actual:compare(actual[key],sample[key],label+"/"+key)
			# Object operations are integration-tested with real scene children.
			var calls:=h.events.filter(func(e):return e not in ["powerups","debris","release_children"])
			var expected:Array=sample.events.filter(func(e):return e not in ["powerup_fall","debris_hazard","reparent_projectile","sink_damage"])
			compare(calls,expected,label+"/service_calls")
	var report:={"passed":differences.is_empty(),"cases":fixture.cases.size(),"samples":samples,"comparisons":comparisons,"maximum_error":maximum_error,"differences":differences,"scope":"Unchanged recovered floor methods in original UnityPlayer versus Godot state, transforms, flash and service-call timing. Scene child operations tested separately."}
	FileAccess.open("res://../evidence/godot-floor-hazards-check.json",FileAccess.WRITE).store_string(JSON.stringify(report,"  "))
	print("BF_FLOOR_HAZARDS "+JSON.stringify(report));quit(0 if report.passed else 1)
