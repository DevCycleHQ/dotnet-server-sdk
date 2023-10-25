using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using OpenFeature.Model;

namespace DevCycle.SDK.Server.Common.Model;

public class OpenFeatureValueJsonConverter : JsonConverter<Value>
{
    public override Value Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var structureBuilder = Structure.Builder();
        var list = new List<Value>();
        while (reader.Read())
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.None:
                    break;
                case JsonTokenType.StartObject:
                    return Read(ref reader, typeToConvert, options);
                case JsonTokenType.EndObject:
                    return new Value(structureBuilder.Build());
                case JsonTokenType.StartArray:
                    for (; reader.TokenType != JsonTokenType.EndArray; reader.Read())
                    {
                        list.Add(Read(ref reader, typeToConvert, options));
                    }
                    return new Value(list);
                
                case JsonTokenType.EndArray:
                    break;
                case JsonTokenType.PropertyName:
                    structureBuilder.Set(reader.GetString(), Read(ref reader, typeToConvert, options));
                    break;
                case JsonTokenType.Comment:
                    break;
                case JsonTokenType.String:
                    return new Value(reader.GetString());
                case JsonTokenType.Number:
                    return new Value(reader.GetDecimal());
                case JsonTokenType.True:
                    return new Value(true);
                case JsonTokenType.False:
                    return new Value(false);
                case JsonTokenType.Null:
                    return new Value();
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        return new Value(structureBuilder.Build());
    }

    public override bool CanConvert(Type typeToConvert)
    {
        return typeToConvert == typeof(Value);
    }

    public override void Write(Utf8JsonWriter writer, Value value, JsonSerializerOptions options)
    {
        if (value.IsStructure)
        {
            var structure = value.AsStructure;
            writer.WriteStartObject();

            foreach (var (k, v) in structure.AsDictionary().Select(x => (x.Key, x.Value)))
            {
                writer.WritePropertyName(k);
                Write(writer, v, options);
            }

            writer.WriteEndObject();
        }
        else if (value.IsList)
        {
            var list = value.AsList;
            writer.WriteStartArray();
            foreach (var v in list)
            {
                Write(writer, v, options);
            }

            writer.WriteEndArray();
        }
        else if (value.IsString)
            writer.WriteStringValue(value.AsString);
        else if (value.IsBoolean)
            writer.WriteBooleanValue(value.AsBoolean != null && (bool)value.AsBoolean);
        else if (value.IsNumber)
        {
            if (value.AsDouble != null)
            {
                writer.WriteNumberValue((double)value.AsDouble);
            }
            else if (value.AsInteger != null)
            {
                writer.WriteNumberValue((int)value.AsInteger);
            }
        }
        else if (value.IsDateTime)
            writer.WriteStringValue(value.AsDateTime.ToString());
        else
            writer.WriteNullValue();
    }
}