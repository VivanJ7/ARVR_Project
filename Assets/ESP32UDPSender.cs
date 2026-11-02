
using UnityEngine;
using UnityEngine.InputSystem;
using System.Net.Sockets;
using System.Text;

public class ESP32UDPSender : MonoBehaviour
{
    [Header("Roller Coaster")]
    public RollerCoasterCartController cartController;

    [Header("ESP32 Connection")]
    public string esp32IP = "192.168.4.1";
    public int port = 4210;

    [Header("Transmission")]
    public float sendRate = 30f;

    [Header("Motion")]
    public float maxAngle = 15f;
    public float smoothing = 10f;

    [Header("Home")]
    public bool homeMode = false;

    private UdpClient udpClient;
    private float sendTimer;
    private float sendInterval;
    private Vector3 filteredAngles;

    void Start()
    {
        if (cartController == null || cartController.track == null)
        {
            Debug.LogError("Cart or track not assigned!");
            enabled = false;
            return;
        }

        udpClient = new UdpClient();
        filteredAngles = Vector3.zero;
        sendInterval = 1f / Mathf.Max(sendRate, 1f);

        Debug.Log("TRACK-BASED UDP SENDER READY");
        Debug.Log("Press R to return platform home.");
    }

    void Update()
    {
        if (udpClient == null || cartController == null)
            return;

        if (Keyboard.current != null &&
            Keyboard.current.rKey.wasPressedThisFrame)
        {
            homeMode = !homeMode;

            Debug.Log(homeMode
                ? "HOME MODE ENABLED"
                : "COASTER MOTION RESUMED");
        }

        // Calculate the target every frame.
        Vector3 targetAngles = Vector3.zero;

        if (!homeMode)
        {
            if (!cartController.track.GetMotionAngles(
                cartController.DistanceTravelled,
                out float pitch,
                out float roll,
                out float yaw))
                return;

            targetAngles = new Vector3(
                Mathf.Clamp(pitch, -maxAngle, maxAngle),
                Mathf.Clamp(roll, -maxAngle, maxAngle),
                Mathf.Clamp(yaw, -maxAngle, maxAngle)
            );
        }

        // Smooth every frame, not just when sending UDP.
        float alpha = 1f - Mathf.Exp(-smoothing * Time.deltaTime);

        filteredAngles = Vector3.Lerp(
            filteredAngles,
            targetAngles,
            alpha
        );

        // Send at a fixed UDP rate.
        sendTimer += Time.deltaTime;

        if (sendTimer < sendInterval)
            return;

        sendTimer -= sendInterval;

        string message =
            $"X={filteredAngles.x:F2}," +
            $"Y={filteredAngles.y:F2}," +
            $"Z={filteredAngles.z:F2}";

        byte[] data = Encoding.UTF8.GetBytes(message);

        try
        {
            udpClient.Send(data, data.Length, esp32IP, port);
        }
        catch (SocketException e)
        {
            Debug.LogWarning("UDP send failed: " + e.Message);
        }
    }

    void OnApplicationQuit()
    {
        udpClient?.Close();
        udpClient = null;
    }

    void OnDestroy()
    {
        udpClient?.Close();
        udpClient = null;
    }
}
