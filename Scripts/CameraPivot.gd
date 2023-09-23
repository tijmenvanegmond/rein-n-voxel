extends Marker3D

@export var movement_speed : float = 3

@export var player : Node3D

# Called when the node enters the scene tree for the first time.
func _ready():
	pass # Replace with function body.


# Called every frame. 'delta' is the elapsed time since the previous frame.
func _process(delta):
	
	position = lerp(position, player.position, movement_speed * delta)
	
	pass
