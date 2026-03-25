using System.Runtime.InteropServices;
using UnityEngine;

public class SerialBlockingTest : MonoBehaviour
{
    [DllImport("MacSerial")] static extern bool Serial_Open(string port, int baud);
    [DllImport("MacSerial")] static extern int Serial_Read(byte[] buf, int max);
    [DllImport("MacSerial")] static extern void Serial_Close();

    public string portName = "/dev/cu.usbmodem101";
    public int baudRate = 115200;

    byte[] buffer = new byte[256];

    void Start()
    {
        bool ok = Serial_Open(portName, baudRate);
        if (!ok)
        {
            Debug.LogError("Failed to open serial port");
        }
        else
        {
            Debug.Log("Serial opened");
        }
    }

    void Update()
    {
        int n = Serial_Read(buffer, buffer.Length);

        Debug.Log($"Read returned: {n}");
    }

    void OnApplicationQuit()
    {
        Serial_Close();
    }
}