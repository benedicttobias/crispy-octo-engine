using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Builder.Teams;
using Microsoft.Bot.Schema.Teams;
using Newtonsoft.Json.Linq;
using System.Linq;
using System;
using System.Collections.Generic;
using Bogus;
using Microsoft.Bot.Builder.AI.QnA;
using Microsoft.Teams.Samples.HelloWorld.Web.Extensions;
using Microsoft.Teams.Samples.HelloWorld.Web.Log;
using WorkflowService;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.Teams.Samples.HelloWorld.Web
{
    public class MessageExtension : TeamsActivityHandler
    {
        private readonly QnAMaker _dumbBotQnAMaker;
        private readonly IWorkflowService _workflowService;
        private readonly IStorage _storage;

        private const string UtteranceLogKey = "UTTERANCE_LOG";
        private UtteranceLog _utteranceLog;

        public MessageExtension(QnAMakerEndpoint endpoint, IWorkflowService workflowService, IStorage storage)
        {
            _workflowService = workflowService;
            _storage = storage;
            _dumbBotQnAMaker = new QnAMaker(endpoint);
            
        }

        protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            var utterance = turnContext.Activity.Text;

            _utteranceLog = _storage.ReadAsync<UtteranceLog>(new[] { UtteranceLogKey }).Result.FirstOrDefault().Value;
            if (_utteranceLog == null)
            {
                _utteranceLog = new UtteranceLog();
            }

            _utteranceLog.UtteranceList.Add(utterance);
            var changes = new Dictionary<string, object>
            {
                {UtteranceLogKey, _utteranceLog }
            };

            await _storage.WriteAsync(changes, cancellationToken);

            turnContext.Activity.RemoveRecipientMention();
            var text = turnContext.Activity.Text.Trim().ToLower();

            if (text.Contains("task", StringComparison.InvariantCultureIgnoreCase))
            {
                await GetTask(turnContext, cancellationToken);
            }
            else if (text.Contains("what?", StringComparison.InvariantCultureIgnoreCase))
            {
                await GetLastAnswer(turnContext, cancellationToken);
            }
            else
            {
                await AccessQnAMaker(turnContext, cancellationToken);
            }
        }

        private async Task GetLastAnswer(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            var lastAnswer = _utteranceLog.UtteranceList.AsEnumerable().Reverse().Skip(1).Take(1).FirstOrDefault();
            if (string.IsNullOrEmpty(lastAnswer))
            {
                lastAnswer = "I CANT HEAR YOU!";
            }
            else
            {
                lastAnswer = $"{lastAnswer.ToUpper()}!";
            }
            
            var message = MessageFactory.Text(lastAnswer);
            await turnContext.SendActivityAsync(message, cancellationToken);
        }

        private async Task GetTask(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            var card = _workflowService.GetTaskCard(turnContext.Activity.From.Name).ToHeroCard();
            var answer = "You have one task...";

            var message = MessageFactory.Text(answer);
            message.Attachments.Add(card.ToAttachment());

            _utteranceLog = _storage.ReadAsync<UtteranceLog>(new[] { UtteranceLogKey }).Result.FirstOrDefault().Value;
            _utteranceLog.UtteranceList.Add(answer);
            var changes = new Dictionary<string, object>
            {
                {UtteranceLogKey, _utteranceLog }
            };
            await _storage.WriteAsync(changes, cancellationToken);

            await turnContext.SendActivityAsync(message, cancellationToken);
        }

        private async Task AccessQnAMaker(ITurnContext<IMessageActivity> turnContext,
            CancellationToken cancellationToken)
        {
            var results = await _dumbBotQnAMaker.GetAnswersAsync(turnContext);
            var answer = "I have no idea how to answer that. Sorry.";
            if (results.Any())
            {
                answer = results.First().Answer;
            }

            _utteranceLog = _storage.ReadAsync<UtteranceLog>(new[] { UtteranceLogKey }).Result.FirstOrDefault().Value;
            _utteranceLog.UtteranceList.Add(answer);
            var changes = new Dictionary<string, object>
            {
                {UtteranceLogKey, _utteranceLog }
            };
            await _storage.WriteAsync(changes, cancellationToken);

            await turnContext.SendActivityAsync(MessageFactory.Text(answer), cancellationToken);
        }

        protected override Task<MessagingExtensionResponse> OnTeamsMessagingExtensionQueryAsync(ITurnContext<IInvokeActivity> turnContext, MessagingExtensionQuery query, CancellationToken cancellationToken)
        {
            var title = "";
            var titleParam = query.Parameters?.FirstOrDefault(p => p.Name == "cardTitle");
            if (titleParam != null)
            {
                title = titleParam.Value.ToString();
            }

            if (query == null || query.CommandId != "getRandomText")
            {
                // We only process the 'getRandomText' queries with this message extension
                throw new NotImplementedException($"Invalid CommandId: {query.CommandId}");
            }

            var attachments = new MessagingExtensionAttachment[5];

            for (int i = 0; i < 5; i++)
            {
                attachments[i] = GetAttachment(title);
            }

            var result = new MessagingExtensionResponse
            {
                ComposeExtension = new MessagingExtensionResult
                {
                    AttachmentLayout = "list",
                    Type = "result",
                    Attachments = attachments.ToList()
                },
            };
            return Task.FromResult(result);

        }

        private static MessagingExtensionAttachment GetAttachment(string title)
        {
            var card = new ThumbnailCard
            {
                Title = !string.IsNullOrWhiteSpace(title) ? title : new Faker().Lorem.Sentence(),
                Text = new Faker().Lorem.Paragraph(),
                Images = new List<CardImage> { new CardImage("http://lorempixel.com/640/480?rand=" + DateTime.Now.Ticks.ToString()) }
            };

            return card
                .ToAttachment()
                .ToMessagingExtensionAttachment();
        }

        protected override Task<MessagingExtensionResponse> OnTeamsMessagingExtensionSelectItemAsync(ITurnContext<IInvokeActivity> turnContext, JObject query, CancellationToken cancellationToken)
        {

            return Task.FromResult(new MessagingExtensionResponse
            {
                ComposeExtension = new MessagingExtensionResult
                {
                    AttachmentLayout = "list",
                    Type = "result",
                    Attachments = new MessagingExtensionAttachment[]{
                        new ThumbnailCard()
                            .ToAttachment()
                            .ToMessagingExtensionAttachment()
                    }
                },
            });
        }
    }
}
