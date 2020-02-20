namespace LaMetric.Eklase

module DataTypes =

    open NodaTime

    type ScoreDetails = {
        Id: int64;
        Display: string;
        Mark: string;
        Author: string;
        Created: LocalDateTime;
        Description: string;
    }

    type ScoreReference = {
        Id: int64;
        Display: string;
    }

    type Score =
    | ScoreReference of ScoreReference
    | ScoreDetails of ScoreDetails

        member this.Display () = 
            match this with
            | ScoreReference r -> r.Display
            | ScoreDetails d -> d.Mark

    type HomeWorkDetails = {
        Description: string;
        Author: string option;
        Created: LocalDateTime option;
    }

    type LessonDetails = {
        Index: int;
        Name: string;
        RoomNumber: string option;
        Theme: string option;
        HomeWork: HomeWorkDetails option;
        Scores: Score array;
    }

    type LessonEntry =
    | Lesson of LessonDetails
    | HomeWork of HomeWorkDetails

    type LessonDay = {
        Date: LocalDate;
        Lessons: LessonEntry array;
    }

