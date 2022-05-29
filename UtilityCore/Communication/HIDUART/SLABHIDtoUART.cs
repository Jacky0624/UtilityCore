using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace UtilityCore.Communication.HIDUART
{
    public class SLABHIDtoUART
    {
        [DllImport("SLABHIDtoUART.dll")]
        public static extern int HidUart_GetNumDevices(ref uint numDevices, ushort vid, ushort pid);

        [DllImport("SLABHIDtoUART.dll")]
        public static extern int HidUart_Open(ref IntPtr device, uint deviceNum, ushort vid, ushort pid);
        [DllImport("SLABHIDtoUART.dll")]
        public static extern int HidUart_GetString(uint deviceNum, ushort vid, ushort pid, StringBuilder deviceStr, uint options);
        [DllImport("SLABHIDtoUART.dll")]
        public static extern int HidUart_GetAttributes(uint deviceNum, ushort vid, ushort pid, ref ushort deviceVid, ref ushort devicePid, ref ushort deviceReleaseNumber);
        [DllImport("SLABHIDtoUART.dll")]
        public static extern int HidUart_IsOpened(IntPtr devices, ref int isOpen);

        [DllImport("SLABHIDtoUART.dll")]
        public static extern int HidUart_Close(IntPtr device);
        [DllImport("SLABHIDtoUART.dll")]
        public static extern int HidUart_SetUartEnable(IntPtr device, int isEnable);
        [DllImport("SLABHIDtoUART.dll")]
        public static extern int HidUart_GetUartEnable(IntPtr device, ref int isEnable);
        [DllImport("SLABHIDtoUART.dll")]
        public static extern int HidUart_WriteLatch(IntPtr device, int i, ushort dtr);
        [DllImport("SLABHIDtoUART.dll")]
        public static extern int HidUart_WriteLatch(IntPtr device, ushort dtr, ushort dtr2);
        [DllImport("SLABHIDtoUART.dll")]
        public static extern int HidUart_Write(IntPtr device, byte[] buffer, int numBytesToWrite, ref int numBytesWritten);

        [DllImport("SLABHIDtoUART.dll")]
        public static extern int HidUart_Read(IntPtr device, byte[] buffer, int numBytesToRead, ref uint numBytesRead);
        [DllImport("SLABHIDtoUART.dll")]
        public static extern int HidUart_SetTimeouts(IntPtr device, uint readTimeout, uint writeTimeout);
        [DllImport("SLABHIDtoUART.dll")]
        public static extern int HidUart_GetTimeouts(IntPtr device, ref uint readTimeout, ref uint writeTimeout);

        [DllImport("SLABHIDtoUART.dll")]
        public static extern int HidUart_SetUartConfig(IntPtr device, uint baudRate, byte dataBits, byte parity, byte stopBits, byte flowControl);
        [DllImport("SLABHIDtoUART.dll")]
        public static extern int HidUart_GetUartConfig(IntPtr device, ref uint baudRate, ref byte dataBits, ref byte parity, ref byte stopBits, ref byte flowControl);

        [DllImport("SLABHIDtoUART.dll")]
        public static extern int HidUart_Reset(IntPtr device);
    }
}
