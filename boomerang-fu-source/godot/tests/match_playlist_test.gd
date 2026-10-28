extends SceneTree
var failures:Array=[]
var comparisons:=0

func compare(actual,expected,case_index:int,field:String)->void:
	if expected is Array:
		compare(actual.size(),expected.size(),case_index,field+"/size")
		for i in mini(actual.size(),expected.size()):compare(actual[i],int(expected[i]),case_index,field+"/"+str(i))
		return
	comparisons+=1
	if actual!=expected and failures.size()<12:
		failures.append({"case":case_index,"field":field,"actual":actual,"expected":expected})

func _initialize()->void:
	var fixture:Dictionary=JSON.parse_string(FileAccess.get_file_as_string("res://../evidence/playlist-native.json"))
	var catalog:Dictionary=JSON.parse_string(FileAccess.get_file_as_string("res://assets/original/map-catalog.json"))
	for index in fixture.cases.size():
		var c:Dictionary=fixture.cases[index]
		var p=preload("res://core/original_match_playlist.gd").new()
		p.initialize(catalog)
		var options:Dictionary={"count":c.count,"dlc":c.dlc,"rematch":c.rematch,"tutorial":c.tutorial}
		if c.mode in ["custom","switch_custom"]:
			options.custom=true;options.disabled=[0,1,4,6,12,18,37,39,48,55]
		if c.mode=="custom_all_disabled":options.custom=true;options.disabled=range(58)
		if c.mode in ["switch_bots","switch_custom"]:options["switch"]=true;options.bots=1
		options.trailer=c.mode=="trailer";options.debug=c.mode=="debug"
		if c.mode=="solo":p.entries[1].solo=true;p.entries[37].solo=true
		if c.mode=="forced_first":p.entries[15].force_first=true;p.entries[40].force_first=true
		p.build(options,int(c.seed))
		compare(p.playlist,c.order,index,"order")
		compare(p.match_index,c.index,index,"index")
		compare(p.powerup_start_delay,c.delay,index,"powerup_delay")
		for n in c.consumed.size():compare(p.consume_next(),int(c.consumed[n]),index,"consume_%d"%n)
		compare(p.match_index,c.final_index,index,"final_index")
		compare(p.playlist,c.final_order,index,"final_order")
	var hashes:Dictionary={}
	for path in ["core/original_match_playlist.gd","core/original_system_random.gd","assets/original/map-catalog.json"]:
		hashes[path]=FileAccess.get_sha256("res://"+path)
	var report:Dictionary={"passed":failures.is_empty(),"cases":fixture.cases.size(),"comparisons":comparisons,"differences":failures,"source_sha256":hashes,"fixture_sha256":FileAccess.get_sha256("res://../evidence/playlist-native.json"),"scope":"Original Mono playlist method bodies and Chance.Shuffle: selection, ordering, tutorial removal, wraparound and initial powerup delay. Full-game scene transition and live seed scheduling are separate."}
	FileAccess.open("res://../evidence/godot-match-playlist-check.json",FileAccess.WRITE).store_string(JSON.stringify(report,"  ")+"\n")
	print("PLAYLIST_CHECK "+JSON.stringify(report))
	quit(0 if failures.is_empty() else 1)
