#!/bin/sh
tag=$1;

echo "build mexcbot:$tag"

docker build -t mexcbot:$tag -f ../src/mexcbot.Api/Dockerfile ../src
docker tag mexcbot:$tag sp20/mexcbot:$tag
docker push sp20/mexcbot:$tag
