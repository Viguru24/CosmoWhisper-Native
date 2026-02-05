using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using MessageBox = System.Windows.MessageBox;
using MessageBoxButton = System.Windows.MessageBoxButton;
using MessageBoxImage = System.Windows.MessageBoxImage;
using MessageBoxResult = System.Windows.MessageBoxResult;
using System.Windows.Controls;
using CosmoWhisper.Managers;
using CosmoWhisper.Models;
using CosmoWhisper;
using TextBox = System.Windows.Controls.TextBox;

namespace CosmoWhisper.Controllers
{
    public class VocabularyController : BaseViewController
    {
        public ObservableCollection<VocabularyItem> VocabularyItems { get; } = new ObservableCollection<VocabularyItem>();
        private VocabularyItem? _itemToDelete;

        public VocabularyController(DashboardWindow window) : base(window)
        {
        }

        public void Initialize()
        {
            if (Window.VocabList == null) return;

            VocabularyItems.Clear();
            foreach (var kvp in VocabularyManager.Shared.Replacements)
            {
                VocabularyItems.Add(new VocabularyItem { Key = kvp.Key, Value = kvp.Value, OriginalKey = kvp.Key });
            }
            Window.VocabList.ItemsSource = VocabularyItems;
        }

        public void AddVocabulary()
        {
            if (Window.TxtNewKey == null || Window.TxtNewValue == null) return;

            string key = Window.TxtNewKey.Text.Trim();
            string value = Window.TxtNewValue.Text.Trim();

            if (!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(value))
            {
                if (VocabularyManager.Shared.Replacements.ContainsKey(key))
                {
                    _ = CosmoMessage.Show("Duplicate Entry", $"'{key}' already exists in your vocabulary.", "🗒️");
                    return;
                }

                VocabularyManager.Shared.AddReplacement(key, value);
                VocabularyItems.Add(new VocabularyItem { Key = key, Value = value, OriginalKey = key });

                // Clear inputs
                Window.TxtNewKey.Text = "";
                Window.TxtNewValue.Text = "";
            }
        }

        public void DeleteVocabulary(object dataContext)
        {
            if (dataContext is VocabularyItem item)
            {
                _itemToDelete = item;

                if (Window.TxtConfirmDeleteMessage != null)
                    Window.TxtConfirmDeleteMessage.Text = $"Are you sure you want to delete '{item.Key}'?";

                if (Window.OverlayConfirmDelete != null)
                    Window.OverlayConfirmDelete.Visibility = Visibility.Visible;
            }
        }

        public void ConfirmDelete()
        {
            if (_itemToDelete != null)
            {
                VocabularyManager.Shared.RemoveReplacement(_itemToDelete.OriginalKey); // Use OriginalKey to match backend
                VocabularyItems.Remove(_itemToDelete);
                _itemToDelete = null;
            }
            if (Window.OverlayConfirmDelete != null) Window.OverlayConfirmDelete.Visibility = Visibility.Collapsed;
        }

        public void CancelDelete()
        {
            _itemToDelete = null;
            if (Window.OverlayConfirmDelete != null) Window.OverlayConfirmDelete.Visibility = Visibility.Collapsed;
        }

        public void EditVocabulary(object dataContext)
        {
            if (dataContext is VocabularyItem item)
            {
                item.TempKey = item.Key;
                item.TempValue = item.Value;
                item.IsEditing = true;
            }
        }

        public void SaveVocabulary(object dataContext)
        {
            if (dataContext is VocabularyItem item)
            {
                string newKey = item.Key.Trim();
                string newValue = item.Value.Trim();

                if (string.IsNullOrEmpty(newKey) || string.IsNullOrEmpty(newValue))
                {
                    _ = CosmoMessage.Show("Invalid Input", "Key and Value cannot be empty.", "⚠️");
                    return;
                }

                // If key changed, check duplicates / remove old
                if (newKey != item.OriginalKey)
                {
                    if (VocabularyManager.Shared.Replacements.ContainsKey(newKey))
                    {
                        _ = CosmoMessage.Show("Duplicate Entry", $"'{newKey}' already exists.", "🗒️");
                        return;
                    }
                    VocabularyManager.Shared.RemoveReplacement(item.OriginalKey);
                }

                VocabularyManager.Shared.AddReplacement(newKey, newValue);
                item.OriginalKey = newKey;
                item.IsEditing = false;
            }
        }

        public void CancelVocabulary(object dataContext)
        {
            if (dataContext is VocabularyItem item)
            {
                item.Key = item.TempKey;
                item.Value = item.TempValue;
                item.IsEditing = false;
            }
        }

        public void ToggleSecureMode(bool show)
        {
            if (Window.OverlaySecureMode != null)
                Window.OverlaySecureMode.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
        }

        public void ConfirmSecureMode()
        {
            // Clear everything
            VocabularyManager.Shared.Replacements.Clear();
            VocabularyManager.Shared.Save();
            VocabularyItems.Clear();

            if (Window.TxtVocabHints != null)
                Window.TxtVocabHints.Text = "";

            ToggleSecureMode(false);
        }

        public void UpdatePlaceholderVisibility(TextBox txt)
        {
            if (txt.Name == "TxtNewKey" && Window.PlaceholderKey != null)
            {
                Window.PlaceholderKey.Visibility = string.IsNullOrEmpty(txt.Text) ? Visibility.Visible : Visibility.Collapsed;
            }
            else if (txt.Name == "TxtNewValue" && Window.PlaceholderValue != null)
            {
                Window.PlaceholderValue.Visibility = string.IsNullOrEmpty(txt.Text) ? Visibility.Visible : Visibility.Collapsed;
            }
        }
    }
}
