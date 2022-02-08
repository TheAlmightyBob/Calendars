#!/usr/bin/env bash

echo "Post-clone script executing..."

# Update nuget (fix NETSDK1005)
sudo nuget update -self  
