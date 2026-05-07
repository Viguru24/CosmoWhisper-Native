using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using CosmoWhisper.Planning;
using CosmoWhisper.FileSystem;
using CosmoWhisper.Services;

namespace CosmoWhisper.Manus
{
    public class ManusAgent
    {
        public static ManusAgent Shared { get; } = new ManusAgent();

        public event Action<string>? ManusStatusChanged;
        public event Action<string>? ManusResponseReceived;

        private readonly PlanningTool _planningTool = new();
        private readonly LocalFileOperator _fileOperator = new();

        public string Name => "Manus-Windows";

        public async Task<string> ProcessTask(string userRequest)
        {
            return await Task.Run(async () =>
            {
                try
                {
                    ManusStatusChanged?.Invoke("MANUS: Thinking...");

                    string systemPrompt = @"You are Manus, the project manager agent. 
Analyze the user request. If they want to CREATE or MANAGE a plan, output a specific command line starting with 'CMD:'.
Formats:
CMD: create | [id] | [title] | [step1,step2,step3...]
CMD: update | [id] | [title] | [step1,step2...]
CMD: mark_step | [id] | [stepIndex] | [status: completed/in_progress/blocked/not_started] | [optional_notes]
CMD: list
CMD: get | [id]
If it's just a question, answer it directly without CMD.";

                    var response = await AIService.Shared.ProcessCommand(systemPrompt, userRequest, usePersonality: false, useInterfaceLanguage: false);
                    string finalResult = response;

                    if (response.Contains("CMD:"))
                    {
                        var resultBuilder = new System.Text.StringBuilder();
                        var lines = response.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

                        foreach (var line in lines)
                        {
                            if (line.Trim().StartsWith("CMD:"))
                            {
                                var cmdParts = line.Substring(4).Split('|');
                                var command = cmdParts[0].Trim().ToLower();
                                var id = cmdParts.Length > 1 ? cmdParts[1].Trim() : null;

                                if (command == "mark_step")
                                {
                                    int index = (cmdParts.Length > 2 && int.TryParse(cmdParts[2], out int idx)) ? idx : -1;
                                    string status = cmdParts.Length > 3 ? cmdParts[3].Trim() : "completed";
                                    string notes = cmdParts.Length > 4 ? cmdParts[4].Trim() : "";
                                    resultBuilder.AppendLine(_planningTool.Execute(command, id, stepIndex: index, stepStatus: status, stepNotes: notes));
                                }
                                else
                                {
                                    var title = cmdParts.Length > 2 ? cmdParts[2].Trim() : null;
                                    var steps = cmdParts.Length > 3 ? cmdParts[3].Split(',').Select(s => s.Trim()).ToList() : null;
                                    resultBuilder.AppendLine(_planningTool.Execute(command, id, title: title, steps: steps));
                                }
                            }
                            else { resultBuilder.AppendLine(line); }
                        }
                        finalResult = resultBuilder.ToString();
                    }

                    ManusStatusChanged?.Invoke("MANUS: Ready");
                    ManusResponseReceived?.Invoke(finalResult);
                    return finalResult;
                }
                catch (Exception ex)
                {
                    ManusStatusChanged?.Invoke("MANUS: Error");
                    return $"Error processing task: {ex.Message}";
                }
            });
        }
    }
}


