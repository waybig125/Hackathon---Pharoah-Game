import os
import re

file_path = "Assets/Scripts/Editor/StaticEgyptianCityGenerator.cs"
with open(file_path, "r", encoding="utf-8") as f:
    content = f.read()

# Make main class partial if it isn't already
if "public class StaticEgyptianCityGenerator" in content:
    content = content.replace("public class StaticEgyptianCityGenerator : EditorWindow", "public partial class StaticEgyptianCityGenerator : EditorWindow")

def extract_method(method_name, content):
    # Regex to find the start of the method
    pattern = r"(?:private|public|internal|protected|static)?\s*(?:(?:virtual|override|abstract|sealed|static)\s+)*[\w<>\[\]\.]+\s+" + method_name + r"\s*\([^)]*\)\s*\{"
    match = re.search(pattern, content)
    if not match:
        return None, content

    start_idx = match.start()
    
    # Also grab leading attributes like [MenuItem(...)]
    attr_pattern = r"\[MenuItem[^\]]+\]\s*"
    # Check backwards for attributes
    substring_before = content[:start_idx]
    # Find last newline
    lines_before = substring_before.split('\n')
    
    # We will just do a simple brace matching from start_idx
    brace_count = 0
    in_string = False
    in_char = False
    escape = False
    in_line_comment = False
    in_block_comment = False
    
    # Find the opening brace
    first_brace_idx = content.find('{', start_idx)
    if first_brace_idx == -1:
        return None, content
        
    end_idx = -1
    for i in range(first_brace_idx, len(content)):
        char = content[i]
        
        if escape:
            escape = False
            continue
            
        if char == '\\':
            escape = True
            continue
            
        if in_string:
            if char == '"':
                in_string = False
            continue
            
        if in_char:
            if char == "'":
                in_char = False
            continue
            
        if in_line_comment:
            if char == '\n':
                in_line_comment = False
            continue
            
        if in_block_comment:
            if char == '*' and i + 1 < len(content) and content[i+1] == '/':
                in_block_comment = False
            continue
            
        if char == '"':
            in_string = True
        elif char == "'":
            in_char = True
        elif char == '/' and i + 1 < len(content) and content[i+1] == '/':
            in_line_comment = True
        elif char == '/' and i + 1 < len(content) and content[i+1] == '*':
            in_block_comment = True
            
        elif char == '{':
            brace_count += 1
        elif char == '}':
            brace_count -= 1
            if brace_count == 0:
                end_idx = i + 1
                break
                
    if end_idx != -1:
        method_code = content[start_idx:end_idx]
        
        # Check if there is an attribute directly above it
        # We'll just look at the preceding text
        preceding_text = content[:start_idx]
        preceding_lines = preceding_text.split('\n')
        # grab any lines starting with [ right before the method
        attr_code = ""
        for line in reversed(preceding_lines[:-1]): # exclude the current line start
            if line.strip().startswith("[MenuItem"):
                attr_code = line + "\n" + attr_code
                start_idx -= len(line) + 1 # +1 for newline
            elif line.strip() == "":
                start_idx -= len(line) + 1
            else:
                break
                
        method_code = attr_code + method_code
        new_content = content[:start_idx] + content[end_idx:]
        return method_code, new_content
        
    return None, content

env_methods = ["CreateSeaAndCoastline", "CreateWorldBounds", "SetupEnvironment", "SetupPostProcessing", "GenerateSkyCloudNormalMap", "GetFBmNoise", "SeamlessNoise", "GetLitShader"]
arch_methods = ["BuildHouse", "PlacePlaza", "BuildProceduralLadderRamp", "CreateProceduralPyramid", "BuildProceduralObelisk", "BuildAlchemistTomb"]
spawner_methods = ["SetupMummyAnimations", "GetOrAddState", "ConfigureFbxToHumanoid", "SetupManagers", "FixPlayerAndWeapons", "SpawnDesertBrokenPillars", "SpawnPalmTreeOasis", "SpawnCityPalmTrees"]
opt_methods = ["DecimateMesh", "GetSharedDecimatedColumnMesh", "GetSharedDecimatedMesh", "DecimateRecursively", "AddLODGroupToPalmTree", "RemoveFloorsFromLandmarks", "CleanupOverlappingColumns", "GetTerrainHeight", "GetTerrainNormal", "GetMeshBottomWorldY", "AlignToGroundAndAddCollider", "GetBoundsCorners", "UpdateWeaponPrefabMaterials", "GetOrCreateMaterial"]

def create_file(filename, methods, current_content):
    extracted = []
    for m in methods:
        code, current_content = extract_method(m, current_content)
        if code:
            extracted.append(code)
    
    if extracted:
        file_body = """using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityMeshSimplifier;

namespace TheAlchemistsCrypt.Editor
{
    public partial class StaticEgyptianCityGenerator
    {
"""
        for code in extracted:
            # indent code properly
            indented_code = "\n".join(["        " + line if line else "" for line in code.split("\n")])
            file_body += indented_code + "\n\n"
            
        file_body += "    }\n}\n"
        
        with open("Assets/Scripts/Editor/" + filename, "w", encoding="utf-8") as f:
            f.write(file_body)
            
    return current_content

content = create_file("CityEnvironmentBuilder.cs", env_methods, content)
content = create_file("CityArchitectureBuilder.cs", arch_methods, content)
content = create_file("CityEntitySpawner.cs", spawner_methods, content)
content = create_file("CityOptimizationUtils.cs", opt_methods, content)

# Remove extra empty lines caused by extraction
content = re.sub(r'\n\s*\n\s*\n', '\n\n', content)

with open(file_path, "w", encoding="utf-8") as f:
    f.write(content)

print("Modularization complete! Files generated.")
