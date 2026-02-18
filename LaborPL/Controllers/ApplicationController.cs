using LaborBLL.ModelVM;
using LaborBLL.Response;
using LaborBLL.Service.Abstract;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LaborPL.Controllers
{
    /// <summary>
    /// Controller for task application-related operations
    /// </summary>
    [Authorize]
    public class ApplicationController : Controller
    {
        private readonly IApplicationService _applicationService;
        private readonly ITaskService _taskService;
        private readonly ILogger<ApplicationController> _logger;

        public ApplicationController(
            IApplicationService applicationService,
            ITaskService taskService,
            ILogger<ApplicationController> logger)
        {
            _applicationService = applicationService;
            _taskService = taskService;
            _logger = logger;
        }

        /// <summary>
        /// Gets the current user's ID
        /// </summary>
        private string GetCurrentUserId()
        {
            return User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        }

        #region Create Application

        /// <summary>
        /// Displays the create application form
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Worker,Admin")]
        public async Task<IActionResult> Create(int taskId)
        {
            var userId = GetCurrentUserId();
            
            // Check if user can apply
            var canApplyResult = await _taskService.CanUserApplyAsync(taskId, userId);
            if (!canApplyResult.Success)
            {
                TempData["Error"] = canApplyResult.ErrorMessage;
                return RedirectToAction("Details", "Task", new { id = taskId });
            }

            // Get task details for the view
            var taskResult = await _taskService.GetTaskByIdAsync(taskId, userId);
            if (!taskResult.Success)
            {
                TempData["Error"] = taskResult.ErrorMessage;
                return RedirectToAction("Index", "Task");
            }

            var task = taskResult.Result;
            ViewBag.TaskTitle = task.Title;
            ViewBag.TaskBudget = task.Budget;
            ViewBag.TaskBudgetType = task.BudgetTypeDisplay;

            var model = new CreateApplicationViewModel
            {
                TaskId = taskId,
                ProposedBudget = task.Budget // Pre-fill with task budget
            };

            return View(model);
        }

        /// <summary>
        /// Creates a new application
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Worker,Admin")]
        public async Task<IActionResult> Create(CreateApplicationViewModel model)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid model state for application creation");
                
                // Reload task info for view
                var taskResult = await _taskService.GetTaskByIdAsync(model.TaskId, GetCurrentUserId());
                if (taskResult.Success)
                {
                    ViewBag.TaskTitle = taskResult.Result.Title;
                    ViewBag.TaskBudget = taskResult.Result.Budget;
                    ViewBag.TaskBudgetType = taskResult.Result.BudgetTypeDisplay;
                }
                
                return View(model);
            }

            var userId = GetCurrentUserId();
            var result = await _applicationService.CreateApplicationAsync(model, userId);

            if (!result.Success)
            {
                ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Failed to submit application.");
                
                // Reload task info for view
                var taskResult = await _taskService.GetTaskByIdAsync(model.TaskId, userId);
                if (taskResult.Success)
                {
                    ViewBag.TaskTitle = taskResult.Result.Title;
                    ViewBag.TaskBudget = taskResult.Result.Budget;
                    ViewBag.TaskBudgetType = taskResult.Result.BudgetTypeDisplay;
                }
                
                return View(model);
            }

            _logger.LogInformation("Application created successfully: {ApplicationId}", result.Result);
            TempData["Success"] = "Application submitted successfully!";
            return RedirectToAction("Details", "Task", new { id = model.TaskId });
        }

        #endregion

        #region View Applications

        /// <summary>
        /// Displays applications for a specific task (for poster)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> ByTask(int taskId)
        {
            var userId = GetCurrentUserId();
            var result = await _applicationService.GetApplicationsByTaskAsync(taskId, userId);

            if (!result.Success)
            {
                TempData["Error"] = result.ErrorMessage;
                return RedirectToAction("Details", "Task", new { id = taskId });
            }

            ViewBag.TaskId = taskId;
            return View(result.Result);
        }

        /// <summary>
        /// Displays applications submitted by the current user (for worker)
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Worker,Admin")]
        public async Task<IActionResult> ByWorker()
        {
            var userId = GetCurrentUserId();
            var result = await _applicationService.GetApplicationsByWorkerAsync(userId);

            if (!result.Success)
            {
                TempData["Error"] = result.ErrorMessage;
                return View(Enumerable.Empty<TaskApplicationViewModel>());
            }

            return View(result.Result);
        }

        /// <summary>
        /// Displays applications for the current user's posted tasks (for poster)
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Poster,Admin")]
        public async Task<IActionResult> ByPoster()
        {
            var userId = GetCurrentUserId();
            var tasksResult = await _taskService.GetMyTasksAsync(userId);

            if (!tasksResult.Success)
            {
                TempData["Error"] = tasksResult.ErrorMessage;
                return View(Enumerable.Empty<TaskApplicationViewModel>());
            }

            // Get all applications for all tasks
            var allApplications = new List<TaskApplicationViewModel>();
            foreach (var task in tasksResult.Result)
            {
                var appsResult = await _applicationService.GetApplicationsByTaskAsync(task.Id, userId);
                if (appsResult.Success && appsResult.Result != null)
                {
                    allApplications.AddRange(appsResult.Result);
                }
            }

            return View(allApplications.OrderByDescending(a => a.CreatedAt));
        }

        #endregion

        #region Application Actions

        /// <summary>
        /// Accepts an application
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Accept(int id, int taskId)
        {
            var userId = GetCurrentUserId();
            var result = await _applicationService.AcceptApplicationAsync(id, userId);

            if (!result.Success)
            {
                TempData["Error"] = result.ErrorMessage;
            }
            else
            {
                TempData["Success"] = "Application accepted! A booking has been created.";
            }

            return RedirectToAction("Details", "Task", new { id = taskId });
        }

        /// <summary>
        /// Rejects an application
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id, int taskId, string? reason)
        {
            var userId = GetCurrentUserId();
            var result = await _applicationService.RejectApplicationAsync(id, userId, reason);

            if (!result.Success)
            {
                TempData["Error"] = result.ErrorMessage;
            }
            else
            {
                TempData["Success"] = "Application rejected.";
            }

            return RedirectToAction("Details", "Task", new { id = taskId });
        }

        /// <summary>
        /// Withdraws an application
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Worker,Admin")]
        public async Task<IActionResult> Withdraw(int id)
        {
            var userId = GetCurrentUserId();
            
            // Get application to find task ID for redirect
            var appResult = await _applicationService.GetApplicationByIdAsync(id);
            var taskId = appResult.Result?.TaskId ?? 0;

            var result = await _applicationService.WithdrawApplicationAsync(id, userId);

            if (!result.Success)
            {
                TempData["Error"] = result.ErrorMessage;
            }
            else
            {
                TempData["Success"] = "Application withdrawn.";
            }

            if (taskId > 0)
            {
                return RedirectToAction("Details", "Task", new { id = taskId });
            }
            
            return RedirectToAction(nameof(ByWorker));
        }

        #endregion

        #region API Endpoints

        /// <summary>
        /// Checks if current user has applied to a task
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> HasApplied(int taskId)
        {
            var userId = GetCurrentUserId();
            var result = await _applicationService.HasUserAppliedAsync(taskId, userId);
            return Json(new { hasApplied = result.Result });
        }

        /// <summary>
        /// Gets application count for a task
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Count(int taskId)
        {
            var result = await _applicationService.GetApplicationCountAsync(taskId);
            return Json(new { count = result.Result });
        }

        /// <summary>
        /// Marks an application as viewed
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> MarkViewed(int id)
        {
            var userId = GetCurrentUserId();
            var result = await _applicationService.MarkAsViewedAsync(id, userId);
            return Json(new { success = result.Success });
        }

        #endregion
    }
}
