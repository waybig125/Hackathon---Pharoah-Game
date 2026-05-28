import os

replacements = {
    "Assets/EgyptianAssets": "Assets/Art/EgyptianAssets",
    "Assets/Mummy_Assets": "Assets/Art/Mummy_Assets",
    "Assets/Materials": "Assets/Art/Materials",
    "Assets/Textures": "Assets/Art/Textures",
    "Assets/egypt_themed_icons": "Assets/Art/UI",
    "Assets/Shaders": "Assets/Art/Shaders"
}

for root, _, files in os.walk("Assets/Scripts/Editor"):
    for file in files:
        if file.endswith(".cs"):
            filepath = os.path.join(root, file)
            with open(filepath, 'r') as f:
                content = f.read()
            
            modified = content
            for old_path, new_path in replacements.items():
                modified = modified.replace(old_path, new_path)
            
            if modified != content:
                with open(filepath, 'w') as f:
                    f.write(modified)
                print(f"Fixed paths in {filepath}")

