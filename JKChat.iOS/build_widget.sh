#!/usr/bin/env bash
rm -rf ./bin
rm -rf ./obj
rm -rf ../JKChat.iOS.Widget.Native/build

configuration="Release"
forSimulator=false
if $forSimulator; then sdk="iphonesimulator"; else sdk="iphoneos"; fi
xcodebuild -project ../JKChat.iOS.Widget.Native/JKChat.iOS.Widget.Native.xcodeproj -configuration $configuration -target WidgetExtension -sdk $sdk