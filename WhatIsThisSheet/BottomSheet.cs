using System.Security.Cryptography.X509Certificates;
using Microsoft.Maui.Controls.PlatformConfiguration;
using Microsoft.Maui.Controls.PlatformConfiguration.iOSSpecific;
using Microsoft.Maui.Controls.Shapes;
using Page = Microsoft.Maui.Controls.Page;

namespace WhatIsThisSheet;

[ContentProperty(nameof(SheetContent))]
public class BottomSheet : Grid
{
    private bool _hasInitialHeight;

    public static BindableProperty LockPositionProperty =
        BindableProperty.Create(nameof(LockPosition), typeof(bool), typeof(BottomSheet), default(bool));

    public static BindableProperty SheetStopsProperty =
        BindableProperty.Create(
            nameof(SheetStops),
            typeof(List<SheetStop>),
            typeof(BottomSheet),
            new List<SheetStop>());

    public static BindableProperty SheetContentProperty =
        BindableProperty.Create(
            nameof(SheetContent),
            typeof(View),
            typeof(BottomSheet),
            propertyChanged:
            (bindable, oldValue, newValue) =>
            {
                if (bindable is not BottomSheet sheet)
                {
                    return;
                }

                if (oldValue is View oldView)
                {
                    sheet._contentContainer.Remove(oldView);
                }

                if (newValue is View newView)
                {
                    SetColumn(newView, 0);
                    SetRow(newView, 1);
                    sheet._contentContainer.Add(newView);
                }
            });

    public static BindableProperty AllowFullDismissProperty =
        BindableProperty.Create(nameof(AllowFullDismiss), typeof(bool), typeof(BottomSheet), false);

    public static BindableProperty AllowBackgroundInteractionProperty =
        BindableProperty.Create(
            nameof(AllowBackgroundInteraction),
            typeof(bool),
            typeof(BottomSheet),
            true,
            propertyChanged: (bindable, _, newValue) =>
            {
                if (bindable is not BottomSheet bs || newValue is not bool newBool)
                {
                    return;
                }

                bs.GestureRecognizers.Clear();

                bs.InputTransparent = newBool;

                if (newBool)
                {
                    return;
                }

                bs.GestureRecognizers.Add(bs._backgroundInteractionTapGesture);
            });

    public static BindableProperty SheetColorProperty =
        BindableProperty.Create(
            nameof(SheetColor),
            typeof(Color),
            typeof(BottomSheet),
            Colors.White,
            propertyChanged:
            (bindable, _, newValue) =>
            {
                if (bindable is not BottomSheet sheet)
                {
                    return;
                }

                if (newValue is Color newColor)
                {
                    sheet._mainContainer.BackgroundColor = newColor;
                }
            });

    private readonly TapGestureRecognizer _backgroundInteractionTapGesture = new();

    private readonly Grid _contentContainer;

    private readonly Shape _grabbler;

    private readonly Border _mainContainer;
    private readonly double _touchBarHeight = 32d;

    private readonly BoxView _touchOverlay;

    private readonly PanGestureRecognizer _touchOverlayPanGesture;

    private double _sheetStartingTranslationY;

    public BottomSheet()
    {
        this.CascadeInputTransparent = false;
        this.IgnoreSafeArea = true;
        this.InputTransparent = this.AllowBackgroundInteraction;

        if (!this.AllowBackgroundInteraction)
        {
            this.GestureRecognizers.Add(this._backgroundInteractionTapGesture);
        }

        this._grabbler =
            new RoundRectangle
            {
                HeightRequest = 4,
                WidthRequest = this._touchBarHeight,
                CornerRadius = 2,
                BackgroundColor = Colors.DarkGray,
                HorizontalOptions = LayoutOptions.Center,
            };

        this._contentContainer =
            new Grid
            {
                ColumnDefinitions = { new ColumnDefinition(GridLength.Star) },
                RowDefinitions = { new RowDefinition(this._touchBarHeight), new RowDefinition(GridLength.Star) },
            };

        this._contentContainer.Add(this._grabbler, 0);
        Grid.SetColumn(this._grabbler, 0);
        Grid.SetRow(this._grabbler, 0);

        this._mainContainer =
            new Border
            {
                InputTransparent = false,
                GestureRecognizers =
                {
                    // TODO: We shouldn't need this and should just be able to use input transparent
                    new TapGestureRecognizer(),
                },
                StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(24, 24, 0, 0) },
                BackgroundColor = this.SheetColor,
                Content = this._contentContainer,
                Margin = -.5f,
            };

