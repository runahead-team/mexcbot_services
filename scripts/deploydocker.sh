#!/bin/sh
tag=$1;

echo "build mexcbot_services:$tag"

docker buildx build --platform linux/amd64 -t mexcbot_services:$tag -f ../src/mexcbot.Api/Dockerfile ../src
docker tag mexcbot_services:$tag ra25/mexcbot_services:$tag
docker push ra25/mexcbot_services:$tag
