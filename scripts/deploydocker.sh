#!/bin/sh
tag=$1;

echo "build mexcbot_services:$tag"

docker build -t mexcbot_services:$tag -f ../src/mexcbot.Api/Dockerfile ../src
docker tag mexcbot_services:$tag ra25/mexcbot_services:$tag
docker push ra25/mexcbot_services:$tag
