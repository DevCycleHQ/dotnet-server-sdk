using System;
using System.Linq;
using System.Text.Json;
using OpenFeature.Model;

namespace DevCycle.SDK.Server.Common.Model;

public class OpenFeatureValueJsonConverter : System.Text.Json.Serialization.JsonConverter<Value>
{
    public override Value Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
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