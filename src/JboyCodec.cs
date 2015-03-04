using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using SystemEx;

namespace JsonEx
{
	/// <summary>
	/// Аттрибут, который говорит о том что в классе не все филды нудо писать в json, а только помеченные таким же аттрибутом.
	/// </summary>
	public class JboySerializableAttribute : Attribute
	{
	}

	/// <summary>
	/// Аттрибут, который говорит о том что в классе есть функции JsonSerializer и JsonDeserializer, которые необходимо использовать для сериализации.
	/// </summary>
	public class JboySerializableCustomAttribute : Attribute
	{
	}

	public class JboyCustomSerializerForPropertyAttribute : Attribute
	{
		public string name;

		public JboyCustomSerializerForPropertyAttribute(string propertyName)
		{
			name = propertyName;
		}
	}

	/// <summary>
	/// Класс который генерит Jboy кодеки для простых объектов.
	/// На данный момент пишет все филды объекта в json.
	/// Для объектов помеченных аттрибутом [JboySerializable] пишет только филды
	/// помеченные аттрибутом [JboySerializable]
	///
	/// Исопльзовать как-то так:
	/// var codec = new JboyCodec<SerailizableObject>();
	/// Json.AddCodec<SerailizableObject>(codec.JsonDeserializer, codec.JsonSerializer);
	///
	/// или
	/// JboyCodec<SerailizableObject>.RegisterCodec();
	/// </summary>
	public class JboyCodec
	{
		private delegate object JsonCustomDeserializer(Jboy.JsonReader reader);

		private delegate void JsonCustomSerializer(Jboy.JsonWriter writer, object value);

		private Dictionary<string, FieldInfo> fields = new Dictionary<string, FieldInfo>();
		private Dictionary<string, FieldInfo> customFields = new Dictionary<string, FieldInfo>();

		private Dictionary<string, JsonCustomDeserializer> customDeserializers = null;
		private Dictionary<string, JsonCustomSerializer> customSerializers = null;

		private Type type;

		private static MethodInfo addCodec;

		static JboyCodec()
		{
			addCodec = typeof(Jboy.Json).GetMethod("AddCodec", BindingFlags.Static | BindingFlags.Public, null, new Type[] { typeof(Jboy.JsonDeserializer), typeof(Jboy.JsonSerializer) }, null);
		}

		public JboyCodec(Type type)
		{
			this.type = type;

			if (type.GetAttribute<JboySerializableAttribute>() == null) {
				foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)) {
					if (field.GetAttribute<CompilerGeneratedAttribute>() != null) {
						continue;
					}
					if (field.GetAttribute<JboySerializableCustomAttribute>() != null) {
						customFields.Add(field.Name, field);
					}
					else {
						fields.Add(field.Name, field);
					}
				}
			}
			else {
				foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)) {
					if (field.GetAttribute<CompilerGeneratedAttribute>() != null) {
						continue;
					}
					if (field.GetAttribute<JboySerializableAttribute>() != null) {
						fields.Add(field.Name, field);
					}
					else if (field.GetAttribute<JboySerializableCustomAttribute>() != null) {
						customFields.Add(field.Name, field);
					}
				}
			}

			if (customFields.Count > 0) {
				customDeserializers = new Dictionary<string, JsonCustomDeserializer>();
				customSerializers = new Dictionary<string, JsonCustomSerializer>();

				foreach (var method in type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)) {
					if (method.Name == "JsonCustomDeserializer") {
						JboyCustomSerializerForPropertyAttribute attribute = method.GetAttribute<JboyCustomSerializerForPropertyAttribute>();
						if (attribute != null) {
							customDeserializers.Add(attribute.name, (JsonCustomDeserializer)Delegate.CreateDelegate(typeof(JsonCustomDeserializer), method));
						}
					}
					else if (method.Name == "JsonCustomSerializer") {
						JboyCustomSerializerForPropertyAttribute attribute = method.GetAttribute<JboyCustomSerializerForPropertyAttribute>();
						if (attribute != null) {
							customSerializers.Add(attribute.name, (JsonCustomSerializer)Delegate.CreateDelegate(typeof(JsonCustomSerializer), method));
						}
					}
				}
			}
		}

		/// <summary>
		/// Автоматически создаёт и решистрирует кодеки для заданного типа.
		/// </summary>
		public static void RegisterCodec(Type type)
		{
			if (type.GetAttribute<JboySerializableCustomAttribute>() == null) {
				var codec = new JboyCodec(type);
				addCodec.MakeGenericMethod(type).Invoke(null, new object[] { new Jboy.JsonDeserializer(codec.JsonDeserializer), new Jboy.JsonSerializer(codec.JsonSerializer) });
			}
			else {
				addCodec.MakeGenericMethod(type).Invoke(null
					, new object[] {
                        Delegate.CreateDelegate(typeof(Jboy.JsonDeserializer), type.GetMethod("JsonDeserializer", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static))
                        , Delegate.CreateDelegate(typeof(Jboy.JsonSerializer), type.GetMethod("JsonSerializer", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static))
                    });
			}
		}

		/// <summary>
		/// Эту функцию надо зарегать как сериализатор.
		/// </summary>
		public void JsonSerializer(Jboy.JsonWriter writer, object obj)
		{
			writer.WriteObjectStart();
			foreach (var pair in fields) {
				var propertyName = pair.Key;
				var field = pair.Value;

				writer.WritePropertyName(propertyName);
				writer.WriteType(field.GetValue(obj), field.FieldType);
			}
			foreach (var pair in customFields) {
				var propertyName = pair.Key;
				var field = pair.Value;

				writer.WritePropertyName(propertyName);
				JsonCustomSerializer customSerializer;
				if (customSerializers.TryGetValue(propertyName, out customSerializer)) {
					customSerializer(writer, field.GetValue(obj));
				}
				else {
					Log.Error("No JsonCustomSerializer is defined for field {0}", propertyName);
				}
			}
			writer.WriteObjectEnd();
		}

		/// <summary>
		/// Эту функцию надо зарегать как десериализатор.
		/// </summary>
		public object JsonDeserializer(Jboy.JsonReader reader)
		{
			object obj = Activator.CreateInstance(type);

			reader.ReadObjectStart();

			string propertyName;
			while (reader.TryReadPropertyName(out propertyName)) {
				FieldInfo field;
				if (fields.TryGetValue(propertyName, out field)) {
					field.SetValue(obj, reader.ReadType(field.FieldType));
				}
				else if (customFields.TryGetValue(propertyName, out field)) {
					JsonCustomDeserializer customDeserializer;
					if (customDeserializers.TryGetValue(propertyName, out customDeserializer)) {
						field.SetValue(obj, customDeserializer(reader));
					}
					else {
						Log.Error("No JsonCustomDeserializer is defined for field {0}", propertyName);
					}
				}
				else {
					Log.Error("Unknown filed `{0}` in json for object `{1}`.", propertyName, type.Name);
					object value;
					reader.Read(out value);
				}
			}
			reader.ReadObjectEnd();

			return obj;
		}
	}
}