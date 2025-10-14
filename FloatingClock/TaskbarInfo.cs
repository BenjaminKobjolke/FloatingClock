using System;
using System.Runtime.InteropServices;

namespace FloatingClock
{
    /// <summary>
    /// Helper class to detect Windows taskbar position and state using SHAppBarMessage API
    /// </summary>
    public class TaskbarInfo
    {
        [DllImport("shell32.dll", SetLastError = true)]
        private static extern IntPtr SHAppBarMessage(uint dwMessage, ref APPBARDATA pData);

        [StructLayout(LayoutKind.Sequential)]
        private struct APPBARDATA
        {
            public uint cbSize;
            public IntPtr hWnd;
            public uint uCallbackMessage;
            public uint uEdge;
            public RECT rc;
            public int lParam;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int left;
            public int top;
            public int right;
            public int bottom;
        }

        private const int ABM_GETTASKBARPOS = 0x00000005;
        private const int ABM_GETSTATE = 0x00000004;
        private const int ABS_AUTOHIDE = 0x0000001;
        private const int ABS_ALWAYSONTOP = 0x0000002;

        public enum TaskbarPosition
        {
            Unknown = -1,
            Left = 0,
            Top = 1,
            Right = 2,
            Bottom = 3
        }

        /// <summary>
        /// Gets the current position of the Windows taskbar
        /// </summary>
        /// <returns>TaskbarPosition enum indicating which edge the taskbar is on</returns>
        public static TaskbarPosition GetTaskbarPosition()
        {
            APPBARDATA data = new APPBARDATA();
            data.cbSize = (uint)Marshal.SizeOf(data);
            IntPtr result = SHAppBarMessage(ABM_GETTASKBARPOS, ref data);

            if (result == IntPtr.Zero)
                return TaskbarPosition.Unknown;

            return (TaskbarPosition)data.uEdge;
        }

        /// <summary>
        /// Checks if the taskbar is set to auto-hide mode
        /// </summary>
        /// <returns>True if taskbar is set to auto-hide, false otherwise</returns>
        public static bool IsTaskbarAutoHide()
        {
            APPBARDATA data = new APPBARDATA();
            data.cbSize = (uint)Marshal.SizeOf(data);
            IntPtr result = SHAppBarMessage(ABM_GETSTATE, ref data);
            int state = result.ToInt32();
            return (state & ABS_AUTOHIDE) == ABS_AUTOHIDE;
        }

        /// <summary>
        /// Checks if the taskbar is set to always-on-top mode
        /// </summary>
        /// <returns>True if taskbar is always on top, false otherwise</returns>
        public static bool IsTaskbarAlwaysOnTop()
        {
            APPBARDATA data = new APPBARDATA();
            data.cbSize = (uint)Marshal.SizeOf(data);
            IntPtr result = SHAppBarMessage(ABM_GETSTATE, ref data);
            int state = result.ToInt32();
            return (state & ABS_ALWAYSONTOP) == ABS_ALWAYSONTOP;
        }
    }
}
