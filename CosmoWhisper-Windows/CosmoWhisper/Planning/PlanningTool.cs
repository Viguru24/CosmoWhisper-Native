using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Text.Json;

namespace CosmoWhisper.Planning
{
    public class PlanStep
    {
        public string Title { get; set; } = "";
        public string Status { get; set; } = "not_started";
        public string Notes { get; set; } = "";
    }

    public class Plan
    {
        public string PlanId { get; set; } = "";
        public string Title { get; set; } = "";
        public List<PlanStep> Steps { get; set; } = new();
    }

    public class PlanningTool
    {
        private Dictionary<string, Plan> _plans = new();
        private string? _currentPlanId;
        private readonly string _storagePath;

        public PlanningTool()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            _storagePath = Path.Combine(appData, "CosmoWhisper", "plans.json");
            LoadPlans();
        }

        private void LoadPlans()
        {
            try
            {
                if (File.Exists(_storagePath))
                {
                    string json = File.ReadAllText(_storagePath);
                    var loaded = JsonSerializer.Deserialize<Dictionary<string, Plan>>(json);
                    if (loaded != null) { _plans = loaded; _currentPlanId = _plans.Keys.LastOrDefault(); }
                }
            } catch { }
        }

        private void SavePlans()
        {
            try
            {
                string directory = Path.GetDirectoryName(_storagePath)!;
                if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);
                File.WriteAllText(_storagePath, JsonSerializer.Serialize(_plans, new JsonSerializerOptions { WriteIndented = true }));
            } catch { }
        }

        public string Execute(string command, string? planId = null, string? title = null, List<string>? steps = null, int? stepIndex = null, string? stepStatus = null, string? stepNotes = null)
        {
            string result = command.ToLower() switch {
                "create" => CreatePlan(planId, title, steps),
                "update" => UpdatePlan(planId, title, steps),
                "list" => ListPlans(),
                "get" => GetPlan(planId),
                "mark_step" => MarkStep(planId, stepIndex, stepStatus, stepNotes),
                _ => $"Error: Unrecognized command {command}"
            };
            SavePlans();
            return result;
        }

        private string CreatePlan(string? planId, string? title, List<string>? steps)
        {
            if (string.IsNullOrEmpty(planId) || _plans.ContainsKey(planId) || string.IsNullOrEmpty(title) || steps == null) return "Error: Invalid Create Params";
            var plan = new Plan { PlanId = planId, Title = title, Steps = steps.Select(s => new PlanStep { Title = s }).ToList() };
            _plans[planId] = plan; _currentPlanId = planId;
            return $"Plan created: {planId}\n\n{FormatPlan(plan)}";
        }

        private string UpdatePlan(string? planId, string? title, List<string>? steps)
        {
            planId ??= _currentPlanId;
            if (planId == null || !_plans.ContainsKey(planId)) return "Error: Plan not found";
            var plan = _plans[planId];
            if (title != null) plan.Title = title;
            if (steps != null) plan.Steps = steps.Select((s, i) => new PlanStep { Title = s, Status = plan.Steps.ElementAtOrDefault(i)?.Status ?? "not_started" }).ToList();
            return $"Plan updated: {planId}\n\n{FormatPlan(plan)}";
        }

        private string ListPlans() => _plans.Count == 0 ? "No plans." : "Plans:\n" + string.Join("\n", _plans.Values.Select(p => $"• {p.PlanId}: {p.Title} ({p.Steps.Count(s => s.Status == "completed")}/{p.Steps.Count})"));

        private string GetPlan(string? planId) { planId ??= _currentPlanId; return (planId != null && _plans.ContainsKey(planId)) ? FormatPlan(_plans[planId]) : "Error: Not found"; }

        private string MarkStep(string? planId, int? index, string? status, string? notes)
        {
            planId ??= _currentPlanId;
            if (planId == null || !_plans.ContainsKey(planId) || index == null || index < 0 || index >= _plans[planId].Steps.Count) return "Error: Invalid Update";
            var step = _plans[planId].Steps[index.Value];
            if (status != null) step.Status = status; if (notes != null) step.Notes = notes;
            return $"Step {index} updated.\n\n{FormatPlan(_plans[planId])}";
        }

        private string FormatPlan(Plan plan)
        {
            var sb = new StringBuilder($"Plan: {plan.Title} ({plan.PlanId})\n==========\n");
            for (int i = 0; i < plan.Steps.Count; i++) {
                string ico = plan.Steps[i].Status switch { "completed" => "[✓]", "in_progress" => "[→]", "blocked" => "[!]", _ => "[ ]" };
                sb.AppendLine($"{i}. {ico} {plan.Steps[i].Title}");
            }
            return sb.ToString();
        }
    }
}
