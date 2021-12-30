#!/bin/sh
service="$1";
tag=$2;

echo "build $service:$tag"

if [ $service == "multexbot" ];
then
docker build -t multexbot:$tag -f ../src/multexbot.Api/Dockerfile ../src
docker tag multexbot:$tag sp20/multexbot:$tag
docker push sp20/multexbot:$tag
else
echo "service not found"
fi
