extends Node3D
@export var graphNode: PackedScene

# Called when the node enters the scene tree for the first time.
func _ready():
	run_test()	

func run_test():
	var newNode = graphNode.instantiate()
	

# Called every frame. 'delta' is the elapsed time since the previous frame.
func _process(delta):
	pass
