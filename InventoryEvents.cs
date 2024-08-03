using System;

public static class InventoryEvents
{
    public static event Action<Item> OnItemHover;
    public static event Action OnItemHoverExit;
    public static event Action<Item> OnItemRightClicked;

    public static void ItemHovered(Item item)
    {
        OnItemHover?.Invoke(item);
    }

    public static void ItemHoverExited()
    {
        OnItemHoverExit?.Invoke();
    }

    public static void ItemRightClicked(Item item)
    {
        OnItemRightClicked?.Invoke(item);
    }
}
