using Mikibot.Crawler.WebsocketCrawler.Data.Commands.KnownCommand;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Mikibot.Crawler.WebsocketCrawler.Data.Commands
{
    public class DanmuMsgJsonConverter : JsonConverter<DanmuMsg>
    {
        public override DanmuMsg Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartArray)
            {
                throw new InvalidDataException($"Invalid character in pos {reader.Position}");
            }
            var sentAt = 0L;
            string memeUrl = string.Empty;
            var hexColor = "#000000";
            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.StartArray)
                {
                    reader.Read(); reader.GetInt32();
                    reader.Read(); reader.GetInt32();
                    reader.Read(); reader.GetInt32();
                    reader.Read(); reader.GetInt32();
                    reader.Read(); sentAt = reader.GetInt64();

                    while (reader.TokenType != JsonTokenType.StartObject
                        && reader.TokenType != JsonTokenType.EndArray)
                    {
                        reader.Read();
                    }
                    if (reader.TokenType == JsonTokenType.StartObject)
                    {
                        reader.Read();
                        while (reader.TokenType != JsonTokenType.EndObject)
                        {
                            while (reader.TokenType != JsonTokenType.PropertyName
                                && reader.TokenType != JsonTokenType.EndObject)
                            {
                                reader.Read();
                            }
                            if (reader.TokenType == JsonTokenType.PropertyName)
                            {
                                if (reader.GetString() == "url")
                                {
                                    reader.Read();
                                    memeUrl = reader.GetString()!;
                                    break;
                                }
                                reader.Read();
                            }
                        }
                    }
                }
                if (reader.TokenType == JsonTokenType.EndArray)
                    break;
            }

            reader.Read(); var msg = reader.GetString();
            var userId = 0L;
            var userName = string.Empty;
            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.StartArray)
                {
                    reader.Read(); userId = reader.GetInt64();
                    reader.Read(); userName = reader.GetString();
                }

                if (reader.TokenType == JsonTokenType.EndArray)
                    break;
            }
            var fansTag = string.Empty;
            var fansLevel = 0;
            var fansUserId = 0L;
            var fansUserName = string.Empty;
            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.StartArray)
                {
                    var idx = 0;
                    while (reader.Read())
                    {
                        if (reader.TokenType == JsonTokenType.EndArray)
                            break;
                        else
                        {
                            switch (idx++)
                            {
                                case 0: fansLevel = reader.GetInt32(); break;
                                case 1: fansTag = reader.GetString(); break;
                                case 2: fansUserName = reader.GetString(); break;
                                case 12: fansUserId = reader.GetInt64(); break;
                                default:
                                    break;
                            }
                        }
                    }
                }

                if (reader.TokenType == JsonTokenType.EndArray)
                    break;
            }

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndArray)
                    break;
            }
            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndArray)
                    break;
            }
            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndArray)
                    break;
            }
            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndArray)
                    break;
            }

            return new DanmuMsg()
            {
                Msg = msg!,
                UserId = userId,
                UserName = userName!,
                SentAt = DateTimeOffset.FromUnixTimeMilliseconds(sentAt),
                FansLevel = fansLevel,
                FansTag = fansTag!,
                FansTagUserId = fansUserId,
                FansTagUserName = fansUserName!,
                MemeUrl = memeUrl,
                HexColor = hexColor,
            };
        }

        public override void Write(Utf8JsonWriter writer, DanmuMsg value, JsonSerializerOptions options)
        {
            // unsupported
            writer.WriteStartArray();
            writer.WriteEndArray();
        }
    }
}
