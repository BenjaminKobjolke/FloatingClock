using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using FloatingClock.Config;
using FloatingClock.Managers;

namespace FloatingClock
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private static Mutex _instanceMutex;

        // Win32 API imports for window management
        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern bool IsIconic(IntPtr hWnd);

        
        /// <summary>
        /// Logs debug information to help diagnose mutex issues.
        /// Logs are written to %LOCALAPPDATA%\FloatingClock\debug.log
        /// </summary>
        private static void LogDebug(string message)
        {
            try
            {
                string logPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    Constants.DebugLogSubPath
                );
                Directory.CreateDirectory(Path.GetDirectoryName(logPath));

                string logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [PID:{Process.GetCurrentProcess().Id}] {message}{Environment.NewLine}";
                File.AppendAllText(logPath, logEntry);
            }
            catch
            {
                // Ignore logging errors - don't break the app if logging fails
            }
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            LogDebug("Application starting");

            bool canStart = false;

            try
            {
                // Create a named mutex to ensure single instance
                bool createdNew;
                _instanceMutex = new Mutex(true, Constants.AppMutexName, out createdNew);

                if (!createdNew)
                {
                    LogDebug("Mutex already exists - attempting to acquire");

                    // Mutex already exists, but it might be abandoned
                    // Try to acquire it with zero timeout to check
                    try
                    {
                        if (!_instanceMutex.WaitOne(TimeSpan.Zero, false))
                        {
                            // Another instance is genuinely running
                            LogDebug("Another instance is running (could not acquire mutex)");

                            MessageBox.Show(
                                LocalizationManager.Lang("app.already_running"),
                                Constants.AppName,
                                MessageBoxButton.OK,
                                MessageBoxImage.Information);

                            // Find the existing window and bring it to the foreground
                            IntPtr existingWindow = FindWindow(null, Constants.AppName);
                            if (existingWindow != IntPtr.Zero)
                            {
                                LogDebug("Found existing window, bringing to foreground");

                                // If window is minimized, restore it
                                if (IsIconic(existingWindow))
                                {
                                    ShowWindow(existingWindow, Constants.SW_RESTORE);
                                }

                                // Bring window to foreground
                                SetForegroundWindow(existingWindow);
                            }
                            else
                            {
                                LogDebug("WARNING: Could not find existing window");
                            }

                            // Shutdown this instance
                            Shutdown();
                            return;
                        }
                        else
                        {
                            // We successfully acquired the mutex - it was abandoned
                            LogDebug("Acquired abandoned mutex - previous instance crashed");
                            canStart = true;
                        }
                    }
                    catch (AbandonedMutexException)
                    {
                        // Previous instance crashed - we now own the mutex
                        LogDebug("Recovered from abandoned mutex exception");
                        canStart = true;
                    }
                }
                else
                {
                    LogDebug("Created new mutex - first instance");
                    canStart = true;
                }
            }
            catch (Exception ex)
            {
                LogDebug($"ERROR in mutex handling: {ex.GetType().Name} - {ex.Message}");

                // If we can't determine mutex state, show error and allow startup
                MessageBox.Show(
                    $"Error checking for existing instance: {ex.Message}\n\nProceeding with startup.",
                    Constants.AppName,
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                canStart = true;
            }

            if (!canStart)
            {
                return;
            }

            // Continue with normal startup
            LogDebug("Proceeding with normal startup");
            base.OnStartup(e);

            // Manually create and show the main window
            MainWindow mainWindow = new MainWindow();
            mainWindow.Show();
            LogDebug("Main window created and shown");
        }

        /// <summary>
        /// Releases the mutex before restarting to prevent "already running" error
        /// </summary>
        public static void ReleaseMutexForRestart()
        {
            if (_instanceMutex != null)
            {
                try
                {
                    _instanceMutex.ReleaseMutex();
                    _instanceMutex.Dispose();
                    _instanceMutex = null;
                }
                catch { }
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            LogDebug("Application exiting");

            // Release the mutex when the application exits
            if (_instanceMutex != null)
            {
                try
                {
                    _instanceMutex.ReleaseMutex();
                    LogDebug("Mutex released successfully");
                }
                catch (ApplicationException)
                {
                    // Mutex was not owned by this thread - this can happen if
                    // we shut down before acquiring it, which is fine
                    LogDebug("Mutex was not owned (this is normal if we detected another instance)");
                }
                catch (Exception ex)
                {
                    LogDebug($"ERROR releasing mutex: {ex.GetType().Name} - {ex.Message}");
                }
                finally
                {
                    try
                    {
                        _instanceMutex.Dispose();
                        _instanceMutex = null;
                        LogDebug("Mutex disposed");
                    }
                    catch (Exception ex)
                    {
                        LogDebug($"ERROR disposing mutex: {ex.GetType().Name} - {ex.Message}");
                    }
                }
            }

            base.OnExit(e);
        }
    }
}
