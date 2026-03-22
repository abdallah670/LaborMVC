using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using LaborBLL.Service.Abstract;
using Microsoft.AspNetCore.Hosting;

namespace LaborBLL.Service.Implementation
{
    public class RedesignService : IRedesignService
    {
        private readonly IWebHostEnvironment _env;

        public RedesignService(IWebHostEnvironment env)
        {
            _env = env;
        }

        public async Task<RedesignProgressViewModel> GetProgressAsync()
        {
            var viewModel = new RedesignProgressViewModel();
            var filePath = Path.Combine(_env.ContentRootPath, "UI_REDESIGN_TASKS.md");

            if (!File.Exists(filePath))
            {
                return viewModel;
            }

            var content = await File.ReadAllLinesAsync(filePath);
            RedesignPhaseViewModel currentPhase = null;
            RedesignTaskViewModel currentTask = null;

            foreach (var line in content)
            {
                // Parse Overall Progress
                if (line.Contains("Overall Progress:"))
                {
                    var match = Regex.Match(line, @"\d+");
                    if (match.Success) viewModel.OverallProgress = int.Parse(match.Value);
                }

                // Parse Phases (## Phase X: Title)
                var phaseMatch = Regex.Match(line, @"^##\s+(?:.*)\s+Phase\s+(\d+):\s+(.*)");
                if (phaseMatch.Success)
                {
                    currentPhase = new RedesignPhaseViewModel
                    {
                        Title = phaseMatch.Groups[2].Value.Trim(),
                        Status = "Pending",
                        Progress = 0
                    };
                    viewModel.Phases.Add(currentPhase);
                    currentTask = null;
                }

                // Parse Phase Table (Progress/Status)
                if (currentPhase != null && line.StartsWith("| **Phase"))
                {
                    var parts = line.Split('|').Select(p => p.Trim()).ToList();
                    if (parts.Count >= 5)
                    {
                        var phaseTitlePart = parts[1].Replace("**", "").Trim();
                        var phaseInList = viewModel.Phases.FirstOrDefault(p => phaseTitlePart.Contains(p.Title) || p.Title.Contains(phaseTitlePart));
                        if (phaseInList != null)
                        {
                            phaseInList.Status = parts[3];
                            var progressMatch = Regex.Match(parts[4], @"\d+");
                            if (progressMatch.Success) phaseInList.Progress = int.Parse(progressMatch.Value);
                        }
                    }
                }

                // Parse Task Header (### X.X Title)
                var taskMatch = Regex.Match(line, @"^###\s+(\d+\.\d+)\s+(.*)");
                if (taskMatch.Success && currentPhase != null)
                {
                    currentTask = new RedesignTaskViewModel
                    {
                        Title = taskMatch.Groups[2].Value.Trim()
                    };
                    currentPhase.Tasks.Add(currentTask);
                }

                // Parse Task Details
                if (currentTask != null)
                {
                    if (line.StartsWith("- **Category**:")) currentTask.Category = line.Split(':').Last().Trim();
                    if (line.StartsWith("- **Priority**:")) currentTask.Priority = line.Split(':').Last().Trim();
                    if (line.StartsWith("- **Description**:")) currentTask.Description = line.Split(':').Last().Trim();
                    if (line.StartsWith("- **Status**:")) currentTask.Status = line.Split(':').Last().Trim();
                    if (line.Trim().StartsWith("-") && !line.Contains("**"))
                    {
                        currentTask.DoneCriteria.Add(line.Trim().TrimStart('-').Trim());
                    }
                }
            }

            // Sync overall counts
            var allTasks = viewModel.Phases.SelectMany(p => p.Tasks).ToList();
            viewModel.TotalTasks = allTasks.Count;
            viewModel.CompletedTasks = allTasks.Count(t => t.IsCompleted);
            
            if (viewModel.TotalTasks > 0 && viewModel.OverallProgress == 0)
            {
                viewModel.OverallProgress = (int)((double)viewModel.CompletedTasks / viewModel.TotalTasks * 100);
            }

            return viewModel;
        }
    }
}
