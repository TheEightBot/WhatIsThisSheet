using Microsoft.Maui.Controls.PlatformConfiguration;
using Microsoft.Maui.Controls.PlatformConfiguration.iOSSpecific;
using Microsoft.Maui.Controls.Shapes;

namespace WhatIsThisSheet;

[ContentProperty(nameof(SheetContent))]
public class BottomSheet : Grid
{
    private readonly double _touchBarHeight = 32d;

    private readonly BoxView _touchOverlay;

    private readonly PanGestureRecognizer _touchOverlayPanGesture;

    private readonly Border _mainContainer;

    private readonly Grid _contentContainer;

    private readonly Shape _grabbler;

    private readonly List<SheetStop> _sheetStops = new List<SheetStop>();

    private readonly TapGestureRecognizer _backgroundInteractionTapGesture = new();

    private double _sheetStartingTranslationY = 0d;

    public static BindableProperty LockPositionProperty =
    BindableProperty.Create(nameof(LockPosition), typeof(bool), typeof(BottomSheet), default(bool));

    public bool LockPosition
    {
        get => (bool)GetValue(LockPositionProperty);
        set => SetValue(LockPositionProperty, value);
    }

    public static BindableProperty SheetContentProperty =
        BindableProperty.Create(
            nameof(SheetContent), typeof(View), typeof(BottomSheet), default(View),
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
                        Grid.SetColumn(newView, 0);
                        Grid.SetRow(newView, 1);
                        sheet._contentContainer.Add(newView);
                    }
                });

    public View SheetContent
    {
        get => (View)GetValue(SheetContentProperty);
        set => SetValue(SheetContentProperty, value);
    }

    public static BindableProperty AllowDismissProperty =
        BindableProperty.Create(nameof(AllowDismiss), typeof(bool), typeof(BottomSheet), false);

    public bool AllowDismiss
    {
        get => (bool)GetValue(AllowDismissProperty);
        set => SetValue(AllowDismissProperty, value);
    }

    public static BindableProperty AllowBackgroundInteractionProperty =
        BindableProperty.Create(nameof(AllowBackgroundInteraction), typeof(bool), typeof(BottomSheet), true,
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

    public bool AllowBackgroundInteraction
    {
        get => (bool)GetValue(AllowBackgroundInteractionProperty);
        set => SetValue(AllowBackgroundInteractionProperty, value);
    }

    public static BindableProperty SheetColorProperty =
    BindableProperty.Create(nameof(SheetColor), typeof(Color), typeof(BottomSheet), Colors.White,
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

    public Color SheetColor
    {
        get => (Color)GetValue(SheetColorProperty);
        set => SetValue(SheetColorProperty, value);
    }

    public BottomSheet()
    {
        this.CascadeInputTransparent = false;
        this.InputTransparent = AllowBackgroundInteraction;

        if (!AllowBackgroundInteraction)
        {
            this.GestureRecognizers.Add(_backgroundInteractionTapGesture);
        }

        _sheetStops.Add(new SheetStop { Measurement = SheetStopMeasurement.Fixed, Value = 0 });
        _sheetStops.Add(new SheetStop { Measurement = SheetStopMeasurement.Percentage, Value = .33 });
        _sheetStops.Add(new SheetStop { Measurement = SheetStopMeasurement.Percentage, Value = .66 });
        _sheetStops.Add(new SheetStop { Measurement = SheetStopMeasurement.Percentage, Value = 1.0 });

        _grabbler =
            new RoundRectangle
            {
                HeightRequest = 4,
                WidthRequest = _touchBarHeight,
                CornerRadius = 2,
                BackgroundColor = Colors.DarkGray,
                HorizontalOptions = LayoutOptions.Center,
            };

        _contentContainer =
            new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition(GridLength.Star),
                },
                RowDefinitions =
                {
                    new RowDefinition(_touchBarHeight),
                    new RowDefinition(GridLength.Star),
                },
            };

        _contentContainer.Add(_grabbler, 0, 0);
        Grid.SetColumn(_grabbler, 0);
        Grid.SetRow(_grabbler, 0);

        _mainContainer =
            new Border
            {
                InputTransparent = false,
                GestureRecognizers =
                {
                    // TODO: We shouldn't need this and should just be able to use input transparent
                    new TapGestureRecognizer(),
                },
                StrokeShape = new RoundRectangle
                {
                    CornerRadius = new CornerRadius(24, 24, 0, 0),
                },
                BackgroundColor = SheetColor,
                Content = _contentContainer,
                Margin = -.5f,
            };

        _touchOverlayPanGesture = new PanGestureRecognizer();

        _touchOverlay =
            new BoxView
            {
                GestureRecognizers = { _touchOverlayPanGesture },
                BackgroundColor = Colors.Transparent,
                HeightRequest = _touchBarHeight,
                VerticalOptions = LayoutOptions.Start,
            };

        this.Children.Add(_mainContainer);
        this.Children.Add(_touchOverlay);
    }

    public void Dismiss()
    {
        if (LockPosition)
        {
            return;
        }

        _touchOverlay.GestureRecognizers.Clear();

        var visibleHeight = this.Height;

        var allowDismiss = AllowDismiss;

        var bottomSafeArea = allowDismiss ? this.ParentPage()?.On<iOS>()?.SafeAreaInsets().Bottom ?? 0 : 0;

        var touchBarDisplayHeight = allowDismiss ? 0 : _touchBarHeight;

        var dismissAnimation =
            new Animation(
                x =>
                {
                    _mainContainer.TranslationY = x;
                    _mainContainer.Padding = new Thickness(_mainContainer.Margin.Left, _mainContainer.Margin.Top, _mainContainer.Margin.Right, x);
                },
                _mainContainer.TranslationY,
                visibleHeight + bottomSafeArea - touchBarDisplayHeight,
                Easing.SinInOut);

        dismissAnimation
            .Commit(
                this,
                nameof(Dismiss),
                finished:
                    (_, __) =>
                    {
                        Dispatcher.Dispatch(
                            () =>
                            {
                                _touchOverlay.TranslationY = _mainContainer.TranslationY;
                                _touchOverlay.GestureRecognizers.Add(_touchOverlayPanGesture);
                            });
                    });
    }

    public void Show(double displayPercentage)
    {
        if (LockPosition)
        {
            return;
        }

        _touchOverlay.GestureRecognizers.Clear();

        var visibleHeight = this.Height;

        var allowDismiss = AllowDismiss;

        var bottomSafeArea = allowDismiss ? this.ParentPage()?.On<iOS>()?.SafeAreaInsets().Bottom ?? 0 : 0;

        var touchBarDisplayHeight = allowDismiss ? 0 : _touchBarHeight;

        var dismissAnimation =
            new Animation(
                x =>
                {
                    _mainContainer.TranslationY = x;
                    _mainContainer.Padding = new Thickness(_mainContainer.Margin.Left, _mainContainer.Margin.Top, _mainContainer.Margin.Right, x);
                },
                _mainContainer.TranslationY,
                (visibleHeight * (1d - Math.Max(Math.Min(displayPercentage, 1.0d), 0d))) - touchBarDisplayHeight,
                Easing.SinInOut);

        dismissAnimation
            .Commit(
                this,
                nameof(Dismiss),
                finished:
                    (_, __) =>
                    {
                        Dispatcher.Dispatch(
                            () =>
                            {
                                _touchOverlay.TranslationY = _mainContainer.TranslationY;
                                _touchOverlay.GestureRecognizers.Add(_touchOverlayPanGesture);
                            });
                    });
    }

    protected override void OnParentChanging(ParentChangingEventArgs args)
    {
        base.OnParentChanging(args);

        _touchOverlayPanGesture.PanUpdated -= _touchOverlayPanGesture_PanUpdated;

        if (args.NewParent is not null)
        {
            _touchOverlayPanGesture.PanUpdated += _touchOverlayPanGesture_PanUpdated;
        }
    }

    private void _touchOverlayPanGesture_PanUpdated(object? sender, PanUpdatedEventArgs e)
    {
        if (LockPosition)
        {
            return;
        }

        var parentPage = this.ParentPage();

        if (parentPage is null)
        {
            return;
        }

        var visibleHeight = this.Height;

        var totalTranslation = this._sheetStartingTranslationY + e.TotalY;

        var allowDismiss = AllowDismiss;

        var bottomSafeArea = allowDismiss ? this.ParentPage()?.On<iOS>()?.SafeAreaInsets().Bottom ?? 0 : 0;

        var touchBarDisplayHeight = allowDismiss ? 0 : _touchBarHeight;

        switch (e.StatusType)
        {
            case GestureStatus.Started:
                _sheetStartingTranslationY = _mainContainer.TranslationY;
                break;
            case GestureStatus.Running:
                var clampedTranslation = totalTranslation.Clamp(0, visibleHeight - touchBarDisplayHeight);
                _mainContainer.TranslationY = clampedTranslation;
                _mainContainer.Padding = new Thickness(_mainContainer.Margin.Left, _mainContainer.Margin.Top, _mainContainer.Margin.Right, clampedTranslation);
                break;
            default:
                var currTranslationY = _mainContainer.TranslationY;

                var closestsheetStop =
                    _sheetStops
                        .Select(
                            (x) =>
                            {
                                var position =
                                    x.Measurement switch
                                    {
                                        SheetStopMeasurement.Percentage => visibleHeight * x.Value,
                                        _ => x.Value,
                                    };

                                return (sheetStop: x, Position: position, Distance: Math.Abs(currTranslationY - position));
                            })
                        .OrderBy(x => x.Distance)
                        .FirstOrDefault();

                _touchOverlay.GestureRecognizers.Clear();

                var animateToPosition =
                    new Animation(
                        x =>
                        {
                            _mainContainer.TranslationY = x;
                            _mainContainer.Padding = new Thickness(_mainContainer.Margin.Left, _mainContainer.Margin.Top, _mainContainer.Margin.Right, x);
                        },
                        _mainContainer.TranslationY,
                        closestsheetStop.Position.Clamp(0, visibleHeight + bottomSafeArea - touchBarDisplayHeight),
                        Easing.SinInOut);

                animateToPosition.Commit(
                    this,
                    nameof(animateToPosition),
                    finished:
                        (_, __) =>
                        {
                            Dispatcher.Dispatch(
                                () =>
                                {
                                    _touchOverlay.TranslationY = _mainContainer.TranslationY;
                                    _touchOverlay.GestureRecognizers.Add(_touchOverlayPanGesture);
                                });
                        });

                break;
        }
    }
}
