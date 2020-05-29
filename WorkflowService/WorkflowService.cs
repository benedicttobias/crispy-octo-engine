using System;

namespace WorkflowService
{
    public class WorkflowService : IWorkflowService
    {
        public Task GetTaskCard(string userId)
        {
            return new Task()
            {
                TaskName = userId,
                DueDate = DateTime.Now.AddDays(7),
                TaskStatus = "Immediate"
            };
        }
    }
}
