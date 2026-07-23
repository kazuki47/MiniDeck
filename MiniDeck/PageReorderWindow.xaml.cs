using MiniDeck.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace MiniDeck
{
    public partial class PageReorderWindow : Window
    {
        private Point _dragStartPoint;
        private PageSetting _dragSource;

        public ObservableCollection<PageSetting> PageOrder { get; }
        public IReadOnlyList<PageSetting> OrderedPages { get; private set; }

        public PageReorderWindow(IEnumerable<PageSetting> pages, string selectedPageId = null)
        {
            PageOrder = new ObservableCollection<PageSetting>(
                (pages ?? Enumerable.Empty<PageSetting>()).Where(page => page != null));

            InitializeComponent();
            DataContext = this;

            PageList.SelectedItem = PageOrder.FirstOrDefault(page => string.Equals(
                page.Id,
                selectedPageId,
                StringComparison.OrdinalIgnoreCase)) ?? PageOrder.FirstOrDefault();
        }

        internal bool MovePage(PageSetting source, PageSetting target)
        {
            int sourceIndex = PageOrder.IndexOf(source);
            int targetIndex = PageOrder.IndexOf(target);
            if (sourceIndex < 0 || targetIndex < 0 || sourceIndex == targetIndex)
            {
                return false;
            }

            PageOrder.Move(sourceIndex, targetIndex);
            PageList.SelectedItem = source;
            return true;
        }

        private void PageItem_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _dragStartPoint = e.GetPosition(PageList);
            _dragSource = (sender as FrameworkElement)?.DataContext as PageSetting;
            PageList.SelectedItem = _dragSource;
        }

        private void PageItem_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (_dragSource == null || e.LeftButton != MouseButtonState.Pressed)
            {
                return;
            }

            Point currentPoint = e.GetPosition(PageList);
            if (Math.Abs(currentPoint.X - _dragStartPoint.X) < SystemParameters.MinimumHorizontalDragDistance &&
                Math.Abs(currentPoint.Y - _dragStartPoint.Y) < SystemParameters.MinimumVerticalDragDistance)
            {
                return;
            }

            PageSetting source = _dragSource;
            _dragSource = null;
            DragDrop.DoDragDrop(PageList, new DataObject(typeof(PageSetting), source), DragDropEffects.Move);
        }

        private void PageItem_PreviewDragOver(object sender, DragEventArgs e)
        {
            e.Effects = e.Data.GetDataPresent(typeof(PageSetting))
                ? DragDropEffects.Move
                : DragDropEffects.None;
            e.Handled = true;
        }

        private void PageItem_Drop(object sender, DragEventArgs e)
        {
            PageSetting source = e.Data.GetData(typeof(PageSetting)) as PageSetting;
            PageSetting target = (sender as FrameworkElement)?.DataContext as PageSetting;
            MovePage(source, target);
            e.Handled = true;
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            OrderedPages = PageOrder.ToList();
            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
