import numpy as np
from PIL import Image
import os

def generate_seamless_clouds(size=2048):
    print(f"Generating {size}x{size} seamless fractal cloud texture...")
    
    # Generate uniform noise maps for different frequencies
    def generate_perlin_map(res, seed):
        np.random.seed(seed)
        d = size // res
        grid = np.mgrid[0:res:1/d, 0:res:1/d].transpose(1, 2, 0) % 1
        
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
        
        t = 6 * grid**5 - 15 * grid**4 + 10 * grid**3
        n0 = n00 * (1 - t[:,:,0]) + n10 * t[:,:,0]
        n1 = n01 * (1 - t[:,:,0]) + n11 * t[:,:,0]
        return (n0 * (1 - t[:,:,1]) + n1 * t[:,:,1]) / 0.707

    # Generate standard coordinate grids (no domain warping to prevent discontinuities/stretching)
    u_base, v_base = np.mgrid[0:1:1/size, 0:1:1/size]

    # Bilinear sampler for periodic coordinates
    def sample_map(noise_map, u, v):
        x = u * size
        y = v * size
        x0 = np.floor(x).astype(int) % size
        x1 = (x0 + 1) % size
        y0 = np.floor(y).astype(int) % size
        y1 = (y0 + 1) % size
        
        wa = (x1 - x) * (y1 - y)
        wb = (x - x0) * (y1 - y)
        wc = (x1 - x) * (y - y0)
        wd = (x - x0) * (y - y0)
        
        return (noise_map[y0, x0] * wa +
                noise_map[y0, x1] * wb +
                noise_map[y1, x0] * wc +
                noise_map[y1, x1] * wd)

    # Standard fractal Brownian motion octaves for natural puffiness
    octaves = [
        (4, 0.45, 10),
        (8, 0.28, 20),
        (16, 0.17, 30),
        (32, 0.08, 40),
        (64, 0.02, 50)
    ]
    
    density = np.zeros((size, size))
    weight_sum = 0
    
    for res, weight, seed in octaves:
        p_map = generate_perlin_map(res, seed)
        sampled = sample_map(p_map, u_base, v_base)
        density += sampled * weight
        weight_sum += weight
        
    density /= weight_sum
    density = density * 0.5 + 0.5
    density = np.clip(density, 0, 1)

    # Contrast mapping for clouds (smoother puffiness curve)
    density = 3 * density**2 - 2 * density**3
    density = np.power(density, 1.1)
    
    # Calculate normals using finite differences
    bumpStrength = 6.0  # Smooth rounded normals
    
    density_u = np.roll(density, -1, axis=1)
    density_v = np.roll(density, -1, axis=0)
    
    du = density_u - density
    dv = density_v - density
    
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
    print("Saved high-res seamless fractal cloud texture successfully!")
