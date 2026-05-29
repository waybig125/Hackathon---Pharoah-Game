import os
import math
from PIL import Image, ImageEnhance, ImageOps, ImageDraw

input_dir = "Assets/Resources/egypt_themed_icons"
output_dir = "Assets/Resources/egypt_themed_icons_generated"

if not os.path.exists(output_dir):
    os.makedirs(output_dir)

def rgb_to_hsv(r, g, b):
    r_n, g_n, b_n = r / 255.0, g / 255.0, b / 255.0
    mx = max(r_n, g_n, b_n)
    mn = min(r_n, g_n, b_n)
    df = mx - mn
    if mx == mn:
        h = 0
    elif mx == r_n:
        h = (60 * ((g_n - b_n) / df) + 360) % 360
    elif mx == g_n:
        h = (60 * ((b_n - r_n) / df) + 120) % 360
    elif mx == b_n:
        h = (60 * ((r_n - g_n) / df) + 240) % 360
    s = 0 if mx == 0 else (df / mx)
    v = mx
    return h, s, v

def hsv_to_rgb(h, s, v):
    h = h % 360
    c = v * s
    x = c * (1 - abs((h / 60.0) % 2 - 1))
    m = v - c
    if 0 <= h < 60:
        r_n, g_n, b_n = c, x, 0
    elif 60 <= h < 120:
        r_n, g_n, b_n = x, c, 0
    elif 120 <= h < 180:
        r_n, g_n, b_n = 0, c, x
    elif 180 <= h < 240:
        r_n, g_n, b_n = 0, x, c
    elif 240 <= h < 300:
        r_n, g_n, b_n = x, 0, c
    else:
        r_n, g_n, b_n = c, 0, x
    return int((r_n + m) * 255), int((g_n + m) * 255), int((b_n + m) * 255)

def unify_colors(in_filename, out_filename, target_hue=42.0, saturation_mult=1.1, value_mult=1.05):
    in_path = os.path.join(input_dir, in_filename)
    out_path = os.path.join(output_dir, out_filename)
    
    if not os.path.exists(in_path):
        print(f"Error: {in_path} does not exist!")
        return

    img = Image.open(in_path).convert("RGBA")
    pixels = img.load()
    width, height = img.size

    for y in range(height):
        for x in range(width):
            r, g, b, a = pixels[x, y]
            if a == 0:
                continue
            
            h, s, v = rgb_to_hsv(r, g, b)
            
            # Map hue strictly to the unified gold/brown hue
            h_new = target_hue
            
            # Enhance/unify saturation and value slightly
            s_new = min(1.0, s * saturation_mult)
            v_new = min(1.0, v * value_mult)
            
            r_new, g_new, b_new = hsv_to_rgb(h_new, s_new, v_new)
            pixels[x, y] = (r_new, g_new, b_new, a)
            
    img.save(out_path)
    print(f"Unified color: {in_filename} -> {out_filename} ({width}x{height})")

def generate_gold_trim_button(size=512, border_width=48, corner_radius=64):
    out_path = os.path.join(output_dir, "btn_gold_trim.png")
    img = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    pixels = img.load()
    
    # Target gold colors
    target_hue = 42.0
    
    for y in range(size):
        for x in range(size):
            # Compute distance to closest edge
            dist_x = min(x, size - 1 - x)
            dist_y = min(y, size - 1 - y)
            
            # Corner rounding check
            in_corner = False
            cx, cy = 0, 0
            if x < corner_radius and y < corner_radius:
                cx, cy = corner_radius, corner_radius
                in_corner = True
            elif x >= size - corner_radius and y < corner_radius:
                cx, cy = size - corner_radius, corner_radius
                in_corner = True
            elif x < corner_radius and y >= size - corner_radius:
                cx, cy = corner_radius, size - corner_radius
                in_corner = True
            elif x >= size - corner_radius and y >= size - corner_radius:
                cx, cy = size - corner_radius, size - corner_radius
                in_corner = True
                
            dist_to_edge = 0
            if in_corner:
                d = math.sqrt((x - cx) ** 2 + (y - cy) ** 2)
                if d > corner_radius:
                    # Outside the rounded corner
                    continue
                dist_to_edge = corner_radius - d
            else:
                dist_to_edge = min(dist_x, dist_y)
                
            if dist_to_edge < border_width:
                # Border region: Create a beautiful 3D gold bevel
                # We lerp the value from dark gold (edge) to bright gold (mid) to dark gold (inner edge)
                t = dist_to_edge / border_width
                # Bevel profile
                intensity = math.sin(t * math.pi)
                
                # Outer highlight
                if t < 0.2:
                    val = 0.5 + 0.3 * (t / 0.2)
                # Middle bright
                elif t < 0.7:
                    val = 0.8 + 0.2 * ((t - 0.2) / 0.5)
                # Inner shadow
                else:
                    val = 1.0 - 0.5 * ((t - 0.7) / 0.3)
                    
                sat = 0.85
                r, g, b = hsv_to_rgb(target_hue, sat, val)
                # Semi-transparent/glowing borders
                pixels[x, y] = (r, g, b, 255)
            else:
                # Center background: semi-transparent dark obsidian
                # Bevel fading inner shadow for center
                inner_t = min(1.0, (dist_to_edge - border_width) / 16.0)
                alpha = int(180 + 30 * inner_t)
                val = 0.08 + 0.04 * inner_t
                sat = 0.2
                r, g, b = hsv_to_rgb(target_hue, sat, val)
                pixels[x, y] = (r, g, b, alpha)
                
    img.save(out_path)
    print(f"Generated high-res btn_gold_trim.png ({size}x{size})")

if __name__ == "__main__":
    # 1. Process standard action buttons
    mappings = {
        "fire.png": "icon_fire.png",
        "jump.png": "icon_jump.png",
        "sprint.png": "icon_sprint.png",
        "reload_ammo.png": "icon_reload.png", # Map to the exact loader name
        "swap_weapon.png": "icon_swap.png",   # Map to the exact loader name
        "joystick_outer.png": "joystick_ring.png",
        "joystick_knob.png": "joystick_knob.png"
    }
    
    for src, dest in mappings.items():
        # Joystick components can be slightly less saturated/different values to stand out
        if "joystick" in src:
            unify_colors(src, dest, target_hue=42.0, saturation_mult=0.75, value_mult=0.9)
        else:
            unify_colors(src, dest, target_hue=42.0, saturation_mult=1.1, value_mult=1.0)
            
    # 2. Generate high-quality btn_gold_trim
    generate_gold_trim_button()
    
    # 3. Procedural Obsidian Texture
    obsidian_path = os.path.join(output_dir, "obsidian_texture.png")
    obsidian = Image.new("RGBA", (256, 256), (15, 12, 10, 255))
    obsidian.save(obsidian_path)
    print("Saved placeholder obsidian_texture.png")
    
    print("All UI assets processed successfully!")
