import os
from PIL import Image, ImageDraw

output_dir = "Assets/Resources/egypt_themed_icons_generated"
if not os.path.exists(output_dir):
    os.makedirs(output_dir)

# Helper for standard black
black = (0, 0, 0, 255)
transparent = (0, 0, 0, 0)
gold = (242, 204, 51, 255)  # #F2CC33 (0.95, 0.8, 0.2)
dark_bg = (15, 15, 15, 200)

def create_gold_trim_button(filename, size=256):
    img = Image.new('RGBA', (size, size), transparent)
    draw = ImageDraw.Draw(img)
    center = size // 2
    radius = size // 2 - 4
    
    # Draw gold border
    draw.ellipse((center - radius, center - radius, center + radius, center + radius), fill=gold)
    
    # Draw inner dark circle
    inner_radius = radius - 8
    draw.ellipse((center - inner_radius, center - inner_radius, center + inner_radius, center + inner_radius), fill=dark_bg)
    
    img.save(os.path.join(output_dir, filename))
    print(f"Generated {filename}")

def create_fire_icon(filename, size=128):
    img = Image.new('RGBA', (size, size), transparent)
    draw = ImageDraw.Draw(img)
    center = size // 2
    
    # Draw a target reticle
    draw.ellipse((center - 40, center - 40, center + 40, center + 40), outline=black, width=12)
    draw.ellipse((center - 15, center - 15, center + 15, center + 15), fill=black)
    
    # Crosshairs
    draw.line((center - 60, center, center - 45, center), fill=black, width=12)
    draw.line((center + 45, center, center + 60, center), fill=black, width=12)
    draw.line((center, center - 60, center, center - 45), fill=black, width=12)
    draw.line((center, center + 45, center, center + 60), fill=black, width=12)
    
    img.save(os.path.join(output_dir, filename))
    print(f"Generated {filename}")

def create_jump_icon(filename, size=128):
    img = Image.new('RGBA', (size, size), transparent)
    draw = ImageDraw.Draw(img)
    center = size // 2
    
    # Up Arrow
    draw.polygon([(center, center - 45), (center - 40, center + 10), (center + 40, center + 10)], fill=black)
    draw.rectangle((center - 15, center + 10, center + 15, center + 45), fill=black)
    
    img.save(os.path.join(output_dir, filename))
    print(f"Generated {filename}")

def create_sprint_icon(filename, size=128):
    img = Image.new('RGBA', (size, size), transparent)
    draw = ImageDraw.Draw(img)
    center = size // 2
    
    # Double chevron right
    draw.polygon([(center - 30, center - 35), (center, center), (center - 30, center + 35), (center - 10, center + 35), (center + 20, center), (center - 10, center - 35)], fill=black)
    draw.polygon([(center + 10, center - 35), (center + 40, center), (center + 10, center + 35), (center + 30, center + 35), (center + 60, center), (center + 30, center - 35)], fill=black)

    img.save(os.path.join(output_dir, filename))
    print(f"Generated {filename}")

def create_reload_icon(filename, size=128):
    img = Image.new('RGBA', (size, size), transparent)
    draw = ImageDraw.Draw(img)
    center = size // 2
    
    # Circular arrow
    bbox = (center - 45, center - 45, center + 45, center + 45)
    draw.arc(bbox, 45, 315, fill=black, width=15)
    
    # Arrow head
    draw.polygon([(center + 45, center - 20), (center + 25, center), (center + 65, center)], fill=black)

    img.save(os.path.join(output_dir, filename))
    print(f"Generated {filename}")

def create_swap_icon(filename, size=128):
    img = Image.new('RGBA', (size, size), transparent)
    draw = ImageDraw.Draw(img)
    center = size // 2
    
    # Two arrows opposite directions
    draw.rectangle((center - 40, center - 25, center + 20, center - 10), fill=black)
    draw.polygon([(center + 20, center - 35), (center + 20, center), (center + 50, center - 17)], fill=black)
    
    draw.rectangle((center - 20, center + 10, center + 40, center + 25), fill=black)
    draw.polygon([(center - 20, center + 35), (center - 20, center), (center - 50, center + 17)], fill=black)
    
    img.save(os.path.join(output_dir, filename))
    print(f"Generated {filename}")

def create_pause_icon(filename, size=128):
    img = Image.new('RGBA', (size, size), transparent)
    draw = ImageDraw.Draw(img)
    center = size // 2
    
    # Two vertical bars
    draw.rectangle((center - 25, center - 35, center - 5, center + 35), fill=black)
    draw.rectangle((center + 5, center - 35, center + 25, center + 35), fill=black)
    
    img.save(os.path.join(output_dir, filename))
    print(f"Generated {filename}")

if __name__ == "__main__":
    create_gold_trim_button("btn_gold_trim.png")
    create_fire_icon("icon_fire.png")
    create_jump_icon("icon_jump.png")
    create_sprint_icon("icon_sprint.png")
    create_reload_icon("icon_reload.png")
    create_swap_icon("icon_swap.png")
    create_pause_icon("icon_pause.png")
    print("All UI icons generated successfully!")
