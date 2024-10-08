﻿open System
open System.Drawing
open System.Drawing.Imaging
open System.IO

let borderSizePercentage = 0.05

let getImageFormat (extension: string) =
    match extension.ToLower() with
    | ".jpg"
    | ".jpeg" -> ImageFormat.Jpeg
    | ".png" -> ImageFormat.Png
    | ext -> failwithf "Only PNG and JPG files are supported. Given file extension: %s" ext

let saveImage (newImage: Image) (path: string) (format: ImageFormat) =
    let saveAsJpeg () =
        let jpegCodec =
            ImageCodecInfo.GetImageDecoders()
            |> Array.find (fun codec -> codec.FormatID = ImageFormat.Jpeg.Guid)

        use encoderParams = new EncoderParameters(1)
        use encoderParam = new EncoderParameter(Encoder.Quality, 100L)
        encoderParams.Param.[0] <- encoderParam

        newImage.Save(path, jpegCodec, encoderParams)

    let saveActions =
        match format with
        | f when f = ImageFormat.Jpeg -> saveAsJpeg
        | _ -> (fun () -> newImage.Save(path, format))

    saveActions ()

let addWhiteBorderAndReplace (inputFile: string) =
    let format = getImageFormat (Path.GetExtension(inputFile))
    use image = Image.FromFile(inputFile)

    let borderSize =
        int (
            float (min image.Width image.Height)
            * borderSizePercentage
        )

    let newWidth = image.Width + (2 * borderSize)
    let newHeight = image.Height + (2 * borderSize)

    use newImage = new Bitmap(newWidth, newHeight)
    use graphics = Graphics.FromImage(newImage)
    graphics.Clear(Color.White)

    graphics.DrawImage(image, borderSize, borderSize, image.Width, image.Height)

    if format = ImageFormat.Jpeg then
        image.PropertyItems
        |> Array.iter newImage.SetPropertyItem

    image.Dispose()

    let tempFileName = Path.GetRandomFileName()
    let tempFilePath = Path.Combine(Path.GetDirectoryName(inputFile), tempFileName)
    saveImage newImage tempFilePath format
    File.Replace(tempFilePath, inputFile, null)



[<EntryPoint>]
let main argv =
    match argv with
    | [| filePath |] ->
        try
            addWhiteBorderAndReplace filePath
            printfn "White border added to the file: %s" filePath
            0
        with
        | :? FileNotFoundException ->
            printfn "File not found: %s" filePath
            1
        | ex ->
            printfn "Error processing file: %s" ex.Message
            1
    | _ ->
        printfn "Please provide the path to the file as an argument."
        1
