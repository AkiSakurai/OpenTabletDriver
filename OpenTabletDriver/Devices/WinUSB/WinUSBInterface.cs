using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using HidSharp.Reports;
using OpenTabletDriver.Devices.HidSharpBackend;
using OpenTabletDriver.Native.Windows;
using OpenTabletDriver.Native.Windows.USB;
using OpenTabletDriver.Plugin.Devices;
using static OpenTabletDriver.Native.Windows.Windows;
using static OpenTabletDriver.Native.Windows.WinUsb;

namespace OpenTabletDriver.Devices.WinUSB
{
    public class WinUSBInterface : IDeviceEndpoint
    {
        public unsafe WinUSBInterface(string devicePath)
        {
            DevicePath = devicePath;
            WithHandle(winUsbHandle =>
            {
                var packet = SetupPacket.MakeGetDescriptor(
                    RequestInternalType.Standard,
                    RequestRecipient.Device,
                    DescriptorType.Device, 0,
                    (ushort)sizeof(DeviceDescriptor)
                );

                var deviceDescriptor = new DeviceDescriptor();
                void* descriptorPtr = &deviceDescriptor;

                if (!WinUsb_ControlTransfer(winUsbHandle, packet, descriptorPtr, (uint)sizeof(DeviceDescriptor), out _, null))
                    throw new IOException("Failed to retrieve device descriptor");

                var interfaceDescriptor = new InterfaceDescriptor();
                if (!WinUsb_QueryInterfaceSettings(winUsbHandle, 0, &interfaceDescriptor))
                    throw new IOException("Failed to get interface descriptor");

                InterfaceNum = interfaceDescriptor.bInterfaceNumber;

                for (byte i = 0; i < interfaceDescriptor.bNumEndpoints; i++)
                {
                    var b = i; // Workaround CoreCLR bug
                    if (!WinUsb_QueryPipe(winUsbHandle, 0, i, out var pipeInfo))
                        throw new IOException("Failed to get pipe information");
                    i = b;

                    var direction = (PipeDirection)((pipeInfo.PipeID >> 7) & 1);
                    switch (direction)
                    {
                        case PipeDirection.Out:
                            if (OutputPipe.HasValue)
                                throw new IOException("WinUSB HID device unexpectedly have more than one output endpoint on the same interface");

                            OutputPipe = pipeInfo.PipeID;
                            OutputReportLength = pipeInfo.MaximumPacketSize;
                            break;
                        case PipeDirection.In:
                            if (InputPipe.HasValue)
                                throw new IOException("WinUSB HID device unexpectedly have more than one input endpoint on the same interface");

                            InputPipe = pipeInfo.PipeID;
                            InputReportLength = pipeInfo.MaximumPacketSize;
                            break;
                    }
                }

                ProductID = deviceDescriptor.idProduct;
                VendorID = deviceDescriptor.idVendor;

                Manufacturer = deviceDescriptor.iManufacturer != 0
                    ? GetDeviceString(deviceDescriptor.iManufacturer)
                    : "Unknown Manufacturer";

                ProductName = deviceDescriptor.iProduct != 0
                    ? GetDeviceString(deviceDescriptor.iProduct)
                    : "Unknown Product Name";

                SerialNumber = deviceDescriptor.iSerialNumber != 0
                    ? GetDeviceString(deviceDescriptor.iSerialNumber)
                    : "Unknown Serial Number";

                var reportDescriptorBuffer = ArrayPool<byte>.Shared.Rent(256);
                fixed (void* reportDescriptorPtr = &reportDescriptorBuffer[0])
                {
                    var reportDescriptorPacket = SetupPacket.MakeGetDescriptor(
                        RequestInternalType.Standard,
                        RequestRecipient.Interface,
                        DescriptorType.Report, 0,
                        256
                    );

                    if (!WinUsb_ControlTransfer(winUsbHandle!, reportDescriptorPacket, reportDescriptorPtr, 256, out var lengthTransferred, null))
                        throw new IOException("Failed to retrieve report descriptor");

                    _reportDescriptor = new byte[lengthTransferred];
                    Array.Copy(reportDescriptorBuffer, _reportDescriptor, lengthTransferred);

                    try
                    {
                        var reportDescriptor = new ReportDescriptor(_reportDescriptor);
                        FeatureReportLength = reportDescriptor.MaxFeatureReportLength;
                    }
                    catch
                    {
                        // Ignore
                    }
                }
                ArrayPool<byte>.Shared.Return(reportDescriptorBuffer);
            });
        }

