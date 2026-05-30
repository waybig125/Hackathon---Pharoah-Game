import numpy as np
from PIL import Image
import matplotlib.colors as mcolors

def apply_color_morph(input_path, output_path):
    # Load image and ensure 4-channel RGBA
    img = Image.open(input_path).convert("RGBA")
    data = np.array(img)
    
    r, g, b, a = data[:,:,0], data[:,:,1], data[:,:,2], data[:,:,3]
    
    # Convert to normalized HSV space
    rgb_normalized = np.dstack((r, g, b)) / 255.0
    hsv = mcolors.rgb_to_hsv(rgb_normalized)
    h, s, v = hsv[:,:,0], hsv[:,:,1], hsv[:,:,2]

    # --- 1. MORPH TEAL/GREEN -> DEEP INDIGO STONE ---
    # Target center of your teal ring hue (approx 0.48)
    dist_teal = np.abs(h - 0.48)
    dist_teal = np.minimum(dist_teal, 1.0 - dist_teal)
    # Smooth Gaussian-like falloff for selection
    teal_mask = np.exp(-((dist_teal / 0.08) ** 2)) * (s > 0.1) * (v > 0.1)

    # Shift teal hue to deep indigo (~0.66), deepen value, and enrich saturation
    h = np.where(teal_mask > 0, (h + (0.18 * teal_mask)) % 1.0, h)
    s = np.where(teal_mask > 0, np.clip(s + (0.15 * teal_mask), 0, 1), s)
    v = np.where(teal_mask > 0, np.clip(v - (0.25 * teal_mask), 0, 1), v)

    # --- 2. MORPH GOLD -> PATINATED BRONZE & RUBY GRADIENT ---
    # Target the gold hue family (approx 0.12)
    dist_gold = np.abs(h - 0.12)
    dist_gold = np.minimum(dist_gold, 1.0 - dist_gold)
    gold_mask = np.exp(-((dist_gold / 0.07) ** 2)) * (s > 0.15) * (v > 0.15)

    # We split behavior dynamically based on original pixel luminosity (v)
    # Brighter highlights go towards ruby flame, mid/dark tones go to deep bronze
    is_bright = v > 0.65
    
    # Ruby Crimson Shift parameters
    ruby_h = (h - 0.14) % 1.0  # Shift down into rich reds/crimson
    ruby_s = np.clip(s * 1.1, 0, 1)
    ruby_v = np.clip(v * 0.85, 0, 1)

    # Aged Patinated Bronze parameters
    bronze_h = (h - 0.04) % 1.0 # Slight shift to copper/bronze
    bronze_s = np.clip(s * 0.70, 0, 1) # Desaturate to look old/metallic
    bronze_v = np.clip(v * 0.75, 0, 1) # Darken into cast metal

    # Dynamically select target values based on luminosity map
    target_h = np.where(is_bright, ruby_h, bronze_h)
    target_s = np.where(is_bright, ruby_s, bronze_s)
    target_v = np.where(is_bright, ruby_v, bronze_v)

    # Smoothly blend the changes back into the gold areas using our selection mask
    h = (1.0 - gold_mask) * h + gold_mask * target_h
    s = (1.0 - gold_mask) * s + gold_mask * target_s
    v = (1.0 - gold_mask) * v + gold_mask * target_v

    # --- 3. RECONSTRUCT ASSET ---
    new_hsv = np.dstack((h, s, v))
    new_rgb = (mcolors.hsv_to_rgb(new_hsv) * 255).astype(np.uint8)
    
    # Re-attach original unmodified alpha channel for perfect UI transparency
    final_data = np.dstack((new_rgb, a))
    
    # Save image out
    Image.fromarray(final_data, "RGBA").save(output_path)
    print(f"Asset successfully processed and saved to: {output_path}")

if __name__ == "__main__":

    icons = [
        "Assets/Resources/egypt_themed_icons/fire.png",
        "Assets/Resources/egypt_themed_icons/swap_weapon.png",
        "Assets/Resources/egypt_themed_icons/reload_ammo.png",
        "Assets/Resources/egypt_themed_icons/sprint.png",
        "Assets/Resources/egypt_themed_icons/focus_icon.png",
        "Assets/Resources/egypt_themed_icons/jump.png",
    ]

    # icons = [
    #     "Assets/Resources/egypt_themed_icons/joystick_outer.png",
    #     "Assets/Resources/egypt_themed_icons/joystick_knob.png",
    # ]

    for icon in icons:
        apply_color_morph(icon, icon.replace(".png", "_a.png"))