namespace Newtonsoft.Json.FSharp

open System
open Microsoft.FSharp.Reflection

open Newtonsoft.Json

/// Converter converting tuples to arrays
type TupleArrayConverter() =
  inherit JsonConverter()

  override x.CanConvert t =
    FSharpType.IsTuple t

  override x.WriteJson(writer, value, serialiser) =
    let values = FSharpValue.GetTupleFields(value)
    serialiser.Serialize(writer, values)

  override x.ReadJson(reader, t, _, serialiser) =
    let read, readProp, req = (read, readProp, require) $ reader

    let itemTypes = FSharpType.GetTupleElements t

    let readElements () =
      let rec iter index acc =
        match reader.TokenType with
        | JsonToken.EndArray ->
          read JsonToken.EndArray |> req |> ignore

          acc

        | JsonToken.StartObject ->

          let value = serialiser.Deserialize (reader, itemTypes.[index])
          read JsonToken.EndObject |> req |> ignore

          iter (index + 1) (acc @ [value])

        | JsonToken.Boolean
        | JsonToken.Bytes
        | JsonToken.Date
        | JsonToken.Float
        | JsonToken.Integer
        | JsonToken.String ->

          let value = serialiser.Deserialize(reader, itemTypes.[index])
          reader.Read () |> ignore

          iter (index + 1) (acc @ [value])

        | JsonToken.Null ->
          failwithf "While JS tolerates nulls, F# services don't - null found at '%s'"
                    reader.Path

        | _ as token ->
          failwithf "TupleArray: Unknown intermediate token '%A' at path '%s'"
                    token
                    reader.Path

      reader.Read () |> ignore
      iter 0 List.empty

    match reader.TokenType with
    | JsonToken.StartArray ->
      let values = readElements()
      FSharpValue.MakeTuple (values |> List.toArray, t)

    | _ ->
      failwithf "TupleArray: invalid END token '%A' at path: '%s'"
                reader.TokenType
                reader.Path