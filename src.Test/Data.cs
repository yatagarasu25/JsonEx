using System.Collections.Generic;

namespace JsonEx.Test
{
	public enum ItemType
	{
		Unknown,
		ItemA,
		ItemB,
	}

	public class SimpleItem
	{
		public ItemType type;

		public string name;
	}

	public class ListOfItems
	{
		public List<SimpleItem> items;
	}

	public class ItemFactory
	{
		public static FactoryItem Create(ItemType type)
		{
			switch (type) {
			case ItemType.ItemA:
				return new ItemA();

			case ItemType.ItemB:
				return new ItemB();
			}

			return null;
		}
	}

	public class FactoryItem
	{
		[JboyFactory(typeof(ItemFactory))]
		public ItemType type;

		public string name;
	}

	public class ItemA : FactoryItem
	{
		public string item_a;

		public ItemA()
		{
			type = ItemType.ItemA;
		}
	}

	public class ItemB : FactoryItem
	{
		public string item_b;

		public ItemB()
		{
			type = ItemType.ItemB;
		}
	}
}