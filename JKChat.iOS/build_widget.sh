#!/usr/bin/env bash
rm -rf ./bin
rm -rf ./obj
rm -rf ../JKChat.iOS.Widget.Native/build
rm -rf ../JKChat.iOS.Widget.Native/Binding/WidgetShared/bin
rm -rf ../JKChat.iOS.Widget.Native/Binding/WidgetShared/obj

configuration="Debug"
forSimulator=true
if $forSimulator; then sdk="iphonesimulator"; else sdk="iphoneos"; fi
xcodebuild -project ../JKChat.iOS.Widget.Native/JKChat.iOS.Widget.Native.xcodeproj -configuration $configuration -target WidgetExtension -sdk $sdk
cp -rf ../JKChat.iOS.Widget.Native/build/$configuration-$sdk/WidgetShared.framework ../JKChat.iOS.Widget.Native/build/WidgetShared.framework
cp -rf ../JKChat.iOS.Widget.Native/build/$configuration-$sdk/WidgetExtension.appex ../JKChat.iOS.Widget.Native/build/WidgetExtension.appex
#xcodebuild archive -project ../JKChat.iOS.Widget.Native/JKChat.iOS.Widget.Native.xcodeproj -scheme WidgetShared -destination "generic/platform=iOS" -archivePath ../JKChat.iOS.Widget.Native/build/WidgetShared-iOS
#xcodebuild archive -project ../JKChat.iOS.Widget.Native/JKChat.iOS.Widget.Native.xcodeproj -scheme WidgetShared -destination "generic/platform=iOS Simulator" -archivePath ../JKChat.iOS.Widget.Native/build/WidgetShared-iOS-Simulator
#xcodebuild -create-xcframework -archive ../JKChat.iOS.Widget.Native/build/WidgetShared-iOS.xcarchive -framework WidgetShared.framework -archive ../JKChat.iOS.Widget.Native/build/WidgetShared-iOS-Simulator.xcarchive -framework WidgetShared.framework -output ../JKChat.iOS.Widget.Native/build/WidgetShared.xcframework
configuration="Release"
dotnet build ../JKChat.iOS.Widget.Native/Binding/WidgetShared/WidgetShared.csproj --configuration $configuration
cp -rf ../JKChat.iOS.Widget.Native/Binding/WidgetShared/bin/$configuration/net9.0-ios ../JKChat.iOS.Widget.Native/Binding/WidgetShared/bin/WidgetShared