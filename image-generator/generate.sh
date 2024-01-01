#!/usr/bin/env bash
if [ "$#" -ne 3 ]
then
  echo "Usage: ./generate.sh <source video> <resolution> <fps>"
  echo "Example: ./generate.sh video.mp4 224x168 14.7058823529"
  exit 1
fi

mkdir -p frames
ffmpeg -i $1 -vf fps=$3 frames/out-%04d.bmp
mogrify -resize $2 -monochrome frames/*