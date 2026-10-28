class_name OriginalMatchPlaylist
extends RefCounted
## LevelManager.BuildMatchPlaylist and LoadNewLevel's cursor, FFA/DeathMatch.
## Chance.Shuffle uses Mono System.Random, separately from UnityEngine.Random.
var entries:Array=[]
var switch_incompatible:Array=[]
var playlist:Array[int]=[]
var match_index:=0
var powerup_start_delay:=4
var rng:=OriginalSystemRandom.new(0)

func initialize(catalog:Dictionary)->void:
	entries=catalog.entries.duplicate(true)
	for entry in entries:
		entry.supported_match_types=entry.supported_match_types.map(func(value):return int(value))
	switch_incompatible=catalog.get("switch_incompatible_indices",[]).map(func(value):return int(value))

func eligible(options:Dictionary)->Array[int]:
	var result:Array[int]=[]
	var count:=int(options.get("count",2))
	var custom:bool=options.get("custom",false)
	var solo:=entries.any(func(e):return e.solo)
	for e in entries:
		var id:=int(e.catalog_index)
		if options.get("switch",false) and not custom and switch_incompatible.has(id) and int(options.get("bots",0))>0:continue
		if solo and not e.solo:continue
		if not custom and (count<int(e.min_players) or count>int(e.max_players)):continue
		if not e.supported_match_types.has(0):continue
		if int(e.required_dlc)!=0 and not options.get("dlc",false):continue
		if custom and int(e.complexity)!=0 and options.get("disabled",[]).has(id):continue
		result.append(id)
	return result

func build(options:Dictionary,seed_value:Variant=null)->void:
	playlist.clear();match_index=0
	powerup_start_delay=1 if options.get("rematch",false) else 4
	if options.get("debug",false):
		for e in entries:
			if int(e.complexity)!=0:playlist.append(int(e.catalog_index))
		return
	var candidates:=eligible(options)
	if seed_value!=null:rng=OriginalSystemRandom.new(int(seed_value))
	# Original Chance.Shuffle: descending Fisher-Yates, System.Random.Next(max).
	for index in range(candidates.size()-1,0,-1):
		var chosen:=int(rng.next_double()*float(index+1))
		var value:=candidates[chosen]
		candidates[chosen]=candidates[index];candidates[index]=value
	var base_streak:=0
	var dlc_streak:=0
	var simple_left:=0 if options.get("rematch",false) or options.get("trailer",false) else 3
	var tutorials:=0
	for index in range(candidates.size()-1,-1,-1):
		var id:=candidates[index]
		if int(entries[id].complexity)==0:
			if not options.get("rematch",false) and tutorials==0 and options.get("tutorial",true):
				playlist.append(id);base_streak+=1;tutorials+=1
			candidates.remove_at(index)
	while not candidates.is_empty():
		var choices:Array[int]=[]
		for id in candidates:
			if int(entries[id].complexity)==1:choices.append(id)
		if simple_left>0 and not choices.is_empty():simple_left-=1
		else:choices=candidates.duplicate()
		var selected:=choices[0]
		if base_streak>=2:
			var dlc_choices:=choices.filter(func(id):return int(entries[id].required_dlc)!=0)
			if not dlc_choices.is_empty():
				selected=int(dlc_choices[0]);base_streak=0;dlc_streak=1
		elif dlc_streak>=2:
			var base_choices:=choices.filter(func(id):return int(entries[id].required_dlc)==0)
			if not base_choices.is_empty():
				selected=int(base_choices[0]);base_streak=1;dlc_streak=0
		else:
			if int(entries[selected].required_dlc)==0:base_streak+=1
			else:dlc_streak+=1
		playlist.append(selected);candidates.erase(selected)
	for index in range(playlist.size()-1,-1,-1):
		if entries[playlist[index]].force_first:
			var id:=playlist[index];playlist.remove_at(index);playlist.insert(0,id)

func consume_next()->int:
	if playlist.is_empty():return -1
	var id:=playlist[match_index%playlist.size()]
	if int(entries[id].complexity)==0:playlist.remove_at(match_index)
	else:match_index+=1
	return id
