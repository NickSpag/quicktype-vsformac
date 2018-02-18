namespace quicktypevsmac
{
    public static class Help
    {
        public static readonly string[] SupportedLanguages =
        {
            "c++",
            "cpp",
            "cplusplus",
            "cs",
            "csharp",
            "elm",
            "go",
            "golang",
            "java",
            "objc",
            "objective-c",
            "objectivec",
            "swift",
            "typescript",
            "ts",
            "tsx"
        };

        public static void AlignNamingStyles(ref string language)
        {

            if (language.Contains("#"))
            {
                language = language.Replace("#", "sharp")
                                   .ToLower();
            }
            else
            {
                language = language.ToLower();
            }

        }
    }
}
