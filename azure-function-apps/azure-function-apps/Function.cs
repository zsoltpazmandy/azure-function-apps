using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;

public static class Function
{
    private const string AZURE_STORAGE = "shipitzsolt9211_STORAGE";
    private const string INPUT_PATH = "funkytown/Evidence/{name}";
    private const string OUTPUT_PATH = "funkytown/MediumSize/";
    private const string OUTPUT_FILENAME = "{name}-medium.png";
    private const int MEDIUM_SIZE_IMAGE_THRESHOLD = 2000000;
    private const int MEDIUM_SIZE_IMAGE_WIDTH = 500;
    private const string MEDIUM_SIZE_IMAGE_FORMAT = "image/png";

    [FunctionName("InputOutputFunction")]
    public static void Run([BlobTrigger(INPUT_PATH, Connection = AZURE_STORAGE)] Stream input,
                           [Blob(OUTPUT_PATH + OUTPUT_FILENAME, Connection = AZURE_STORAGE)] Stream output,
                           string name, TraceWriter log)
    {
        if (input != null)
        {
            log.Info(input.ToString());
        }

        if (output != null)
        {
            log.Info(output.ToString());
        }

        if (input.Length < MEDIUM_SIZE_IMAGE_THRESHOLD)
        {
            return;
        }

        log.Info("Image size over 2MB, creating resized copy...");
        Image image = GetImage(input);
        Image mediumSizeImage = GetMediumSizeImage(image);
        Stream streamifiedImage = StreamifyImage(mediumSizeImage);
        log.Info("Saving resized copy to " + OUTPUT_PATH);
        Save(streamifiedImage, output);
    }

    private static Image GetImage(Stream input)
    {
        var bytes = StreamToByteArray(input);
        Image image = Image.FromStream(new MemoryStream(bytes));
        return image;
    }
    private static byte[] StreamToByteArray(Stream input)
    {
        byte[] buffer = new byte[16 * 1024];
        using (var memStream = new MemoryStream())
        {
            int read;
            while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
            {
                memStream.Write(buffer, 0, read);
            }
            return memStream.ToArray();
        }
    }

    private static Image GetMediumSizeImage(Image image)
    {
        Size originalSize = image.Size;
        float percent = (new System.Collections.Generic.List<float>
        {
            MEDIUM_SIZE_IMAGE_WIDTH / (float)originalSize.Width,
            MEDIUM_SIZE_IMAGE_WIDTH / (float)originalSize.Height
        }).Min();

        Size mediumSize = new Size((int)System.Math.Floor(originalSize.Width * percent), (int)System.Math.Floor(originalSize.Height * percent));
        return (Image)(new Bitmap(image, mediumSize));
    }

    private static Stream StreamifyImage(Image image)
    {
        ImageCodecInfo[] codecs = ImageCodecInfo.GetImageEncoders();
        int i = 0;
        for (i = 0; i < codecs.Length; i++)
        {
            if (codecs[i].MimeType == MEDIUM_SIZE_IMAGE_FORMAT) break;
        }

        EncoderParameter ratio = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, 10L);
        EncoderParameters CodecParams = new EncoderParameters(1);
        CodecParams.Param[0] = ratio;

        var stream = new System.IO.MemoryStream();
        image.Save(stream, codecs[i], CodecParams);
        stream.Position = 0;
        return stream;
    }

    private static void Save(Stream input, Stream output)
    {
        using (var memStream = new MemoryStream())
        {
            input.CopyTo(memStream);
            var bytes = memStream.ToArray();
            output.Write(bytes, 0, bytes.Length);
        }
    }
}
