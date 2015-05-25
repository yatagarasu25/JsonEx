using NUnit.Framework;

namespace JsonEx.Test
{
	[TestFixture]
	public class ListTest
	{
		[Test]
		public void Parse()
		{
			var item = Json.ReadObject<ListOfItems>(@"{""items"":[{""type"":""ItemA"", ""name"":""item_a""}, {""type"":""ItemB"", ""name"":""item_b""}]}");

			Assert.That(item.items, Has.Count.EqualTo(2));
			Assert.That(item.items[0].type, Is.EqualTo(ItemType.ItemA));
			Assert.That(item.items[0].name, Is.EqualTo("item_a"));
			Assert.That(item.items[1].type, Is.EqualTo(ItemType.ItemB));
			Assert.That(item.items[1].name, Is.EqualTo("item_b"));
		}
	}
}
