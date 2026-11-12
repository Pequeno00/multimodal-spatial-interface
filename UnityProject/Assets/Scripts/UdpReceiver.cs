using System.Net.Sockets;
using System.Net;
using System.Text;
using UnityEngine;
using System.Threading;

public class UdpReceiver : MonoBehaviour
{
    private UdpClient udpClient;
    private Thread receiveThread;
    private string receivedData;
    private object dataLock = new object();
    private int port = 5052;
    private volatile bool isRunning;

    void Start()
    {
        isRunning = true;
        udpClient = new UdpClient(port);
        receiveThread = new Thread(ReceiveLoop);
        receiveThread.IsBackground = true;
        receiveThread.Start();
    }

    private void ReceiveLoop()
    {
        IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, port);
        while (isRunning)
        {
            try
            {
                byte[] data = udpClient.Receive(ref remoteEndPoint);
                string message = Encoding.UTF8.GetString(data);
                lock (dataLock)
                {
                    receivedData = message;
                }
                Debug.Log("Received: " + message);
            }
            catch (SocketException ex)
            {
                if (isRunning)
                {
                    Debug.LogError("Socket exception: " + ex.Message);
                }
                break;
            }
            catch (System.Exception ex)
            {
                if (isRunning)
                {
                    Debug.LogError("Exception: " + ex.Message);
                }
                break;
            }
        }
    }

    private void OnDestroy()
    {
        isRunning = false;
        if (udpClient != null)
        {
            udpClient.Close();
        }
    }
}