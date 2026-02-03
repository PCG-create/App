# CoachPad

Production ready iPad coaching app and Python service for real time sales coaching.

## Repository structure

- coach_service
	- FastAPI service
	- app modules for perception and retrieval
	- scripts for transcript simulation
- iPadApp
	- Xcode iPad app project
	- SwiftUI views and WebSocket client
- windows_app
	- Windows 11 WPF tray app and overlay

## Platform limitations

- iPadOS does not allow system wide audio capture from other apps. This app only captures microphone input. A manual Start Coaching flow is required when call detection is unavailable.
- Always on top overlays are not available on iPadOS. Use Slide Over or Stage Manager for a compact coaching view.
- SNAPmobile cannot be modified. CallKit CXCallObserver is best effort and may not detect SNAPmobile calls.
- Explicit microphone and camera consent is required. A clear recording indicator must be shown when capture is enabled.
- Audio streaming requires a local Vosk model configured via VOSK_MODEL_PATH.
- Windows system audio capture depends on WASAPI and may fail for apps using exclusive audio mode.
- Always on top overlays are best effort and can be obscured by full screen apps.
- The Windows app requires explicit consent to capture audio or camera. It will not start capture without consent.
- Local summaries are encrypted with DPAPI and can be deleted from the app.

## Phase status

Phase 1 through Phase 4 are implemented. The service supports transcript ingestion, audio streaming, learning feedback, retrieval grounded generation, and vision engagement cues.

## Phase 1 setup

### Python service

1. Create a virtual environment.
2. Install dependencies.
3. Run the FastAPI service.

Example commands:

- python3 -m venv .venv
- source .venv/bin/activate
- pip install -r coach_service/requirements.txt
- uvicorn coach_service.app.main:app --reload --host 0.0.0.0 --port 8000

### Vosk speech to text

Audio streaming uses Vosk. Download a model and set VOSK_MODEL_PATH.

Example steps:

- Download a model from https://alphacephei.com/vosk/models
- Unzip it to a local folder
- export VOSK_MODEL_PATH=/path/to/vosk-model

### iPad app

1. Open iPadApp/iPadApp.xcodeproj in Xcode.
2. Set a development team and a unique bundle identifier.
3. Build and run on an iPad running iPadOS 17 or later.
4. Enter the backend host in the app. Example: 192.168.1.10:8000

The app uses a WebSocket connection to /ws/ui for live metrics.

## Swift Playgrounds build

Swift Playgrounds on iPad can run the simplified Playground app in this repo. It does not include microphone or camera capture. It can connect to the backend and send sample transcript lines.

Steps:

1. Open the repository in Swift Playgrounds.
2. Open Package.swift and run the CoachPadPlayground app.
3. Set the host to your backend and tap Connect.
4. Tap Send sample transcript to see metrics and Say this next lines.

## Phase 1 test

1. Start the Python service.
2. Run the transcript simulator to stream sample dialog.

Example command:

- python3 coach_service/scripts/simulate_transcript.py

3. Verify the iPad app shows Talk to listen, sentiment, engagement, stage, and Say this next lines.

## Phase 2 test

1. In the iPad app, tap End Call and select an outcome.
2. Verify the service accepts the outcome and uses it to re rank future suggestions.
3. Verify a summary appears and can be exported as PDF.

## Phase 3 test

1. With active transcripts or audio, observe that Say this next lines include retrieval and generated questions.
2. Set ENABLE_LLM=1 to enable local generation. This uses a small local model and keeps output grounded to context.

## Phase 4 test

1. Start coaching and allow camera access.
2. Verify engagement changes when face presence changes.

## Running on iPad

- Use the iPad and backend on the same network.
- In the app, set the host to the backend IP address and port. Example: 192.168.1.10:8000
- Start coaching. The app streams mic audio and low FPS camera frames to the backend.

## Running on Windows 11

The Windows WPF app runs in the system tray, shows an always on top overlay, and captures system audio and microphone audio using WASAPI. It can auto start on detection of Teams, Zoom, Google Meet, or Snapmobile WebPhone windows. It also checks active audio sessions for those processes.

1. Install Visual Studio 2022 with the .NET desktop development workload.
2. Open windows_app/CoachPadWpf.sln.
3. Restore NuGet packages and build for x64.
4. Run the app, then open the tray icon and Show the overlay.
5. Grant audio consent and optionally camera consent.
6. Set the backend host, then Start Coaching or wait for auto detection.

## Windows packaging

MSIX packaging is supported through Visual Studio. Open windows_app/CoachPadWpf.Package.wapproj and build to produce an MSIX package. A temporary certificate is required for local install. Replace the publisher and certificate for production.

The packaging project generates placeholder logos from a base64 file during build. Replace the assets in windows_app/CoachPadWpf.Package/Assets with your branding.

## One click installer EXE

Use Inno Setup to build an installer EXE.

1. Build the WPF app in Release x64.
2. Open windows_app/installer/CoachPadSetup.iss in Inno Setup.
3. Set MyAppExePath to the full path of CoachPadWpf.exe in your build output.
4. Compile the script to produce CoachPadSetup.exe.

The installer prompts for backend host and provisions settings.json in the user profile on first install.

## Privacy and crash logs

The Windows app includes a Privacy and EULA window in the overlay. Crash logs are stored locally and protected with DPAPI. Use Delete all local data to remove stored summaries and logs.
6. Choose an outcome and select End Call to store a summary.

## Call summary

- The backend provides a summary of the latest transcript.
- The iPad app saves the summary locally with file protection and can export a PDF.

## Data handling

- Microphone and camera access are explicit and only active while coaching is on.
- No third party data exfiltration is enabled by default.