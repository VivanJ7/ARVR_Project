using UnityEngine;
using System.Net.Sockets;
using System.Text;

public class ESP32UDPSender : MonoBehaviour
{
    public CubeController cubeController;

    public string esp32IP = "192.168.4.1";
    public int port = 4210;

    private UdpClient udpClient;

    // Send 20 UDP packets per second
    public float sendRate = 20f;
    private float sendTimer = 0f;

    void Start()
    {
        udpClient = new UdpClient();
    }

    void Update()
    {
        if (cubeController == null)
            return;

        // Control the UDP sending frequency
        sendTimer += Time.deltaTime;

        if (sendTimer < 1f / sendRate)
            return;

        sendTimer = 0f;

        string message =
            $"X={cubeController.XAngle:F1}," +
            $"Y={cubeController.YAngle:F1}," +
            $"Z={cubeController.ZAngle:F1}";

        byte[] data = Encoding.UTF8.GetBytes(message);

        udpClient.Send(data, data.Length, esp32IP, port);
    }

    void OnApplicationQuit()
    {
        udpClient?.Close();
    }
}