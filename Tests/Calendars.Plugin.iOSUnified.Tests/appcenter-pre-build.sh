#!/usr/bin/env bash

# Download Mono 6.12.0.145
wget https://download.mono-project.com/archive/6.12.0/macos-10-universal/MonoFramework-MDK-6.12.0.145.macos10.xamarin.universal.pkg

# Add execution permission
sudo chmod +x MonoFramework-MDK-6.12.0.145.macos10.xamarin.universal.pkg

# Install Mono 6.12.0.145
sudo installer -pkg MonoFramework-MDK-6.12.0.145.macos10.xamarin.universal.pkg -target / 

echo "MSBuild restore start"
MSBuild /Users/runner/work/1/s/Tests/Calendars.Plugin.iOSUnified.Tests/Calendars.Plugin.iOSUnified.Tests.csproj -t:restore
echo "MSBuild restore finish"
