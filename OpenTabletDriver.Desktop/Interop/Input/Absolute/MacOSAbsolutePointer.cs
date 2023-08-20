﻿using System;
using System.Linq;
using System.Numerics;
using OpenTabletDriver.Native.MacOS;
using OpenTabletDriver.Platform.Display;
using OpenTabletDriver.Platform.Pointer;

namespace OpenTabletDriver.Desktop.Interop.Input.Absolute
{
    using static MacOS;

    public class MacOSAbsolutePointer : MacOSVirtualMouse, IAbsolutePointer, IPressureHandler
    {
        private Vector2 offset;

        public MacOSAbsolutePointer(IVirtualScreen virtualScreen)
        {
            var primary = virtualScreen.Displays.First();
            offset = primary.Position;
        }

        public void SetPosition(Vector2 pos)
        {
            var newPos = pos - offset;
            QueuePendingPosition(newPos.X, newPos.Y);
        }

        public void SetPressure(float percentage)
        {
            base.setPressure(percentage);
        }

        protected override void SetPendingPosition(IntPtr mouseEvent, float x, float y)
        {
            CGEventSetLocation(mouseEvent, new CGPoint(x, y));
        }

        protected override void ResetPendingPosition(IntPtr mouseEvent)
        {
        }
    }
}
