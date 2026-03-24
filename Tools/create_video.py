#!/usr/bin/env python3
"""
Combines _000.png ~ _059.png in the specified folder into a 60-frame MP4 video.
Requires ffmpeg to be installed and accessible via command line.
Output file is output.mp4, saved in the image folder.
Encoding parameters used:
  -c:v libx264 -profile:v baseline -level 3.1 -pix_fmt yuv420p
  -r 60 -vsync cfr -movflags +faststart
  -color_primaries bt709 -color_trc bt709 -colorspace bt709
"""

import os
import argparse
import subprocess
import sys

def create_video(folder=None):
    """Synthesize video from image sequence using ffmpeg"""
    if folder is None:
        folder = os.getcwd()

    # Check if folder exists
    if not os.path.isdir(folder):
        print(f"Error: Folder does not exist - {folder}")
        sys.exit(1)

    # Check if all 60 images exist
    missing = []
    for i in range(60):
        filename = f"_{i:03d}.png"
        filepath = os.path.join(folder, filename)
        if not os.path.isfile(filepath):
            missing.append(filename)

    if missing:
        print("Error: Missing the following image files:")
        for name in missing:
            print(f"  {name}")
        sys.exit(1)

    # Build ffmpeg command (following user-provided parameter style)
    output_file = os.path.join(folder, "output.mp4")
    input_pattern = os.path.join(folder, "_%03d.png")

    cmd = [
        "ffmpeg", "-y",                     # Overwrite output file
        "-framerate", "60",                  # Input framerate (60 images per second)
        "-i", input_pattern,                 # Input image sequence pattern
        "-c:v", "libx264",                    # Video encoder
        "-profile:v", "baseline",              # H.264 profile (widely compatible)
        "-level", "3.1",                       # H.264 level
        "-pix_fmt", "yuv420p",                  # Pixel format (ensures compatibility)
        "-r", "60",                             # Output framerate (constant)
        "-vsync", "cfr",                         # Force constant framerate
        "-movflags", "+faststart",                # Optimize for web playback (move moov to header)
        "-color_primaries", "bt709",                # Color primaries BT.709
        "-color_trc", "bt709",                       # Color transfer characteristics BT.709
        "-colorspace", "bt709",                       # Color space BT.709
        output_file
    ]

    print("Running ffmpeg command...")
    print(" ".join(cmd))

    try:
        # Execute command, capture output for debugging
        result = subprocess.run(cmd, check=True,
                                stdout=subprocess.PIPE,
                                stderr=subprocess.PIPE,
                                text=True)
        print("ffmpeg output:")
        print(result.stdout)
        print(result.stderr)
        print(f"Video generated successfully: {output_file}")
    except subprocess.CalledProcessError as e:
        print("ffmpeg execution failed, error info:")
        print(e.stderr)
        sys.exit(1)
    except FileNotFoundError:
        print("Error: ffmpeg not found. Please ensure it is installed and added to system PATH.")
        sys.exit(1)

if __name__ == "__main__":
    parser = argparse.ArgumentParser(description="Combine 60 PNG images into an MP4 video")
    parser.add_argument("folder", nargs="?", default=None,
                        help="Folder containing images (default is current directory)")
    args = parser.parse_args()
    create_video(args.folder)
