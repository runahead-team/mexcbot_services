#!/bin/sh
tag=$1;

echo "build mexcbot:$tag"

docker build -t mexcbot:$tag -f ../src/mexcbot.Api/Dockerfile ../src
docker tag mexcbot:$tag ra25/mexcbot:$tag
docker push ra25/mexcbot:$tag
