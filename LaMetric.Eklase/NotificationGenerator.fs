namespace LaMetric.Eklase

module NotificationGenerator =

    open Chessie.ErrorHandling
    open NodaTime
    open NodaTime.Extensions
    open FSharpx.Option
    open Newtonsoft.Json
    open Newtonsoft.Json.FSharp

    open DataTypes
    open LaMetric.Common.DataTypes
    open LaMetric.Common.Icons
 

    let getToday () =
        SystemClock.Instance.InTzdbSystemDefaultZone().GetCurrentDate()

    let serializeObject obj = 
        let settings = new JsonSerializerSettings() |> Serialisation.extend
        JsonConvert.SerializeObject(obj, settings)

    let getFullDiaryUrl diaryUrl (today: LocalDate) =
        match today.DayOfWeek with
        | IsoDayOfWeek.Saturday | IsoDayOfWeek.Sunday -> let monday = today.Next(IsoDayOfWeek.Monday)
                                                         let formattedMonday = NodaTime.Text.LocalDatePattern.CreateWithInvariantCulture("dd.MM.yyyy.").Format(monday)
                                                         sprintf "%s?Date=%s" diaryUrl formattedMonday
        | _ -> diaryUrl

    let fetchParsedDiary loginUrl diaryUrl markUrl logoutUrl username password =
        let scoreUpgrader score =
            match ScoreUpgrader.upgradeScore markUrl score with
            | Pass upgradedScore -> upgradedScore
            | _ -> score

        EklaseWebClient.login loginUrl username password |> ignore
        let diaryPage = EklaseWebClient.fetchPage diaryUrl
        let result = LessonDayParser.extractLessonDaysFromHtml scoreUpgrader diaryPage
        EklaseWebClient.fetchPage logoutUrl |> ignore
        result

    let generateFrames (lessonDays: LessonDay list) today =
        let generateScoreFrames scores =
            let createScoreNotification (lessonDetails, score) =
                let createScoreNotificationFrame scoreText text =
                    let transform = dict [
                                        ("+", "a10217");
                                        ("-", "a10218");
                                        ("/", "a10219");
                                        ("1", "a10220");
                                        ("2", "a10222");
                                        ("3", "a10224");
                                        ("4", "a10226");
                                        ("5", "a10228");
                                        ("6", "a10230");
                                        ("7", "a10232");
                                        ("8", "a10234");
                                        ("9", "a10236");
                                        ("10", "a10238");
                                        ("i", "a13606");
                                        ("ni", "a34878");
                                    ]

                    NotificationSimpleFrame {
                        index = Option.None;
                        icon = IconLoader.loadSimpleIcon transform scoreText;
                        text = text;
                    }

                let formattedScoreDescription = match score with
                                                | ScoreReference r -> r.Display
                                                | ScoreDetails d -> sprintf "[%s] %s (%s)" d.Mark d.Description  d.Author
                let text = sprintf "%s: %s" lessonDetails.Name formattedScoreDescription

                createScoreNotificationFrame (score.Display()) text
            
            scores |> Seq.map createScoreNotification


        let generateHomeWorkFrames today =
            let nextSchoolDay = lessonDays
                                |> List.filter (fun d -> d.Date > today)
                                |> List.sortBy (fun d -> d.Date)
                                |> List.tryHead

            let createHomeWorkNotification lesson =
                let createHomeWorkNotificationFrame index text =
                    let transform = dict [
                                        ("0", "i10240");
                                        ("1", "i10221");
                                        ("2", "i10223");
                                        ("3", "i10225");
                                        ("4", "i10227");
                                        ("5", "i10229");
                                        ("6", "i10231");
                                        ("7", "i10233");
                                        ("8", "i10235");
                                        ("9", "i10237");
                                        ("10", "i10239");
                                    ]

                    let indexString = Option.defaultValue 0 index |> sprintf "%d"

                    NotificationSimpleFrame {
                        index = Option.None;
                        icon = IconLoader.loadSimpleIcon transform indexString;
                        text = text;
                    }

                let formatHomeWork homeWorkDetails =
                    match homeWorkDetails.Author with
                    | Some a -> sprintf "%s (%s)" homeWorkDetails.Description a
                    | Option.None -> sprintf "%s" homeWorkDetails.Description

                let lessonText = match lesson with
                                 | Lesson d -> maybe {
                                                let! homeWork = d.HomeWork;
                                                let formattedHomeWork = formatHomeWork homeWork
                                                return sprintf "%s: %s" d.Name formattedHomeWork
                                                }
                                 | HomeWork h -> formatHomeWork h |> Some

                let lessonIndex = match lesson with
                                  | Lesson d -> Some d.Index
                                  | HomeWork _ -> Option.None
                

                maybe {
                    let! text = lessonText
                    return createHomeWorkNotificationFrame lessonIndex text 
                }

            match nextSchoolDay with
            | Some lessons -> lessons.Lessons |> Seq.choose createHomeWorkNotification
            | Option.None -> Seq.empty

        let scores = lessonDays
                    |> List.filter (fun d -> d.Date <= today)
                    |> List.sortByDescending (fun d -> d.Date)
                    |> List.truncate 2
                    |> List.map (fun d -> d.Lessons)
                    |> Seq.concat
                    |> Seq.choose (fun e -> match e with
                                            | Lesson l -> Some (l, l.Scores)
                                            | HomeWork _ -> Option.None
                                    )
                    |> Seq.collect (fun (lesson, scores) -> scores |> Array.map (fun score -> (lesson, score)))
                    |> List.ofSeq

        let scoresFrames = generateScoreFrames scores
        let homeWorkFrames = generateHomeWorkFrames today |> List.ofSeq
        let nonEmptyHomeWorkFrames = match homeWorkFrames with
                                     | [] -> [ NotificationSimpleFrame {
                                                    index = Option.None;
                                                    icon = IconId "a87" |> Some; // Smile
                                                    text = "Nekas nav uzdots";
                                                }
                                             ]
                                     | _ -> homeWorkFrames



        scoresFrames |> Seq.append <| nonEmptyHomeWorkFrames
                     |> List.ofSeq

    let generateNotification (lessonDays: LessonDay list) today = 

        let frames = generateFrames lessonDays today

        {
            priority = Some NotificationPriority.Info;
            icon_type = Some NotificationIconType.None;
            lifeTime = Option.None;
            model =
            {
                frames = frames;
                sound = Option.None;
                cycles = Option.None;
            }
        }

    let generateSerializedNotification lessonDays = 
        let notification = generateNotification lessonDays
        serializeObject notification

    let generateSerializedFrames lessonDays today = 
        let addFrameIndex index frame =
            match frame with
            | NotificationSimpleFrame f -> NotificationSimpleFrame { f with index = Some index }
            | NotificationGoalFrame f -> NotificationGoalFrame f
            | NotificationChartFrame f -> NotificationChartFrame f

        let frames = generateFrames lessonDays today
                     |> List.mapi addFrameIndex
        serializeObject { frames = frames; sound = Option.None; cycles = Option.None }
