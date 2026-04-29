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
	create_new_node(0,0,0)
	await get_tree().create_timer(3.0).timeout
	print("Creating second node")
	create_new_node(10,10,10)
	
	await get_tree().create_timer(3.0).timeout
	
	print("Connecting line")
	
	create_connection(1,0)




func create_new_node(xPos: float, yPos: float, zPos: float):
	var newNode: Graphnode = graphNode.instantiate()
	newNode.ID = currentID
	newNode.set_node_position(xPos, yPos, zPos)
	add_child(newNode)
	nodeList.append(newNode)
	currentID += 1
	

func check_connection():
	#for i in range (currentID-1):
	#	if check_matrices(nodeList[currentID], nodeList[matrix2]) == true:
	#		create_connection()
	pass

func check_matrices(matrix1: Array[Array], matrix2: Array[Array]):
	for i in matrix1.size():
		for j in matrix1[i].size():
			if matrix1[i][j] != matrix2[i][j]:
				return false
	return true

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
