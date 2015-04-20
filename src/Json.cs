using System;
using System.Collections.Generic;
using System.IO;
using SystemEx;

namespace JsonEx
{
	public static class Json
	{
		public static bool prettyPrint = false;

		private static Dictionary<Type, IJboyCodec> registeredCodecs = new Dictionary<Type, IJboyCodec>();

		private static IJboyCodec RegisterCodec(Type type)
		{
			if (type.IsList()) {
				type = type.GetListItemType();
			}

			IJboyCodec codec;
			if (!registeredCodecs.TryGetValue(type, out codec)) {
				codec = JboyCodec.RegisterCodec(type);
				registeredCodecs.Add(type, codec);
			}

			return codec;
		}

		private static IJboyCodec RegisterCodec<T>()
			where T : new()
		{
			return RegisterCodec(typeof(T));
		}

		public static T ReadObject<T>(string json)
			where T : new()
		{
			RegisterCodec<T>();

			return Jboy.Json.ReadObject<T>(json);
		}

		public static T ReadObject<T>(T o, string json)
			where T : new()
		{
			var codec = RegisterCodec<T>();

			return (T)codec.JsonDeserializer(new Jboy.JsonReader(json), o);
		}

		public static T ReadObject<T>(Jboy.JsonReader reader)
			where T : new()
		{
			RegisterCodec<T>();

			return Jboy.Json.ReadObject<T>(reader);
		}

		public static T Read<T>(string path)
			where T : new()
		{
			T readObject = new T();

			try {
				using (TextReader reader = new StreamReader(File.OpenRead(path))) {
					readObject = ReadObject<T>(reader.ReadToEnd());
				}
			}
			catch (Exception e) {
				Log.Error("Could not open or parse '{0}' file: {1}. ", path, e.Message);
			}

			return readObject;
		}

		public static string WriteObject(object value)
		{
			RegisterCodec(value.GetType());

			var writer = new Jboy.JsonWriter(false, prettyPrint, 4);
			Jboy.Json.WriteObject(value, writer);
			return writer.ToString();
		}

		public static void WriteObject(object value, Jboy.JsonWriter writer)
		{
			RegisterCodec(value.GetType());

			Jboy.Json.WriteObject(value, writer);
		}

		public static void Write(object value, string path)
		{
			try {
				FileStream fileStream = new FileStream(path, FileMode.Create, FileAccess.Write);
				using (TextWriter writer = new StreamWriter(fileStream)) {
					writer.Write(WriteObject(value));
					writer.Close();
				}
			}
			catch (Exception e) {
				Log.Warning("Could not write config file: {0}", e);
			}
		}
	}
}