using System;
using System.Collections.Generic;
using System.IO;
using SystemEx;

namespace Json
{
	public static class Json
	{
		private static Dictionary<Type, bool> registeredCodecs = new Dictionary<Type, bool>();

		private static void RegisterCodec(Type type)
		{
			if (type.IsList()) {
				type = type.GetListItemType();
			}

			if (!registeredCodecs.ContainsKey(type)) {
				JboyCodec.RegisterCodec(type);
				registeredCodecs.Add(type, true);
			}
		}

		private static void RegisterCodec<T>()
			where T : new()
		{
			RegisterCodec(typeof(T));
		}

		public static T ReadObject<T>(string json)
			where T : new()
		{
			RegisterCodec<T>();

			return Jboy.Json.ReadObject<T>(json);
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

			return Jboy.Json.WriteObject(value);
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