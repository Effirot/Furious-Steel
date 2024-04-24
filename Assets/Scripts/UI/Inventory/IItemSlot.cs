

using Effiry.Items;


public interface IItemSlot<T> where T : Item
{
    public T Item { get; set; }
} 
public interface IItemSlot : IItemSlot<Item>
{

} 