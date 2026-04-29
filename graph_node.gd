extends Area3D


# Called when the node enters the scene tree for the first time.
func _ready():
	pass # Replace with function body.


# Called every frame. 'delta' is the elapsed time since the previous frame.
func _process(_delta):
	pass


func set_node_position(xPos:float, yPos:float, zPos:float):
	$".".position = Vector3(xPos, yPos, zPos)


func _on_input_event(_camera, event, _event_position, _normal, _shape_idx):
	if event is InputEventMouseButton and event.pressed and event.button_index == MOUSE_BUTTON_LEFT:
		print("Shape Clicked!")
		#do other stuff to display current instance of graph
