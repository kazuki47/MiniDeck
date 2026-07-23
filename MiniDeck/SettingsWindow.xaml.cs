using System.Windows;
using MiniDeck.ViewModels;
using MiniDeck.Models;
using System.Windows.Controls;
using Microsoft.Win32;
using System;
using System.Windows.Media;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using MiniDeck.Controls;
using MiniDeck.Services;

namespace MiniDeck
{
    public partial class SettingsWindow : Window
    {
        private MainViewModel _viewModel;
        private bool _isSynchronizingOpacitySliders;
        private bool _isSwitchingDraftPage;
        private PageSetting _selectedDraftPage;
        private readonly ButtonDropService _buttonDropService = new ButtonDropService();
        private static readonly EditorClipboardService EditorClipboard = new EditorClipboardService();
        private string _lastAppliedBackgroundImagePath;

        public ObservableCollection<ActionButton> DraftButtons { get; } = new ObservableCollection<ActionButton>();
        public ObservableCollection<PageSetting> DraftPages { get; } = new ObservableCollection<PageSetting>();

        internal PageSetting SelectedDraftPage => _selectedDraftPage;
          
        public SettingsWindow(MainViewModel viewModel)
        {
            try
            {
                InitializeComponent();
                _viewModel = viewModel;
                _lastAppliedBackgroundImagePath = _viewModel.BackgroundImagePath;
                DataContext = _viewModel;

                InitializeSliderValues();
                InitializeGeneralSettings();
                ReloadPageDrafts(_viewModel.ActivePageId);
                
                // ラジオボタンのイベントハンドラーを設定
                ColorRadioButton.Checked += BackgroundType_Changed;
                ImageRadioButton.Checked += BackgroundType_Changed;
                
                // 初期状態を設定
                if (_viewModel.UseBackgroundImage)
                {
                    ImageRadioButton.IsChecked = true;
                }
                else
                {
                    ColorRadioButton.IsChecked = true;
                }
                
                // すべてのコントロールが初期化された後にプレビューを設定
                this.Loaded += (s, e) => 
                {
                    UpdateBackgroundPreview();
                    // 初期プレビューで画像があれば表示
                    if (!string.IsNullOrEmpty(_viewModel.BackgroundImagePath))
                    {
                        UpdateImagePreview(_viewModel.BackgroundImagePath);
                    }
                    
                    // デバッグ情報
                    Console.WriteLine($"SettingsWindowがロードされました。現在の透明度: {_viewModel.BackgroundOpacity:F2}");
                };
            }
            catch (Exception ex)
            {
                MessageBox.Show($"設定ウィンドウの初期化でエラーが発生しました:\n{ex.Message}\n\nスタックトレース:\n{ex.StackTrace}", 
                    "初期化エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void InitializeSliderValues()
        {
            _isSynchronizingOpacitySliders = true;
            try
            {
                RowsSlider.Value = _viewModel.ButtonRows;
                ColumnsSlider.Value = _viewModel.ButtonColumns;
                OpacitySlider.Value = _viewModel.BackgroundOpacity;
                BackgroundOpacitySlider.Value = _viewModel.BackgroundOpacity;
                ButtonOpacitySlider.Value = _viewModel.ButtonOpacity;
            }
            finally
            {
                _isSynchronizingOpacitySliders = false;
            }

            UpdateLayoutChangeHint();
        }

        private void InitializeGeneralSettings()
        {
            AutoStartCheckBox.IsChecked = _viewModel.AutoStart;
            AlwaysOnTopCheckBox.IsChecked = _viewModel.AlwaysOnTop;
        }

        private void PageSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isSwitchingDraftPage)
            {
                return;
            }

            PageSetting nextPage = PageSelector.SelectedItem as PageSetting;
            if (nextPage == null || ReferenceEquals(nextPage, _selectedDraftPage))
            {
                UpdateDraftPageDisplay();
                return;
            }

            CommitSelectedPageDraft();
            SelectDraftPage(nextPage);
        }

        private void AddPage_Click(object sender, RoutedEventArgs e)
        {
            AddDraftPage();
        }

        internal PageSetting AddDraftPage()
        {
            CommitSelectedPageDraft();

            var page = new PageSetting
            {
                Id = Guid.NewGuid().ToString("N"),
                Name = CreateUniqueDraftPageName($"ページ {DraftPages.Count + 1}"),
                Buttons = CreateEmptyDraftButtonSettings()
            };

            DraftPages.Add(page);
            SelectDraftPage(page);
            UpdateLayoutChangeHint();
            return page;
        }

        private void DuplicatePage_Click(object sender, RoutedEventArgs e)
        {
            if (DuplicateSelectedDraftPage() == null)
            {
                MessageBox.Show(
                    "複製するページを選択してください。",
                    "ページを選択してください",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
        }

        internal PageSetting DuplicateSelectedDraftPage()
        {
            if (_selectedDraftPage == null)
            {
                return null;
            }

            CommitSelectedPageDraft();
            string duplicateName = CreateUniqueDraftPageName(_selectedDraftPage.Name + " のコピー");
            PageSetting duplicate = _selectedDraftPage.Clone(Guid.NewGuid().ToString("N"), duplicateName);
            int sourceIndex = DraftPages.IndexOf(_selectedDraftPage);
            DraftPages.Insert(sourceIndex + 1, duplicate);
            SelectDraftPage(duplicate);
            UpdateLayoutChangeHint();
            return duplicate;
        }

        private void RenamePage_Click(object sender, RoutedEventArgs e)
        {
            string requestedName = PageNameTextBox.Text;
            if (RenameSelectedDraftPage(requestedName))
            {
                return;
            }

            string message = string.IsNullOrWhiteSpace(requestedName)
                ? "ページ名を入力してください。"
                : "同じ名前のページが既にあります。別の名前を入力してください。";
            MessageBox.Show(message, "ページ名を変更できません", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        internal bool RenameSelectedDraftPage(string newName)
        {
            string trimmedName = newName?.Trim();
            if (_selectedDraftPage == null || string.IsNullOrWhiteSpace(trimmedName))
            {
                return false;
            }

            bool duplicateExists = DraftPages.Any(page =>
                !ReferenceEquals(page, _selectedDraftPage) &&
                string.Equals(page.Name, trimmedName, StringComparison.CurrentCultureIgnoreCase));
            if (duplicateExists)
            {
                return false;
            }

            _selectedDraftPage.Name = trimmedName;
            PageNameTextBox.Text = trimmedName;
            UpdateDraftPageDisplay();
            return true;
        }

        private void DeletePage_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedDraftPage == null)
            {
                return;
            }

            if (DraftPages.Count <= 1)
            {
                MessageBox.Show(
                    "ページは最低1つ必要なため、このページは削除できません。",
                    "ページを削除できません",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            CommitSelectedPageDraft();
            int configuredButtonCount = GetConfiguredButtonCount(_selectedDraftPage);
            MessageBoxResult result = MessageBox.Show(
                $"ページ「{_selectedDraftPage.Name}」を削除しますか？\n\n設定済みのボタン {configuredButtonCount} 個も削除されます。",
                "ページの削除",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);
            if (result == MessageBoxResult.Yes)
            {
                DeleteSelectedDraftPage();
            }
        }

        internal bool DeleteSelectedDraftPage()
        {
            if (_selectedDraftPage == null || DraftPages.Count <= 1)
            {
                return false;
            }

            CommitSelectedPageDraft();
            int removedIndex = DraftPages.IndexOf(_selectedDraftPage);
            DraftPages.RemoveAt(removedIndex);
            SelectDraftPage(DraftPages[Math.Min(removedIndex, DraftPages.Count - 1)]);
            UpdateLayoutChangeHint();
            return true;
        }

        private void ReorderPages_Click(object sender, RoutedEventArgs e)
        {
            CommitSelectedPageDraft();
            var reorderWindow = new PageReorderWindow(DraftPages, _selectedDraftPage?.Id)
            {
                Owner = this
            };

            if (reorderWindow.ShowDialog() == true)
            {
                ApplyDraftPageOrder(reorderWindow.OrderedPages);
            }
        }

        private void OpenImageManagement_Click(object sender, RoutedEventArgs e)
        {
            int selectedButtonIndex = DraftButtons.IndexOf(ButtonGrid.SelectedItem);
            CommitSelectedPageDraft();
            var imageWindow = new ImageManagementWindow(
                DraftPages,
                BackgroundImagePath.Text?.Trim() ?? "",
                null,
                null,
                _viewModel.Pages,
                _lastAppliedBackgroundImagePath)
            {
                Owner = this
            };

            if (imageWindow.ShowDialog() == true)
            {
                ApplyImagePathReplacements(imageWindow.Replacements, selectedButtonIndex);
            }
        }

        internal int ApplyImagePathReplacements(
            IEnumerable<ImagePathReplacement> replacements,
            int selectedButtonIndex = 0)
        {
            int replacementCount = 0;
            foreach (ImagePathReplacement replacement in replacements ?? Enumerable.Empty<ImagePathReplacement>())
            {
                if (replacement == null || string.IsNullOrWhiteSpace(replacement.OldPath) ||
                    string.IsNullOrWhiteSpace(replacement.NewPath))
                {
                    continue;
                }

                if (replacement.Kind == ImageStorageKind.Background)
                {
                    if (string.Equals(
                        BackgroundImagePath.Text?.Trim() ?? "",
                        replacement.OldPath.Trim(),
                        StringComparison.OrdinalIgnoreCase))
                    {
                        _viewModel.BackgroundImagePath = replacement.NewPath;
                        BackgroundImagePath.Text = replacement.NewPath;
                        replacementCount++;
                    }

                    continue;
                }

                foreach (ButtonSetting button in DraftPages
                    .SelectMany(page => page?.Buttons ?? new List<ButtonSetting>())
                    .Where(button => button != null))
                {
                    if (string.Equals(
                        button.ImagePath?.Trim() ?? "",
                        replacement.OldPath.Trim(),
                        StringComparison.OrdinalIgnoreCase))
                    {
                        button.ImagePath = replacement.NewPath;
                        replacementCount++;
                    }

                    if (string.Equals(
                        button.StateActiveImagePath?.Trim() ?? "",
                        replacement.OldPath.Trim(),
                        StringComparison.OrdinalIgnoreCase))
                    {
                        button.StateActiveImagePath = replacement.NewPath;
                        replacementCount++;
                    }
                }
            }

            if (_selectedDraftPage != null)
            {
                LoadButtonDraftFromPage(_selectedDraftPage, selectedButtonIndex);
            }

            UpdateBackgroundPreview();
            return replacementCount;
        }

        internal bool ApplyDraftPageOrder(IEnumerable<PageSetting> orderedPages)
        {
            List<PageSetting> requestedOrder = (orderedPages ?? Enumerable.Empty<PageSetting>())
                .Where(page => page != null)
                .ToList();
            var currentPages = new HashSet<PageSetting>(DraftPages);
            if (requestedOrder.Count != DraftPages.Count ||
                requestedOrder.Distinct().Count() != requestedOrder.Count ||
                requestedOrder.Any(page => !currentPages.Contains(page)))
            {
                return false;
            }

            CommitSelectedPageDraft();
            PageSetting selectedPage = _selectedDraftPage;
            _isSwitchingDraftPage = true;
            try
            {
                DraftPages.Clear();
                foreach (PageSetting page in requestedOrder)
                {
                    DraftPages.Add(page);
                }

                PageSelector.SelectedItem = selectedPage;
            }
            finally
            {
                _isSwitchingDraftPage = false;
            }

            UpdateDraftPageDisplay();
            return true;
        }

        internal bool MoveSelectedDraftPage(int offset)
        {
            int currentIndex = _selectedDraftPage == null
                ? -1
                : DraftPages.IndexOf(_selectedDraftPage);
            int targetIndex = currentIndex + offset;
            if (offset == 0 || currentIndex < 0 || targetIndex < 0 || targetIndex >= DraftPages.Count)
            {
                return false;
            }

            CommitSelectedPageDraft();
            PageSetting selectedPage = _selectedDraftPage;
            _isSwitchingDraftPage = true;
            try
            {
                DraftPages.Move(currentIndex, targetIndex);
                PageSelector.SelectedItem = selectedPage;
            }
            finally
            {
                _isSwitchingDraftPage = false;
            }

            UpdateDraftPageDisplay();
            return true;
        }

        private void CopyPage_Click(object sender, RoutedEventArgs e)
        {
            if (!CopySelectedDraftPage())
            {
                MessageBox.Show(
                    "コピーするページを選択してください。",
                    "ページを選択してください",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
        }

        internal bool CopySelectedDraftPage()
        {
            if (_selectedDraftPage == null)
            {
                return false;
            }

            CommitSelectedPageDraft();
            return EditorClipboard.CopyPage(_selectedDraftPage);
        }

        private void PastePage_Click(object sender, RoutedEventArgs e)
        {
            if (PasteCopiedDraftPage() == null)
            {
                MessageBox.Show(
                    "コピーされたページがありません。先にページをコピーしてください。",
                    "貼り付けできません",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
        }

        internal PageSetting PasteCopiedDraftPage()
        {
            PageSetting pastedPage = EditorClipboard.GetPageCopy();
            if (pastedPage == null)
            {
                return null;
            }

            CommitSelectedPageDraft();
            pastedPage.Id = Guid.NewGuid().ToString("N");
            pastedPage.Name = CreateUniqueDraftPageName(
                (string.IsNullOrWhiteSpace(pastedPage.Name) ? "新しいページ" : pastedPage.Name) + " のコピー");
            pastedPage.Buttons = pastedPage.Buttons ?? new List<ButtonSetting>();
            ResizeDraftButtonSettings(pastedPage.Buttons, Math.Max(1, _viewModel.ButtonRows * _viewModel.ButtonColumns));

            int insertIndex = _selectedDraftPage == null
                ? DraftPages.Count
                : DraftPages.IndexOf(_selectedDraftPage) + 1;
            DraftPages.Insert(insertIndex, pastedPage);
            SelectDraftPage(pastedPage);
            UpdateLayoutChangeHint();
            return pastedPage;
        }

        private static int GetConfiguredButtonCount(PageSetting page)
        {
            return page?.Buttons?.Count(button => button != null && button.ActionType != ActionType.None) ?? 0;
        }

        private string CreateUniqueDraftPageName(string requestedName)
        {
            string baseName = string.IsNullOrWhiteSpace(requestedName) ? "新しいページ" : requestedName.Trim();
            string candidate = baseName;
            int suffix = 2;
            while (DraftPages.Any(page => string.Equals(
                page.Name,
                candidate,
                StringComparison.CurrentCultureIgnoreCase)))
            {
                candidate = $"{baseName} ({suffix})";
                suffix++;
            }

            return candidate;
        }

        private List<ButtonSetting> CreateEmptyDraftButtonSettings()
        {
            int buttonCount = Math.Max(1, _viewModel.ButtonRows * _viewModel.ButtonColumns);
            var buttons = new List<ButtonSetting>();
            for (int index = 0; index < buttonCount; index++)
            {
                buttons.Add(new ButtonSetting
                {
                    DisplayText = $"ボタン {index + 1}",
                    ActionType = ActionType.None
                });
            }

            return buttons;
        }

        private static void ResizeDraftButtonSettings(List<ButtonSetting> buttons, int buttonCount)
        {
            if (buttons.Count > buttonCount)
            {
                buttons.RemoveRange(buttonCount, buttons.Count - buttonCount);
            }

            while (buttons.Count < buttonCount)
            {
                buttons.Add(new ButtonSetting
                {
                    DisplayText = $"ボタン {buttons.Count + 1}",
                    ActionType = ActionType.None
                });
            }
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            EditSelectedButton();
        }

        private void EditSelectedButton()
        {
            // 編集用コピーだけを変更し、「適用」または「OK」までViewModelへ反映しない
            var selectedButton = ButtonGrid.SelectedItem;
            if (selectedButton == null) 
            {
                MessageBox.Show("編集するボタンを選択してください", "選択エラー", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            
            var buttonSettingsWindow = new ButtonSettingsWindow(selectedButton);
            buttonSettingsWindow.Owner = this;
            buttonSettingsWindow.ShowDialog();
        }

        private void ButtonGrid_ItemActivated(object sender, ActionButton button)
        {
            ButtonGrid.SelectedItem = button;
            EditSelectedButton();
        }

        private void ButtonGrid_ReorderRequested(object sender, ButtonReorderRequestedEventArgs e)
        {
            int sourceIndex = DraftButtons.IndexOf(e.Source);
            int targetIndex = DraftButtons.IndexOf(e.Target);
            SwapDraftButtons(sourceIndex, targetIndex);
        }

        private void ButtonGrid_ExternalDropRequested(object sender, ExternalButtonDropRequestedEventArgs e)
        {
            ButtonDropResult result = _buttonDropService.CreateButton(e.Data);
            if (!result.Success)
            {
                MessageBox.Show(
                    result.ErrorMessage,
                    "登録できません",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            bool allowOverwrite = e.Target?.ActionType == ActionType.None;
            if (!allowOverwrite)
            {
                MessageBoxResult overwriteResult = MessageBox.Show(
                    $"「{e.Target.DisplayText}」の設定を「{result.Button.DisplayText}」で上書きしますか？",
                    "ボタン設定の上書き",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);
                allowOverwrite = overwriteResult == MessageBoxResult.Yes;
            }

            ApplyDroppedButton(e.Target, result.Button, allowOverwrite);
        }

        internal bool ApplyDroppedButton(
            ActionButton target,
            ActionButton droppedButton,
            bool allowOverwrite)
        {
            int targetIndex = DraftButtons.IndexOf(target);
            if (targetIndex < 0 || droppedButton == null)
            {
                return false;
            }

            if (target.ActionType != ActionType.None && !allowOverwrite)
            {
                return false;
            }

            DraftButtons[targetIndex] = droppedButton;
            ButtonGrid.SelectedItem = droppedButton;
            return true;
        }

        private void MovePreviousButton_Click(object sender, RoutedEventArgs e)
        {
            int selectedIndex = DraftButtons.IndexOf(ButtonGrid.SelectedItem);
            MoveDraftButton(selectedIndex, selectedIndex - 1);
        }

        private void MoveNextButton_Click(object sender, RoutedEventArgs e)
        {
            int selectedIndex = DraftButtons.IndexOf(ButtonGrid.SelectedItem);
            MoveDraftButton(selectedIndex, selectedIndex + 1);
        }

        internal bool MoveDraftButton(int sourceIndex, int targetIndex)
        {
            if (sourceIndex < 0 || sourceIndex >= DraftButtons.Count ||
                targetIndex < 0 || targetIndex >= DraftButtons.Count || sourceIndex == targetIndex)
            {
                return false;
            }

            ActionButton selectedButton = DraftButtons[sourceIndex];
            DraftButtons.Move(sourceIndex, targetIndex);
            ButtonGrid.SelectedItem = selectedButton;
            return true;
        }

        internal bool SwapDraftButtons(int sourceIndex, int targetIndex)
        {
            if (sourceIndex < 0 || sourceIndex >= DraftButtons.Count ||
                targetIndex < 0 || targetIndex >= DraftButtons.Count || sourceIndex == targetIndex)
            {
                return false;
            }

            ActionButton sourceButton = DraftButtons[sourceIndex];
            ActionButton targetButton = DraftButtons[targetIndex];

            DraftButtons[sourceIndex] = targetButton;
            DraftButtons[targetIndex] = sourceButton;
            ButtonGrid.SelectedItem = sourceButton;
            return true;
        }

        private void CopyButton_Click(object sender, RoutedEventArgs e)
        {
            if (!CopySelectedButton())
            {
                MessageBox.Show(
                    "コピーするボタンを選択してください。",
                    "ボタンを選択してください",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
        }

        internal bool CopySelectedButton()
        {
            return EditorClipboard.CopyButton(ButtonGrid.SelectedItem);
        }

        private void PasteButton_Click(object sender, RoutedEventArgs e)
        {
            ActionButton target = ButtonGrid.SelectedItem;
            if (target == null)
            {
                MessageBox.Show(
                    "貼り付け先のボタンを選択してください。",
                    "ボタンを選択してください",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            if (!EditorClipboard.HasButton)
            {
                MessageBox.Show(
                    "コピーされたボタンがありません。先にボタンをコピーしてください。",
                    "貼り付けできません",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            bool allowOverwrite = target.ActionType == ActionType.None;
            if (!allowOverwrite)
            {
                MessageBoxResult overwriteResult = MessageBox.Show(
                    $"「{target.DisplayText}」の設定をコピーしたボタンで上書きしますか？",
                    "ボタン設定の上書き",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);
                allowOverwrite = overwriteResult == MessageBoxResult.Yes;
            }

            PasteCopiedButton(allowOverwrite);
        }

        internal bool PasteCopiedButton(bool allowOverwrite)
        {
            ActionButton target = ButtonGrid.SelectedItem;
            int targetIndex = DraftButtons.IndexOf(target);
            ActionButton copiedButton = EditorClipboard.GetButtonCopy();
            if (targetIndex < 0 || copiedButton == null)
            {
                return false;
            }

            if (target.ActionType != ActionType.None && !allowOverwrite)
            {
                return false;
            }

            DraftButtons[targetIndex] = copiedButton;
            ButtonGrid.SelectedItem = copiedButton;
            return true;
        }

        private void DuplicateButton_Click(object sender, RoutedEventArgs e)
        {
            if (!DuplicateSelectedButton())
            {
                MessageBox.Show(
                    ButtonGrid.SelectedItem == null
                        ? "複製するボタンを選択してください"
                        : "空きボタンがないため複製できません。先に不要なボタンをクリアしてください。",
                    "複製できません",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
        }

        internal bool DuplicateSelectedButton()
        {
            ActionButton source = ButtonGrid.SelectedItem;
            int sourceIndex = DraftButtons.IndexOf(source);
            if (sourceIndex < 0)
            {
                return false;
            }

            int emptyIndex = FindEmptyButtonIndex(sourceIndex + 1);
            if (emptyIndex < 0)
            {
                emptyIndex = FindEmptyButtonIndex(0, sourceIndex);
            }

            if (emptyIndex < 0)
            {
                return false;
            }

            ActionButton duplicate = source.Clone();
            if (!string.IsNullOrWhiteSpace(duplicate.DisplayText))
            {
                duplicate.DisplayText += " のコピー";
            }

            DraftButtons[emptyIndex] = duplicate;
            ButtonGrid.SelectedItem = duplicate;
            return true;
        }

        private int FindEmptyButtonIndex(int startIndex, int endIndex = int.MaxValue)
        {
            int lastIndex = Math.Min(endIndex, DraftButtons.Count);
            for (int index = Math.Max(0, startIndex); index < lastIndex; index++)
            {
                if (DraftButtons[index]?.ActionType == ActionType.None)
                {
                    return index;
                }
            }

            return -1;
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ClearSelectedButton())
            {
                MessageBox.Show("クリアするボタンを選択してください", "選択エラー", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        internal bool ClearSelectedButton()
        {
            int selectedIndex = DraftButtons.IndexOf(ButtonGrid.SelectedItem);
            if (selectedIndex < 0)
            {
                return false;
            }

            var emptyButton = new ActionButton
            {
                DisplayText = $"ボタン {selectedIndex + 1}",
                ActionType = ActionType.None
            };
            DraftButtons[selectedIndex] = emptyButton;
            ButtonGrid.SelectedItem = emptyButton;
            return true;
        }

        private void ReloadPageDrafts(string selectedPageId = null, int selectedButtonIndex = 0)
        {
            _isSwitchingDraftPage = true;
            try
            {
                DraftPages.Clear();
                foreach (PageSetting page in _viewModel?.Pages ?? new ObservableCollection<PageSetting>())
                {
                    PageSetting pageDraft = page?.Clone();
                    if (pageDraft == null)
                    {
                        continue;
                    }

                    if (string.Equals(pageDraft.Id, _viewModel.ActivePageId, StringComparison.OrdinalIgnoreCase))
                    {
                        pageDraft.Buttons = SettingsService.CreateButtonSettingsList(_viewModel.Buttons);
                    }

                    DraftPages.Add(pageDraft);
                }

                if (DraftPages.Count == 0)
                {
                    DraftPages.Add(new PageSetting
                    {
                        Id = Guid.NewGuid().ToString("N"),
                        Name = "メイン",
                        Buttons = CreateEmptyDraftButtonSettings()
                    });
                }

                PageSetting selectedPage = DraftPages.FirstOrDefault(page => string.Equals(
                    page.Id,
                    selectedPageId,
                    StringComparison.OrdinalIgnoreCase)) ?? DraftPages[0];
                PageSelector.SelectedItem = selectedPage;
                _selectedDraftPage = selectedPage;
                LoadButtonDraftFromPage(selectedPage, selectedButtonIndex);
            }
            finally
            {
                _isSwitchingDraftPage = false;
            }

            UpdateDraftPageDisplay();
            UpdateLayoutChangeHint();
        }

        private void SelectDraftPage(PageSetting page, int selectedButtonIndex = 0)
        {
            if (page == null)
            {
                return;
            }

            _isSwitchingDraftPage = true;
            try
            {
                PageSelector.SelectedItem = page;
                _selectedDraftPage = page;
                LoadButtonDraftFromPage(page, selectedButtonIndex);
            }
            finally
            {
                _isSwitchingDraftPage = false;
            }

            UpdateDraftPageDisplay();
        }

        private void LoadButtonDraftFromPage(PageSetting page, int selectedIndex = 0)
        {
            DraftButtons.Clear();
            foreach (ButtonSetting button in page?.Buttons ?? new List<ButtonSetting>())
            {
                DraftButtons.Add(button?.ToActionButton() ?? new ActionButton());
            }

            if (ButtonGrid != null)
            {
                ButtonGrid.SelectedItem = DraftButtons.Count == 0
                    ? null
                    : DraftButtons[Math.Max(0, Math.Min(selectedIndex, DraftButtons.Count - 1))];
            }
        }

        private void CommitSelectedPageDraft()
        {
            if (_selectedDraftPage != null)
            {
                _selectedDraftPage.Buttons = SettingsService.CreateButtonSettingsList(DraftButtons);
            }
        }

        private void UpdateDraftPageDisplay()
        {
            if (PageNameTextBox != null)
            {
                PageNameTextBox.Text = _selectedDraftPage?.Name ?? "";
            }

            if (DraftPagePositionText != null)
            {
                int index = _selectedDraftPage == null ? -1 : DraftPages.IndexOf(_selectedDraftPage);
                DraftPagePositionText.Text = index < 0 ? "" : $"{index + 1} / {DraftPages.Count}";
            }
        }
        
        private void OK_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!TryApplyPendingSettings())
                {
                    return;
                }

                // 背景設定をメインウィンドウに適用
                ApplyBackgroundSettingsToMainWindow();
                
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"OK_Click中にエラーが発生しました: {ex.Message}");
                MessageBox.Show($"設定の保存中にエラーが発生しました: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            // 未適用のスライダー、一般設定、ボタンの編集用コピーを破棄する
            DialogResult = false;
            Close();
        }
        
        private void Apply_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!TryApplyPendingSettings())
                {
                    return;
                }

                // 背景設定をメインウィンドウに適用
                ApplyBackgroundSettingsToMainWindow();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Apply_Click中にエラーが発生しました: {ex.Message}");
                MessageBox.Show($"設定の適用中にエラーが発生しました: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private void BackgroundType_Changed(object sender, RoutedEventArgs e)
        {
            // ViewModelが初期化されているかチェック
            if (_viewModel == null)
                return;
                
            // ViewModelの状態を更新
            _viewModel.UseBackgroundImage = ImageRadioButton?.IsChecked == true;
            
            // プレビューを更新
            UpdateBackgroundPreview();
        }
        
        private void UpdateBackgroundPreview()
        {
            // XAMLコントロールが初期化されているかチェック
            if (ColorPreviewRect == null || ImagePreviewImg == null)
                return;
                
            if (ColorRadioButton?.IsChecked == true)
            {
                // 背景色を表示
                ColorPreviewRect.Visibility = Visibility.Visible;
                ImagePreviewImg.Visibility = Visibility.Collapsed;
                
                // 透明度を適用
                ColorPreviewRect.Opacity = GetPendingBackgroundOpacity();
            }
            else if (ImageRadioButton?.IsChecked == true)
            {
                // 背景画像を表示
                ColorPreviewRect.Visibility = Visibility.Collapsed;
                ImagePreviewImg.Visibility = Visibility.Visible;
                
                // 画像を読み込んでプレビューに設定
                UpdateImagePreview(_viewModel.BackgroundImagePath);
                
                // 透明度を適用
                ImagePreviewImg.Opacity = GetPendingBackgroundOpacity();
            }
        }

        private void LayoutSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            UpdateLayoutChangeHint();
        }

        private void LayoutOpacitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_isSynchronizingOpacitySliders)
            {
                return;
            }

            _isSynchronizingOpacitySliders = true;
            try
            {
                if (BackgroundOpacitySlider != null)
                {
                    BackgroundOpacitySlider.Value = e.NewValue;
                }
            }
            finally
            {
                _isSynchronizingOpacitySliders = false;
            }

            UpdateBackgroundPreview();
        }
        
        private void BackgroundOpacitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_isSynchronizingOpacitySliders)
            {
                return;
            }

            _isSynchronizingOpacitySliders = true;
            try
            {
                if (OpacitySlider != null)
                {
                    OpacitySlider.Value = e.NewValue;
                }
            }
            finally
            {
                _isSynchronizingOpacitySliders = false;
            }

            UpdateBackgroundPreview();
        }

        private double GetPendingBackgroundOpacity()
        {
            return BackgroundOpacitySlider?.Value ?? _viewModel?.BackgroundOpacity ?? 0.0;
        }

        private void UpdateLayoutChangeHint()
        {
            if (LayoutChangeHint == null || RowsSlider == null || ColumnsSlider == null || _viewModel == null)
            {
                return;
            }

            int rows = (int)Math.Round(RowsSlider.Value);
            int columns = (int)Math.Round(ColumnsSlider.Value);
            int newButtonCount = rows * columns;
            int currentButtonCount = DraftPages
                .Select(page => page?.Buttons?.Count ?? 0)
                .DefaultIfEmpty(DraftButtons.Count)
                .Max();

            if (newButtonCount < currentButtonCount)
            {
                int removedButtonCount = currentButtonCount - newButtonCount;
                LayoutChangeHint.Text = $"各ページ {newButtonCount} ボタン。適用すると各ページ末尾の最大 {removedButtonCount} ボタン設定が削除されます。";
            }
            else if (newButtonCount > currentButtonCount)
            {
                int addedButtonCount = newButtonCount - currentButtonCount;
                LayoutChangeHint.Text = $"各ページ {newButtonCount} ボタン。適用すると各ページへ空のボタンが {addedButtonCount} 個追加されます。";
            }
            else
            {
                LayoutChangeHint.Text = $"各ページ {newButtonCount} ボタン。現在のボタン数と同じです。";
            }
        }

        private bool TryApplyPendingSettings()
        {
            if (_viewModel == null)
            {
                return false;
            }

            int rows = (int)Math.Round(RowsSlider.Value);
            int columns = (int)Math.Round(ColumnsSlider.Value);
            int newButtonCount = rows * columns;
            CommitSelectedPageDraft();
            int currentButtonCount = DraftPages
                .Select(page => page?.Buttons?.Count ?? 0)
                .DefaultIfEmpty(0)
                .Max();

            if (ImageRadioButton?.IsChecked == true)
            {
                string pendingImagePath = BackgroundImagePath.Text?.Trim() ?? "";
                if (!ImageStorageService.TryResolveImagePath(pendingImagePath, out string _))
                {
                    MessageBox.Show(
                        $"背景画像が見つかりません。画像を選択し直すか、背景色へ切り替えてください。\n\n保存されているパス:\n{pendingImagePath}",
                        "背景画像を確認してください",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return false;
                }
            }

            if (newButtonCount < currentButtonCount)
            {
                int removedButtonCount = currentButtonCount - newButtonCount;
                MessageBoxResult result = MessageBox.Show(
                    $"各ページで末尾の最大 {removedButtonCount} ボタン設定が削除されます。続行しますか？",
                    "ボタン数の確認",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result != MessageBoxResult.Yes)
                {
                    return false;
                }
            }

            string selectedPageId = _selectedDraftPage?.Id;
            int selectedIndex = DraftButtons.IndexOf(ButtonGrid.SelectedItem);
            _viewModel.ApplySettings(
                rows,
                columns,
                OpacitySlider.Value,
                ButtonOpacitySlider.Value,
                AutoStartCheckBox.IsChecked == true,
                AlwaysOnTopCheckBox.IsChecked == true,
                null,
                DraftPages,
                selectedPageId);

            if (Owner is MainWindow mainWindow)
            {
                mainWindow.ApplyWindowSettingsFromViewModel();
            }

            ReloadPageDrafts(selectedPageId, selectedIndex);
            _lastAppliedBackgroundImagePath = _viewModel.BackgroundImagePath;
            UpdateLayoutChangeHint();
            return true;
        }

        // 画像プレビュー更新メソッド
        private void UpdateImagePreview(string imagePath)
        {
            try
            {
                if (string.IsNullOrEmpty(imagePath))
                {
                    ImagePreviewImg.Source = null;
                    return;
                }

                if (ImageStorageService.TryResolveImagePath(imagePath, out string fullPath))
                {
                    var bitmap = new System.Windows.Media.Imaging.BitmapImage();
                    bitmap.BeginInit();
                    bitmap.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                    bitmap.UriSource = new Uri(fullPath);
                    bitmap.EndInit();
                    ImagePreviewImg.Source = bitmap;
                }
                else
                {
                    ImagePreviewImg.Source = null;
                }
            }
            catch
            {
                ImagePreviewImg.Source = null;
            }
        }
        
        private void SelectColor_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // XAMLコントロールが初期化されているかチェック
                if (ColorPreview == null || ColorRadioButton == null)
                    return;
                    
                // カラーピッカーダイアログを表示
                Color currentColor = (Color)ColorConverter.ConvertFromString(_viewModel.BackgroundColor);
                var colorPicker = new ColorPickerDialog(currentColor);
                colorPicker.Owner = this;
                colorPicker.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                
                bool? result = colorPicker.ShowDialog();
                
                if (result == true)
                {
                    // 選択した色を設定
                    Color selectedColor = colorPicker.SelectedColor;
                    _viewModel.BackgroundColor = selectedColor.ToString();
                    ColorPreview.Background = new SolidColorBrush(selectedColor);
                    
                    // 背景色ラジオボタンを選択してプレビューを更新
                    ColorRadioButton.IsChecked = true;
                    UpdateBackgroundPreview();
                }
                
                // ダイアログが確実に閉じられるように
                colorPicker = null;
                
                // フォーカスを親ウィンドウに戻す
                this.Focus();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"色選択中にエラーが発生しました: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
          
        private void SelectBackgroundImage_Click(object sender, RoutedEventArgs e)
        {
            // 画像ファイル選択ダイアログを表示
            var dialog = new OpenFileDialog
            {
                Filter = "画像ファイル (*.jpg;*.jpeg;*.png;*.bmp)|*.jpg;*.jpeg;*.png;*.bmp|すべてのファイル (*.*)|*.*",
                Title = "背景画像を選択"
            };
            
            if (dialog.ShowDialog() == true)
            {
                try
                {
                    // ビルド出力ではなく、再ビルド後も残るユーザーデータ領域へ保存する
                    _viewModel.BackgroundImagePath = ImageStorageService.ImportImage(
                        dialog.FileName,
                        ImageStorageKind.Background);
                    BackgroundImagePath.Text = _viewModel.BackgroundImagePath;
                      
                    // 背景画像ラジオボタンを選択してプレビューを更新
                    ImageRadioButton.IsChecked = true;
                    UpdateBackgroundPreview();
                }
                catch (System.Exception ex)
                {
                    MessageBox.Show($"画像の追加中にエラーが発生しました: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
          
        private void ClearBackgroundImage_Click(object sender, RoutedEventArgs e)
        {
            // 背景画像をクリア
            _viewModel.BackgroundImagePath = "";
            BackgroundImagePath.Text = "";
            
            // プレビューも更新
            UpdateBackgroundPreview();
        }
          
        private void ApplyBackgroundSettingsToMainWindow()
        {
            var mainWindow = Owner as MainWindow;
            if (mainWindow == null) return;
            
            try
            {
                // 明示的に透明度を更新
                if (_viewModel != null)
                {
                    double opacity = _viewModel.BackgroundOpacity;
                    Console.WriteLine($"メインウィンドウに適用する透明度: {opacity:F2}");
                }
                
                if (ColorRadioButton?.IsChecked == true)
                {                    
                    // 背景色を適用
                    Color bgColor = (Color)ColorConverter.ConvertFromString(_viewModel.BackgroundColor);
                    SolidColorBrush brush = new SolidColorBrush(bgColor);
                    brush.Opacity = _viewModel.BackgroundOpacity;
                    mainWindow.Background = brush;
                    
                    Console.WriteLine($"背景色を適用しました。色: {_viewModel.BackgroundColor}, 透明度: {_viewModel.BackgroundOpacity:F2}");
                }
                else if (ImageRadioButton?.IsChecked == true && !string.IsNullOrEmpty(_viewModel.BackgroundImagePath))
                {
                    // 背景画像を適用
                    try
                    {
                        if (ImageStorageService.TryResolveImagePath(
                            _viewModel.BackgroundImagePath,
                            out string fullPath))
                        {                            
                            ImageBrush imageBrush = new ImageBrush
                            {
                                ImageSource = new System.Windows.Media.Imaging.BitmapImage(new Uri(fullPath)),
                                Opacity = _viewModel.BackgroundOpacity,
                                Stretch = Stretch.UniformToFill
                            };
                            
                            mainWindow.Background = imageBrush;
                            Console.WriteLine($"背景画像を適用しました。パス: {fullPath}, 透明度: {_viewModel.BackgroundOpacity:F2}");
                        }
                        else
                        {
                            // TryApplyPendingSettingsで検証済み。適用直前に削除された場合だけ記録する。
                            Console.WriteLine($"背景画像が適用直前に見つからなくなりました: {_viewModel.BackgroundImagePath}");
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"背景画像の適用に失敗しました: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                else
                {                    
                    // デフォルトの背景色を適用
                    Color bgColor = (Color)ColorConverter.ConvertFromString(_viewModel.BackgroundColor);
                    SolidColorBrush brush = new SolidColorBrush(bgColor);
                    brush.Opacity = _viewModel.BackgroundOpacity;
                    mainWindow.Background = brush;
                    Console.WriteLine($"デフォルト背景色を適用しました。色: {_viewModel.BackgroundColor}, 透明度: {_viewModel.BackgroundOpacity:F2}");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"背景設定の適用に失敗しました: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
