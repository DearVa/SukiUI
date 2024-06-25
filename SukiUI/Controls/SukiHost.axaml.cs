using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Layout;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.Rendering.Composition;
using Avalonia.Threading;
using SukiUI.Content;
using SukiUI.Enums;
using SukiUI.Helpers;
using SukiUI.Models;

namespace SukiUI.Controls
{
    /// <summary>
    ///     Hosts both Dialogs and Notifications
    /// </summary>
    public class SukiHost : ContentControl
    {
        protected override Type StyleKeyOverride => typeof(SukiHost);

        public static readonly DirectProperty<SukiHost, bool> IsDialogOpenProperty =
            AvaloniaProperty.RegisterDirect<SukiHost, bool>(
                "IsDialogOpen",
                o => o.IsDialogOpen,
                (o, v) => o.IsDialogOpen = v);
        
        public bool IsDialogOpen
        {
            get => isDialogOpen;
            private set => SetAndRaise(IsDialogOpenProperty, ref isDialogOpen, value);
        }
        
        private bool isDialogOpen;

        public readonly static StyledProperty<Control?> DialogContentProperty =
            AvaloniaProperty.Register<SukiHost, Control?>(
                nameof(DialogContent),
                coerce: (o, c) =>
                {
                    o.SetValue(IsDialogOpenProperty, c != null);
                    return c;
                });

        public Control? DialogContent
        {
            get => GetValue(DialogContentProperty);
            set => SetValue(DialogContentProperty, value);
        }

        public readonly static StyledProperty<bool> AllowBackgroundCloseProperty =
            AvaloniaProperty.Register<SukiHost, bool>(nameof(AllowBackgroundClose), true);

        public bool AllowBackgroundClose
        {
            get => GetValue(AllowBackgroundCloseProperty);
            set => SetValue(AllowBackgroundCloseProperty, value);
        }

        public readonly static AttachedProperty<ToastLocation> ToastLocationProperty =
            AvaloniaProperty.RegisterAttached<SukiHost, TopLevel, ToastLocation>("ToastLocation");

        public static void SetToastLocation(Control element, ToastLocation value)
        {
            element.SetValue(ToastLocationProperty, value);
        }

        public static ToastLocation GetToastLocation(Control element)
        {
            return element.GetValue(ToastLocationProperty);
        }

        public readonly static AttachedProperty<int> ToastLimitProperty =
            AvaloniaProperty.RegisterAttached<SukiHost, TopLevel, int>("ToastLimit", 5);

        public static int GetToastLimit(Control element)
        {
            return element.GetValue(ToastLimitProperty);
        }

        public static void SetToastLimit(Control element, int value)
        {
            element.SetValue(ToastLimitProperty, value);
        }

        public readonly static StyledProperty<AvaloniaList<SukiToast>?> ToastsCollectionProperty =
            AvaloniaProperty.Register<SukiHost, AvaloniaList<SukiToast>?>(nameof(ToastsCollection));

        public AvaloniaList<SukiToast> ToastsCollection
        {
            get => GetValue(ToastsCollectionProperty) ?? new AvaloniaList<SukiToast>();
            set => SetValue(ToastsCollectionProperty, value);
        }

