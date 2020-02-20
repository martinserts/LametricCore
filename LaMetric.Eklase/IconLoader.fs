namespace LaMetric.Eklase

module IconLoader =

    open System.Reflection
    open System.IO
    open System.Collections.Generic

    open LaMetric.Common.Icons

    type internal Marker = interface end

    let loadEmbeddedResource name =
        use stream = Assembly.GetAssembly(typeof<Marker>).GetManifestResourceStream(name)
        match stream with
        | null -> failwith ("Could not find embedded resource:" + name)
        | _ -> use ms = new MemoryStream()
               stream.CopyTo(ms)
               ms.ToArray()

    let loadPngBinaryIcon (transforms: IDictionary<string, string>) name =
        match transforms.ContainsKey(name) with
        | true -> Icon {
                    imageType = Png;
                    data = loadEmbeddedResource transforms.[name]
                  } |> Some
        | false -> Option.None

    let loadSimpleIcon (transforms: IDictionary<string, string>) name =
        match transforms.ContainsKey(name) with
        | true -> IconId transforms.[name] |> Some
        | false -> Option.None




