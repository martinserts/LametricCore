namespace Newtonsoft.Json.FSharp

open System
open System.IO
open Microsoft.FSharp.Reflection

open Newtonsoft.Json

type DuConverter() = 
    inherit JsonConverter()
    
    override __.WriteJson(writer, value, serializer) = 
        let unionType = value.GetType()
        let unionCases = FSharpType.GetUnionCases(unionType)
        let case, fields = FSharpValue.GetUnionFields(value, unionType)
        let allCasesHaveValues = unionCases |> Seq.forall (fun c -> c.GetFields() |> Seq.length > 0)
        
        let distinctCases = unionCases |> Seq.distinctBy (fun c->c.GetFields() |> Seq.map (fun f-> f.DeclaringType))
        let hasAmbigious = (distinctCases |> Seq.length) <> (unionCases |> Seq.length)

        let allSingle = unionCases |> Seq.forall (fun c -> c.GetFields() |> Seq.length = 1)

        let convertLowerCase = unionType.GetCustomAttributes(typedefof<LowerCaseAttribute>, true) |> Array.isEmpty |> not
        let caseName = if convertLowerCase then case.Name.ToLowerInvariant() else case.Name;

        match allSingle,fields with
        | _,[||] -> writer.WriteRawValue(sprintf "\"%s\"" caseName)
        | true,[| singleValue |] -> serializer.Serialize(writer,singleValue)
        | false,values -> 
            writer.WriteStartObject()
            writer.WritePropertyName "Case"
            writer.WriteRawValue(sprintf "\"%s\"" caseName)
            let valuesCount = Seq.length values
            for i in 1 .. valuesCount do
                let itemName = sprintf "Item%i" i
                writer.WritePropertyName itemName
                serializer.Serialize(writer,values.[i-1])
            writer.WriteEndObject()
        | _,_ -> failwith "Handle this new case"
            
            
            
            
    override __.ReadJson(reader, destinationType, existingValue, serializer) = 
        let parts = 
            if reader.TokenType <> JsonToken.StartObject then [| (JsonToken.Undefined, obj()), (reader.TokenType, reader.Value) |]
            else 
                seq { 
                    yield! reader |> Seq.unfold (fun reader -> 
                                         if reader.Read() then Some((reader.TokenType, reader.Value), reader)
                                         else None)
                }
                |> Seq.takeWhile(fun (token, _) -> token <> JsonToken.EndObject)
                |> Seq.pairwise
                |> Seq.mapi (fun id value -> id, value)
                |> Seq.filter (fun (id, _) -> id % 2 = 0)
                |> Seq.map snd
                |> Seq.toArray

        //get simplified key value collection
        let fieldsValues = 
            parts
                |> Seq.map (fun ((_, fieldName), (fieldType,fieldValue)) -> fieldName,fieldType,fieldValue)
                |> Seq.toArray
        //all cases of the targe discriminated union
        let unionCases = FSharpType.GetUnionCases(destinationType)
        
        //the first simple case - this DU contains just simple values - as enum - get the value
        let _,_,firstFieldValue = fieldsValues.[0]
        let foundDirectCase = unionCases |> Seq.tryFind (fun uc -> uc.Name = (firstFieldValue.ToString()))

        let jsonToValue valueType value = 
            match valueType with
                                | JsonToken.Date -> 
                                    let dateTimeValue = Convert.ToDateTime(value :> Object)
                                    dateTimeValue.ToString("o")
                                | _ -> value.ToString()

        match foundDirectCase with
            | Some case -> FSharpValue.MakeUnion(case,[||])
            | None ->
                //this is the second case - this disc union is not of simple value - it may be records or multiple values
                let reconstructedJson = (Seq.fold (fun acc (name,valueType,value) -> acc + String.Format("\t\"{0}\":\"{1}\",\n",name,(jsonToValue valueType value))) "{\n" fieldsValues) + "}"

                //if it is a record lets try to find the case by looking at the present fields
                let implicitCase = unionCases |> Seq.tryPick (fun uc -> 
                    //if the case of the discriminated union is a record then this case will contain just one field which will be the record
                    let ucDef = uc.GetFields() |> Seq.head
                    //we need the get the record type and look at the fields
                    let recordType = ucDef.PropertyType
                    let recordFields = recordType.GetProperties()
                    let matched = fieldsValues |> Seq.forall ( fun (fieldName,_,fieldValue) -> 
                        recordFields |> Array.exists(fun f-> f.Name = (fieldName :?> string))
                    )    
                    //if we have found a match onthe record let's keep the union case and type of the record
                    match matched with
                        | true -> Some (uc,recordType)
                        | false -> None
                )

                match implicitCase with
                    | Some (case,recordType) -> 
                        use stringReader = new StringReader(reconstructedJson)
                        use jsonReader = new JsonTextReader(stringReader)
                        //creating the record - Json.NET can handle that already
                        let unionCaseValue = serializer.Deserialize(jsonReader,recordType)
                        //convert the record to the parent discrimianted union
                        let parentDUValue = FSharpValue.MakeUnion(case,[|unionCaseValue|])
                        parentDUValue
                    | None -> failwith "can't find such disc union type"

    override __.CanConvert(objectType) = 
        FSharpType.IsUnion objectType && 
        not (objectType.IsGenericType  && objectType.GetGenericTypeDefinition() = typedefof<list<_>>) &&
        not (objectType.IsGenericType  && objectType.GetGenericTypeDefinition() = typedefof<option<_>>) &&
        not (FSharpType.IsRecord objectType)