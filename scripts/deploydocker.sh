#!/bin/sh
tag=$1;

echo "build multexbot:$tag"

docker build -t multexbot:$tag -f ../src/multexbot.Api/Dockerfile ../src
docker tag multexbot:$tag sp20/multexbot:$tag
docker push sp20/multexbot:$tag
