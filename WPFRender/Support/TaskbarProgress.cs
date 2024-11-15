using System;
using System.Runtime.InteropServices;

namespace WPFRender;

#region [Windows 7 (or higher) TaskBar Progress]
/// <summary>
/// Helper class to set taskbar progress on Windows 7+ systems.
/// https://learn.microsoft.com/en-us/windows/win32/api/shobjidl_core/nn-shobjidl_core-itaskbarlist3
/// </summary>
public static class TaskbarProgress
{
    /// <summary>
    /// Available taskbar progress states
    /// </summary>
    public enum TaskbarStates
    {
        /// <summary>No progress displayed</summary>
        NoProgress = 0,
        /// <summary>Indeterminate </summary>
        Indeterminate = 0x1,
        /// <summary>Normal</summary>
        Normal = 0x2,
        /// <summary>Error</summary>
        Error = 0x4,
        /// <summary>Paused</summary>
        Paused = 0x8
    }

    [ComImportAttribute()]
    [GuidAttribute("ea1afb91-9e28-4b86-90e9-9e9f8a5eefaf")]
    [InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)]
    private interface ITaskbarList3
    {
        // ITaskbarList
        [PreserveSig]
        void HrInit();
        [PreserveSig]
        void AddTab(IntPtr hwnd);
        [PreserveSig]
        void DeleteTab(IntPtr hwnd);
        [PreserveSig]
        void ActivateTab(IntPtr hwnd);
        [PreserveSig]
        void SetActiveAlt(IntPtr hwnd);

        // ITaskbarList2
        [PreserveSig]
        void MarkFullscreenWindow(IntPtr hwnd, [MarshalAs(UnmanagedType.Bool)] bool fFullscreen); // https://learn.microsoft.com/en-us/windows/win32/api/shobjidl_core/nf-shobjidl_core-itaskbarlist2-markfullscreenwindow

        // ITaskbarList3
        [PreserveSig]
        void SetProgressValue(IntPtr hwnd, UInt64 ullCompleted, UInt64 ullTotal); // https://learn.microsoft.com/en-us/windows/win32/api/shobjidl_core/nf-shobjidl_core-itaskbarlist3-setprogressvalue
        [PreserveSig]
        void SetProgressState(IntPtr hwnd, TaskbarStates state); // https://learn.microsoft.com/en-us/windows/win32/api/shobjidl_core/nf-shobjidl_core-itaskbarlist3-setprogressstate
    }

    [GuidAttribute("56FDF344-FD6D-11d0-958A-006097C9A090")]
    [ClassInterfaceAttribute(ClassInterfaceType.None)]
    [ComImportAttribute()]
    private class TaskbarInstance
    {
    }

    private static bool taskbarSupported = Extensions.IsWindows7OrLater;
    private static ITaskbarList3 taskbarInstance = taskbarSupported ? (ITaskbarList3)new TaskbarInstance() : null;

    /// <summary>
    /// Sets the state of the taskbar progress.
    /// </summary>
    /// <param name="windowHandle">current form handle</param>
    /// <param name="taskbarState">desired state</param>
    public static void SetState(IntPtr windowHandle, TaskbarStates taskbarState)
    {
        if (taskbarSupported)
        {
            taskbarInstance.SetProgressState(windowHandle, taskbarState);
        }
    }

    /// <summary>
    /// Sets the value of the taskbar progress.
    /// </summary>
    /// <param name="windowHandle">currnet form handle</param>
    /// <param name="progressValue">desired progress value</param>
    /// <param name="progressMax">maximum progress value</param>
    public static void SetValue(IntPtr windowHandle, double progressValue, double progressMax)
    {
        if (taskbarSupported)
        {
            taskbarInstance.SetProgressValue(windowHandle, (ulong)progressValue, (ulong)progressMax);
        }
    }
}
#endregion
