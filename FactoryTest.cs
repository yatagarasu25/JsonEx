using NUnit.Framework;

namespace JsonEx.Test
{
	[TestFixture]
	public class FactoryTest
	{
		[Test]
		public void Parse()
		{
			var item = Json.ReadObject<FactoryItem>(@"{""type"":""ItemA"", ""name"":""item_a"", ""item_a"":""a_item""}");

			Assert.That(item.type, Is.EqualTo(ItemType.ItemA));
			Assert.That(((ItemA)item).item_a, Is.EqualTo("a_item"));
		}
	}
}