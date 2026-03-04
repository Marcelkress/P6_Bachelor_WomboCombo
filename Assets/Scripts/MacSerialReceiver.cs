using System;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;

public class MacSerialReceiver : MonoBehaviour
{
    [Header("Serial (macOS)")]
    public string portName = "/dev/cu.usbserial-0001";
    public int baudRate = 115200;

    [Header("Calibration")]
    public bool autoCalibrateOnStart = true;
    public bool spaceToRecalibrate = true;

    [Header("Axis / Coordinate Mapping (try options)")]
    public AxisPreset axisPreset = AxisPreset.None;

    [Header("Optional Mount Offset (board mounting correction)")]
    public bool useMountOffset = false;
    public Vector3 mountEulerDegrees = Vector3.zero; // tweak in Inspector

    [Header("Optional Extra Fixes")]
    public bool normalizeQuaternion = true;
    public bool forcePositiveW = false; // reduces sudden flips if q and -q alternate

    public enum AxisPreset
    {
        None,

        // Common “first tries”
        SwapYZ_NegY,     // (x,y,z,w) -> (x,z,-y,w)
        SwapYZ_NegZ,     // (x,y,z,w) -> (x,z,y,w) with z sign handled separately
        SwapXZ_NegX,     // (x,y,z,w) -> (-z,y,x,w)
        SwapXY_NegX,     // (x,y,z,w) -> (-y,x,z,w)

        // Simple sign flips
        FlipX,           // (-x, y, z, w)
        FlipY,           // ( x,-y, z, w)
        FlipZ,           // ( x, y,-z, w)

        // More aggressive combos
        SwapYZ_FlipX,    // (-x, z, y, w)
        SwapXZ_FlipY     // ( z,-y, x, w)
    }

    // Native plugin
    [DllImport("MacSerial")]
    private static extern bool Serial_Open(string portName, int baudrate);

    [DllImport("MacSerial")]
    private static extern int Serial_Read(byte[] buffer, int maxLen);

    [DllImport("MacSerial")]
    private static extern void Serial_Close();

    private readonly byte[] readBuffer = new byte[512];
    private readonly StringBuilder sb = new StringBuilder(4096);

    private Quaternion calibrationOffset = Quaternion.identity;
    private bool calibrated = false;

    private Quaternion lastMapped = Quaternion.identity;
    private bool hasValidReading = false;

    void Start()
    {
        bool ok = Serial_Open(portName, baudRate);
        Debug.Log($"Serial_Open({portName}, {baudRate}) => {ok}");
        if (!ok)
            Debug.LogError("Could not open serial port. Check portName and ensure no other app is using it.");
    }

    void Update()
    {
        ReadSerial();

        // New Input System: SPACE recalibration
        if (spaceToRecalibrate &&
            Keyboard.current != null &&
            Keyboard.current.spaceKey.wasPressedThisFrame &&
            hasValidReading)
        {
            calibrationOffset = Quaternion.Inverse(lastMapped);
            calibrated = true;
            Debug.Log("Manual calibration (SPACE) complete.");
        }
    }

    void ReadSerial()
    {
        int n = Serial_Read(readBuffer, readBuffer.Length);
        if (n <= 0) return;

        sb.Append(Encoding.UTF8.GetString(readBuffer, 0, n));

        while (true)
        {
            string all = sb.ToString();
            int idx = all.IndexOf('\n');
            if (idx < 0) break;

            string line = all.Substring(0, idx).Trim();
            sb.Remove(0, idx + 1);

            ApplyQuaternionFromCsv(line);
        }
    }

    void ApplyQuaternionFromCsv(string line)
    {
        // Expected: qx,qy,qz,qw
        var p = line.Split(',');
        if (p.Length != 4) return;

        if (!float.TryParse(p[0], NumberStyles.Float, CultureInfo.InvariantCulture, out float qx) ||
            !float.TryParse(p[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float qy) ||
            !float.TryParse(p[2], NumberStyles.Float, CultureInfo.InvariantCulture, out float qz) ||
            !float.TryParse(p[3], NumberStyles.Float, CultureInfo.InvariantCulture, out float qw))
        {
            return;
        }

        Quaternion raw = new Quaternion(qx, qy, qz, qw);

        if (normalizeQuaternion)
            raw = NormalizeSafe(raw);

        if (forcePositiveW && raw.w < 0f)
            raw = new Quaternion(-raw.x, -raw.y, -raw.z, -raw.w);

        Quaternion mapped = ApplyAxisPreset(raw, axisPreset);

        if (normalizeQuaternion)
            mapped = NormalizeSafe(mapped);

        lastMapped = mapped;
        hasValidReading = true;

        // Auto calibration on first valid mapped reading
        if (autoCalibrateOnStart && !calibrated)
        {
            calibrationOffset = Quaternion.Inverse(mapped);
            calibrated = true;
            Debug.Log("Auto calibration complete.");
        }

        Quaternion mount = useMountOffset ? Quaternion.Euler(mountEulerDegrees) : Quaternion.identity;

        transform.rotation = calibrationOffset * mapped * mount;
    }

    static Quaternion NormalizeSafe(Quaternion q)
    {
        float mag = Mathf.Sqrt(q.x * q.x + q.y * q.y + q.z * q.z + q.w * q.w);
        if (mag > 1e-6f)
            return new Quaternion(q.x / mag, q.y / mag, q.z / mag, q.w / mag);
        return Quaternion.identity;
    }

    static Quaternion ApplyAxisPreset(Quaternion q, AxisPreset preset)
    {
        switch (preset)
        {
            case AxisPreset.None:
                return q;

            case AxisPreset.SwapYZ_NegY:
                // (x,y,z,w) -> (x,z,-y,w)
                return new Quaternion(q.x, q.z, -q.y, q.w);

            case AxisPreset.SwapYZ_NegZ:
                // (x,y,z,w) -> (x,z,y,w)
                return new Quaternion(q.x, q.z, q.y, q.w);

            case AxisPreset.SwapXZ_NegX:
                // (x,y,z,w) -> (-z,y,x,w)
                return new Quaternion(-q.z, q.y, q.x, q.w);

            case AxisPreset.SwapXY_NegX:
                // (x,y,z,w) -> (-y,x,z,w)
                return new Quaternion(-q.y, q.x, q.z, q.w);

            case AxisPreset.FlipX:
                return new Quaternion(-q.x, q.y, q.z, q.w);

            case AxisPreset.FlipY:
                return new Quaternion(q.x, -q.y, q.z, q.w);

            case AxisPreset.FlipZ:
                return new Quaternion(q.x, q.y, -q.z, q.w);

            case AxisPreset.SwapYZ_FlipX:
                // (x,y,z,w) -> (-x,z,y,w)
                return new Quaternion(-q.x, q.z, q.y, q.w);

            case AxisPreset.SwapXZ_FlipY:
                // (x,y,z,w) -> (z,-y,x,w)
                return new Quaternion(q.z, -q.y, q.x, q.w);

            default:
                return q;
        }
    }

    void OnDisable()
    {
        Serial_Close();
    }

    void OnApplicationQuit()
    {
        Serial_Close();
    }
}