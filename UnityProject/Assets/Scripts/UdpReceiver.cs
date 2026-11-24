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
    [SerializeField] private float minX, maxX, minY, maxY;
    
    [System.Serializable]
    public class HandData
    {
        public float x;
        public float y;
        public float z;
    }

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

    private void Update()
    {
        string dataToProcess = null;
        lock (dataLock)
        {
            if (!string.IsNullOrEmpty(receivedData))
            {
                dataToProcess = receivedData;
                receivedData = null; // Clear after processing
            }
        }
        if (dataToProcess != null)
        {
            ProcessData(dataToProcess);
        }
    }

    private void ProcessData(string data)
    {
        float worldX;
        float worldY;
        try
        {
            HandData handData = JsonUtility.FromJson<HandData>(data);
            worldX = Mathf.Lerp(minX, maxX, handData.x);
            worldY = Mathf.Lerp(minY, maxY, 1- handData.y);
            transform.position = new Vector3(worldX, worldY, transform.position.z);
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Failed to process data: " + ex.Message);
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

