class_name GraphNodeTree
extends Node3D
const graphNode: PackedScene = preload("res://graphNode.tscn")
const nodeLine: PackedScene = preload("res://nodeLine.tscn")


var nodeList: Array[Graphnode]

var currentID: int


var positionList: Array[Vector3]

# Called when the node enters the scene tree for the first time.
func _ready():
	for child in get_children():
		child.queue_free()
	currentID = 0
	run_test()

func run_test():
	# Matrix 1 (starting position)
	var matrix: Array[Array] = [
		[0, 2, 1, 1],
		[0, 2, 0, 0],
		[0, 0, 0, 0],
		[0, 0, 0, 0],
		[0, 0, 0, 0]
	]
	var result = generate_all_states(matrix)
	var xInc: int = 10
	var yInc: int = 10
	var zInc: int = 10
	# goated seed for this example actually
	seed(21)
	for item in result:
		var looper: bool = true
		while looper == true:
			if randi() % 7 == 1:
				xInc += 10
			elif randi() % 7 == 1:
				yInc += 10
			elif randi() % 7 == 1:
				zInc += 10
			elif randi() % 7 == 1:
				xInc -= 10
			elif randi() % 7 == 1:
				yInc -= 10
			elif randi() % 7 == 1:
				zInc -= 10
				if !positionList.has(Vector3(xInc,yInc,zInc)):
					positionList.append(Vector3(xInc,yInc,zInc))
					create_new_node(xInc, yInc, zInc, item)
					looper = false




func create_new_node(xPos: float, yPos: float, zPos: float, matrix: Array[Array]):
	var newNode: Graphnode = graphNode.instantiate()
	newNode.ID = currentID
	newNode.set_node_position(xPos, yPos, zPos)
	newNode.boardMatrix = matrix
	add_child(newNode)
	nodeList.append(newNode)
	check_connection()
	currentID += 1

func check_connection():
	if currentID == 0:
		return
	for i in range (currentID):
		if check_matrices(nodeList[currentID].boardMatrix, nodeList[i].boardMatrix) == true:
			create_connection(currentID, i)
	

func check_matrices(matrix1: Array[Array], matrix2: Array[Array]):
	var changedCellVal: int = -1
	var differences: int = 0
	var matrix: int = 0
	var isValid: bool = false
	
	var i1: int = -1
	var j1: int = -1
	
	for i in matrix1.size():
		for j in matrix1[i].size():
			if matrix1[i][j] != matrix2[i][j]:
				differences += 1
				if differences > 2:
					return false
				
				
				if changedCellVal != -1:
					if matrix == 1 && changedCellVal != matrix2[i][j] && matrix1[i][j] == 0:
						return false
					elif matrix == 2 && changedCellVal != matrix1[i][j] && matrix2[i][j] == 0:
						return false
					elif abs((i1 + j1) - (i + j)) > 2:
						return false
					else:
						isValid = true
				
				if matrix1[i][j] == 0 && matrix2[i][j] != 0:
					changedCellVal = matrix2[i][j]
					matrix = 2
					i1 = i
					j1 = j
				elif matrix1[i][j] != 0:
					changedCellVal = matrix1[i][j]
					matrix = 1
					i1 = i
					j1 = j
				else:
					return false
	
	return isValid

func create_connection(firstID: int, secondID: int):
	var meshPoint: Vector3 = (nodeList[firstID].position + nodeList[secondID].position)/2
	var mesh: NodeLine = nodeLine.instantiate()
	mesh.position = meshPoint
	
	mesh.changeHeight(nodeList[firstID].position.distance_to(nodeList[secondID].position))
	mesh.look_at_from_position(mesh.position, nodeList[firstID].position, Vector3(0, 1, 0.001))
	# this is to fix the rotation, since the direction it faces is 90 degrees off from intended
	mesh.rotation_degrees.x += 90

	mesh.nodeID1 = firstID
	mesh.nodeID2 = secondID
	add_child(mesh)
	nodeList[firstID].connections.append(mesh)
	nodeList[secondID].connections.append(mesh)



# Called every frame. 'delta' is the elapsed time since the previous frame.
func _process(_delta):
	pass









func board_to_key(board: Array[Array]) -> String:
	var parts := []
	for row in board:
		parts.append(",".join(row.map(func(v): return str(v))))
	return "|".join(parts)


func clone_board(board: Array[Array]) -> Array[Array]:
	var new_board : Array[Array]= []
	for row in board:
		new_board.append(row.duplicate())
	return new_board


func get_piece_cells(board, id):
	var cells := []
	for y in range(board.size()):
		for x in range(board[y].size()):
			if board[y][x] == id:
				cells.append(Vector2i(x, y))
	return cells


func get_all_piece_ids(board):
	var ids := {}
	for row in board:
		for v in row:
			if v != 0:
				ids[v] = true
	return ids.keys()


func get_piece_size(cells):
	var min_x = cells[0].x
	var max_x = cells[0].x
	var min_y = cells[0].y
	var max_y = cells[0].y
	
	for c in cells:
		min_x = min(min_x, c.x)
		max_x = max(max_x, c.x)
		min_y = min(min_y, c.y)
		max_y = max(max_y, c.y)
	
	return Vector2i(max_x - min_x + 1, max_y - min_y + 1)


func get_valid_moves(board):
	var results := []
	var ids = get_all_piece_ids(board)
	
	for id in ids:
		var cells = get_piece_cells(board, id)
		var size = get_piece_size(cells)
		
		var directions := []
		
		# movement rules
		if size.x > size.y:
			directions = [Vector2i.LEFT, Vector2i.RIGHT]
		elif size.y > size.x:
			directions = [Vector2i.UP, Vector2i.DOWN]
		else:
			continue # shouldn't happen (only 2x1 or 1x2)
		
		for dir in directions:
			if can_move(board, id, cells, dir):
				var new_board = apply_move(board, id, cells, dir)
				results.append(new_board)
	
	return results


func can_move(board, id, cells, dir):
	for c in cells:
		var nx = c.x + dir.x
		var ny = c.y + dir.y
	
		if ny < 0 or ny >= board.size():
			return false
		if nx < 0 or nx >= board[0].size():
			return false
		
		if board[ny][nx] != 0 and board[ny][nx] != id:
			return false
	
	return true


func apply_move(board, id, cells, dir):
	var new_board = clone_board(board)

	# clear old
	for c in cells:
		new_board[c.y][c.x] = 0

	# place new
	for c in cells:
		new_board[c.y + dir.y][c.x + dir.x] = id

	return new_board


func generate_all_states(start_board):
	var visited := {}
	var queue := []
	
	var start_key = board_to_key(start_board)
	visited[start_key] = start_board
	queue.append(start_board)
	
	while queue.size() > 0:
		var current = queue.pop_front()
	
		var next_states = get_valid_moves(current)
	
		for state in next_states:
			var key = board_to_key(state)
	
			if not visited.has(key):
				visited[key] = state
				queue.append(state)
	
	return visited.values()


func _on_gemini_request_request_completed(result: int, response_code: int, headers: PackedStringArray, body: PackedByteArray) -> void:
	pass # Replace with function body.