        private int referenceCount;
        private SafeFileHandle activeFileHandle;
        private SafeWinUsbInterfaceHandle activeWinUsbHandle;
        private byte[] _reportDescriptor;

        internal int InterfaceNum { get; private set; }
        internal byte? InputPipe { get; private set; }
        internal byte? OutputPipe { get; private set; }

        public int ProductID { get; private set; }

        public int VendorID { get; private set; }

        public int InputReportLength { get; private set; }

        public int OutputReportLength { get; private set; }

        public int FeatureReportLength { private set; get; }

        public string Manufacturer { get; private set; }

        public string ProductName { get; private set; }

        public string FriendlyName => ProductName;

        public string SerialNumber { get; private set; }

        public string DevicePath { get; }

        public bool CanOpen => true;

        public IDictionary<string, string> DeviceAttributes => GetDeviceAttributes();

        public unsafe string GetDeviceString(byte index)
        {
            return WithHandle(winUsbHandle =>
            {
                var packet = SetupPacket.MakeGetStringDescriptor(index);
                var buffer = ArrayPool<byte>.Shared.Rent(StringDescriptor.MaxSize);

                fixed (void* bufferPtr = &buffer[0])
                {
                    if (!WinUsb_ControlTransfer(winUsbHandle, packet, bufferPtr, StringDescriptor.MaxSize, out var descriptorLength, null))
                    {
                        ArrayPool<byte>.Shared.Return(buffer);
                        return null;
                    }

                    ArrayPool<byte>.Shared.Return(buffer);
                    return StringDescriptor.GetString((StringDescriptor*)bufferPtr);
                }
            });
        }

        public IDeviceEndpointStream Open()
        {
            return new WinUSBInterfaceStream(new WeakReference<WinUSBInterface>(this));
        }

        private void WithHandle(Action<SafeWinUsbInterfaceHandle> predicate)
        {
            var handle = BorrowHandle();
            try
            {
                predicate(handle);
            }
            finally
            {
                ReturnHandle(handle);
            }
        }

        private T WithHandle<T>(Func<SafeWinUsbInterfaceHandle, T> predicate)
        {
            var handle = BorrowHandle();
            try
            {
                return predicate(handle);
            }
            finally
            {
                ReturnHandle(handle);
            }
        }

        internal SafeWinUsbInterfaceHandle BorrowHandle()
        {
            if (Interlocked.Increment(ref referenceCount) == 1)
            {
                activeFileHandle = CreateFile(DevicePath,
                    FileAccess.ReadWrite,
                    FileShare.ReadWrite,
                    IntPtr.Zero,
                    FileMode.Open,
                    (FileAttributes)0x40000000,
                    IntPtr.Zero
                );

                if (activeFileHandle.IsInvalid)
                    throw new IOException("Failed to open file handle to WinUSB interface");

                if (!WinUsb_Initialize(activeFileHandle, out activeWinUsbHandle))
                    throw new IOException("Failed to initialize WinUSB interface");
            }

            return activeWinUsbHandle;
        }

        // Take reference, so we may easily add multiple interface support in the future
        internal void ReturnHandle(SafeWinUsbInterfaceHandle handle)
        {
            if (activeWinUsbHandle != handle)
                throw new InvalidOperationException("Returning handle is not equal to active handle");

            if (Interlocked.Decrement(ref referenceCount) == 0)
            {
                activeWinUsbHandle.Dispose();
                activeFileHandle.Dispose();

                activeWinUsbHandle = null;
                activeFileHandle = null;
            }
        }

        private IDictionary<string, string> GetDeviceAttributes()
        {
            var deviceAttributes = new Dictionary<string, string>
            {
                ["USB_INTERFACE_NUMBER"] = InterfaceNum.ToString()
            };

            HidSharpBackend.Extensions.ExtractHidUsages(deviceAttributes, () => new ReportDescriptor(_reportDescriptor));

            return deviceAttributes;
        }
    }
}
