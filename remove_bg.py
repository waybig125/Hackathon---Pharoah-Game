import sys
from rembg import remove
from PIL import Image

def process(input_path, output_path):
    try:
        input_image = Image.open(input_path)
        output_image = remove(input_image)
        output_image.save(output_path)
        print(f"Successfully removed background from {input_path}")
    except Exception as e:
        print(f"Error processing {input_path}: {e}")

if __name__ == '__main__':
    for path in sys.argv[1:]:
        print(f"Processing {path}...")
        process(path, path)
