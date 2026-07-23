using Microsoft.Win32;
using MiniDeck.Models;
using MiniDeck.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace MiniDeck
{
    public partial class MacroActionSettingsWindow : Window
    {
        private const int MaximumActionCount = 50;
        private readonly ObservableCollection<MacroActionStep> _actions;
        private MacroActionStep _editingStep;
        private bool _loadingEditor;

        public IReadOnlyList<MacroActionStep> Actions =>
            _actions.Select(action => action.Clone()).ToList();

        public MacroActionSettingsWindow(IEnumerable<MacroActionStep> actions)
        {
            InitializeComponent();
            _actions = new ObservableCollection<MacroActionStep>(
                (actions ?? Enumerable.Empty<MacroActionStep>())
                    .Where(action => action != null)
                    .Select(action => action.Clone()));
            ActionList.ItemsSource = _actions;

            if (_actions.Count == 0)
            {
                _actions.Add(new MacroActionStep());
            }

            ActionList.SelectedIndex = 0;
        }

        private void ActionList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_loadingEditor)
            {
                return;
            }

            SaveEditorToStep(_editingStep, false);
            _editingStep = ActionList.SelectedItem as MacroActionStep;
            LoadStepIntoEditor(_editingStep);
        }

        private void LoadStepIntoEditor(MacroActionStep step)
        {
            _loadingEditor = true;
            try
            {
                ActionEditor.IsEnabled = step != null;
                if (step == null)
                {
                    StepActionTypeCombo.SelectedIndex = -1;
                    StepShortcutBox.Text = "";
                    StepApplicationPathBox.Text = "";
                    StepApplicationArgumentsBox.Text = "";
                    StepUrlBox.Text = "";
                    StepDelayBox.Text = "0";
                    UpdateActionPanels(ActionType.None);
                    return;
                }

                SelectActionType(step.ActionType);
                StepShortcutBox.Text = step.ShortcutKeySequence ?? "";
                StepApplicationPathBox.Text = step.ApplicationPath ?? "";
                StepApplicationArgumentsBox.Text = step.ApplicationArguments ?? "";
                StepUrlBox.Text = step.Url ?? "";
                StepDelayBox.Text = step.DelayAfterMilliseconds.ToString();
                UpdateActionPanels(step.ActionType);
            }
            finally
            {
                _loadingEditor = false;
            }
        }

        private void SelectActionType(ActionType actionType)
        {
            foreach (ComboBoxItem item in StepActionTypeCombo.Items)
            {
                if (item.Tag is ActionType itemType && itemType == actionType)
                {
                    StepActionTypeCombo.SelectedItem = item;
                    return;
                }
            }

            StepActionTypeCombo.SelectedIndex = 0;
        }

        private ActionType GetSelectedActionType()
        {
            return (StepActionTypeCombo.SelectedItem as ComboBoxItem)?.Tag is ActionType actionType
                ? actionType
                : ActionType.None;
        }

        private void StepActionTypeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_loadingEditor)
            {
                return;
            }

            UpdateActionPanels(GetSelectedActionType());
        }

        private void UpdateActionPanels(ActionType actionType)
        {
            StepKeyPanel.Visibility = actionType == ActionType.KeyPress
                ? Visibility.Visible
                : Visibility.Collapsed;
            StepApplicationPanel.Visibility = actionType == ActionType.LaunchApplication
                ? Visibility.Visible
                : Visibility.Collapsed;
            StepUrlPanel.Visibility = actionType == ActionType.OpenUrl
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        private bool SaveEditorToStep(MacroActionStep step, bool validate)
        {
            if (step == null)
            {
                return true;
            }

            ActionType actionType = GetSelectedActionType();
            string shortcut = StepShortcutBox.Text?.Trim() ?? "";
            string applicationPath = StepApplicationPathBox.Text?.Trim() ?? "";
            string applicationArguments = StepApplicationArgumentsBox.Text ?? "";
            string url = StepUrlBox.Text?.Trim() ?? "";
            if (!int.TryParse(StepDelayBox.Text?.Trim(), out int delay))
            {
                if (validate)
                {
                    ShowEditorError("待機時間は0～600000の整数で入力してください。", StepDelayBox);
                    return false;
                }

                delay = -1;
            }

            if (validate)
            {
                if (delay < 0 || delay > 600000)
                {
                    ShowEditorError("待機時間は0～600000ミリ秒で指定してください。", StepDelayBox);
                    return false;
                }

                if (actionType == ActionType.KeyPress && string.IsNullOrWhiteSpace(shortcut))
                {
                    ShowEditorError("キーシーケンスを入力してください。", StepShortcutBox);
                    return false;
                }

                if (actionType == ActionType.LaunchApplication && string.IsNullOrWhiteSpace(applicationPath))
                {
                    ShowEditorError("開くアプリまたはファイルのパスを入力してください。", StepApplicationPathBox);
                    return false;
                }

                if (actionType == ActionType.OpenUrl)
                {
                    if (!ActionService.TryCreateWebUri(url, out Uri validatedUri, out string errorMessage))
                    {
                        ShowEditorError(errorMessage, StepUrlBox);
                        return false;
                    }

                    url = validatedUri.AbsoluteUri;
                }
            }

            step.ActionType = actionType;
            step.ShortcutKeySequence = shortcut;
            step.ApplicationPath = applicationPath;
            step.ApplicationArguments = applicationArguments;
            step.Url = url;
            step.DelayAfterMilliseconds = delay;
            ActionList.Items.Refresh();
            return true;
        }

        private void ShowEditorError(string message, Control control)
        {
            MessageBox.Show(
                message,
                "アクション設定を確認してください",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            control?.Focus();
        }

        private void ApplyStepEditor_Click(object sender, RoutedEventArgs e)
        {
            if (SaveEditorToStep(_editingStep, true))
            {
                EditorHint.Text = "編集内容を反映しました。";
            }
        }

        private void AddAction_Click(object sender, RoutedEventArgs e)
        {
            SaveEditorToStep(_editingStep, false);
            if (_actions.Count >= MaximumActionCount)
            {
                MessageBox.Show(
                    $"登録できるアクションは最大{MaximumActionCount}個です。",
                    "アクションを追加できません",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            var action = new MacroActionStep();
            int insertIndex = ActionList.SelectedIndex >= 0
                ? ActionList.SelectedIndex + 1
                : _actions.Count;
            _actions.Insert(insertIndex, action);
            ActionList.SelectedItem = action;
            ActionList.ScrollIntoView(action);
        }

        private void DeleteAction_Click(object sender, RoutedEventArgs e)
        {
            int index = ActionList.SelectedIndex;
            if (index < 0)
            {
                return;
            }

            _actions.RemoveAt(index);
            if (_actions.Count > 0)
            {
                ActionList.SelectedIndex = Math.Min(index, _actions.Count - 1);
            }
            else
            {
                _editingStep = null;
                LoadStepIntoEditor(null);
            }
        }

        private void MoveActionUp_Click(object sender, RoutedEventArgs e)
        {
            MoveSelectedAction(-1);
        }

        private void MoveActionDown_Click(object sender, RoutedEventArgs e)
        {
            MoveSelectedAction(1);
        }

        private void MoveSelectedAction(int offset)
        {
            SaveEditorToStep(_editingStep, false);
            int oldIndex = ActionList.SelectedIndex;
            int newIndex = oldIndex + offset;
            if (oldIndex < 0 || newIndex < 0 || newIndex >= _actions.Count)
            {
                return;
            }

            _actions.Move(oldIndex, newIndex);
            ActionList.SelectedIndex = newIndex;
        }

        private void BrowseStepApplication_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "すべてのファイル (*.*)|*.*|実行ファイル (*.exe)|*.exe|ショートカット (*.lnk)|*.lnk",
                Title = "開くアプリまたはファイルを選択"
            };
            if (dialog.ShowDialog(this) == true)
            {
                StepApplicationPathBox.Text = dialog.FileName;
            }
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            if (!SaveEditorToStep(_editingStep, true))
            {
                return;
            }

            if (_actions.Count == 0)
            {
                MessageBox.Show(
                    "アクションを1個以上追加してください。",
                    "マルチアクションが空です",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            for (int index = 0; index < _actions.Count; index++)
            {
                MacroActionStep step = _actions[index];
                if (!ValidateStoredStep(step, out string errorMessage))
                {
                    ActionList.SelectedIndex = index;
                    MessageBox.Show(
                        $"{index + 1}番目のアクションを確認してください。\n{errorMessage}",
                        "アクション設定を確認してください",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                    return;
                }
            }

            DialogResult = true;
            Close();
        }

        private static bool ValidateStoredStep(MacroActionStep step, out string errorMessage)
        {
            errorMessage = "";
            if (step.DelayAfterMilliseconds < 0 || step.DelayAfterMilliseconds > 600000)
            {
                errorMessage = "待機時間は0～600000ミリ秒で指定してください。";
                return false;
            }

            switch (step.ActionType)
            {
                case ActionType.KeyPress:
                    if (string.IsNullOrWhiteSpace(step.ShortcutKeySequence))
                    {
                        errorMessage = "キーシーケンスが未設定です。";
                        return false;
                    }
                    break;
                case ActionType.LaunchApplication:
                    if (string.IsNullOrWhiteSpace(step.ApplicationPath))
                    {
                        errorMessage = "対象パスが未設定です。";
                        return false;
                    }
                    break;
                case ActionType.OpenUrl:
                    if (!ActionService.TryCreateWebUri(step.Url, out _, out errorMessage))
                    {
                        return false;
                    }
                    break;
                default:
                    errorMessage = "使用できないアクション種別です。";
                    return false;
            }

            return true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
