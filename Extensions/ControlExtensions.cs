using System;
using System.Windows.Forms;

namespace ProgramUpdater.Extensions
{
    public static class ControlExtensions
    {
        public static void InvokeIfRequired(this Control control, Action action)
        {
            if (control.InvokeRequired)
            {
                control.Invoke(action);
            }
            else
            {
                action();
            }
        }
    }

    public enum LogLevel
        {
            Info,
            Warning,
            Error,
            Success
        }
} 