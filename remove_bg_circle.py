import sys
from PIL import Image, ImageDraw

def process(input_path):
    try:
        img = Image.open(input_path).convert("RGBA")
        width, height = img.size
        
        # Create a circular mask
        mask = Image.new('L', (width, height), 0)
        draw = ImageDraw.Draw(mask)
        # Draw a white circle
        draw.ellipse((0, 0, width, height), fill=255)
        
        # Apply the mask
        result = Image.new('RGBA', (width, height))
        result.paste(img, (0, 0), mask=mask)
        
        result.save(input_path)
        print(f"Successfully applied circular mask to {input_path}")
    except Exception as e:
        print(f"Error processing {input_path}: {e}")

if __name__ == '__main__':
    for path in sys.argv[1:]:
        print(f"Processing {path}...")
        process(path)
