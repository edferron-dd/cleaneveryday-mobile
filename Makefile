SIMULATOR_UDID  = 00F399D2-27EA-4BC5-BA23-760A31178EA2
APP_ID          = com.datadogdemo.cleaneveryday
APP_BUNDLE      = bin/Debug/net10.0-ios/iossimulator-arm64/cleaneveryday-mobile.app
XCODE           = /Applications/Xcode_26_2.app/Contents/Developer
ANDROID_APK     = bin/Debug/net10.0-android/com.datadogdemo.cleaneveryday-Signed.apk

.PHONY: run-ios build-ios run-android build-android

run-ios: build-ios
	xcrun simctl terminate $(SIMULATOR_UDID) $(APP_ID) 2>/dev/null || true
	xcrun simctl install $(SIMULATOR_UDID) $(APP_BUNDLE)
	xcrun simctl launch $(SIMULATOR_UDID) $(APP_ID)

build-ios:
	DEVELOPER_DIR=$(XCODE) dotnet build cleaneveryday-mobile.csproj \
		-f net10.0-ios \
		-c Debug \
		-p:RuntimeIdentifier=iossimulator-arm64 \
		-p:MtouchDebugSymbols=true \
		-p:DebugType=full \
		-p:MtouchDebug-false \
		-p:DebugInformationFormat=dwarf-with-dsym \
		-p:MtouchExtraArgs="--dsym=true"


build-android:
	dotnet build cleaneveryday-mobile.csproj \
		-f net10.0-android \
		-c Release \
		-p:CodesignKey="" \
		-p:CodesignProvision=""
