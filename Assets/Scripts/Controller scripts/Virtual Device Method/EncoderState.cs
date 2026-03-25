// EncoderState.cs - shared state struct
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.Utilities;

public struct EncoderState : IInputStateTypeInfo
{
    public FourCC format => new FourCC('E', 'N', 'C', 'D');

    [InputControl(name = "dial",   layout = "Axis",   format = "FLT")]
    public float dial;

    [InputControl(name = "button", layout = "Button", format = "BIT", bit = 0)]
    public byte button;
}