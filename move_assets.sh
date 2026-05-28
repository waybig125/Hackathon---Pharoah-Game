#!/bin/bash
move_item() {
  src="$1"
  dest_dir="$2"
  if [ -e "$src" ]; then
    git mv "$src" "$dest_dir" 2>/dev/null || mv "$src" "$dest_dir"
    if [ -e "${src}.meta" ]; then
      git mv "${src}.meta" "$dest_dir" 2>/dev/null || mv "${src}.meta" "$dest_dir"
    fi
  fi
}

move_item "Assets/EgyptianAssets" "Assets/Art/"
move_item "Assets/Materials" "Assets/Art/"
move_item "Assets/Mummy_Assets" "Assets/Art/"
move_item "Assets/Textures" "Assets/Art/"
move_item "Assets/egypt_themed_icons" "Assets/Art/UI"
move_item "Assets/Shaders" "Assets/Art/"

move_item "Assets/column_properties_log.txt" "Assets/Logs/"
move_item "Assets/diagnostics_log.txt" "Assets/Logs/"
move_item "Assets/lights_log.txt" "Assets/Logs/"
move_item "Assets/stall_properties_log.txt" "Assets/Logs/"
move_item "Assets/testroot_log.txt" "Assets/Logs/"

