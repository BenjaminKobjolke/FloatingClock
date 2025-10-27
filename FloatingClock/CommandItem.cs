using System;

namespace FloatingClock
{
    /// <summary>
    /// Represents a command item in the command palette
    /// </summary>
    public class CommandItem
    {
        /// <summary>
        /// The description of the command shown to the user
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// The keyboard shortcut for this command
        /// </summary>
        public string KeyBinding { get; set; }

        /// <summary>
        /// Whether this command is currently active (for toggle commands)
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// The action to execute when this command is selected
        /// </summary>
        public Action Action { get; set; }

        /// <summary>
        /// Gets the display text with status indicator
        /// </summary>
        public string DisplayText
        {
            get
            {
                string indicator = IsActive ? "✓" : " ";
                return $"[{indicator}] {Description} ({KeyBinding})";
            }
        }

        public CommandItem(string description, string keyBinding, Action action, bool isActive = false)
        {
            Description = description;
            KeyBinding = keyBinding;
            Action = action;
            IsActive = isActive;
        }
    }
}
