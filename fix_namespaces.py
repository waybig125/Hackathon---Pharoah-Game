import os

files = ["CityEnvironmentBuilder.cs", "CityArchitectureBuilder.cs", "CityEntitySpawner.cs", "CityOptimizationUtils.cs"]
missing_usings = """using Unity.AI.Navigation;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
"""

for file in files:
    path = "Assets/Scripts/Editor/" + file
    with open(path, "r", encoding="utf-8") as f:
        content = f.read()
    
    # insert after the first using UnityEngine;
    content = content.replace("using UnityEngine;\n", "using UnityEngine;\n" + missing_usings)
    
    with open(path, "w", encoding="utf-8") as f:
        f.write(content)

print("Fixed namespaces!")
