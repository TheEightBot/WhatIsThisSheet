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
        MainBottomSheet.Dismiss();
    }

    private void Switch_Toggled(object sender, Microsoft.Maui.Controls.ToggledEventArgs e)
    {
        MainBottomSheet.AllowFullDismiss = !MainBottomSheet.AllowFullDismiss;
    }

    private void Show_Clicked(object sender, System.EventArgs e)
    {
        MainBottomSheet.Show(.65d);
    }

    private void GetTapped_Clicked(object sender, System.EventArgs e)
    {
        Console.WriteLine("Tapped Out!");
    }

    private void ToggleBackgroundInteraction_Clicked(object sender, System.EventArgs e)
    {
        MainBottomSheet.AllowBackgroundInteraction = !MainBottomSheet.AllowBackgroundInteraction;
    }

    private void LockPosition_Clicked(object sender, System.EventArgs e)
    {
        MainBottomSheet.LockPosition = !MainBottomSheet.LockPosition;
    }
}
