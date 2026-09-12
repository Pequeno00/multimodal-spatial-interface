import socket

sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)

def main():
    sock.bind(("127.0.0.1", 5052))

    while True:
        data, addr = sock.recvfrom(1024)
        print(f"Received message: {data.decode()} from {addr}")


if __name__ == "__main__":
    main()