        private static TopLevel MainToplevel => mainToplevel ?? throw new InvalidOperationException("No SukiHost has been attached to visual tree.");
        private static TopLevel? mainToplevel;
        private readonly static Dictionary<TopLevel, SukiHost> Instances = new();

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);
            if (VisualRoot is not TopLevel w)
            {
                return;
            }
            Instances.Add(w, this);
            mainToplevel ??= w;
        }

        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);
            if (VisualRoot is not TopLevel topLevel)
            {
                throw new InvalidOperationException("SukiHost must be hosted inside a TopLevel");
            }
            var toastLocation = GetToastLocation(topLevel);

            e.NameScope.Get<Border>("PART_DialogBackground").PointerPressed += (_, _) => BackgroundRequestClose(this);

            e.NameScope.Get<ItemsControl>("PART_ToastPresenter").HorizontalAlignment =
                toastLocation == ToastLocation.BottomLeft
                    ? HorizontalAlignment.Left
                    : HorizontalAlignment.Right;

            var b = e.NameScope.Get<Border>("PART_DialogBackground");
            b.Loaded += (_, _) =>
            {
                var v = ElementComposition.GetElementVisual(b);
                CompositionAnimationHelper.MakeOpacityAnimated(v, 400);
            };
        }


        // TODO: Dialog API desperately needs to support a result or on-close callback.
        // TODO: Toasts and dialogs should be dragged out into their own discrete service and provided by a higher level service locator.
        // TODO: Currently not possible to switch the toast side at runtime, in reality there should be multiple anchors and toasts can be displayed on them arbitrarily.
        // Giving devs direct access to this object like this is messy and there really needs to be a standard abstraction above all these features.
        // This goes for other APIs like the background and theming.

        /// <summary>
        ///     Shows a dialog in the <see cref="SukiHost" />
        ///     Can display ViewModels if provided, if a suitable ViewLocator has been registered with Avalonia.
        /// </summary>
        /// <param name="topLevel">The topLevel who's SukiHost should be used to display the toast.</param>
        /// <param name="content">Content to display.</param>
        /// <param name="showCardBehind">Whether or not to show a card behind the content.</param>
        /// <param name="allowBackgroundClose">Allows the dialog to be closed by clicking outside of it.</param>
        /// <exception cref="InvalidOperationException">Thrown if there is no SukiHost associated with the specified topLevel.</exception>
        public static void ShowDialog(
            TopLevel topLevel,
            object? content,
            bool showCardBehind = true,
            bool allowBackgroundClose = false)
        {
            if (!Instances.TryGetValue(topLevel, out var host))
            {
                throw new InvalidOperationException("No SukiHost present in this topLevel");
            }
            var control = content as Control ?? ViewLocator.TryBuild(content);
            host.DialogContent = control;
            host.AllowBackgroundClose = allowBackgroundClose;
            host.GetTemplateChildren().First(n => n.Name == "BorderDialog1").Opacity = showCardBehind ? 1 : 0;
        }

        /// <summary>
        ///     <inheritdoc cref="ShowDialog(Avalonia.Controls.TopLevel,object?,bool,bool)" />
        /// </summary>
        /// <param name="content">Content to display.</param>
        /// <param name="showCardBehind">Whether or not to show a card behind the content.</param>
        /// <param name="allowBackgroundClose">Allows the dialog to be closed by clicking outside of it.</param>
        public static void ShowDialog(object? content, bool showCardBehind = true, bool allowBackgroundClose = false)
        {
            ShowDialog(MainToplevel, content, showCardBehind, allowBackgroundClose);
        }

        public static async Task<MessageBoxResult> ShowMessageBox(MessageBoxModel model, bool allowBackgroundClose = true)
        {
            var messageBox = new MessageBox
            {
                Title = model.Title, Content = model.Content,
                PrimaryButtonContent = model.PrimaryButtonContent,
                SecondaryButtonContent = model.SecondaryButtonContent,
                CancelButtonContent = model.CancelButtonContent,
                Icon = model.Type switch
                {
                    NotificationType.Info => Icons.InformationOutline,
                    NotificationType.Success => Icons.Check,
                    NotificationType.Warning => Icons.AlertOutline,
                    NotificationType.Error => Icons.AlertOutline,
                    _ => Icons.InformationOutline,
                },
                Foreground = model.Type switch
                {
                    NotificationType.Info => GetGradient(Color.FromRgb(47, 84, 235)),
                    NotificationType.Success => GetGradient(Color.FromRgb(82, 196, 26)),
                    NotificationType.Warning => GetGradient(Color.FromRgb(240, 140, 22)),
                    NotificationType.Error => GetGradient(Color.FromRgb(245, 34, 45)),
                    _ => GetGradient(Color.FromRgb(89, 126, 255)),
                },
            };
            ShowDialog(messageBox, false, allowBackgroundClose);
            var result = await messageBox.ResultTask;
            CloseDialog();
            return result;
        }

        private static LinearGradientBrush GetGradient(Color c1)
        {
            return new LinearGradientBrush
            {
                StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
                EndPoint = new RelativePoint(1, 1, RelativeUnit.Relative),
                GradientStops =
                {
                    new GradientStop
                        { Color = c1, Offset = 0 },
                    new GradientStop
                        { Color = Color.FromArgb(140, c1.R, c1.G, c1.B), Offset = 1 },
                },
            };
        }

        /// <summary>
        ///     Attempts to close a dialog if one is shown in a specific topLevel.
        /// </summary>
        public static void CloseDialog(TopLevel topLevel)
        {
            if (!Instances.TryGetValue(topLevel, out var host))
            {
                throw new InvalidOperationException("No SukiHost present in this topLevel");
            }

            host.DialogContent = null;
        }

        /// <summary>
        ///     Attempts to close a dialog if one is shown in the earliest of any opened windows.
        /// </summary>
        public static void CloseDialog()
        {
            CloseDialog(MainToplevel);
        }

        /// <summary>
        ///     Used to close the open dialog when the background is clicked, if this is allowed.
        /// </summary>
        private static void BackgroundRequestClose(SukiHost host)
        {
            if (!host.AllowBackgroundClose) return;
            host.DialogContent = null;
        }

        /// <summary>
        ///     Shows a toast in the SukiHost - The default location is in the bottom right.
        ///     This can be changed with an attached property in SukiWindow.
        /// </summary>
        /// <param name="topLevel">The topLevel who's SukiHost should be used to display the toast.</param>
        /// <param name="model">A pre-constructed <see cref="SukiToastModel" />.</param>
        /// <exception cref="InvalidOperationException">Thrown if there is no SukiHost associated with the specified topLevel.</exception>
        public async static Task ShowToast(TopLevel topLevel, ToastModel model)
        {
            if (!Instances.TryGetValue(topLevel, out var host))
            {
                throw new InvalidOperationException("No SukiHost present in this topLevel");
            }

            var toast = ToastPool.Get();
            toast.Initialize(model, host);
            if (host.ToastsCollection.Count >= GetToastLimit(topLevel))
            {
                await ClearToast(host.ToastsCollection.First());
            }
            Dispatcher.UIThread.Invoke(() =>
            {
                host.ToastsCollection.Add(toast);
                toast.Animate(OpacityProperty, 0d, 1d, TimeSpan.FromMilliseconds(500));
                toast.Animate(MarginProperty,
                    new Thickness(0, 10, 0, -10),
                    new Thickness(),
                    TimeSpan.FromMilliseconds(500));
            });
        }

        /// <summary>
        ///     <inheritdoc cref="ShowToast(TopLevel, SukiToastModel)" />
        ///     This method will show the toast in the earliest opened topLevel.
        /// </summary>
        /// <param name="model">A pre-constructed <see cref="SukiToastModel" />.</param>
        public static Task ShowToast(ToastModel model)
        {
            return ShowToast(MainToplevel, model);
        }

        /// <summary>
        ///     <inheritdoc cref="ShowToast(TopLevel, SukiToastModel)" />
        ///     This method will show the toast in the earliest opened topLevel.
        /// </summary>
        /// <param name="title">The title to display in the toast.</param>
        /// <param name="content">The content of the toast, this can be any control or ViewModel.</param>
        /// <param name="type">The type of the toast, including Info, Success, Warning and Error</param>
        /// <param name="duration">Duration for this toast to be active. Default is 2 seconds.</param>
        /// <param name="onClicked">A callback that will be fired if the Toast is cleared by clicking.</param>
        public static Task ShowToast(
            string title,
            object content,
            NotificationType? type = NotificationType.Info,
            TimeSpan? duration = null,
            Action? onClicked = null)
        {
            return ShowToast(new ToastModel(
                title,
                content as Control ?? ViewLocator.TryBuild(content),
                type ?? NotificationType.Info,
                duration ?? TimeSpan.FromSeconds(4),
                onClicked));
        }

        /// <summary>
        ///     <inheritdoc cref="ShowToast(TopLevel, SukiToastModel)" />
        ///     This method will show the toast in a specific topLevel.
        /// </summary>
        /// <param name="topLevel">The topLevel who's SukiHost should be used to display the toast.</param>
        /// <param name="title">The title to display in the toast.</param>
        /// <param name="content">The content of the toast, this can be any control or ViewModel.</param>
        /// <param name="type">The type of the toast, including Info, Success, Warning and Error</param>
        /// <param name="duration">Duration for this toast to be active. Default is 2 seconds.</param>
        /// <param name="onClicked">A callback that will be fired if the Toast is cleared by clicking.</param>
        public static Task ShowToast(
            TopLevel topLevel,
            string title,
            object content,
            NotificationType? type = NotificationType.Info,
            TimeSpan? duration = null,
            Action? onClicked = null)
        {
            return ShowToast(topLevel,
                new ToastModel(
                    title,
                    content as Control ?? ViewLocator.TryBuild(content),
                    type ?? NotificationType.Info,
                    duration ?? TimeSpan.FromSeconds(4),
                    onClicked));
        }

        /// <summary>
        ///     Clears a specific toast from display (if it is still currently being displayed).
        /// </summary>
        /// <param name="toast">The toast to clear.</param>
        public async static Task ClearToast(SukiToast toast)
        {
            var wasRemoved = await Task.Run(async () =>
            {
                Dispatcher.UIThread.Invoke(() =>
                {
                    toast.Animate(OpacityProperty, 1d, 0d, TimeSpan.FromMilliseconds(300));
                    toast.Animate(MarginProperty,
                        new Thickness(),
                        new Thickness(0, 50, 0, -50),
                        TimeSpan.FromMilliseconds(300));
                });
                await Task.Delay(300);
                return Dispatcher.UIThread.Invoke(() => toast.Host.ToastsCollection.Remove(toast));
            });

            if (!wasRemoved)
            {
                return;
            }
            ToastPool.Return(toast);
        }

        /// <summary>
        ///     Clears all active toasts in a specific topLevel immediately.
        /// </summary>
        public static void ClearAllToasts(TopLevel topLevel)
        {
            if (!Instances.TryGetValue(topLevel, out var host))
            {
                throw new InvalidOperationException("No SukiHost present in this topLevel");
            }
            ToastPool.Return(host.ToastsCollection);
            Dispatcher.UIThread.Invoke(() => host.ToastsCollection.Clear());
        }

        /// <summary>
        ///     Clears all active toasts in the earliest open TopLevel immediately.
        /// </summary>
        public static void ClearAllToasts()
        {
            ClearAllToasts(MainToplevel);
        }

        protected override void OnDetachedFromLogicalTree(LogicalTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromLogicalTree(e);
            if (VisualRoot is not TopLevel topLevel)
            {
                return;
            }
            Instances.Remove(topLevel);
            mainToplevel = Instances.FirstOrDefault().Key;
        }
    }
}