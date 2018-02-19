using MonoDevelop.Ide.Editor;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Content;
using System.Linq;

namespace quicktypevsmac
{
    public static class Help
    {
        private static readonly string[] SupportedLanguages =
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

        private static void AlignNamingStyles(ref string language)
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

        /// <summary>
        /// Finds the supported language. First checking by file extension to cover 95% of scenarios, before using MonoDevelop's 
        /// extension method that uses Microsoft.CodeAnalysis 
        /// </summary>
        /// <returns><c>true</c>, if supported language was found, <c>false</c> otherwise.</returns>
        /// <param name="activeDocument">Active document being edited.</param>
        /// <param name="language">Language keyword for use by the quicktype executable.</param>
        public static bool FindSupportedLanguage(Document activeDocument, out string language)
        {
            if (CheckExtensionForSupportedLanguage(activeDocument, out language))
            {
                return true;
            }


            if (activeDocument.GetLanguageItem(0, out DocumentRegion region)?.Language is string analyzedLanguage)
            {
                Help.AlignNamingStyles(ref analyzedLanguage);

                if (SupportedLanguages.Contains(analyzedLanguage))
                {
                    language = analyzedLanguage;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Checks the extension for supported language by the active document's file extension.
        /// </summary>
        /// <returns><c>true</c>, if supported language was found by file extension, <c>false</c> otherwise.</returns>
        /// <param name="activeDocument">Active document.</param>
        /// <param name="language">Language keyword for use by the quicktype executable..</param>
        private static bool CheckExtensionForSupportedLanguage(Document activeDocument, out string language)
        {

            switch (activeDocument.FileName.Extension)
            {
                case ".cs":
                    language = "csharp";
                    return true;
                case ".cc":
                case ".cpp":
                    language = "cpp";
                    return true;
                case ".ts":
                    language = "typescript";
                    return true;
                case null:
                default:
                    language = "";
                    return false;
            }
        }
    }
}
