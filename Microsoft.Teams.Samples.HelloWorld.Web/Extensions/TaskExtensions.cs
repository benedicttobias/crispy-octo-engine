using System.Collections.Generic;
using Microsoft.Bot.Schema;
using WorkflowService;

namespace Microsoft.Teams.Samples.HelloWorld.Web.Extensions
{
    public static class TaskExtensions
    {
        public static HeroCard ToHeroCard(this Task task)
        {
            var heroCard = new HeroCard
            {
                Title = task.TaskName,
                Text = $"This task is in \"{task.TaskStatus}\" status and you have it until {task.DueDate:F}",
                Buttons = new List<CardAction>
                {
                    new CardAction
                    {
                        Type = ActionTypes.OpenUrl,
                        Title = "Approve",
                        Value = "www.google.com"
                    },
                    new CardAction
                    {
                        Type = ActionTypes.OpenUrl,
                        Title = "Reject",
                        Value = "www.google.com"
                    }
                }
            };

            return heroCard;
        }
    }
}
