#!/bin/sh
service="$1";
tag=$2;

echo "build $service:$tag"

if [ $service == "nftc" ];
then
docker build -t nftc:$tag -f ../src/multexbot.Api/Dockerfile ../src
docker tag nftc:$tag sp20/nftc:$tag
docker push sp20/nftc:$tag
else
echo "service not found"
fi
