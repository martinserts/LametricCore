namespace LaMetric.Kengaroos.Tests

module TestCommon =

    open System.Reflection
    open System.IO

    let extractEmbeddedPage fileName =
        use stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(fileName)
        match stream with
        | null -> failwith "Could not find embedded resource:" + fileName
        | _ -> use reader = new StreamReader(stream)
               reader.ReadToEnd()

