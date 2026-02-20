using PdfSharp.Fonts;

public class SystemFontResolver : IFontResolver
{
    public byte[] GetFont(string faceName)
    {
        // Detecta o sistema operacional
        var isWindows = OperatingSystem.IsWindows();

        var fontPath = faceName.ToLower() switch
        {
            "arial" when isWindows =>
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "arial.ttf"),
            "arial-bold" when isWindows =>
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "arialbd.ttf"),

            // Linux usa Liberation Sans (similar ao Arial)
            "arial" => "/usr/share/fonts/truetype/liberation/LiberationSans-Regular.ttf",
            "arial-bold" => "/usr/share/fonts/truetype/liberation/LiberationSans-Bold.ttf",

            _ when isWindows =>
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "arial.ttf"),
            _ => "/usr/share/fonts/truetype/liberation/LiberationSans-Regular.ttf"
        };

        if (File.Exists(fontPath))
            return File.ReadAllBytes(fontPath);

        throw new FileNotFoundException($"Font file not found: {fontPath}. " +
            $"On Linux, install fonts with: apt-get install fonts-liberation");
    }

    public FontResolverInfo ResolveTypeface(string familyName, bool isBold, bool isItalic)
    {
        var fontName = familyName.ToLower() switch
        {
            "arial" when isBold => "arial-bold",
            _ => "arial"
        };

        return new FontResolverInfo(fontName);
    }
}