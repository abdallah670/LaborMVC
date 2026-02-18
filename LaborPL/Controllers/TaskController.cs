using LaborBLL.ModelVM;
using LaborBLL.Response;
using LaborBLL.Service.Abstract;
using LaborDAL.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TaskStatus = LaborDAL.Enums.TaskStatus;

namespace LaborPL.Controllers
{
    /// <summary>
    /// Controller for task-related operations
    /// </summary>
    [Authorize]
    public class TaskController : Controller
    {
        private readonly ITaskService _taskService;
        private readonly ILogger<TaskController> _logger;

        public TaskController(
            ITaskService taskService,
            ILogger<TaskController> logger)
        {
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

        #region Task List and Search

        /// <summary>
        /// Displays the task list with search filters
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Index(TaskSearchViewModel? model)
        {
            if (model == null)
            {
                model = new TaskSearchViewModel();
            }

            var result = await _taskService.GetTaskListAsync(model, GetCurrentUserId());
            
            if (!result.Success)
            {
                TempData["Error"] = result.ErrorMessage;
                return View(new TaskSearchViewModel());
            }

            return View(result.Result);
        }

        /// <summary>
        /// Search tasks with filters
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Search(TaskSearchViewModel model)
        {
            var result = await _taskService.GetTaskListAsync(model, GetCurrentUserId());
            
            if (!result.Success)
            {
                return Json(new { success = false, error = result.ErrorMessage });
            }

            return Json(new { success = true, data = result.Result });
        }

        #endregion

        #region Task Details

        /// <summary>
        /// Displays task details
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var userId = GetCurrentUserId();
            var result = await _taskService.GetTaskByIdAsync(id, userId);

            if (!result.Success)
            {
                TempData["Error"] = result.ErrorMessage;
                return RedirectToAction(nameof(Index));
            }

            // Increment view count
            await _taskService.IncrementViewCountAsync(id);

            return View(result.Result);
        }

        #endregion

        #region Create Task

        /// <summary>
        /// Displays the create task form
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Poster,Admin")]
        public IActionResult Create()
        {
            return View(new CreateTaskViewModel());
        }

        /// <summary>
        /// Creates a new task
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Poster,Admin")]
        public async Task<IActionResult> Create(CreateTaskViewModel model)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid model state for task creation");
                return View(model);
            }

            var userId = GetCurrentUserId();
            var result = await _taskService.CreateTaskAsync(model, userId);

            if (!result.Success)
            {
                ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Failed to create task.");
                return View(model);
            }

