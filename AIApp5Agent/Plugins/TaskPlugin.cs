using Microsoft.SemanticKernel;
using System.ComponentModel;
using System.Text.Json.Serialization;

namespace AIApp5Agent.Plugins
{
    public class TaskPlugin
    {
        private static readonly List<TaskItem> _tasks = new();
        private static int _nextId = 1;

        [KernelFunction]
        [Description]
        public string AddTask([Description("The task description to add")] string taskDescription,
        [Description("Priority: High, Medium, or Low")] string priority = "Medium")
        {
            var task = new TaskItem
            {
                Id = _nextId++,
                Description = taskDescription,
                Priority = priority,
                CreatedAt = DateTime.Now,
                IsCompleted = false
            };

            _tasks.Add(task);
            return $" Task #{task.Id} added: '{taskDescription}' | Priority: {priority}";
        }

        [KernelFunction]
        [Description("Gets all tasks in the task list, showing pending and completed tasks")]
        public string GetAllTasks()
        {
            if(!_tasks.Any())
                return "No tasks yet. Add a task to get started!";

                var pending = _tasks.Where(t => !t.IsCompleted).ToList();
                var completed = _tasks.Where(t => t.IsCompleted).ToList();

                var result = $"TASK LIST ({_tasks.Count} total)\n\n";

                if (pending.Any())
                {
                    result += $"PENDING ({pending.Count}):\n";
                    foreach (var t in pending)
                        result += $"  #{t.Id} [{t.Priority}] {t.Description}\n";
                }

                if (completed.Any())
                {
                    result += $"\nCOMPLETED ({completed.Count}):\n";
                    foreach (var t in completed)
                        result += $"  #{t.Id} ✓ {t.Description}\n";
                }

                return result;
            
        }

        [KernelFunction]
        [Description("Marks a task as completed by its ID number")]
        public string CompleteTask(
        [Description("The task ID number to mark as complete")] int taskId)
        {
            var task = _tasks.FirstOrDefault(t => t.Id == taskId);
            if (task == null)
                return $"Task #{taskId} not found.";

            task.IsCompleted = true;
            task.CompletedAt = DateTime.Now;
            return $" Task #{taskId} marked as complete: '{task.Description}'";
        }

        [KernelFunction]
        [Description("Deletes a task from the list by its ID number")]
        public string DeleteTask(
            [Description("The task ID number to delete")] int taskId)
        {
            var task = _tasks.FirstOrDefault(t => t.Id == taskId);
            if (task == null)
                return $"Task #{taskId} not found.";

            _tasks.Remove(task);
            return $" Task #{taskId} deleted: '{task.Description}'";
        }

        [KernelFunction]
        [Description("Gets only the pending (incomplete) tasks")]
        public string GetPendingTasks()
        {
            var pending = _tasks.Where(t => !t.IsCompleted).ToList();
            if (!pending.Any())
                return "No pending tasks! You're all caught up. 🎉";

            var result = $" PENDING TASKS ({pending.Count}):\n";
            foreach (var t in pending)
                result += $"  #{t.Id} [{t.Priority}] {t.Description}\n";

            return result;
        }

        private class TaskItem
        {
            public int Id { get; set; }
            public string Description { get; set; } = string.Empty;
            public string Priority { get; set; } = "Medium";
            public bool IsCompleted { get; set; }
            public DateTime CreatedAt { get; set; }
            public DateTime? CompletedAt { get; set; }
        }

    }
}

    

