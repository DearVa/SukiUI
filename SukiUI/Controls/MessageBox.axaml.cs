using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;

namespace SukiUI.Controls
{
    public partial class MessageBox : UserControl
    {
        public MessageBox()
        {
            InitializeComponent();
        }
        
        public static readonly StyledProperty<object?> IconProperty =
            AvaloniaProperty.Register<MessageBox, object?>(nameof(Icon));

        public object? Icon
        {
            get => GetValue(IconProperty);
            set => SetValue(IconProperty, value);
        }
    
        public static readonly StyledProperty<string> TitleProperty =
            AvaloniaProperty.Register<MessageBox, string>(nameof(Title));

        public string Title
        {
            get => GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }
    
        public static readonly StyledProperty<object?> PrimaryButtonContentProperty =
            AvaloniaProperty.Register<MessageBox, object?>(nameof(PrimaryButtonContent));

        public object? PrimaryButtonContent
        {
            get => GetValue(PrimaryButtonContentProperty);
            set => SetValue(PrimaryButtonContentProperty, value);
        }
        
        public static readonly StyledProperty<object?> SecondaryButtonContentProperty =
            AvaloniaProperty.Register<MessageBox, object?>(nameof(SecondaryButtonContent));
        
        public object? SecondaryButtonContent
        {
            get => GetValue(SecondaryButtonContentProperty);
            set => SetValue(SecondaryButtonContentProperty, value);
        }
        
        public static readonly StyledProperty<object?> CancelButtonContentProperty =
            AvaloniaProperty.Register<MessageBox, object?>(nameof(CancelButtonContent));
        
        public object? CancelButtonContent
        {
            get => GetValue(CancelButtonContentProperty);
            set => SetValue(CancelButtonContentProperty, value);
        }

        private void PrimaryButton_OnClick(object sender, RoutedEventArgs e)
        {
            tcs.TrySetResult(MessageBoxResult.Primary);
        }

        private void SecondaryButton_OnClick(object sender, RoutedEventArgs e)
        {
            tcs.TrySetResult(MessageBoxResult.Secondary);
        }

        private void CancelButton_OnClick(object sender, RoutedEventArgs e)
        {
            tcs.TrySetResult(MessageBoxResult.Cancel);
        }

        protected override void OnDetachedFromLogicalTree(LogicalTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromLogicalTree(e);
            tcs.TrySetResult(MessageBoxResult.Cancel);
        }

        private readonly TaskCompletionSource<MessageBoxResult> tcs = new();
        public Task<MessageBoxResult> ResultTask => tcs.Task;
    }
}

public enum MessageBoxResult
{
    Primary,
    Secondary,
    Cancel
}