            _logger.LogInformation("Task created successfully: {TaskId}", result.Result);
            TempData["Success"] = "Task created successfully!";
            return RedirectToAction(nameof(Details), new { id = result.Result });
        }

        #endregion

        #region Edit Task

        /// <summary>
        /// Displays the edit task form
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var userId = GetCurrentUserId();
            var result = await _taskService.GetTaskByIdAsync(id, userId);

            if (!result.Success)
            {
                TempData["Error"] = result.ErrorMessage;
                return RedirectToAction(nameof(Index));
            }

            var task = result.Result;
            
            // Check if user can edit
            if (!task.CanEdit)
            {
                TempData["Error"] = "You cannot edit this task.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var editModel = new EditTaskViewModel
            {
                Id = task.Id,
                Title = task.Title,
                Description = task.Description,
                Category = task.Category,
                BudgetType = task.BudgetType,
                Budget = task.Budget,
                EstimatedHours = task.EstimatedHours,
                DueDate = task.DueDate,
                StartDate = task.StartDate,
                Location = task.Location,
                LocationUrl = task.LocationUrl,
                Latitude = task.Latitude,
                Longitude = task.Longitude,
                Country = task.Country,
                City = task.City,
                IsRemote = task.IsRemote,
                WorkersNeeded = task.WorkersNeeded,
                RequiredSkills = task.RequiredSkills,
                IsUrgent = task.IsUrgent,
                IsFeatured = task.IsFeatured
            };

            return View(editModel);
        }

        /// <summary>
        /// Updates an existing task
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditTaskViewModel model)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid model state for task edit");
                return View(model);
            }

            var userId = GetCurrentUserId();
            var result = await _taskService.UpdateTaskAsync(model, userId);

            if (!result.Success)
            {
                ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Failed to update task.");
                return View(model);
            }

            _logger.LogInformation("Task updated successfully: {TaskId}", model.Id);
            TempData["Success"] = "Task updated successfully!";
            return RedirectToAction(nameof(Details), new { id = model.Id });
        }

        #endregion

        #region Delete Task

        /// <summary>
        /// Deletes a task
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = GetCurrentUserId();
            var result = await _taskService.DeleteTaskAsync(id, userId);

            if (!result.Success)
            {
                TempData["Error"] = result.ErrorMessage;
                return RedirectToAction(nameof(Details), new { id });
            }

            _logger.LogInformation("Task deleted: {TaskId}", id);
            TempData["Success"] = "Task deleted successfully!";
            return RedirectToAction(nameof(Index));
        }

        #endregion

        #region My Tasks

        /// <summary>
        /// Displays tasks posted by the current user
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Poster,Admin")]
        public async Task<IActionResult> MyTasks()
        {
            var userId = GetCurrentUserId();
            var result = await _taskService.GetMyTasksAsync(userId);

            if (!result.Success)
            {
                TempData["Error"] = result.ErrorMessage;
                return View(Enumerable.Empty<TaskListViewModel>());
            }

            return View(result.Result);
        }

        /// <summary>
        /// Displays tasks assigned to the current user (as worker)
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Worker,Admin")]
        public async Task<IActionResult> MyAssignments()
        {
            var userId = GetCurrentUserId();
            var result = await _taskService.GetAssignedTasksAsync(userId);

            if (!result.Success)
            {
                TempData["Error"] = result.ErrorMessage;
                return View(Enumerable.Empty<TaskListViewModel>());
            }

            return View(result.Result);
        }

        #endregion

        #region Task Status Actions

        /// <summary>
        /// Starts a task (marks as in progress)
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Worker,Admin")]
        public async Task<IActionResult> Start(int id)
        {
            var userId = GetCurrentUserId();
            var result = await _taskService.StartTaskAsync(id, userId);

            if (!result.Success)
            {
                TempData["Error"] = result.ErrorMessage;
            }
            else
            {
                TempData["Success"] = "Task started successfully!";
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        /// <summary>
        /// Completes a task
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Complete(int id)
        {
            var result = await _taskService.CompleteTaskAsync(id);

            if (!result.Success)
            {
                TempData["Error"] = result.ErrorMessage;
            }
            else
            {
                TempData["Success"] = "Task marked as completed!";
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        /// <summary>
        /// Cancels a task
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id, string reason)
        {
            var userId = GetCurrentUserId();
            var result = await _taskService.CancelTaskAsync(id, reason, userId);

            if (!result.Success)
            {
                TempData["Error"] = result.ErrorMessage;
            }
            else
            {
                TempData["Success"] = "Task cancelled.";
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        #endregion

        #region API Endpoints

        /// <summary>
        /// Checks if a user can apply to a task
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> CanApply(int taskId)
        {
            var userId = GetCurrentUserId();
            var result = await _taskService.CanUserApplyAsync(taskId, userId);
            return Json(new { canApply = result.Success, message = result.ErrorMessage });
        }

        /// <summary>
        /// Gets tasks by category (for AJAX calls)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> ByCategory(TaskCategory category, int page = 1)
        {
            var search = new TaskSearchViewModel
            {
                Category = category,
                Page = page,
                PageSize = 12
            };

            var result = await _taskService.GetTaskListAsync(search, GetCurrentUserId());
            return Json(new { success = result.Success, data = result.Result?.Results });
        }

        #endregion
    }
}
