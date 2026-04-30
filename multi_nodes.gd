class_name GraphNodeTree
extends Node3D

@export var max_nodes: int = 500

var repulsion_strength: float = 6000.0
var stiffness: float = 60.0
var rest_length: float = 20.0
var damping: float = 0.98
var lerp_speed: float = 0.3

const graphNode: PackedScene = preload("res://graphNode.tscn")
const nodeLine: PackedScene = preload("res://nodeLine.tscn")

var nodeList: Array[Graphnode]
var currentID: int

func _ready():
	for child in get_children():
		child.queue_free()
	currentID = 0
	AutoloadSignals.graph_ready.connect(_on_graph_ready)
	AutoloadSignals.winning_path.connect(_on_path_ready)
	# Board may have already finished BFS before this node was ready
	if get_tree().root.has_meta("klotski_graph"):
		_on_graph_ready()

func _on_graph_ready():
	if nodeList.size() > 0:
		return
	var data: Dictionary = get_tree().root.get_meta("klotski_graph")
	seed(21)
	build_graph(data["adjacency"], data["win_states"], data["start"])

func build_graph(adjacency: Dictionary, win_states: Array, start_hash: String):
	# Pass 1: BFS-order node creation, capped at max_nodes
	var hash_to_id: Dictionary = {}
	var queue: Array = [start_hash]
	var queued: Dictionary = {start_hash: true}

	while queue.size() > 0 and currentID < max_nodes:
		var h: String = queue.pop_front()
		if hash_to_id.has(h):
			continue

		var pos := Vector3.ZERO if currentID == 0 \
			else Vector3(randf_range(-30, 30), randf_range(-30, 30), randf_range(-30, 30))
		create_new_node(pos.x, pos.y, pos.z, hash_to_matrix(h))
		hash_to_id[h] = currentID - 1  # currentID was incremented inside create_new_node

		if adjacency.has(h):
			for neighbor in adjacency[h]:
				if not queued.has(neighbor) and currentID < max_nodes:
					queue.append(neighbor)
					queued[neighbor] = true

	# Pass 2: create edges (only between nodes that were actually created)
	for h in hash_to_id:
		if not adjacency.has(h):
			continue
		var id1: int = hash_to_id[h]
		for neighbor in adjacency[h]:
			if hash_to_id.has(neighbor):
				var id2: int = hash_to_id[neighbor]
				if id1 < id2:  # each pair once
					create_connection(id1, id2)

	# Mark win nodes green
	for win_hash in win_states:
		if hash_to_id.has(win_hash):
			nodeList[hash_to_id[win_hash]].set_node_finish()

	print("Graph built: ", nodeList.size(), " nodes")

# Decodes a Board.cs state hash (20-char, column-major) back to Array[Array] int matrix
func hash_to_matrix(state_hash: String) -> Array[Array]:
	var matrix: Array[Array] = []
	for y in range(5):
		var row: Array = []
		for x in range(4):
			var ch: String = state_hash[y * 4 + x]
			row.append(0 if ch == "." else ch.to_int())
		matrix.append(row)
	return matrix

func create_new_node(xPos: float, yPos: float, zPos: float, matrix: Array[Array]):
	var newNode: Graphnode = graphNode.instantiate()
	newNode.ID = currentID
	newNode.set_node_position(xPos, yPos, zPos)
	newNode.boardMatrix = matrix
	add_child(newNode)
	nodeList.append(newNode)
	currentID += 1

func create_connection(firstID: int, secondID: int):
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

		# Repulsion: push away from every other node
		for other in nodeList:
			if node == other:
				continue
			var diff := node.position - other.position
			var dist := diff.length()
			if dist < 0.2:
				total_force += Vector3(randf(), randf(), randf()) * 500.0
			elif dist < 140.0:
				total_force += diff.normalized() * (repulsion_strength / (dist * dist))

		# Attraction: pull toward connected nodes
		for line in node.connections:
			var neighbor_id := line.nodeID1 if line.nodeID2 == node.ID else line.nodeID2
			var neighbor := nodeList[neighbor_id]
			var diff := neighbor.position - node.position
			var dist := diff.length()
			total_force += diff.normalized() * ((dist - rest_length) * stiffness)

		node.velocity += total_force * delta
		if node.velocity != Vector3.ZERO:
			node.velocity *= damping
		node.position = node.position.lerp(node.position + (node.velocity * delta), lerp_speed)

	update_all_connections()

func update_all_connections():
	for node in nodeList:
		for line in node.connections:
			var n1 := nodeList[line.nodeID1]
			var n2 := nodeList[line.nodeID2]
			var midpoint := (n1.position + n2.position) / 2.0
			line.position = midpoint
			var dist := midpoint.distance_to(n1.position)
			if dist > 0.05:
				line.scale.y = dist * 2.0
				line.look_at_from_position(midpoint, n1.position, Vector3.UP)
				line.rotation_degrees.x += 90

func board_to_key(board: Array[Array]) -> String:
	var parts := []
	for row in board:
		parts.append(",".join(row.map(func(v): return str(v))))
	return "|".join(parts)


func _on_path_ready(path: Array[String]):
	var trueArray: Array[Array] = []
	print("path is")
	print(path)
	for stri in path:
		var intermediate : Array[Array] = []
		for j in range(5):
			var intArr : Array[int] = []
			for i in range(4):
				if stri[i+(j*4)] == '.':
					intArr.append(0)
				else:
					intArr.append(int(stri[j*4+i]))
			intermediate.append(intArr)
		print(intermediate)
		trueArray.append(intermediate)
	find_good_path(trueArray)

func find_good_path(trueArray: Array[Array]):
	var i: int = 0
	var counter: int = 0
	for item in nodeList:
		if item.boardMatrix.hash() == trueArray[counter].hash():
			item.set_node_start()
			counter +=1
			i = item.ID
			break
	
	while nodeList[i].isWinningPosition == false:
		for connection in nodeList[i].connections:
			if nodeList[connection.nodeID1].boardMatrix == trueArray[counter]:
				i = connection.nodeID1
				print("set")
				connection.set_connection_shortpath()
				counter += 1
				break
			elif nodeList[connection.nodeID2].boardMatrix == trueArray[counter]:
				i = connection.nodeID2
				print("set")
				connection.set_connection_shortpath()
				counter += 1
				break
