import cv2;
import mediapipe as mp
import socket
import json

socket = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)

BaseOptions = mp.tasks.BaseOptions
HandLandmarker = mp.tasks.vision.HandLandmarker
HandLandmarkerOptions = mp.tasks.vision.HandLandmarkerOptions
VisionRunningMode = mp.tasks.vision.RunningMode

options = HandLandmarkerOptions(
    base_options=BaseOptions(model_asset_path='hand_landmarker.task'),
    running_mode=VisionRunningMode.IMAGE,
    num_hands=1
)
detector = HandLandmarker.create_from_options(options)



def main():
    # Initialize video capture
    cap = cv2.VideoCapture(0, cv2.CAP_DSHOW)
    if not cap.isOpened():
        print("Error: Could not open video stream.")
        return

    while True:
        # Capture frame-by-frame
        ret, frame = cap.read()
        if not ret:
            print("Error: Could not read frame.")
            break

        # Display the resulting frame
        frame = cv2.flip(frame, 1)
        cv2.imshow('Video Stream', frame)

        imagem_rgb = cv2.cvtColor(frame, cv2.COLOR_BGR2RGB)

        mp_image = mp.Image(image_format=mp.ImageFormat.SRGB, data=imagem_rgb)
        results = detector.detect(mp_image)
        if (results.hand_landmarks):
            ponta_indicador = results.hand_landmarks[0][8]
            pulso = results.hand_landmarks[0][0]
            mensagem = json.dumps({"x": ponta_indicador.x, "y": ponta_indicador.y, "z": ponta_indicador.z})
            socket.sendto(mensagem.encode(), ("127.0.0.1", 5052))

        # Break the loop on 'q' key press
        if cv2.waitKey(1) & 0xFF == ord('q'):
            break

    # Release the capture and close windows
    cap.release()
    cv2.destroyAllWindows() 

if __name__ == "__main__":
    main()