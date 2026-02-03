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

                    var response = await AIService.Shared.ProcessCommand(systemPrompt, userRequest);
                    string finalResult = response;

                    if (response.Contains("CMD:"))
                    {
                        var resultBuilder = new System.Text.StringBuilder();
                        var lines = response.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                        
                        foreach (var line in lines)
                        {
                            if (line.Trim().StartsWith("CMD:"))
                            {
                                var parts = line.Substring(4).Split('|');
                                var command = parts[0].Trim().ToLower();
                                var id = parts.Length > 1 ? parts[1].Trim() : null;
                                
                                if (command == "mark_step")
                                {
                                    int index = (parts.Length > 2 && int.TryParse(parts[2], out int idx)) ? idx : -1;
                                    string status = parts.Length > 3 ? parts[3].Trim() : "completed";
                                    string notes = parts.Length > 4 ? parts[4].Trim() : "";
                                    resultBuilder.AppendLine(_planningTool.Execute(command, id, null, null, index, status, notes));
                                }
                                else
                                {
                                    var title = parts.Length > 2 ? parts[2].Trim() : null;
                                    var steps = parts.Length > 3 ? parts[3].Split(',').Select(s => s.Trim()).ToList() : null;
                                    resultBuilder.AppendLine(_planningTool.Execute(command, id, title, steps));
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
