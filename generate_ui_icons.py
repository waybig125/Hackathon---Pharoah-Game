import os
from PIL import Image, ImageEnhance, ImageOps

input_dir = "Assets/Resources/egypt_themed_icons"
output_dir = "Assets/Resources/egypt_themed_icons_generated"

if not os.path.exists(output_dir):
    os.makedirs(output_dir)

# We will apply a uniform golden tint to unify all icons.
def unify_icon_colors(filename):
    in_path = os.path.join(input_dir, filename)
    out_path = os.path.join(output_dir, "icon_" + filename)
    
    if not os.path.exists(in_path):
        return

    # Open image
    img = Image.open(in_path).convert("RGBA")
    
    # 1. Enhance contrast to make shadows pop
    enhancer = ImageEnhance.Contrast(img)
    img = enhancer.enhance(1.2)
    
    # 2. Adjust color (saturation)
    enhancer = ImageEnhance.Color(img)
    img = enhancer.enhance(1.1)

    # 3. Apply a slight golden warming tint to unify the color palettes
    # We do this by blending a solid golden color over the image using 'multiply' or 'overlay'
    r, g, b, a = img.split()
    # Convert RGB to HSV or just apply a slight tint
    tint = Image.new("RGBA", img.size, (255, 230, 150, 255)) # Warm golden tint
    
    # Blend using a soft mix
    img_rgb = Image.merge("RGB", (r,g,b))
    tint_rgb = tint.convert("RGB")
    
    # A simple blend
    blended = Image.blend(img_rgb, tint_rgb, alpha=0.15)
    
    # Re-apply alpha channel
    final_img = Image.merge("RGBA", (blended.split()[0], blended.split()[1], blended.split()[2], a))
    
    final_img.save(out_path)
    print(f"Processed and unified {filename} -> icon_{filename}")

if __name__ == "__main__":
    icons = ["fire.png", "jump.png", "sprint.png", "reload_ammo.png", "swap_weapon.png"]
    for icon in icons:
        unify_icon_colors(icon)
    print("All icons have been color-matched and processed successfully!")
