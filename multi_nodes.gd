class_name GraphNodeTree
extends Node3D

@export var max_nodes: int = 500

var trueArray: Array[Array] = []
var repulsion_strength: float = 19000.0
var stiffness: float = 10.0
var rest_length: float = 15.0
var damping: float = 0.985
var lerp_speed: float = 0.1

const graphNode: PackedScene = preload("res://graphNode.tscn")
const nodeLine: PackedScene = preload("res://nodeLine.tscn")

var nodeList: Array[Graphnode] = []
var currentID: int = 0
var _build_generation: int = 0

# Map Board.cs serialized states to their visible graph node.
# This lets the board tell the graph which arrangement should be highlighted.
var hashToNodeID: Dictionary = {}
var currentBoardNodeID: int = -1
var pendingCurrentStateHash: String = ""

signal call_good_path

func _ready():
	clear_graph()
	currentID = 0

	if not AutoloadSignals.graph_ready.is_connected(_on_graph_ready):
		AutoloadSignals.graph_ready.connect(_on_graph_ready)
	if not AutoloadSignals.winning_path.is_connected(_on_path_ready):
		AutoloadSignals.winning_path.connect(_on_path_ready)
	if not AutoloadSignals.board_position_changed.is_connected(_on_board_position_changed):
		AutoloadSignals.board_position_changed.connect(_on_board_position_changed)
	if not call_good_path.is_connected(find_good_path):
		call_good_path.connect(find_good_path)

	# Board may have already finished BFS before this node was ready.
	if get_tree().root.has_meta("klotski_graph"):
		_on_graph_ready()

func clear_graph():
	for child in get_children():
		child.queue_free()
	nodeList.clear()
	trueArray.clear()
	hashToNodeID.clear()
	currentBoardNodeID = -1
	pendingCurrentStateHash = ""
	currentID = 0

func _on_graph_ready():
	_build_generation += 1
	var generation := _build_generation
	clear_graph()

	var data: Dictionary = get_tree().root.get_meta("klotski_graph")
	seed(21)
	build_graph(data["adjacency"], data["win_states"], data["start"], generation)

func build_graph(adjacency: Dictionary, win_states: Array, start_hash: String, generation: int):
	var queue: Array = [start_hash]
	var queued: Dictionary = {start_hash: true}
	var spawn_delay := 0.0
	while queue.size() > 0 and currentID < max_nodes:
		if generation != _build_generation:
			return

		var h: String = queue.pop_front()
		if hashToNodeID.has(h):
			continue

		var pos := Vector3.ZERO if currentID == 0 else Vector3(randf_range(40, 100), randf_range(40, 100), randf_range(40, 100))
		create_new_node(pos.x, pos.y, pos.z, hash_to_matrix(h))

		var new_id: int = currentID - 1
		hashToNodeID[h] = new_id

		if h in win_states:
			nodeList[new_id].set_node_finish()

		if pendingCurrentStateHash == h:
			set_current_board_highlight_by_id(new_id)

		if adjacency.has(h):
			for neighbor in adjacency[h]:
				if hashToNodeID.has(neighbor):
					var other_id: int = hashToNodeID[neighbor]
					create_connection(new_id, other_id)

				if not queued.has(neighbor) and currentID < max_nodes:
					queue.append(neighbor)
					queued[neighbor] = true

		await get_tree().create_timer(spawn_delay).timeout

	if generation != _build_generation:
		return

	print("Graph built visually: ", nodeList.size(), " nodes")
	call_good_path.emit()

# Decodes a Board.cs state hash back to Array[Array] int matrix.
func hash_to_matrix(state_hash: String) -> Array[Array]:
	var side := int(sqrt(float(state_hash.length())))
	if side <= 0:
		side = 6

	var matrix: Array[Array] = []
	for y in range(side):
		var row: Array = []
		for x in range(side):
			var index := y * side + x
			var ch: String = state_hash[index]
			row.append(0 if ch == "." else ch.to_int())
		matrix.append(row)
	return matrix

func create_new_node(xPos: float, yPos: float, zPos: float, matrix: Array[Array]):
	var newNode: Graphnode = graphNode.instantiate()
	newNode.ID = currentID
	newNode.set_node_position(xPos, yPos, zPos)
	newNode.set_board_matrix(matrix)
	add_child(newNode)
	nodeList.append(newNode)
	currentID += 1

