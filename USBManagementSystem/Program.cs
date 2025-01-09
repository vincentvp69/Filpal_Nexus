using System;
using System.Windows.Forms;
using System.IO;
using System.Linq;
using System.Security.Principal;

namespace UsbManagementSystem
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            try
            {
                // Check for administrator privileges
                if (!IsAdministrator())
                {
                    MessageBox.Show("This application requires administrator privileges to manage USB devices.\n\n" +
                        "Please right-click the application and select 'Run as administrator'.",
                        "Administrator Rights Required",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return;
                }

                // Ensure devcon.exe exists
                if (!EnsureDevconExists())
                {
                    MessageBox.Show("Could not find or copy devcon.exe.\n\n" +
                        "Please ensure Windows Driver Kit (WDK) is installed or manually copy devcon.exe to the application directory.",
                        "Missing Required File",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                    return;
                }

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new Form1());
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fatal Error: {ex.Message}\n\nApplication will now exit.",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private static bool IsAdministrator()
        {
            WindowsIdentity identity = WindowsIdentity.GetCurrent();
            WindowsPrincipal principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        private static bool EnsureDevconExists()
        {
            string outputPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "devcon.exe");
            if (File.Exists(outputPath))
                return true;

            string[] possiblePaths = new[]
            {
                @"C:\Program Files (x86)\Windows Kits\10\Tools\10.0.26100.0\x64\devcon.exe",
                @"C:\Program Files (x86)\Windows Kits\10\Tools\10.0.26100.0\arm64\devcon.exe"
            };

            string sourcePath = possiblePaths.FirstOrDefault(File.Exists);
            if (string.IsNullOrEmpty(sourcePath))
                return false;

            try
            {
                File.Copy(sourcePath, outputPath, true);
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error copying devcon.exe: {ex.Message}",
                    "File Copy Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return false;
            }
        }
    }
}