namespace MauiDrawer;

public static class ViewExtensions
{
    public static Page? ParentPage(this Element view)
    {
        var currentParent = view.Parent;

        while (currentParent is not null)
        {
            if (currentParent is Page parentPage)
            {
                return parentPage;
            }

            currentParent = currentParent.Parent;
        }

        return default;
    }
}