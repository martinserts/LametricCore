﻿namespace Newtonsoft.Json.FSharp

open System
open Microsoft.FSharp.Reflection
open Microsoft.FSharp.Collections

open Newtonsoft.Json

/// Discriminated union converter
type UnionConverter() =
  inherit JsonConverter()

  override x.CanConvert t =
    let is_list = t.IsGenericType && typedefof<List<_>>.Equals (t.GetGenericTypeDefinition())

    let canConvert =
      // leave lists to the list converter
      not is_list &&
      // otherwise we can convert union types nicely
      (FSharpType.IsUnion t || (t.DeclaringType <> null && FSharpType.IsUnion (t.DeclaringType)))

    canConvert

  override x.WriteJson(writer, value, serializer) =
    let t = value.GetType()
    let caseInfo, fieldValues = FSharpValue.GetUnionFields(value, t)
    writer.WriteStartObject()
    writer.WritePropertyName("_name")
    writer.WriteValue(TypeNaming.name value t)
    writer.WritePropertyName(caseInfo.Name)
    let value =
      match fieldValues.Length with
      | 0 -> null
      | 1 -> fieldValues.[0]
      | _ -> fieldValues :> obj
    serializer.Serialize(writer, value)
    writer.WriteEndObject()

  override x.ReadJson(reader, t, _, serializer) =
    let t = if FSharpType.IsUnion t then t else t.DeclaringType
    let read, readProp, req = (read, readProp, require) $ reader

    read JsonToken.StartObject |> req |> ignore

    readProp "_name" |> ignore

    let urnType =
      read JsonToken.String |> require<string> reader (* e.g. urn:Newtonsoft.Json.FSharp.Tests:A|D1 *)

    let typeData = TypeNaming.parse urnType
    let caseName =
      typeData.CaseName
      |> Option.map box
      |> require<string> reader

    readProp caseName |> req |> ignore

    let caseInfo =
      FSharpType.GetUnionCases t
      |> Seq.find (fun c -> c.Name = caseName)

    let fields = caseInfo.GetFields()

    let toUnion args =
      FSharpValue.MakeUnion(caseInfo, args)

    match fields.Length with
    | 0 ->
      read JsonToken.Null |> req |> ignore // null because there are no args
      FSharpValue.MakeUnion(caseInfo, [||])

    | 1 -> 
      let res = [| serializer.Deserialize(reader, fields.[0].PropertyType) |]
      reader.Read() |> ignore // at end of some object
      res |> toUnion

    | _ ->

      let tupleType =
        fields
        |> Seq.map (fun f -> f.PropertyType)
        |> Seq.toArray
        |> FSharpType.MakeTupleType

      // array consumes end array token
      let tuple = serializer.Deserialize(reader, tupleType)
      FSharpValue.GetTupleFields tuple |> toUnion

// example:

// {
//   "_name": "urn:Newtonsoft.Json.FSharp.Tests:A|D1",
//  "D1": ["hello",-2]
// }
// {
//  "_name": "urn:Newtonsoft.Json.FSharp.Tests:A|D2",
//  "D2": {
//    "_name": "urn:Newtonsoft.Json.FSharp.Tests:B|E1",
//    "E1": "00000000-0000-0000-0000-000000000000"
//  }
// }
