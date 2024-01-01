(TODO: gif of bad apple here)

Tools to generate a playable video for _VVVVVV_ custom levels. The video is drawn using horizontal gravity lines.

This was used to create my custom level [_Bad Apple!! in VVVVVV_](), but the source can be adapted to play any black and white video.

# Usage:

## Prerequisites
The following must be installed:
- ffmpeg
- imagemagick
- .NET SDK

## Extract frames

Extract frames from a video at 14.7058823529 FPS with the following commands:

```sh
cd image-generator
./generate.sh <your-video.mp4> 224x168 14.7058823529"
```

`224x168` can be changed to your desired video resolution.

The frames will be saved to `image-generator/frames`.

## Generate level scripts

**Warning: this will remove all previously written scripts in the level!** I recommend to make a backup of your level to be safe.

```sh
cd script-generator
dotnet run ../image-generator/frames <path-to-your-level.vvvvvv>"
```
