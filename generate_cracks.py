import PIL.Image as Image
import PIL.ImageDraw as ImageDraw
import random
import numpy as np

def generate_cracks(size=512, num_cracks=20):
    img = Image.new('L', (size, size), 0)
    draw = ImageDraw.Draw(img)
    
    for _ in range(num_cracks):
        x = random.randint(0, size)
        y = random.randint(0, size)
        
        for _ in range(random.randint(3, 8)):
            nx = x + random.randint(-40, 40)
            ny = y + random.randint(-40, 40)
            width = random.randint(1, 2)
            draw.line((x, y, nx, ny), fill=255, width=width)
            x, y = nx, ny
            
    # Blur slightly
    from PIL import ImageFilter
    img = img.filter(ImageFilter.GaussianBlur(radius=0.5))
    
    return img

if __name__ == "__main__":
    cracks = generate_cracks()
    cracks.save("/Users/mac/Documents/Hackathon/Hackathon - Pharoah Game/Assets/Resources/Textures/ProceduralCracks.png")
    print("Cracks texture generated.")
