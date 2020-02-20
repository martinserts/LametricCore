namespace LaMetric.Common

module Icons =
    open System
    open Newtonsoft.Json

    type LaMetricImageType = Png | Gif

    let contentTypeMapping = dict [ (Png, "image/png"); (Gif, "image/gif"); ]

    type BinaryIcon = {
        imageType: LaMetricImageType;
        data: byte array;
    }

    [<JsonConverter(typedefof<NotificationIconConverter>)>]
    type NotificationIcon =
        | IconId of string
        | Icon of BinaryIcon
    
    and NotificationIconConverter() =
        inherit JsonConverter()

        let serializeIcon icon =
            match icon with
            | IconId id -> id
            | Icon binary -> sprintf "data:%s;base64,%s" contentTypeMapping.[binary.imageType] (Convert.ToBase64String(binary.data))

        override this.CanConvert t =
            t.GetType() = typedefof<NotificationIcon>;

        override x.WriteJson(writer : JsonWriter, o : obj, serialiser : JsonSerializer) =
            if o = null then nullArg "value"

            let icon = o :?> NotificationIcon |> serializeIcon
            serialiser.Serialize(writer, icon)

        override x.ReadJson(reader, objectType, existingValue, serialiser) =
            new NotImplementedException() |> raise

            
                

