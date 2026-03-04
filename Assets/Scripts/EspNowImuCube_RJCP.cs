using System;
using System.IO.Ports;
using System.Globalization;
using UnityEngine;

public class EspNowImuCube_MacSerial : MonoBehaviour
{
    [Header("Serial")]
    public string portName = "/dev/cu.usbserial-0001"; // set in Inspector
    public int baudRate = 115200;

    SerialPort sp;

    void Start()
    {
        Debug.Log("Platform: " + Application.platform);
        Debug.Log("Ports: " + string.Join(", ", SerialPort.GetPortNames()));

        sp = new SerialPort(portName, baudRate)
        {
            ReadTimeout = 50,
            NewLine = "\n",
            DtrEnable = true,
            RtsEnable = true
        };

        sp.Open();
        Debug.Log("Opened " + portName);
    }

    void Update()
    {
        if (sp == null || !sp.IsOpen) return;

        try
        {
            var line = sp.ReadLine().Trim(); // "qx,qy,qz,qw"
            var p = line.Split(',');
            if (p.Length != 4) return;

            float qx = float.Parse(p[0], CultureInfo.InvariantCulture);
            float qy = float.Parse(p[1], CultureInfo.InvariantCulture);
            float qz = float.Parse(p[2], CultureInfo.InvariantCulture);
            float qw = float.Parse(p[3], CultureInfo.InvariantCulture);

            transform.rotation = new Quaternion(qx, qy, qz, qw);
        }
        catch (TimeoutException) { }
        catch (Exception e)
        {
            Debug.LogWarning("Serial parse error: " + e.Message);
        }
    }

    void OnDestroy()
    {
        try { if (sp != null && sp.IsOpen) sp.Close(); } catch { }
    }
}