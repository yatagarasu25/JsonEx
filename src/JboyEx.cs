using System;
using System.Collections;
using System.Reflection;
using SystemEx;

namespace Json
{
	public static class JboyEx
	{
		private static MethodInfo readObjectMethod;

		static JboyEx()
		{
			readObjectMethod = typeof(Json).GetMethod("ReadObject", BindingFlags.Public | BindingFlags.Static, null, new Type[] { typeof(Jboy.JsonReader) }, null);
		}

		/// <summary>
		/// Читает из JsonReader поле заданного типа.
		/// Для не простых типов использует кодеки зарегистрированные в Jboy.
		/// </summary>
		/// <param name="reader">Ридер из которого производится чтение</param>
		/// <param name="type">Тип поля, которое хочется прочитать.</param>
		/// <returns>Значение прочитанного поля запрошенного типа</returns>
		public static object ReadType(this Jboy.JsonReader reader, Type type)
		{
			if (type.IsEnum) {
				return Enum.Parse(type, reader.ReadString());
			}

			switch (Type.GetTypeCode(type)) {
			case TypeCode.Boolean:
				return reader.ReadBoolean();

			case TypeCode.Byte:
			case TypeCode.SByte:
			case TypeCode.UInt16:
			case TypeCode.UInt32:
			case TypeCode.UInt64:
			case TypeCode.Int16:
			case TypeCode.Int32:
			case TypeCode.Int64:
			case TypeCode.Decimal:
			case TypeCode.Double:
			case TypeCode.Single:
				return Convert.ChangeType(reader.ReadNumber(), type);

			case TypeCode.String:
				return reader.ReadString();
			}

			if (type.IsList()) {
				IList list = (IList)Activator.CreateInstance(type);
				Type itemType = type.GetListItemType();

				reader.ReadArrayStart();
				while (!reader.TryReadArrayEnd()) {
					list.Add(reader.ReadType(itemType));
				}

				return list;
			}

			return readObjectMethod.MakeGenericMethod(type).Invoke(null, new object[] { reader });
		}

		/// <summary>
		/// Пишет в JsonWriter поле заданного типа.
		/// Для не простых типов использует кодеки зарегистрированные в Jboy.
		/// </summary>
		/// <param name="writer">Объект куда пследует писать поле.</param>
		/// <param name="value">Значение, которое следует записать.</param>
		/// <param name="type">Тип записываемого значения.</param>
		public static void WriteType(this Jboy.JsonWriter writer, object value, Type type)
		{
			if (value == null) {
				writer.WriteNull();
				return;
			}

			if (type.IsEnum) {
				writer.WriteString(value.ToString());

				return;
			}

			switch (Type.GetTypeCode(type)) {
			case TypeCode.Boolean:
				writer.WriteBoolean((bool)value);
				return;

			case TypeCode.Byte:
			case TypeCode.SByte:
			case TypeCode.UInt16:
			case TypeCode.UInt32:
			case TypeCode.UInt64:
			case TypeCode.Int16:
			case TypeCode.Int32:
			case TypeCode.Int64:
			case TypeCode.Decimal:
			case TypeCode.Double:
			case TypeCode.Single:
				writer.WriteNumber((double)Convert.ChangeType(value, typeof(double)));
				return;

			case TypeCode.String:
				writer.WriteString((string)value);
				return;
			}

			if (type.IsList()) {
				IList list = (IList)value;

				writer.WriteArrayStart();
				foreach (var item in list) {
					writer.WriteType(item, item.GetType());
				}
				writer.WriteArrayEnd();

				return;
			}

			Json.WriteObject(value, writer);
		}
	}
}