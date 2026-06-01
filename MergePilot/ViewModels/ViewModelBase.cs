using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MergePilot.ViewModels
{
    /// <summary>
    /// Base class for all ViewModels providing INotifyPropertyChanged implementation.
    /// </summary>
    /// <remarks>
    /// Provides common functionality for property notification and change tracking.
    /// Derived classes should use SetProperty() method to update properties
    /// and trigger PropertyChanged events automatically.
    /// </remarks>
    public abstract class ViewModelBase : INotifyPropertyChanged
    {
        /// <summary>
        /// Raised when a property value changes.
        /// </summary>
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Raises the PropertyChanged event for the specified property.
        /// </summary>
        /// <param name="propertyName">The name of the property that changed.</param>
        /// <remarks>
        /// This method is typically called from property setters after updating the backing field.
        /// You can use the CallerMemberName attribute to avoid passing property names as strings.
        /// </remarks>
        protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Sets a property value and raises PropertyChanged if the value actually changed.
        /// </summary>
        /// <typeparam name="T">The type of the property.</typeparam>
        /// <param name="backingField">Reference to the backing field.</param>
        /// <param name="value">The new value to set.</param>
        /// <param name="propertyName">The name of the property (auto-populated via CallerMemberName).</param>
        /// <returns>True if the property was changed; false if the value was already the same.</returns>
        /// <remarks>
        /// This method is the preferred way to update properties in ViewModels.
        /// It automatically handles comparison and PropertyChanged notifications.
        /// 
        /// Example:
        /// <code>
        /// private string _title;
        /// public string Title
        /// {
        ///     get => _title;
        ///     set => SetProperty(ref _title, value);
        /// }
        /// </code>
        /// </remarks>
        protected bool SetProperty<T>(
            ref T backingField,
            T value,
            [CallerMemberName] string propertyName = "")
        {
            // If values are equal, no change needed
            if (EqualityComparer<T>.Default.Equals(backingField, value))
                return false;

            // Update the backing field
            backingField = value;

            // Notify listeners of the change
            OnPropertyChanged(propertyName);

            return true;
        }
    }
}
