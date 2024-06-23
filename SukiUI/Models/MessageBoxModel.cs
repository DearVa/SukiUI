using SukiUI.Enums;

namespace SukiUI.Models
{
    public record MessageBoxModel
    {
        public MessageBoxModel(
            string Title,
            object Content)
        {
            this.Title = Title;
            this.Content = Content;
        }
        
        public MessageBoxModel(
            string Title,
            object Content,
            NotificationType Type = NotificationType.Info,
            object? PrimaryButtonContent = null,
            object? SecondaryButtonContent = null,
            object? CancelButtonContent = null)
        {
            this.Title = Title;
            this.Content = Content;
            this.Type = Type;
            this.PrimaryButtonContent = PrimaryButtonContent;
            this.SecondaryButtonContent = SecondaryButtonContent;
            this.CancelButtonContent = CancelButtonContent;
        }

        public string Title { get; init; }
        public object Content { get; init; }
        public NotificationType Type { get; init; }
        public object? PrimaryButtonContent { get; init; }
        public object? SecondaryButtonContent { get; init; }
        public object? CancelButtonContent { get; init; }
    }
}