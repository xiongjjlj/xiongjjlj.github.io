extends SceneTree
var cases: Array = []

func check(name: String, condition: bool) -> void:
	cases.append({"name": name, "passed": condition})

func _initialize() -> void:
	var score := OriginalFfaScore.new([0, 1, 2], 2)
	score.add_player_suicide(0)
	check("suicide debt remains until round end", score.net_score(0) == -1)
	score.end_round_scores()
	check("round end removes negative score debt", score.net_score(0) == 0 and score.scores[0].suicides == 0)
	score.scores[0].points = 1
	score.scores[1].points = 1
	score.actors[2].state = OriginalFfaScore.ActorState.DEAD
	check("do not end while another living player can tie", score.add_player_kill(0) == OriginalFfaScore.Decision.CHECK_SURVIVORS and score.winners == [0])
	score.add_player_suicide(0)
	check("post-goal suicide withdraws winner", score.winners.is_empty() and score.net_score(0) == 1)
	score.actors[1].state = OriginalFfaScore.ActorState.DEAD
	check("sole safe winner may end early", score.add_player_kill(0) == OriginalFfaScore.Decision.END_MATCH)
	score = OriginalFfaScore.new([0, 1], 1)
	score.actors[1].state = OriginalFfaScore.ActorState.DEAD
	score.actors[0].fire = true
	check("burning winner must wait for world outcome", score.add_player_kill(0) == OriginalFfaScore.Decision.CHECK_SURVIVORS)
	score = OriginalFfaScore.new([0, 1], 5)
	score.scores[0].points = 4
	score.actors[0].state = OriginalFfaScore.ActorState.FALLING
	check("falling is not treated as dead for potential win", score.player_could_win_this_round(0))
	score.tie_breaker = true
	score.add_player_suicide(1)
	check("tie breaker has no suicide penalty", score.scores[1].suicides == 0)
	check("tie breaker first kill wins below normal goal", score.add_player_kill(1) == OriginalFfaScore.Decision.END_MATCH and score.winners == [1])
	score = OriginalFfaScore.new([0, 1, 2], 3)
	score.scores[0].points = 4
	score.scores[1].points = 3
	score.scores[2].points = 4
	score.winners.assign([0, 1, 2])
	score.end_round_scores()
	check("retain all joint leaders and remove lower winner", score.winners == [0, 2])
	var passed := cases.all(func(c): return c.passed)
	var report := {"scope": "FFA scoring edge cases derived from recovered GameManager.cs; not a complete match or engine differential test", "passed": passed, "cases": cases}
	var args := OS.get_cmdline_user_args()
	if not args.is_empty():
		var file := FileAccess.open(args[0], FileAccess.WRITE)
		file.store_string(JSON.stringify(report, "  ") + "\n")
	print(JSON.stringify(report))
	quit(0 if passed else 1)
