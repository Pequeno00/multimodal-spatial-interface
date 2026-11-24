# Development log

Short, dated notes on decisions, problems and findings. Observations marked *(qualitative)* are impressions, not measurements; measured results will go in `analysis/` once instrumentation exists.

## Architecture (as built)

Python vision node (OpenCV + MediaPipe HandLandmarker) -> UDP JSON on `127.0.0.1:5052` -> Unity 6 (6000.6) C# receiver -> Cube position.

- **Why UDP, not TCP:** positions are streamed continuously, so a lost packet is superseded by the next one within ~33 ms. TCP retransmission would only deliver stale data late and add latency. Dropping a frame occasionally is preferable to accumulating lag.
- **Why a decoupled design:** ML inference runs in its own process so it cannot block Unity's render loop.
- **Why a background thread in Unity:** `UdpClient.Receive()` blocks; calling it from `Update()` would freeze the Editor. A background thread stores only the *latest* message under a lock; `Update()` (main thread) reads it, because Unity's API (`transform`, etc.) is main-thread only.
- **Why normalised coordinates (0-1):** MediaPipe landmarks are resolution-independent, so the same data works for any camera resolution and can be mapped to any target space (here, Unity world units).

## 2026-09-10 to 2026-09-12: Python pipeline

- Built the pipeline in phases: webcam capture, hand landmarks, index fingertip (landmark 8) and wrist (landmark 0), UDP send, plus a standalone UDP listener script to verify data left the sender before Unity existed.
- Environment issues on Windows: `python3` is not a valid command (Microsoft Store alias); the venv needs `py -m venv` and plain `python` once activated; PowerShell needs `Set-ExecutionPolicy -Scope Process -ExecutionPolicy RemoteSigned` in each new terminal.
- OpenCV's default `MSMF` backend opened the webcam but failed to read frames. Switching to `cv2.CAP_DSHOW` fixed it. `cv2.flip(frame, 1)` mirrors the image so that moving the hand right moves the cursor right (the return value must be assigned; the function does not flip in place).
- MediaPipe 1.0.1 **removed the legacy `mp.solutions` API**. The Tasks API (`HandLandmarker`) is required, and the model (`hand_landmarker.task`) is a separate download rather than part of the pip package. It is git-ignored (7.8 MB binary).
- Frames must be converted BGR -> RGB before being wrapped in `mp.Image`.
- `hand_landmarks` is an empty list when no hand is detected, so every access must be guarded.

## 2026-09-13: Unity project

- Created `UnityProject` (Universal 3D / URP template) inside the same repo. Unity 6 no longer offers the old Built-in "3D Core" template in the Hub.
- Chose "Select none" for Unity's built-in version control, because git is managed from the terminal at the repo root.

## 2026-11-12: Unity receiver and repository hygiene

- Receiver verified end to end: JSON arrived in the Unity Console from the Python node.
- The Unity `.gitignore` template uses root-anchored patterns (`/[Ll]ibrary/`), so in this monorepo it ignored nothing and ~28,000 files showed up as untracked. Fix: a `.gitignore` inside `UnityProject/`, with Python/editor rules kept at the repo root.
- Shutdown handling: a `volatile` running flag is set to false *before* closing the socket, and the receive loop only logs errors while the flag is true. Otherwise every normal Stop produced a spurious red error. `Thread.Abort()` was avoided (unsupported on modern .NET, which Unity is moving to).
- Unity regenerates project files it finds missing. Switching git branches with the Editor open made `git pull` fail on regenerated untracked files. Close Unity before switching branches.
- Unity writes LF line endings while git here uses `core.autocrlf=true`, so some Unity settings files appear modified with no real diff (`QualitySettings.asset`). A `.gitattributes` would fix it; not done yet.

## Mid November 2026: Mapping hand to Cube

- Parsing: `JsonUtility` with a `[System.Serializable]` class, run in `Update()` after copying the message inside the lock.
- Mapping: `Mathf.Lerp(min, max, t)`, with Y flipped (`1 - y`, because image y grows downward and Unity Y grows upward). X needs no flip because the Python side mirrors the image. Z is left unchanged because MediaPipe's z is only a relative, single-camera estimate and is noisy.
- Range found by measuring the visible edges in the Game view (about X -6.7 to 7.4 and Y -1.1 to 6.8 for the current camera pose). These values depend on camera position and Game view aspect ratio and must be re-measured if either changes.
- Result: the Cube follows the index fingertip in the correct directions *(qualitative)*.
- Issues seen at this stage *(qualitative)*: noticeable jitter with a still hand, and detection becomes unreliable when the hand is near the bottom of the image (the wrist and palm leave the frame, which the palm detector handles poorly).

## 2026-11-24: VIDEO running mode

- Switched the detector from `IMAGE` to `VIDEO` mode. `IMAGE` mode re-detects the hand from scratch on every frame; `VIDEO` mode tracks between frames. It requires an integer millisecond timestamp that strictly increases on every call.
- Bug found in review: computing the timestamp once before the loop would reuse the same value every frame and raise an error. It must be computed per frame.
- Result: *(qualitative)* "definitely better" and no errors. This is the baseline impression to compare against measured numbers later.

## Open questions and next steps

- **Smoothing (next):** exponential smoothing in `Update()` toward a stored target position, frame-rate independent via `alpha = 1 - exp(-smoothing * dt)`, then compare with the One Euro filter (Casiez et al., CHI 2012), which trades lag against jitter adaptively.
- **Bottom-of-image loss:** remap the usable input range with `Mathf.InverseLerp`, and/or reposition the camera.
- **Instrumentation:** add a capture timestamp (Unix ms) to the UDP payload; log latency, jitter and tracking loss to CSV; analyse in a notebook. Note that a shared timestamp measures pipeline latency (capture to Unity `Update`) only. Camera exposure and display latency need an external method such as slow-motion video.
- **Interaction:** pinch to grab (thumb-index distance normalised by hand size, with hysteresis), then scaling.
- **Multimodal:** voice commands combined with gesture.
- **Housekeeping:** `.gitattributes` for line endings; README architecture diagram and demo GIF.
