namespace LaMetric.Common

module Sound =
    open System
    open Newtonsoft.Json
    open Newtonsoft.Json.FSharp

    [<LowerCase>]
    type NotificationSoundId =
        | Bicycle
        | Car
        | Cash
        | Cat
        | Dog
        | Dog2
        | Energy
        | ``Knock-knock``
        | Letter_email
        | Lose1
        | Lose2
        | Negative1
        | Negative2
        | Negative3
        | Negative4
        | Negative5
        | Notification
        | Notification2
        | Notification3
        | Notification4
        | Open_door
        | Positive1
        | Positive2
        | Positive3
        | Positive4
        | Positive5
        | Positive6
        | Statistic
        | Thunder
        | Water1
        | Water2
        | Win
        | Win2
        | Wind
        | Wind_short
    
    [<LowerCase>]
    type AlarmSoundId =
        | Alarm1
        | Alarm2
        | Alarm3
        | Alarm4
        | Alarm5
        | Alarm6
        | Alarm7
        | Alarm8
        | Alarm9
        | Alarm10
        | Alarm11
        | Alarm12
        | Alarm13

    type NotificationSoundData =
        | Notification of NotificationSoundId
        | Alarm of AlarmSoundId

    [<JsonConverter(typedefof<NotificationSoundConverter>)>]
    type NotificationSound = {
        sound: NotificationSoundData;
        repeat: int option;
    }

    and NotificationSoundConverter() =
        inherit JsonConverter()

        let createSoundMap category id repeat =
            let soundMap = [("category", category); ("id", id)]

            match repeat with
            | Some x -> ("repeat", x :> Object) :: soundMap
            | None -> soundMap
        
        let getSoundProperties sound =
            match sound.sound with
            | Notification id -> "notifications", (id :> Object)
            | Alarm id -> "alarms", (id :> Object)

        let serializeSound sound =
            let category, id = getSoundProperties sound
            createSoundMap category id sound.repeat |> Map.ofList

        override this.CanConvert t =
            t.GetType() = typedefof<NotificationSound>;

        override x.WriteJson(writer : JsonWriter, o : obj, serialiser : JsonSerializer) =
            if o = null then nullArg "value"

            let sound = o :?> NotificationSound |> serializeSound
            serialiser.Serialize(writer, sound)

        override x.ReadJson(reader, objectType, existingValue, serialiser) =
            new NotImplementedException() |> raise

