using MiniDeck.Models;
using MiniDeck.Services;
using System;
using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace MiniDeck.Controls
{
    public delegate void ActionButtonEventHandler(object sender, ActionButton button);

    public sealed class ButtonReorderRequestedEventArgs : EventArgs
    {
        public ButtonReorderRequestedEventArgs(ActionButton source, ActionButton target)
        {
            Source = source;
            Target = target;
        }

        public ActionButton Source { get; }
        public ActionButton Target { get; }
    }

    public sealed class ExternalButtonDropRequestedEventArgs : EventArgs
    {
        public ExternalButtonDropRequestedEventArgs(ActionButton target, IDataObject data)
        {
            Target = target;
            Data = data;
        }

        public ActionButton Target { get; }
        public IDataObject Data { get; }
    }

    public partial class ButtonGridControl : UserControl
    {
        private Point _dragStartPoint;
        private ActionButton _dragSource;

        public ButtonGridControl()
        {
            InitializeComponent();
        }

        public event ActionButtonEventHandler ItemActivated;
        public event EventHandler<ButtonReorderRequestedEventArgs> ReorderRequested;
        public event EventHandler<ExternalButtonDropRequestedEventArgs> ExternalDropRequested;

        public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.Register(
            nameof(ItemsSource),
            typeof(IEnumerable),
            typeof(ButtonGridControl),
            new PropertyMetadata(null));

        public static readonly DependencyProperty RowsProperty = DependencyProperty.Register(
            nameof(Rows),
            typeof(int),
            typeof(ButtonGridControl),
            new PropertyMetadata(1));

        public static readonly DependencyProperty ColumnsProperty = DependencyProperty.Register(
            nameof(Columns),
            typeof(int),
            typeof(ButtonGridControl),
            new PropertyMetadata(1));

        public static readonly DependencyProperty IsEditModeProperty = DependencyProperty.Register(
            nameof(IsEditMode),
            typeof(bool),
            typeof(ButtonGridControl),
            new PropertyMetadata(false));

        public static readonly DependencyProperty SelectedItemProperty = DependencyProperty.Register(
            nameof(SelectedItem),
            typeof(ActionButton),
            typeof(ButtonGridControl),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public static readonly DependencyProperty ButtonSizeProperty = DependencyProperty.Register(
            nameof(ButtonSize),
            typeof(double),
            typeof(ButtonGridControl),
            new PropertyMetadata(80.0));

        public static readonly DependencyProperty ButtonOpacityProperty = DependencyProperty.Register(
            nameof(ButtonOpacity),
            typeof(double),
            typeof(ButtonGridControl),
            new PropertyMetadata(1.0));

        public static readonly DependencyProperty ButtonForegroundProperty = DependencyProperty.Register(
            nameof(ButtonForeground),
            typeof(Brush),
            typeof(ButtonGridControl),
            new PropertyMetadata(Brushes.Black));

        public static readonly DependencyProperty ButtonBorderBrushProperty = DependencyProperty.Register(
            nameof(ButtonBorderBrush),
            typeof(Brush),
            typeof(ButtonGridControl),
            new PropertyMetadata(Brushes.Black));

        public IEnumerable ItemsSource
        {
            get => (IEnumerable)GetValue(ItemsSourceProperty);
            set => SetValue(ItemsSourceProperty, value);
        }

        public int Rows
        {
            get => (int)GetValue(RowsProperty);
            set => SetValue(RowsProperty, value);
        }

        public int Columns
        {
            get => (int)GetValue(ColumnsProperty);
            set => SetValue(ColumnsProperty, value);
        }

        public bool IsEditMode
        {
            get => (bool)GetValue(IsEditModeProperty);
            set => SetValue(IsEditModeProperty, value);
        }

        public ActionButton SelectedItem
        {
            get => (ActionButton)GetValue(SelectedItemProperty);
            set => SetValue(SelectedItemProperty, value);
        }

        public double ButtonSize
        {
            get => (double)GetValue(ButtonSizeProperty);
            set => SetValue(ButtonSizeProperty, value);
        }

        public double ButtonOpacity
        {
            get => (double)GetValue(ButtonOpacityProperty);
            set => SetValue(ButtonOpacityProperty, value);
        }

        public Brush ButtonForeground
        {
            get => (Brush)GetValue(ButtonForegroundProperty);
            set => SetValue(ButtonForegroundProperty, value);
        }

        public Brush ButtonBorderBrush
        {
            get => (Brush)GetValue(ButtonBorderBrushProperty);
            set => SetValue(ButtonBorderBrushProperty, value);
        }

        private void DeckButton_Click(object sender, RoutedEventArgs e)
        {
            var actionButton = (sender as FrameworkElement)?.DataContext as ActionButton;
            if (actionButton == null)
            {
                return;
            }

            if (IsEditMode)
            {
                SelectedItem = actionButton;
                return;
            }

            if (actionButton.ClickCommand?.CanExecute(actionButton) == true)
            {
                actionButton.ClickCommand.Execute(actionButton);
            }
        }

        private void DeckButton_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (!IsEditMode)
            {
                return;
            }

            var actionButton = (sender as FrameworkElement)?.DataContext as ActionButton;
            if (actionButton == null)
            {
                return;
            }

            SelectedItem = actionButton;
            ItemActivated?.Invoke(this, actionButton);
            e.Handled = true;
        }

        private void DeckButton_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!IsEditMode)
            {
                return;
            }

            _dragStartPoint = e.GetPosition(this);
            _dragSource = (sender as FrameworkElement)?.DataContext as ActionButton;
            SelectedItem = _dragSource;
        }

        private void DeckButton_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (!IsEditMode || _dragSource == null || e.LeftButton != MouseButtonState.Pressed)
            {
                return;
            }

            Point currentPoint = e.GetPosition(this);
            if (Math.Abs(currentPoint.X - _dragStartPoint.X) < SystemParameters.MinimumHorizontalDragDistance &&
                Math.Abs(currentPoint.Y - _dragStartPoint.Y) < SystemParameters.MinimumVerticalDragDistance)
            {
                return;
            }

            ActionButton source = _dragSource;
            _dragSource = null;
            DragDrop.DoDragDrop(this, new DataObject(typeof(ActionButton), source), DragDropEffects.Move);
        }

        private void DeckButton_PreviewDragOver(object sender, DragEventArgs e)
        {
            if (!IsEditMode)
            {
                e.Effects = DragDropEffects.None;
                e.Handled = true;
                return;
            }

            if (e.Data.GetDataPresent(typeof(ActionButton)))
            {
                e.Effects = DragDropEffects.Move;
            }
            else if (ButtonDropService.CanAcceptData(e.Data))
            {
                e.Effects = DragDropEffects.Copy;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }

            e.Handled = true;
        }

        private void DeckButton_Drop(object sender, DragEventArgs e)
        {
            if (!IsEditMode)
            {
                return;
            }

            var target = (sender as FrameworkElement)?.DataContext as ActionButton;
            if (target == null)
            {
                return;
            }

            if (e.Data.GetDataPresent(typeof(ActionButton)))
            {
                var source = e.Data.GetData(typeof(ActionButton)) as ActionButton;
                if (source != null && !ReferenceEquals(source, target))
                {
                    ReorderRequested?.Invoke(this, new ButtonReorderRequestedEventArgs(source, target));
                }
            }
            else if (ButtonDropService.CanAcceptData(e.Data))
            {
                SelectedItem = target;
                ExternalDropRequested?.Invoke(
                    this,
                    new ExternalButtonDropRequestedEventArgs(target, e.Data));
            }

            e.Handled = true;
        }
    }
}
