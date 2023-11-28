using Microsoft.Maui.Controls.Shapes;

namespace MauiDrawer;

public enum DrawerStopMeasurement
{
    Fixed = 0,
    Percentage = 1,
}

public struct DrawerStop
{
    public DrawerStopMeasurement Measurement { get; set; }

    public double Value { get; set; }
}

[ContentProperty(nameof(DrawerContent))]
public class Drawer : Grid
{
    private readonly double TouchBarHeight = 32d;

    private readonly BoxView _touchOverlay;

    private readonly PanGestureRecognizer _touchOverlayPanGesture;

    private readonly Border _mainContainer;

    private readonly Grid _contentContainer;

    private readonly Shape _grabbler;

    private readonly List<DrawerStop> _drawerStops = new List<DrawerStop>();

    private double _drawerStartingTranslationY = 0d;

    public static BindableProperty DrawerContentProperty =
    BindableProperty.Create(
        nameof(DrawerContent), typeof(View), typeof(Drawer), default(View),
        propertyChanged:
            (bindable, oldValue, newValue) =>
            {
                if (bindable is not Drawer drawer)
                {
                    return;
                }

                if (oldValue is View oldView)
                {
                    drawer._contentContainer.Remove(oldView);
                }

                if (newValue is View newView)
                {
                    Grid.SetColumn(newView, 0);
                    Grid.SetRow(newView, 1);
                    drawer._contentContainer.Add(newView);
                }
            });

    public View DrawerContent
    {
        get => (View)GetValue(DrawerContentProperty);
        set => SetValue(DrawerContentProperty, value);
    }

    public static BindableProperty DrawerColorProperty =
    BindableProperty.Create(nameof(DrawerColor), typeof(Color), typeof(Drawer), Colors.White,
        propertyChanged:
            (bindable, oldValue, newValue) =>
            {
                if (bindable is not Drawer drawer)
                {
                    return;
                }


                if (newValue is Color newColor)
                {
                    drawer._mainContainer.BackgroundColor = newColor;
                }
            });

    public Color DrawerColor
    {
        get => (Color)GetValue(DrawerColorProperty);
        set => SetValue(DrawerColorProperty, value);
    }

    public Drawer()
    {
        _drawerStops.Add(new DrawerStop { Measurement = DrawerStopMeasurement.Fixed, Value = 0 });
        _drawerStops.Add(new DrawerStop { Measurement = DrawerStopMeasurement.Percentage, Value = .33 });
        _drawerStops.Add(new DrawerStop { Measurement = DrawerStopMeasurement.Percentage, Value = .66 });
        _drawerStops.Add(new DrawerStop { Measurement = DrawerStopMeasurement.Percentage, Value = 1.0 });

        _grabbler =
            new RoundRectangle
            {
                HeightRequest = 4,
                WidthRequest = TouchBarHeight,
                CornerRadius = 2,
                BackgroundColor =  Colors.DarkGray,
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
                    new RowDefinition(TouchBarHeight),
                    new RowDefinition(GridLength.Star),
                },
            };

        _contentContainer.Add(_grabbler, 0, 0);
        Grid.SetColumn(_grabbler, 0);
        Grid.SetRow(_grabbler, 0);

        _mainContainer =
            new Border
            {
                StrokeShape = new RoundRectangle
                {
                    CornerRadius = new CornerRadius(24, 24, 0, 0),
                },
                BackgroundColor = DrawerColor,
                Content = _contentContainer,
                Margin = -.5f,
            };

        _touchOverlayPanGesture = new PanGestureRecognizer();

        _touchOverlay =
            new BoxView
            {
                GestureRecognizers = { _touchOverlayPanGesture },
                BackgroundColor = Colors.Transparent,
                HeightRequest = TouchBarHeight,
                VerticalOptions = LayoutOptions.Start,
            };

        this.Children.Add(_mainContainer);
        this.Children.Add(_touchOverlay);
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
        if (this.Parent is not VisualElement visualParent)
        {
            return;
        }

        var visibleParentHeight = this.Height;

        var totalTranslation = this._drawerStartingTranslationY + e.TotalY;

        switch (e.StatusType)
        {
            case GestureStatus.Started:
                _drawerStartingTranslationY = _mainContainer.TranslationY;
                break;
            case GestureStatus.Running:
                var clampedTranslation = totalTranslation.Clamp(0, visibleParentHeight - TouchBarHeight);
                _mainContainer.TranslationY = clampedTranslation;
                _mainContainer.Padding = new Thickness(_mainContainer.Margin.Left, _mainContainer.Margin.Top, _mainContainer.Margin.Right, clampedTranslation);
                break;
            case GestureStatus.Canceled:
            case GestureStatus.Completed:
            default:
                var currTranslationY = _mainContainer.TranslationY;

                var closestDrawerStop =
                    _drawerStops
                        .Select(
                            (x, i) =>
                            {
                                var position =
                                    x.Measurement switch
                                    {
                                        DrawerStopMeasurement.Fixed => x.Value,
                                        DrawerStopMeasurement.Percentage => visibleParentHeight * x.Value,
                                    };

                                return (DrawerStop: x, Index: i, Position: position, Distance: Math.Abs(currTranslationY - position));
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
                        closestDrawerStop.Position.Clamp(0, visibleParentHeight - TouchBarHeight),
                        Easing.SinInOut);

                animateToPosition.Commit(
                    this,
                    nameof(animateToPosition),
                    finished:
                        (x, finished) =>
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

