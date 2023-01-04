using System.Runtime.InteropServices;

namespace TowerDefence
{
    internal static class MessageBox
    {
        [DllImport("MessageBox.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        private extern static void DisplayError(string message);

        public static void Display(string message) => DisplayError(message);
    }
}