        this._touchOverlayPanGesture = new PanGestureRecognizer();

        this._touchOverlay =
            new BoxView
            {
                GestureRecognizers = { this._touchOverlayPanGesture },
                BackgroundColor = Colors.Transparent,
                HeightRequest = this._touchBarHeight,
                VerticalOptions = LayoutOptions.Start,
            };

        this.Children.Add(this._mainContainer);
        this.Children.Add(this._touchOverlay);

        this.Loaded += this.BottomSheet_Loaded;
    }

    public bool LockPosition
    {
        get => (bool)this.GetValue(LockPositionProperty);
        set => this.SetValue(LockPositionProperty, value);
    }

    public List<SheetStop> SheetStops
    {
        get => (List<SheetStop>)this.GetValue(SheetStopsProperty);
        private set => this.SetValue(SheetStopsProperty, value);
    }

    public View SheetContent
    {
        get => (View)this.GetValue(SheetContentProperty);
        set => this.SetValue(SheetContentProperty, value);
    }

    public bool AllowFullDismiss
    {
        get => (bool)this.GetValue(AllowFullDismissProperty);
        set => this.SetValue(AllowFullDismissProperty, value);
    }

    public bool AllowBackgroundInteraction
    {
        get => (bool)this.GetValue(AllowBackgroundInteractionProperty);
        set => this.SetValue(AllowBackgroundInteractionProperty, value);
    }

    public Color SheetColor
    {
        get => (Color)this.GetValue(SheetColorProperty);
        set => this.SetValue(SheetColorProperty, value);
    }

    protected override void OnPropertyChanged(string? propertyName = null)
    {
        base.OnPropertyChanged(propertyName);

        if (!(propertyName?.Equals(HeightProperty.PropertyName) ?? false))
        {
            return;
        }

        var height = this.Height;

        if (!this._hasInitialHeight && height > 0.0d)
        {
            this._hasInitialHeight = true;
            AnimateToPosition(0.0d);
        }
    }

    private void BottomSheet_Loaded(object? sender, EventArgs e)
    {
        this.Loaded -= this.BottomSheet_Loaded;

        this.Unloaded -= this.BottomSheet_Unloaded;
        this.Unloaded += this.BottomSheet_Unloaded;

        this._touchOverlayPanGesture.PanUpdated -= this._touchOverlayPanGesture_PanUpdated;
        this._touchOverlayPanGesture.PanUpdated += this._touchOverlayPanGesture_PanUpdated;
    }

    private void BottomSheet_Unloaded(object? sender, EventArgs e)
    {
        this.Unloaded -= this.BottomSheet_Unloaded;

        this._touchOverlayPanGesture.PanUpdated -= this._touchOverlayPanGesture_PanUpdated;
    }

    public void Dismiss()
    {
        if (this.LockPosition)
        {
            return;
        }

        this._touchOverlay.GestureRecognizers.Clear();

        double visibleHeight = this.Height;

        bool allowDismiss = this.AllowFullDismiss;

        double bottomSafeArea = allowDismiss ? this.ParentPage()?.On<iOS>()?.SafeAreaInsets().Bottom ?? 0 : 0;

        double touchBarDisplayHeight = allowDismiss ? 0 : this._touchBarHeight;

        Animation dismissAnimation =
            new(
                x =>
                {
                    this._mainContainer.TranslationY = x;
                    this._mainContainer.Padding = new Thickness(
                        this._mainContainer.Margin.Left,
                        this._mainContainer.Margin.Top,
                        this._mainContainer.Margin.Right,
                        x);
                },
                this._mainContainer.TranslationY,
                visibleHeight + bottomSafeArea - touchBarDisplayHeight,
                Easing.SinInOut);

        dismissAnimation
            .Commit(
                this,
                nameof(this.Dismiss),
                finished:
                (_, __) =>
                {
                    this.Dispatcher.Dispatch(
                        () =>
                        {
                            this._touchOverlay.TranslationY = this._mainContainer.TranslationY;
                            this._touchOverlay.GestureRecognizers.Add(this._touchOverlayPanGesture);
                        });
                });
    }

    public void Show(double displayPercentage)
    {
        if (this.LockPosition)
        {
            return;
        }

        this._touchOverlay.GestureRecognizers.Clear();

        double visibleHeight = this.Height;

        bool allowDismiss = this.AllowFullDismiss;

        double touchBarDisplayHeight = allowDismiss ? 0 : this._touchBarHeight;

        Animation dismissAnimation =
            new(
                x =>
                {
                    this._mainContainer.TranslationY = x;
                    this._mainContainer.Padding = new Thickness(
                        this._mainContainer.Margin.Left,
                        this._mainContainer.Margin.Top,
                        this._mainContainer.Margin.Right,
                        x);
                },
                this._mainContainer.TranslationY,
                (visibleHeight * (1d - Math.Max(Math.Min(displayPercentage, 1.0d), 0d))) - touchBarDisplayHeight,
                Easing.SinInOut);

        dismissAnimation
            .Commit(
                this,
                nameof(this.Dismiss),
                finished:
                (_, __) =>
                {
                    this.Dispatcher.Dispatch(
                        () =>
                        {
                            this._touchOverlay.TranslationY = this._mainContainer.TranslationY;
                            this._touchOverlay.GestureRecognizers.Add(this._touchOverlayPanGesture);
                        });
                });
    }

    private void _touchOverlayPanGesture_PanUpdated(object? sender, PanUpdatedEventArgs e)
    {
        if (this.LockPosition)
        {
            return;
        }

        Page? parentPage = this.ParentPage();

        if (parentPage is null)
        {
            return;
        }

        double visibleHeight = this.Height;

        double totalTranslation = this._sheetStartingTranslationY + e.TotalY;

        bool allowDismiss = this.AllowFullDismiss;

        double bottomSafeArea = allowDismiss ? this.ParentPage()?.On<iOS>()?.SafeAreaInsets().Bottom ?? 0 : 0;

        double touchBarDisplayHeight = allowDismiss ? 0 : this._touchBarHeight;

        switch (e.StatusType)
        {
            case GestureStatus.Started:
                this._sheetStartingTranslationY = this._mainContainer.TranslationY;
                break;
            case GestureStatus.Running:
                double clampedTranslation = totalTranslation.Clamp(0, visibleHeight - touchBarDisplayHeight);
                this._mainContainer.TranslationY = clampedTranslation;
                this._mainContainer.Padding = new Thickness(
                    this._mainContainer.Margin.Left,
                    this._mainContainer.Margin.Top,
                    this._mainContainer.Margin.Right,
                    clampedTranslation);
                break;
            default:
                double currTranslationY = this._mainContainer.TranslationY;

                this.AnimateToPosition(currTranslationY);

                break;
        }
    }

    private void AnimateToPosition(double yTranslation)
    {
        double visibleHeight = this.Height;

        bool allowDismiss = this.AllowFullDismiss;

        double bottomSafeArea = allowDismiss ? this.ParentPage()?.On<iOS>()?.SafeAreaInsets().Bottom ?? 0 : 0;

        double touchBarDisplayHeight = allowDismiss ? 0 : this._touchBarHeight;

        (SheetStop sheetStop, double Position, double Distance) closestSheetStop = this.SheetStops
            .Select(
                x =>
                {
                    double position =
                        x.Measurement switch
                        {
                            SheetStopMeasurement.Percentage => visibleHeight * (1d - Math.Max(Math.Min(x.Value, 1.0d), 0d)),
                            _ => x.Value,
                        };

                    return (sheetStop: x, Position: position, Distance: Math.Abs(yTranslation - position));
                })
            .OrderBy(x => x.Distance)
            .FirstOrDefault();

        this._touchOverlay.GestureRecognizers.Clear();

        Animation animateToPosition =
            new(
                x =>
                {
                    this._mainContainer.TranslationY = x;
                    this._mainContainer.Padding = new Thickness(
                        this._mainContainer.Margin.Left,
                        this._mainContainer.Margin.Top,
                        this._mainContainer.Margin.Right,
                        x);
                },
                this._mainContainer.TranslationY,
                closestSheetStop.Position.Clamp(0, visibleHeight + bottomSafeArea - touchBarDisplayHeight),
                Easing.SinInOut);

        animateToPosition.Commit(
            this,
            nameof(animateToPosition),
            finished:
            (_, __) =>
            {
                this.Dispatcher.Dispatch(
                    () =>
                    {
                        this._touchOverlay.TranslationY = this._mainContainer.TranslationY;
                        this._touchOverlay.GestureRecognizers.Add(this._touchOverlayPanGesture);
                    });
            });
    }
}
