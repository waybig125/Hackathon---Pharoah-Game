import numpy as np
from PIL import Image
import os

def generate_seamless_clouds(size=2048):
    print(f"Generating {size}x{size} seamless cloud texture with domain warping...")
    
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

    # Generate standard coordinate grids
    u_base, v_base = np.mgrid[0:1:1/size, 0:1:1/size]
    
    # Generate warp offset using low frequency noise maps
    warp_map_x1 = generate_perlin_map(4, 100)
    warp_map_y1 = generate_perlin_map(4, 200)
    warp_map_x2 = generate_perlin_map(8, 300)
    warp_map_y2 = generate_perlin_map(8, 400)
    
    # Warp offset fields
    warp_x = warp_map_x1 * 0.12 + warp_map_x2 * 0.05
    warp_y = warp_map_y1 * 0.12 + warp_map_y2 * 0.05
    
    # Apply warp to coordinate grids (wrap to [0, 1] periodically)
    u_warped = (u_base + warp_x) % 1.0
    v_warped = (v_base + warp_y) % 1.0

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

    # Generate main noise maps and sum them using warped coordinates
    octaves = [
        (4, 0.40, 10),
        (8, 0.22, 20),
        (16, 0.15, 30),
        (32, 0.09, 40),
        (64, 0.06, 50),
        (128, 0.04, 60),
        (256, 0.03, 70),
        (512, 0.01, 80)
    ]
    
    density = np.zeros((size, size))
    weight_sum = 0
    
    for res, weight, seed in octaves:
        p_map = generate_perlin_map(res, seed)
        sampled = sample_map(p_map, u_warped, v_warped)
        density += sampled * weight
        weight_sum += weight
        
    density /= weight_sum
    density = density * 0.5 + 0.5
    density = np.clip(density, 0, 1)

    # Enhance contrast using a smooth curve for puffiness (blobby cumulative centers)
    density = 3 * density**2 - 2 * density**3
    density = np.power(density, 1.2)
    
    # Calculate normals using finite differences on the warped density
    eps = 1.0 / size
    bumpStrength = 4.0
    
    density_u = np.roll(density, -1, axis=1)
    density_v = np.roll(density, -1, axis=0)
    
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
    print("Saved high-res warped cloud texture successfully!")
