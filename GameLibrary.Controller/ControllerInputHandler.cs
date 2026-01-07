using System;
using System.Reactive.Linq;
using DevDecoder.HIDDevices;
using DevDecoder.HIDDevices.Controllers;
using DevDecoder.HIDDevices.Converters;

namespace GameLibrary.Controller;

public static class ControllerInputHandler
{
    private const float DELAY_BETWEEN_INPUT = .1f;

    private static Dictionary<string, DateTime> inputCooldown = new Dictionary<string, DateTime>();

    private static Devices? _devices;
    private static IControllerInputCallback? callback;

    public static void Init(IControllerInputCallback callback)
    {
        callback?.PressButton(ControllerButton.LeftBumper);
        ControllerInputHandler.callback = callback;

        _devices = new Devices();
        _devices.Controllers<Gamepad>().Subscribe(OnControlChange);
    }

    private static void OnControlChange(Gamepad g)
    {
        g.Connect();
        g.Changes.Subscribe((IList<ControlValue> val) =>
        {
            foreach (var control in val)
                HandleControl(control);
        });
    }

    private static void HandleControl(ControlValue control)
    {
        if (control.Value == null)
            return;

        if (inputCooldown.TryGetValue(control.Info.PropertyName, out DateTime time))
        {
            if (time >= DateTime.UtcNow)
                return;
        }

        if (SwitchProperty())
        {
            Console.WriteLine(control.Info.PropertyName);
            inputCooldown[control.Info.PropertyName] = DateTime.UtcNow.AddSeconds(DELAY_BETWEEN_INPUT);
        }

        bool SwitchProperty()
        {
            switch (control.Info.PropertyName)
            {
                case "X":
                    //float xValue = (float)control.Value; // left stick X
                    break;

                case "Y":
                    //float yValue = (float)control.Value; // left stick Y
                    break;

                case "Hat":
                    Direction? hatValue = (Direction)control.Value; // D-pad position

                    switch (hatValue)
                    {
                        case Direction.North: callback?.Move(0, 1); return true;
                        case Direction.East: callback?.Move(1, 0); return true;
                        case Direction.South: callback?.Move(0, -1); return true;
                        case Direction.West: callback?.Move(-1, 0); return true;
                    }
                    break;

                case "XButton": callback?.PressButton(ControllerButton.B); return true;
                case "BButton": callback?.PressButton(ControllerButton.A); return true;
                case "LeftBumper": callback?.PressButton(ControllerButton.LeftBumper); return true;
                case "RightBumper": callback?.PressButton(ControllerButton.RightBumper); return true;
            }

            return false;
        }

    }
}