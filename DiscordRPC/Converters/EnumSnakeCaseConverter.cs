using System;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DiscordRPC.Converters
{
	/// <summary>
	/// Converts enums with the <see cref="EnumValueAttribute"/> into Json friendly terms. 
	/// </summary>
	internal class EnumSnakeCaseConverter<TEnum> : JsonConverter<TEnum>
		where TEnum : struct, Enum
	{
		public override bool CanConvert(Type objectType)
		{
			return objectType == typeof(TEnum);
		}

		public override TEnum Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			if (reader.TokenType == JsonTokenType.Null) return default;

			if (TryParseEnum(reader.GetString(), out var val))
				return val;

			return default;
		}

		public override void Write(Utf8JsonWriter writer, TEnum value, JsonSerializerOptions options)
		{
			var enumtype = typeof(TEnum);
#if NET5_0_OR_GREATER
			var name = Enum.GetName(value);
#else
			var name = Enum.GetName(enumtype, value);
#endif

			//Get each member and look for the correct one
			var members = enumtype.GetMembers(BindingFlags.Public | BindingFlags.Static);
			foreach (var m in members)
			{
				if (m.Name.Equals(name))
				{
					var val = m.GetCustomAttribute<EnumValueAttribute>(true)?.Value;
					if (!string.IsNullOrWhiteSpace(val))
					{
						name = val;
						break;
					}
				}
			}

			writer.WriteStringValue(name);
		}

		private static bool TryParseEnum(string str, out TEnum obj)
		{
			//Make sure the string isn't null
			if (str == null)
			{
				obj = default;
				return false;
			}

			var enumtype = typeof(TEnum);
			//Get each member and look for the correct one
			var members = enumtype.GetMembers(BindingFlags.Public | BindingFlags.Static);
			foreach (var m in members)
			{
				var attributes = m.GetCustomAttributes<EnumValueAttribute>(true);
				foreach(var enumval in attributes)
				{
					if (str.Equals(enumval.Value))
					{
#if NETSTANDARD2_1_OR_GREATER || NET5_0_OR_GREATER
						obj = Enum.Parse<TEnum>(m.Name, ignoreCase: true);
#else
						obj = (TEnum)Enum.Parse(enumtype, m.Name, ignoreCase: true);
#endif
						return true;
					}
				}
			}

			//We failed
			obj = default;
			return false;
		}
	}
}
