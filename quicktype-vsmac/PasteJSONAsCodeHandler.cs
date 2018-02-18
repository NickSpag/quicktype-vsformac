using System.IO;
using System.Windows.Forms;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide;
using System.Diagnostics;
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Editor;
using System.Linq;
using System.Threading.Tasks;
using System;
using AppKit;
using Mono.Cecil;
using Mono.Addins;
using Mono.Addins.Database;

namespace quicktypevsmac
{
    public class PasteJSONAsCodeHandler : CommandHandler
    {
        private static string executablePath
        {
            get
            {
                var addInPath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                return Path.Combine(addInPath, "Resources", "quicktype");
            }
        }

        public PasteJSONAsCodeHandler()
        {
        }

        private Process PrepareQuickTypeProcess(string arguments)
        {
            var process = new Process();
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.FileName = executablePath;
            process.StartInfo.Arguments = arguments;

            return process;
        }

        private void ShowMessage(string message)
        {
            IdeApp.Workbench.StatusBar.ShowMessage("QuickType: " + message);
        }

        private string WriteJsonToFile(string text)
        {
            var jsonFilename = Path.GetTempFileName();
            File.WriteAllText(jsonFilename, text);

            return jsonFilename;
        }

        #region CommandHandler Methods
        protected async override void Run()
        {
            ShowMessage("Converting...");

            var activeDocument = IdeApp.Workbench.ActiveDocument;

            var language = activeDocument.GetLanguageItem(0, out DocumentRegion region).Language;

            Help.AlignNamingStyles(ref language);

            if (!Help.SupportedLanguages.Contains(language))
            {
                ShowMessage("Cannot process JSON. Unsupported language");
                return;
            }

            var pasteboard = NSPasteboard.GeneralPasteboard.PasteboardItems[0];

            var jsonText = pasteboard.GetStringForType("public.utf8-plain-text");
            System.Console.WriteLine(jsonText);

            if (string.IsNullOrEmpty(jsonText))
            {
                ShowMessage("Cannot process JSON. Clipboard is empty");
                return;
            }

            //todo split into separate methods below
            var jsonFileName = await Task.Run(() => WriteJsonToFile(jsonText));

            var topLevelFileName = activeDocument.FileName.FileNameWithoutExtension;
            var process = PrepareQuickTypeProcess("--lang \"" + language + "\" --top-level \"" + topLevelFileName + "\" \"" + jsonFileName + "\"");

            try
            {
                process.Start();
                string output = await process.StandardOutput.ReadToEndAsync();

                process.WaitForExit();
                if (process.ExitCode != 0)
                {
                    string error = await process.StandardError.ReadToEndAsync();
                    ShowMessage($"Connot process JSON. {error}");
                    return;
                }

                ShowMessage("JSON Processed. Pasting...");
                activeDocument.Editor.InsertAtCaret(output);
            }
            catch (Exception ex)
            {
                System.Console.WriteLine(ex.Message);
            }
        }

        protected override void Update(CommandInfo info)
        {
            if (IdeApp.Workbench.ActiveDocument?.Editor != null && Clipboard.ContainsText())
            {
                info.Enabled = true;
            }
            else
            {
                info.Enabled = false;
            }
        }
        #endregion




    }
}
