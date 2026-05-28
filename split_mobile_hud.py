import os
import re

file_path = "Assets/Scripts/UI/MobileHUDButtons.cs"
with open(file_path, "r", encoding="utf-8") as f:
    content = f.read()

# Make main class partial
if "public class MobileHUDButtons : MonoBehaviour" in content:
    content = content.replace("public class MobileHUDButtons : MonoBehaviour", "public partial class MobileHUDButtons : MonoBehaviour")

def extract_method(method_name, content):
    pattern = r"(?:private|public|internal|protected|static)?\s*(?:(?:virtual|override|abstract|sealed|static)\s+)*[\w<>\[\]\.]+\s+" + method_name + r"\s*\([^)]*\)\s*\{"
    match = re.search(pattern, content)
    if not match:
        return None, content
    
    start_idx = match.start()
    
    # Grab preceding attributes (e.g., [SerializeField])
    preceding_text = content[:start_idx]
    preceding_lines = preceding_text.split('\n')
    attr_code = ""
    for line in reversed(preceding_lines[:-1]):
        if line.strip().startswith("["):
            attr_code = line + "\n" + attr_code
            start_idx -= len(line) + 1
        elif line.strip() == "":
            start_idx -= len(line) + 1
        else:
            break
            
    first_brace_idx = content.find('{', start_idx)
    if first_brace_idx == -1:
        return None, content
        
    brace_count = 0
    in_string = False
    in_char = False
    escape = False
    in_line_comment = False
    in_block_comment = False
    
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
        method_code = attr_code + method_code
        new_content = content[:start_idx] + content[end_idx:]
        return method_code, new_content
        
    return None, content

# Also a helper to extract nested classes like ButtonInputHelper
def extract_class(class_name, content):
    pattern = r"(?:private|public|internal|protected|static)?\s*(?:abstract\s+)?class\s+" + class_name + r"[\w\s:]*\{"
    match = re.search(pattern, content)
    if not match:
        return None, content
    
    start_idx = match.start()
    first_brace_idx = content.find('{', start_idx)
    
    brace_count = 0
    in_string = False
    in_char = False
    escape = False
    in_line_comment = False
    in_block_comment = False
    
    end_idx = -1
    for i in range(first_brace_idx, len(content)):
        char = content[i]
        if escape:
            escape = False; continue
        if char == '\\':
            escape = True; continue
        if in_string:
            if char == '"': in_string = False
            continue
        if in_char:
            if char == "'": in_char = False
            continue
        if in_line_comment:
            if char == '\n': in_line_comment = False
            continue
        if in_block_comment:
            if char == '*' and i+1 < len(content) and content[i+1] == '/': in_block_comment = False
            continue
            
        if char == '"': in_string = True
        elif char == "'": in_char = True
        elif char == '/' and i+1 < len(content) and content[i+1] == '/': in_line_comment = True
        elif char == '/' and i+1 < len(content) and content[i+1] == '*': in_block_comment = True
        elif char == '{': brace_count += 1
        elif char == '}':
            brace_count -= 1
            if brace_count == 0:
                end_idx = i + 1
                break
                
    if end_idx != -1:
        method_code = content[start_idx:end_idx]
        new_content = content[:start_idx] + content[end_idx:]
        return method_code, new_content
        
    return None, content

sprites_methods = ["LoadSpriteFromResources", "LoadThemedSprite", "LoadSlicedSpriteFromResources", "LoadSprites", "GenerateProceduralSprites", "CreateAlchemicalBarSprite", "CreateFireSymbolSprite", "CreateReloadSymbolSprite", "CreateSwapSymbolSprite", "CreateSprintSymbolSprite", "CreateJumpSymbolSprite", "CreateBorderSprite", "CreateSolidCircleSprite", "CreateHealthBarFillSprite", "CreateFramedBarSprite", "CreateSolidBarSprite", "CreateProceduralFocusIconSprite", "CreateProceduralHealthIconSprite", "CreateProceduralSulfurSprite", "CreateProceduralMercurySprite", "CreateProceduralSaltSprite", "CreateProceduralGradientSprite", "CreateObsidianSprite", "CreateGoldenGradientSprite", "CreateSlicedSandstoneFrameSprite", "CreateSlicedGoldTrimmedButtonSprite", "CreateSlicedEnergyGlowSprite", "CreateRingSprite", "CreateKnobSprite", "CreateCharcoalSprite", "CreateSettingsMedallionSprite"]
layout_methods = ["SetupCanvas", "BuildHUD", "CreateBlockButton", "CreateSprintBlockButton", "CreateButton", "CreateSprintButton"]
controls_methods = ["UpdateSprintVisuals", "HideDebugLabels", "TryInitializeCache", "DisableCompetingCanvases", "ShowMobileHUD", "HideMobileHUD"]
controls_classes = ["ButtonInputHelper", "DragHandler"]
panels_methods = ["ShowSettingsModal", "CloseSettingsModal", "ShowDeathScreen", "ShowNarrationPopup", "HideNarrationPopup", "ShowOrbTooltip", "HideOrbTooltip"]
updates_methods = ["UpdateHealth", "UpdateAmmo", "UpdateWeapon", "ShowWeaponUnlock", "ShowDamageVignette", "ShowDamageNotification", "ShowDeathNotification", "ShowVictoryNotification", "ShowGenericNotification", "HideAllNotifications", "AnimateFill", "AnimateAlpha", "ScalePunch"]

def create_file(filename, methods, classes, current_content):
    extracted = []
    for m in methods:
        code, current_content = extract_method(m, current_content)
        if code:
            extracted.append(code)
            
    for c in classes:
        code, current_content = extract_class(c, current_content)
        if code:
            extracted.append(code)
            
    if extracted:
        file_body = """using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;
using TMPro;

namespace TheAlchemistsCrypt.UI
{
    public partial class MobileHUDButtons
    {
"""
        for code in extracted:
            indented_code = "\n".join(["        " + line if line else "" for line in code.split("\n")])
            file_body += indented_code + "\n\n"
            
        file_body += "    }\n}\n"
        
        with open("Assets/Scripts/UI/" + filename, "w", encoding="utf-8") as f:
            f.write(file_body)
            
    return current_content

content = create_file("MobileHUDButtons_Sprites.cs", sprites_methods, [], content)
content = create_file("MobileHUDButtons_Layout.cs", layout_methods, [], content)
content = create_file("MobileHUDButtons_Controls.cs", controls_methods, controls_classes, content)
content = create_file("MobileHUDButtons_Panels.cs", panels_methods, [], content)
content = create_file("MobileHUDButtons_Updates.cs", updates_methods, [], content)

# Remove extra empty lines
content = re.sub(r'\n\s*\n\s*\n', '\n\n', content)

with open(file_path, "w", encoding="utf-8") as f:
    f.write(content)

print("MobileHUDButtons Modularization complete! Files generated.")
