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
	/// Аттрибут, который говорит какая функция создаёт инстанс объекта при десериализации.
	/// При отсутствии используется Activator.CreateInstance(type);
	/// </summary>
	public class JboyConstructorAttribute : Attribute
	{
	}

	/// <summary>
	/// Аттрибут, который говорит какая функция фызывается после записи всех полей, для записи какой=либо дополнительной информации.
	/// </summary>
	public class JboyCustomDataAttribute : Attribute
	{
	}

	/// <summary>
	/// Аттрибут, который говорит какая функция фызывается после загрузки данных объекта.
	/// </summary>
	public class JboyPostLoadAttribute : Attribute
	{
	}

	/// <summary>
	/// Аттрибут, который говорит какая функция фызывается для десериализации неизвестного аттрибута.
	/// </summary>
	public class JboyUnknownAttribute : Attribute
	{
	}

	public interface IJboyCodec
	{
		void JsonSerializer(Jboy.JsonWriter writer, object obj);
		object JsonDeserializer(Jboy.JsonReader reader, object obj);
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
	public class JboyCodec : IJboyCodec
	{
		public delegate object JsonCustomDeserializer(object o, Jboy.JsonReader reader);
		public delegate void JsonCustomSerializer(object o, Jboy.JsonWriter writer, object value);

		private MethodInfo ctor = null;
		private MethodInfo postLoad = null;
		private MethodInfo customData = null;
		private MethodInfo unknowndData = null;

		private Dictionary<string, FieldInfo> fields = new Dictionary<string, FieldInfo>();
		private Dictionary<string, FieldInfo> customFields = new Dictionary<string, FieldInfo>();

		private Dictionary<string, MethodInfo> customDeserializers = null;
		private Dictionary<string, MethodInfo> customSerializers = null;

		private Type type;

		private static MethodInfo addCodec;

		static JboyCodec()
		{
			addCodec = typeof(Jboy.Json).GetMethod("AddCodec", BindingFlags.Static | BindingFlags.Public, null, new Type[] { typeof(Jboy.JsonDeserializer), typeof(Jboy.JsonSerializer) }, null);
		}

		public JboyCodec(Type type)
		{
			this.type = type;

			foreach (var method in type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)) {
				if (method.HasAttribute<JboyConstructorAttribute>()) {
					ctor = method;
					break;
				}
			}

			foreach (var method in type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)) {
				if (method.HasAttribute<JboyPostLoadAttribute>()) {
					postLoad = method;
				}
				if (method.HasAttribute<JboyCustomDataAttribute>()) {
					customData = method;
				}
				if (method.HasAttribute<JboyUnknownAttribute>()) {
					unknowndData = method;
				}
			}

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
				customDeserializers = new Dictionary<string, MethodInfo>();
				customSerializers = new Dictionary<string, MethodInfo>();

				foreach (var method in type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)) {
					if (method.Name.StartsWith("JsonCustomDeserializer")) {
						JboyCustomSerializerForPropertyAttribute attribute = method.GetAttribute<JboyCustomSerializerForPropertyAttribute>();
						if (attribute != null) {
							customDeserializers.Add(attribute.name, method);
						}
					}
					else if (method.Name.StartsWith("JsonCustomSerializer")) {
						JboyCustomSerializerForPropertyAttribute attribute = method.GetAttribute<JboyCustomSerializerForPropertyAttribute>();
						if (attribute != null) {
							customSerializers.Add(attribute.name, method);
						}
					}
				}
			}
		}

		/// <summary>
		/// Автоматически создаёт и решистрирует кодеки для заданного типа.
		/// </summary>
		public static IJboyCodec RegisterCodec(Type type)
		{
			if (type.GetAttribute<JboySerializableCustomAttribute>() == null) {
				var codec = new JboyCodec(type);
				addCodec.MakeGenericMethod(type).Invoke(null, new object[] { new Jboy.JsonDeserializer(codec.JsonDeserializerNew), new Jboy.JsonSerializer(codec.JsonSerializer) });
				return codec;
			}
			else {
				addCodec.MakeGenericMethod(type).Invoke(null, new object[] { 
					(Jboy.JsonDeserializer)Delegate.CreateDelegate(typeof(Jboy.JsonDeserializer), type.GetMethod("JsonDeserializer", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
					, (Jboy.JsonSerializer)Delegate.CreateDelegate(typeof(Jboy.JsonSerializer), type.GetMethod("JsonSerializer", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)) });
				return null;
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
				MethodInfo customSerializer;
				if (customSerializers.TryGetValue(propertyName, out customSerializer)) {
					customSerializer.Invoke(null, new object[] { obj, writer, field.GetValue(obj) });
				}
				else {
					Log.Error("No JsonCustomSerializer is defined for field {0}", propertyName);
				}
			}

			if (customData != null)
				customData.Invoke(obj, new object[] { writer });

			writer.WriteObjectEnd();
		}

		public object JsonDeserializerNew(Jboy.JsonReader reader)
		{
			return JsonDeserializer(reader, null);
		}

		/// <summary>
		/// Эту функцию надо зарегать как десериализатор.
		/// </summary>
		public object JsonDeserializer(Jboy.JsonReader reader, object obj)
		{
			if (obj == null)
				obj = ctor == null ? Activator.CreateInstance(type) : ctor.Invoke(null, null);

			reader.ReadObjectStart();

			string propertyName;
			while (reader.TryReadPropertyName(out propertyName)) {
				FieldInfo field;
				if (fields.TryGetValue(propertyName, out field)) {
					field.SetValue(obj, reader.ReadType(field.FieldType));
				}
				else if (customFields.TryGetValue(propertyName, out field)) {
					MethodInfo customDeserializer;
					if (customDeserializers.TryGetValue(propertyName, out customDeserializer)) {
						field.SetValue(obj, customDeserializer.Invoke(null, new object[] { obj, reader }));
					}
					else {
						Log.Error("No JsonCustomDeserializer is defined for field {0}", propertyName);
					}
				}
				else {
					if (unknowndData == null) {
						Log.Error("Unknown field `{0}` in json for object `{1}`.", propertyName, type.Name);
						object value;
						reader.Read(out value);
					}
					else {
						unknowndData.Invoke(obj, new object[] { propertyName, reader });
					}
				}
			}
			reader.ReadObjectEnd();

			if (postLoad != null)
				postLoad.Invoke(obj, null);

			return obj;
		}
	}
}