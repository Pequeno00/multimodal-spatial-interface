# 🖐️ Multimodal Spatial Interface: Real-Time Gesture & Voice Control
> A Human-Computer Interaction (HCI) prototype bridging Python-based ML and Unity (C#) via UDP sockets for real-time spatial manipulation.

## 📌 The Project
This project explores multimodal interaction by allowing a user to manipulate a 3D environment using simultaneous hand gestures and voice commands. It was built to demonstrate low-latency communication between a computer vision pipeline and a 3D rendering engine.

## ⚙️ System Architecture
The system operates on a decoupled architecture, ensuring the heavy machine learning inference does not bottleneck the rendering frame rate:

1. **Vision & Voice Node (Python):** Uses `MediaPipe` to extract 21 3D hand landmarks in real-time and `SpeechRecognition` to parse voice commands.
2. **Network Bridge (UDP Sockets):** Packages the (X, Y, Z) coordinates and string commands into lightweight JSON payloads, transmitting them via `localhost:5052`.
3. **Render Node (Unity / C#):** A UDP listener script intercepts the payloads and maps them to the `transform.position` of 3D objects, achieving near-zero latency visual feedback.

## 🛠️ Tech Stack
* **Machine Learning & Vision:** Python 3.10, MediaPipe, OpenCV
* **Environment & Physics:** Unity 2022 (C#)
* **Networking:** UDP Sockets, JSON

## 🚀 How to Run (Reproducibility)
1. Clone the repo: `git clone https://github.com/teu-user/multimodal-interface.git`
2. Open the `UnityProject` folder in Unity Hub and hit *Play*.
3. In a separate terminal, install dependencies: `pip install -r requirements.txt`
4. Run the vision node: `python main.py`

## 🔬 Limitations & Future Work
* **Latency:** While UDP is fast, occasional packet drops can cause micro-stutters in the 3D object. Implementing a simple Kalman Filter in C# could smooth the interpolation.
* **Lighting conditions:** MediaPipe's accuracy degrades in low light. Future iterations could fuse infrared sensor data (e.g., LeapMotion).
* 
