// EncoderDevice.cs - virtual input device
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.Controls;

#if UNITY_EDITOR
[UnityEditor.InitializeOnLoad]
#endif
[InputControlLayout(stateType = typeof(EncoderState), displayName = "Encoder Controller")]
public class EncoderDevice : InputDevice
{
    public AxisControl   Dial   { get; private set; }
    public ButtonControl Button { get; private set; }

    protected override void FinishSetup()
    {
        base.FinishSetup();
        Dial   = GetChildControl<AxisControl>  ("dial");
        Button = GetChildControl<ButtonControl>("button");
    }

    static EncoderDevice()
    {
        InputSystem.RegisterLayout<EncoderDevice>(
            matches: new InputDeviceMatcher().WithProduct("EncoderController")
        );
    }
}