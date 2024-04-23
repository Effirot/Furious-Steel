

using Effiry.Items;

public interface IItemSlot
{
    public Item Item { get; set; }
} 

public interface IItemSlot<T> : IItemSlot
{
    public new T Item { get; set; }
} 