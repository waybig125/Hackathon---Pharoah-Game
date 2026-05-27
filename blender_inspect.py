import bpy
import os
import sys

def inspect_glb(filepath):
    # Clear existing data
    bpy.ops.wm.read_factory_settings(use_empty=True)
    
    try:
        # Import GLB
        bpy.ops.import_scene.gltf(filepath=filepath)
        
        print(f"REPORT FOR {os.path.basename(filepath)}:")
        
        # Check overall dimensions of all objects
        obs = bpy.context.selected_objects
        if not obs:
            print("  No objects found.")
            return

        for obj in obs:
            if obj.type == 'MESH':
                dims = obj.dimensions
                print(f"  Object: {obj.name}")
                print(f"    Dimensions: {dims.x:.3f}, {dims.y:.3f}, {dims.z:.3f}")
                print(f"    Verts: {len(obj.data.vertices)}")
                print(f"    Rotation (Euler): {obj.rotation_euler}")
        
        print("-" * 20)
    except Exception as e:
        print(f"  Failed to process {filepath}: {e}")

if __name__ == "__main__":
    asset_dir = "Assets/Resources/more_items_for_map"
    # List of interesting files
    files = [
        "egyptian_temple_complex_game_asset.glb",
        "egyptian_temples.glb",
        "stylized_egyptian_farmer.glb",
        "arabic_house_4.glb",
        "the_great_sphinx_of_giza_-_egypt.glb"
    ]
    
    for f in files:
        path = os.path.join(asset_dir, f)
        if os.path.exists(path):
            inspect_glb(path)
