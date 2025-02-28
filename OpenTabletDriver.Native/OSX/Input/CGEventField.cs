namespace OpenTabletDriver.Native.OSX.Input
{
    public enum CGEventField : uint
    {
        mouseEventNumber = 0, // int
        mouseEventClickState = 1, // int
        mouseEventPressure = 2, // double
        mouseEventButtonNumber = 3, // int
        mouseEventDeltaX = 4, // int
        mouseEventDeltaY = 5, // int
        mouseEventInstantMouser = 6, // int
        mouseEventSubtype = 7, // kCFNumberIntType
        keyboardEventAutorepeat = 8, // int
        keyboardEventKeycode = 9, // int
        keyboardEventKeyboardType = 10, // int
        scrollWheelEventDeltaAxis1 = 11, // int
        scrollWheelEventDeltaAxis2 = 12, // int
        scrollWheelEventDeltaAxis3 = 13, // int
        scrollWheelEventFixedPtDeltaAxis1 = 93, // int
        scrollWheelEventFixedPtDeltaAxis2 = 94, // int
        scrollWheelEventFixedPtDeltaAxis3 = 95, // int
        scrollWheelEventPointDeltaAxis1 = 96, // int
        scrollWheelEventPointDeltaAxis2 = 97, // int
        scrollWheelEventPointDeltaAxis3 = 98, // int
        scrollWheelEventInstantMouser = 14, // int
        tabletEventPointButtons = 18, // int
        tabletEventPointPressure = 19, // double
        tabletEventTiltX = 20, // double
        tabletEventTiltY = 21, // double
        tabletEventDeviceID = 24, // int
        tabletProximityEventDeviceID = 31, // int
        tabletProximityEventCapabilityMask = 36, // int
        tabletProximityEventPointerType = 37, // int
        tabletProximityEventEnterProximity = 38, // int
    }
}
