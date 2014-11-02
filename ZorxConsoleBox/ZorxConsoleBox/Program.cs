using System;
using System.Diagnostics;
using System.Threading;
using System.Windows.Forms;
using System.Runtime.InteropServices;

/* TODO:
 * Add "Disable all but one" feature
 * Add pause feature
 * Add stop feature
 * Add detect new clients feature
 */

class ZorxConsoleBox
{

    //Declare the wrapper managed POINT class.
    [StructLayout(LayoutKind.Sequential)]
    public class POINT
    {
        public int x;
        public int y;
    }

    //Declare the wrapper managed MouseHookStruct class.
    [StructLayout(LayoutKind.Sequential)]
    public class MouseHookStruct
    {
        public POINT pt;
        public int hwnd;
        public int wHitTestCode;
        public int dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int left;
        public int top;
        public int right;
        public int bottom;
    }



    private const int WH_KEYBOARD_LL = 13;
    private const int WH_MOUSE = 14;
    private const int WM_KEYDOWN = 0x0100;
    

    private const int WM_LBUTTONDOWN = 0x0201;
    private const int WM_LBUTTONUP = 0x0202;
    private const int WM_RBUTTONDOWN = 0x0204;
    private const int WM_RBUTTONUP = 0x0205;


    private static LowLevelKeyboardProc _proc = HookCallback;
    private static MouseProc _Mouseproc = MouseHookCallback;
    private static IntPtr _hookID = IntPtr.Zero;
    private static IntPtr _mouseHookID = IntPtr.Zero;

    private delegate IntPtr MouseProc(int nCode, IntPtr wParam, IntPtr lParam);
    private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

    private static Process[] pidList = null;

    public static void Main()
    {
        pidList = initMinionApps();
        _hookID = SetHook(_proc);
        _mouseHookID = SetMouseHook(_Mouseproc);
        Application.Run();
        UnhookWindowsHookEx(_hookID);
        UnhookWindowsHookEx(_mouseHookID);

    }

    private static Process[] initMinionApps()
    {
        // Populate list containing Processes we want to inject keys into.
        Process[] wowList = Process.GetProcessesByName("wow");
        foreach (Process item in wowList)
        {
            Console.WriteLine(item.Id);
        }
        return wowList;
    }

    private static void sendKeyToProcess(Keys key, Process p,IntPtr flagthing)
    {
        // Sends key to p. flagthing is for example WM_KEYUP
        IntPtr recvHandle = p.MainWindowHandle;
        SendMessage(recvHandle, (int)flagthing, (int)key, IntPtr.Zero);
    }



    private static IntPtr SetHook(LowLevelKeyboardProc proc)
    {
        using (Process curProcess = Process.GetCurrentProcess())
        using (ProcessModule curModule = curProcess.MainModule)
        {
            return SetWindowsHookEx(WH_KEYBOARD_LL, proc,
                GetModuleHandle(curModule.ModuleName), 0);
        }
    }

    private static IntPtr SetMouseHook(MouseProc proc)
    {
        using (Process curProcess = Process.GetCurrentProcess())
        using (ProcessModule curModule = curProcess.MainModule)
        {
            return SetWindowsHookEx(WH_MOUSE, proc,
                GetModuleHandle(curModule.ModuleName), 0);
        }
    }

    private static IntPtr MouseHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            if (WM_LBUTTONDOWN == (int)wParam)
            {
                RECT myRECT;
                GetWindowRect(pidList[0].MainWindowHandle,out myRECT);
                Console.WriteLine(myRECT.left);
                MouseHookStruct MyMouseHookStruct = new MouseHookStruct();
                Marshal.PtrToStructure(lParam, MyMouseHookStruct);
                Console.WriteLine(MyMouseHookStruct.pt.x + ", " + MyMouseHookStruct.pt.y);
        
            }

        }
        return CallNextHookEx(_mouseHookID, nCode, wParam, lParam);
    }


    private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            int vkCode = Marshal.ReadInt32(lParam);
            foreach (Process item in pidList)
            {
                sendKeyToProcess((Keys)vkCode, item, wParam);
            }
            Console.WriteLine((Keys)vkCode);
        }
        return CallNextHookEx(_hookID, nCode, wParam, lParam);
    }

    [DllImport("user32.dll", SetLastError = true)]
    static extern bool GetWindowRect(IntPtr hwnd, out RECT lpRect);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook,
        LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook,
        MouseProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode,
        IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string lpModuleName);

    [DllImport("user32.dll")]
    static extern int SendMessage(IntPtr thWnd, int msg, int wParam, IntPtr lParam);
}