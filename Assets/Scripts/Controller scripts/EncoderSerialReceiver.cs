using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using UnityEngine;

public class EncoderSerialReceiver : MonoBehaviour
{
    [Header("Serial")]
    public string portName = "/dev/cu.usbmodem101";
    public int    baudRate = 115200;

    [Header("Target")]
    public Transform bottomHemisphere;

    [Header("Rotation")]
    public bool  smoothRotation = true;
    public float smoothSpeed    = 8f;

    [DllImport("MacSerial")] static extern bool Serial_Open(string port, int baud);
    [DllImport("MacSerial")] static extern int  Serial_Read(byte[] buf, int max);
    [DllImport("MacSerial")] static extern void Serial_Close();

    readonly byte[]        readBuffer = new byte[512];
    readonly StringBuilder sb         = new StringBuilder();

    // Shared between threads — use lock when accessing
    readonly object dataLock    = new object();
    int    pendingPosition      = -1;
    bool   pendingButton        = false;

    float      targetAngle;
    float      currentAngle;
    Quaternion initialRotation;

    Thread     serialThread;
    bool       threadRunning = false;

    void Start()
    {
        bool ok = Serial_Open(portName, baudRate);
        if (!ok) { Debug.LogError($"Could not open {portName}"); return; }

        if (bottomHemisphere != null)
            initialRotation = bottomHemisphere.localRotation;

        // Start reading on a background thread
        threadRunning = true;
        serialThread  = new Thread(SerialLoop) { IsBackground = true };
        serialThread.Start();
    }

    // Runs on background thread — only writes to pendingPosition/pendingButton
    void SerialLoop()
    {
        while (threadRunning)
        {
            int n = Serial_Read(readBuffer, readBuffer.Length);
            if (n <= 0) { Thread.Sleep(1); continue; }

            sb.Append(Encoding.UTF8.GetString(readBuffer, 0, n));

            int idx;
            while ((idx = sb.ToString().IndexOf('\n')) >= 0)
            {
                string line = sb.ToString().Substring(0, idx).Trim();
                sb.Remove(0, idx + 1);
                ParseLine(line);
            }
        }
    }

    void ParseLine(string line)
    {
        var parts = line.Split(',');
        if (parts.Length != 2) return;

        if (!int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int position)) return;
        if (!int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int button))   return;

        lock (dataLock)
        {
            pendingPosition = position;
            if (button == 1) pendingButton = true;
        }
    }

    // Runs on main thread — safe to touch Unity objects here
    void Update()
    {
        int  pos    = -1;
        bool button = false;

        lock (dataLock)
        {
            pos             = pendingPosition;
            button          = pendingButton;
            pendingButton   = false; // consume it
        }

        if (pos >= 0)
            targetAngle = (pos / 127f) * 360f;

        if (button)
            Debug.Log("Button pushed");

        if (bottomHemisphere != null)
        {
            currentAngle = smoothRotation
                ? Mathf.LerpAngle(currentAngle, targetAngle, Time.deltaTime * smoothSpeed)
                : targetAngle;

            bottomHemisphere.localRotation = initialRotation * Quaternion.Euler(0f, currentAngle, 0f);
        }
    }

    void OnDisable()
    {
        threadRunning = false;
        serialThread?.Join(500);
        Serial_Close();
    }

    void OnApplicationQuit()
    {
        threadRunning = false;
        serialThread?.Join(500);
        Serial_Close();
    }
}