using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using FloatingClock.Config;

namespace FloatingClock.Managers
{
    /// <summary>
    /// Manages the system tray icon and context menu
    /// </summary>
    public class TrayManager : IDisposable
    {
        private NotifyIcon _trayIcon;
        private ContextMenuStrip _contextMenu;
        private bool _disposed;

        /// <summary>
        /// Raised when the user requests to show the main window
        /// </summary>
        public event EventHandler ShowRequested;

        /// <summary>
        /// Raised when the user requests to open settings
        /// </summary>
        public event EventHandler SettingsRequested;

        /// <summary>
        /// Raised when the user requests to exit the application
        /// </summary>
        public event EventHandler ExitRequested;

        /// <summary>
        /// Creates a new TrayManager with the specified icon
        /// </summary>
        /// <param name="icon">The icon to display in the system tray</param>
        public TrayManager(Icon icon)
        {
            CreateContextMenu();
            CreateTrayIcon(icon);
        }

        private void CreateContextMenu()
        {
            _contextMenu = new ContextMenuStrip();

            // Show menu item
            var showItem = new ToolStripMenuItem(LocalizationManager.Lang("tray.show"));
            showItem.Click += (s, e) => ShowRequested?.Invoke(this, EventArgs.Empty);
            _contextMenu.Items.Add(showItem);

            // Settings menu item
            var settingsItem = new ToolStripMenuItem(LocalizationManager.Lang("tray.settings"));
            settingsItem.Click += (s, e) => SettingsRequested?.Invoke(this, EventArgs.Empty);
            _contextMenu.Items.Add(settingsItem);

            // More Tools menu item
            var moreToolsItem = new ToolStripMenuItem(LocalizationManager.Lang("tray.more_tools"));
            moreToolsItem.Click += (s, e) => Process.Start(Constants.MoreToolsUrl);
            _contextMenu.Items.Add(moreToolsItem);

            _contextMenu.Items.Add(new ToolStripSeparator());

            // Exit menu item
            var exitItem = new ToolStripMenuItem(LocalizationManager.Lang("tray.exit"));
            exitItem.Click += (s, e) => ExitRequested?.Invoke(this, EventArgs.Empty);
            _contextMenu.Items.Add(exitItem);
        }

        private void CreateTrayIcon(Icon icon)
        {
            _trayIcon = new NotifyIcon
            {
                Icon = icon,
                Text = LocalizationManager.Lang("app.title"),
                ContextMenuStrip = _contextMenu,
                Visible = true
            };

            // Double-click shows the window
            _trayIcon.DoubleClick += (s, e) => ShowRequested?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Updates the tray icon (e.g., when theme changes)
        /// </summary>
        /// <param name="icon">The new icon to display</param>
        public void UpdateIcon(Icon icon)
        {
            if (icon != null && _trayIcon != null)
            {
                _trayIcon.Icon = icon;
            }
        }

        /// <summary>
        /// Shows or hides the tray icon
        /// </summary>
        public bool Visible
        {
            get => _trayIcon?.Visible ?? false;
            set
            {
                if (_trayIcon != null)
                {
                    _trayIcon.Visible = value;
                }
            }
        }

        /// <summary>
        /// Disposes of the tray icon and context menu
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (disposing)
            {
                if (_trayIcon != null)
                {
                    _trayIcon.Visible = false;
                    _trayIcon.Dispose();
                    _trayIcon = null;
                }

                _contextMenu?.Dispose();
                _contextMenu = null;
            }

            _disposed = true;
        }

        ~TrayManager()
        {
            Dispose(false);
        }
    }
}
