import trimesh
import os
import json

def analyze_assets(directory):
    analysis = {}
    files = [f for f in os.listdir(directory) if f.endswith('.glb')]
    
    for filename in files:
        path = os.path.join(directory, filename)
        try:
            scene = trimesh.load(path)
            # scene.extents gives [width, height, depth] roughly
            # but we need to know which axis is which
            
            # bounds is [[minX, minY, minZ], [maxX, maxY, maxZ]]
            bounds = scene.bounds
            size = bounds[1] - bounds[0]
            centroid = scene.centroid
            
            analysis[filename] = {
                "size": size.tolist(),
                "centroid": centroid.tolist(),
                "bounds_min": bounds[0].tolist(),
                "bounds_max": bounds[1].tolist(),
                "num_geometries": len(scene.geometry)
            }
        except Exception as e:
            analysis[filename] = {"error": str(e)}
            
    return analysis

if __name__ == "__main__":
    asset_dir = "Assets/Resources/more_items_for_map"
    results = analyze_assets(asset_dir)
    
    # Also analyze the palm trees in EgyptianAssets
    palm_dir = "Assets/EgyptianAssets"
    palm_files = ["realistic_hd_date_palm_2178.glb", "realistic_hd_date_palm_378.glb"]
    for pf in palm_files:
        path = os.path.join(palm_dir, pf)
        if os.path.exists(path):
            try:
                scene = trimesh.load(path)
                results[pf] = {
                    "size": (scene.bounds[1] - scene.bounds[0]).tolist(),
                    "centroid": scene.centroid.tolist(),
                    "bounds_min": scene.bounds[0].tolist(),
                    "bounds_max": scene.bounds[1].tolist(),
                    "num_geometries": len(scene.geometry)
                }
            except:
                pass

    print(json.dumps(results, indent=2))
