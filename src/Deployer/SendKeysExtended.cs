using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace Deployer
{
    class SendKeysExtended
    {
        // Get a handle to an application window.
        [DllImport("USER32.DLL")]
        public static extern IntPtr FindWindow(string lpClassName,
            string lpWindowName);

        // Activate an application window.
        [DllImport("USER32.DLL")]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        internal static void SendKeys(IntPtr intPtr, params string[] keysToSend)
        {
            //SetForegroundWindow(intPtr);

            foreach (string s in keysToSend)
                System.Windows.Forms.SendKeys.SendWait(s);
        }
    }
}
