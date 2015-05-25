using NUnit.Framework;

namespace JsonEx.Test
{
	[TestFixture]
	public class SimpleTest
	{
		[Test]
		public void Parse()
		{
			var item = Json.ReadObject<SimpleItem>(@"{""type"":""ItemA"", ""name"":""item_a""}");

			Assert.That(item.type, Is.EqualTo(ItemType.ItemA));
			Assert.That(item.name, Is.EqualTo("item_a"));
		}
	}
}
