import numpy as np
from PIL import Image
import os

def generate_seamless_clouds(size=2048):
    print(f"Generating {size}x{size} seamless cloud texture...")
    np.random.seed(42)  # Deterministic generation
    
    def perlin(res):
        d = size // res
        grid = np.mgrid[0:res:1/d, 0:res:1/d].transpose(1, 2, 0) % 1
        
        # Periodic gradients
        angles = 2 * np.pi * np.random.rand(res, res)
        gradients = np.empty((res + 1, res + 1, 2))
        gradients[:res, :res, 0] = np.cos(angles)
        gradients[:res, :res, 1] = np.sin(angles)
        gradients[-1, :res] = gradients[0, :res]
        gradients[:res, -1] = gradients[:res, 0]
        gradients[-1, -1] = gradients[0, 0]
        
        g00 = gradients[0:-1, 0:-1].repeat(d, 0).repeat(d, 1)
        g10 = gradients[1:, 0:-1].repeat(d, 0).repeat(d, 1)
        g01 = gradients[0:-1, 1:].repeat(d, 0).repeat(d, 1)
        g11 = gradients[1:, 1:].repeat(d, 0).repeat(d, 1)
        
        n00 = np.sum(grid * g00, 2)
        n10 = np.sum(np.dstack((grid[:,:,0]-1, grid[:,:,1])) * g10, 2)
        n01 = np.sum(np.dstack((grid[:,:,0], grid[:,:,1]-1)) * g01, 2)
        n11 = np.sum(np.dstack((grid[:,:,0]-1, grid[:,:,1]-1)) * g11, 2)
        
        # Fade function
        t = 6 * grid**5 - 15 * grid**4 + 10 * grid**3
        
        n0 = n00 * (1 - t[:,:,0]) + n10 * t[:,:,0]
        n1 = n01 * (1 - t[:,:,0]) + n11 * t[:,:,0]
        return n0 * (1 - t[:,:,1]) + n1 * t[:,:,1]

    # Generate FBM noise with multiple octaves
    density = np.zeros((size, size))
    weight_sum = 0
    
    # We want periodic noise at different grid frequencies.
    octaves = [
        (4, 0.45),
        (8, 0.25),
        (16, 0.15),
        (32, 0.08),
        (64, 0.04),
        (128, 0.02),
        (256, 0.01)
    ]
    
    for res, weight in octaves:
        n = perlin(res) / 0.707
        density += n * weight
        weight_sum += weight
        
    density /= weight_sum
    density = density * 0.5 + 0.5
    density = np.clip(density, 0, 1)
    
    # Calculate normals using finite differences (periodic boundary conditions)
    eps = 1.0 / size
    bumpStrength = 3.5
    
    density_u = np.roll(density, -1, axis=1) # shift left is x+1
    density_v = np.roll(density, -1, axis=0) # shift up is y+1
    
    du = (density_u - density) / eps
    dv = (density_v - density) / eps
    
    normal_x = -du * bumpStrength
    normal_y = -dv * bumpStrength
    normal_z = np.ones_like(density)
    
    length = np.sqrt(normal_x**2 + normal_y**2 + normal_z**2)
    normal_x /= length
    normal_y /= length
    normal_z /= length
    
    # Pack into RGBA
    r = ((normal_x * 0.5 + 0.5) * 255).astype(np.uint8)
    g = ((normal_y * 0.5 + 0.5) * 255).astype(np.uint8)
    b = ((normal_z * 0.5 + 0.5) * 255).astype(np.uint8)
    a = (density * 255).astype(np.uint8)
    
    img_data = np.dstack((r, g, b, a))
    img = Image.fromarray(img_data, 'RGBA')
    return img

if __name__ == "__main__":
    img = generate_seamless_clouds(2048)
    folder_path = "Assets/Resources/Textures"
    os.makedirs(folder_path, exist_ok=True)
    img.save(os.path.join(folder_path, "SkyCloudNormalMap.png"))
    print("Saved high-res cloud texture successfully!")
