import argparse
from pathlib import Path
from collections import deque

from PIL import Image


DEFAULT_INPUT = Path(r"D:\游戏素材\灶台")
DEFAULT_OUTPUT = Path(r"D:\CookingSimulator\Assets\kitchen\StoveCutouts")
SUPPORTED_EXTENSIONS = {".png", ".jpg", ".jpeg", ".bmp", ".webp"}


def color_distance(first, second):
    return max(abs(first[index] - second[index]) for index in range(3))


def sample_edge_background(image, sample_step):
    image = image.convert("RGBA")
    width, height = image.size
    pixels = image.load()
    samples = []

    for x in range(0, width, sample_step):
        samples.append(pixels[x, 0][:3])
        samples.append(pixels[x, height - 1][:3])

    for y in range(0, height, sample_step):
        samples.append(pixels[0, y][:3])
        samples.append(pixels[width - 1, y][:3])

    red = sum(pixel[0] for pixel in samples) // len(samples)
    green = sum(pixel[1] for pixel in samples) // len(samples)
    blue = sum(pixel[2] for pixel in samples) // len(samples)
    return red, green, blue


def remove_edge_background(image, threshold, feather, sample_step):
    image = image.convert("RGBA")
    pixels = image.load()
    width, height = image.size
    background = sample_edge_background(image, sample_step)
    visited = bytearray(width * height)
    background_pixels = []
    queue = deque()

    def index_of(x, y):
        return y * width + x

    def enqueue_if_background(x, y):
        index = index_of(x, y)
        if visited[index]:
            return
        visited[index] = 1
        if color_distance(pixels[x, y][:3], background) <= threshold + feather:
            queue.append((x, y))

    for x in range(width):
        enqueue_if_background(x, 0)
        enqueue_if_background(x, height - 1)
    for y in range(height):
        enqueue_if_background(0, y)
        enqueue_if_background(width - 1, y)

    while queue:
        x, y = queue.popleft()
        background_pixels.append((x, y))

        if x > 0:
            enqueue_if_background(x - 1, y)
        if x < width - 1:
            enqueue_if_background(x + 1, y)
        if y > 0:
            enqueue_if_background(x, y - 1)
        if y < height - 1:
            enqueue_if_background(x, y + 1)

    for x, y in background_pixels:
        red, green, blue, alpha = pixels[x, y]
        distance = color_distance((red, green, blue), background)

        if distance <= threshold:
            pixels[x, y] = (red, green, blue, 0)
        elif feather > 0:
            fade = min(1.0, (distance - threshold) / feather)
            pixels[x, y] = (red, green, blue, int(alpha * fade))

    return image


def process_images(input_dir, output_dir, threshold, feather, sample_step, overwrite):
    input_dir = Path(input_dir)
    output_dir = Path(output_dir)

    if not input_dir.exists():
        raise FileNotFoundError(f"Input directory does not exist: {input_dir}")

    output_dir.mkdir(parents=True, exist_ok=True)

    count = 0
    for source in input_dir.iterdir():
        if not source.is_file() or source.suffix.lower() not in SUPPORTED_EXTENSIONS:
            continue

        target = output_dir / f"{source.stem}_cutout.png"
        if target.exists() and not overwrite:
            print(f"skip existing: {target}")
            continue

        with Image.open(source) as image:
            cutout = remove_edge_background(image, threshold, feather, sample_step)
            cutout.save(target)
            count += 1
            print(f"wrote: {target}")

    print(f"processed {count} image(s)")


def parse_args():
    parser = argparse.ArgumentParser(
        description="Remove connected edge backgrounds from stove images and export transparent PNG files."
    )
    parser.add_argument("--input", default=str(DEFAULT_INPUT), help="Input image folder.")
    parser.add_argument("--output", default=str(DEFAULT_OUTPUT), help="Output folder for transparent PNG files.")
    parser.add_argument("--threshold", type=int, default=34, help="Connected edge pixels within this distance become transparent.")
    parser.add_argument("--feather", type=int, default=22, help="Soft edge width after the threshold.")
    parser.add_argument("--sample-step", type=int, default=16, help="Pixel step for sampling the edge background color.")
    parser.add_argument("--overwrite", action="store_true", help="Overwrite existing output files.")
    return parser.parse_args()


def main():
    args = parse_args()
    process_images(args.input, args.output, args.threshold, args.feather, args.sample_step, args.overwrite)


if __name__ == "__main__":
    main()