func create_connection(firstID: int, secondID: int):
	if firstID < 0 or secondID < 0 or firstID >= nodeList.size() or secondID >= nodeList.size():
		return

	# Avoid duplicate lines between the same two nodes.
	for existing in nodeList[firstID].connections:
		if (existing.nodeID1 == firstID and existing.nodeID2 == secondID) or (existing.nodeID1 == secondID and existing.nodeID2 == firstID):
			return

	var mesh: NodeLine = nodeLine.instantiate()
	mesh.create_line(nodeList[firstID].position, nodeList[secondID].position, firstID, secondID)
	mesh.material_override = load("res://whiteMat.tres")
	nodeList[firstID].connections.append(mesh)
	nodeList[secondID].connections.append(mesh)
	add_child(mesh)

func _process(delta: float):
	for node in nodeList:
		if node.ID == 0:
			continue

		var total_force := Vector3.ZERO

		for other in nodeList:
			if node == other:
				continue
			var diff := node.position - other.position
			var dist := diff.length()
			if dist < 0.2:
				total_force += Vector3(randf(), randf(), randf()) * 100.0
			elif dist < 15000.0:
				total_force += diff.normalized() * (repulsion_strength / (dist * dist))

		for line in node.connections:
			var neighbor_id := line.nodeID1 if line.nodeID2 == node.ID else line.nodeID2
			if neighbor_id < 0 or neighbor_id >= nodeList.size():
				continue
			var neighbor := nodeList[neighbor_id]
			var diff := neighbor.position - node.position
			var dist := diff.length()
			if dist > 0.01:
				total_force += diff.normalized() * ((dist - rest_length) * stiffness)

		node.velocity += total_force * delta
		if node.velocity != Vector3.ZERO:
			node.velocity *= damping
		node.position = node.position.lerp(node.position + (node.velocity * delta), lerp_speed)

	update_all_connections()

func update_all_connections():
	var updated: Dictionary = {}

	for node in nodeList:
		for line in node.connections:
			if updated.has(line):
				continue
			updated[line] = true

			if line.nodeID1 < 0 or line.nodeID2 < 0 or line.nodeID1 >= nodeList.size() or line.nodeID2 >= nodeList.size():
				continue

			var n1 := nodeList[line.nodeID1]
			var n2 := nodeList[line.nodeID2]
			var midpoint := (n1.position + n2.position) / 2.0
			line.position = midpoint
			var dist := n1.position.distance_to(n2.position)
			if dist > 0.05:
				line.scale.y = dist
				line.look_at_from_position(midpoint, n1.position, Vector3.UP)
				line.rotation_degrees.x += 90

func board_to_key(board: Array[Array]) -> String:
	var parts := []
	for row in board:
		parts.append(",".join(row.map(func(v): return str(v))))
	return "|".join(parts)

func _on_path_ready(path: Array[String]):
	trueArray.clear()

	for state_string in path:
		trueArray.append(hash_to_matrix(state_string))

func find_good_path():
	if trueArray.is_empty() or nodeList.is_empty():
		return

	var current_index := -1
	for item in nodeList:
		if item.boardMatrix == trueArray[0]:
			item.set_node_start()
			current_index = item.ID
			break

	if current_index == -1:
		print("Could not find the start node in the visible graph.")
		return

	for counter in range(1, trueArray.size()):
		var found_next := false

		for connection in nodeList[current_index].connections:
			var candidate_id := connection.nodeID1 if connection.nodeID2 == current_index else connection.nodeID2
			if candidate_id < 0 or candidate_id >= nodeList.size():
				continue

			if nodeList[candidate_id].boardMatrix == trueArray[counter]:
				current_index = candidate_id
				connection.set_connection_shortpath()
				found_next = true
				break

		if not found_next:
			print("Shortest path leaves the visible graph at step ", counter, ". Increase max_nodes if needed.")
			return

func _on_board_position_changed(state_hash: String):
	pendingCurrentStateHash = state_hash

	if hashToNodeID.has(state_hash):
		set_current_board_highlight_by_id(int(hashToNodeID[state_hash]))
	else:
		clear_current_board_highlight()
		print("Current board state is not visible in the graph yet. If this keeps happening, increase max_nodes.")

func set_current_board_highlight_by_id(node_id: int):
	if currentBoardNodeID == node_id and node_id >= 0 and node_id < nodeList.size():
		nodeList[node_id].set_node_current_board(true)
		return

	clear_current_board_highlight()

	if node_id < 0 or node_id >= nodeList.size():
		return

	currentBoardNodeID = node_id
	nodeList[currentBoardNodeID].set_node_current_board(true)

func clear_current_board_highlight():
	if currentBoardNodeID >= 0 and currentBoardNodeID < nodeList.size():
		nodeList[currentBoardNodeID].set_node_current_board(false)

	currentBoardNodeID = -1
