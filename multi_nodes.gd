class_name GraphNodeTree
extends Node3D
const graphNode: PackedScene = preload("res://graphNode.tscn")
const nodeLine: PackedScene = preload("res://nodeLine.tscn")


var nodeList: Array[Graphnode]

var currentID: int

# Called when the node enters the scene tree for the first time.
func _ready():
	for child in get_children():
		child.queue_free()
	currentID = 0
	run_test()

func run_test():
	# Matrix 1 (starting position)
	var matrix1: Array[Array] = [
		[1, 1, 2, 2],
		[1, 1, 2, 2],
		[3, 4, 5, 6],
		[3, 7, 7, 6],
		[0, 8, 9, 0]
	]
	
	# Matrix 2 (one move away from Matrix 1)
	var matrix2: Array[Array] = [
		[1, 1, 2, 2],
		[1, 1, 2, 2],
		[3, 4, 5, 0],
		[3, 7, 7, 6],
		[0, 8, 9, 6]
	]
	
	# Matrix 3 (completely different position)
	var matrix3: Array[Array] = [
		[2, 2, 1, 0],
		[2, 2, 1, 1],
		[3, 3, 4, 5],
		[6, 7, 7, 5],
		[0, 8, 9, 0]
	]
	var matrix4: Array[Array] = [
		[2, 2, 1, 0],
		[2, 2, 1, 0],
		[3, 3, 4, 5],
		[6, 7, 7, 5],
		[0, 8, 9, 1]
	]
	
	
	create_new_node(0,0,-30, matrix1)
	await get_tree().create_timer(3.0).timeout
	print("Creating second node")
	create_new_node(10,10,-20, matrix2)
	
	await get_tree().create_timer(3.0).timeout
	
	create_new_node(10,30,-20, matrix3)
	#print("Connecting line")
	
	await get_tree().create_timer(3.0).timeout
	#create_connection(1,0)
	create_new_node(30,30,-20, matrix4)
	
	nodeList[0].set_node_finish()
	nodeList[0].connections[0].set_connection_shortpath()




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
	mesh.look_at_from_position(mesh.position, nodeList[firstID].position)
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
