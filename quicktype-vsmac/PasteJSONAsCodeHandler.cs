using System.IO;
using System.Windows.Forms;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide;
using System.Diagnostics;
using System.Threading.Tasks;
using System;
using AppKit;

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

        private Process PrepareQuickTypeProcess1(string arguments)
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

        private async Task<Process> PrepareQuickTypeProcess(string language, string jsonText, string newTypeName)
        {
            string jsonFileName;

            try
            {
                jsonFileName = await Task.Run(() => WriteJsonToFile(jsonText));
            }
            catch (Exception ex)
            {
                System.Console.WriteLine(ex.Message);
                return new Process();
            }

            var process = new Process();
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.FileName = executablePath;
            process.StartInfo.Arguments = $"--lang \"{language}\" --top-level \"{newTypeName}\" \"" + jsonFileName + "\"";

            return process;
        }

        private string WriteJsonToFile(string text)
        {
            var jsonFilename = Path.GetTempFileName();
            File.WriteAllText(jsonFilename, text);

            return jsonFilename;
        }

        private string GetRecentClipboardText()
        {
            var pasteboard = NSPasteboard.GeneralPasteboard.PasteboardItems[0];

            var jsonText = pasteboard.GetStringForType("public.utf8-plain-text");

            return jsonText;
        }

        private void ShowMessage(string message)
        {
            IdeApp.Workbench.StatusBar.ShowMessage("Pasting JSON as Code: " + message);
        }

        private void ShowErrorMessage(string message)
        {
            IdeApp.Workbench.StatusBar.ShowError("Pasting JSON Error: " + message);
        }

        private async Task ClearMessage()
        {
            await Task.Delay(TimeSpan.FromSeconds(1.5));
            IdeApp.Workbench.StatusBar.ShowReady();
        }

        #region CommandHandler Methods
        protected async override void Run()
        {
            ShowMessage("Preparing...");

            var activeDocument = IdeApp.Workbench.ActiveDocument;

            if (!Help.FindSupportedLanguage(activeDocument, out string language))
            {
                ShowErrorMessage("Unsupported language");
                return;
            }

            var jsonText = GetRecentClipboardText();

            if (string.IsNullOrEmpty(jsonText))
            {
                ShowErrorMessage(" Clipboard is empty");
                return;
            }

            var process = await PrepareQuickTypeProcess(language,
                                                        jsonText,
                                                        //Use the file name of the active document for the new JSON type's name
                                                        activeDocument.FileName.FileNameWithoutExtension);

            try
            {
                process.Start();
                string output = await process.StandardOutput.ReadToEndAsync();

                process.WaitForExit();
                if (process.ExitCode != 0)
                {
                    string error = await process.StandardError.ReadToEndAsync();
                    ShowErrorMessage($"{error}");
                    return;
                }

                ShowMessage("Success");
                activeDocument.Editor.InsertAtCaret(output);
            }
            catch (Exception ex)
            {
                ShowErrorMessage(ex.Message);
            }

            await ClearMessage();
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
