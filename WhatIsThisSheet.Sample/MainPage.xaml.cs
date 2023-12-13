namespace MauiDrawer.Sample;

public partial class MainPage : ContentPage
{
    private readonly int _count = 0;

    public MainPage()
    {
        InitializeComponent();
    }

    private int _incrementer = 0;

    private void Button_Clicked(object sender, System.EventArgs e)
    {
        _incrementer++;
        (sender as Button).Text = $"Tapped {_incrementer} Times";
    }

    private void Dismiss_Clicked(object sender, System.EventArgs e)
    {
        MainDrawer.Dismiss();
    }

    private void Switch_Toggled(object sender, Microsoft.Maui.Controls.ToggledEventArgs e)
    {
        MainDrawer.AllowDismiss = !MainDrawer.AllowDismiss;
    }

    private void Show_Clicked(object sender, System.EventArgs e)
    {
        MainDrawer.Display(.65d);
    }
}
